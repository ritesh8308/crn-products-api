using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Tests;

/// <summary>
/// Unit tests for AuthService. Repository, unit of work, password hasher and token
/// service are mocked; the real request validators are used. These verify the auth
/// rules in isolation — duplicate detection, credential checks, and especially the
/// refresh-token rotation and replay-detection logic — without a database or real JWTs.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // Default token behaviour: distinct access/refresh values with future expiry.
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
               .Returns(("access-jwt", DateTime.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.GenerateRefreshToken())
               .Returns(("refresh-new", DateTime.UtcNow.AddDays(7)));

        _sut = new AuthService(
            _users.Object, _uow.Object, _hasher.Object, _tokens.Object,
            new RegisterRequestValidator(), new LoginRequestValidator());
    }

    // ---------- Register ----------

    [Fact]
    public async Task RegisterAsync_Throws_WhenUsernameTaken()
    {
        _users.Setup(u => u.UsernameExistsAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.RegisterAsync(new RegisterRequest { Username = "alice", Password = "Passw0rd" }));
    }

    [Fact]
    public async Task RegisterAsync_CreatesPlainUser_HashesPassword_AndIssuesTokens()
    {
        User? created = null;
        _users.Setup(u => u.UsernameExistsAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((u, _) => created = u)
              .Returns(Task.CompletedTask);
        _hasher.Setup(h => h.Hash("Passw0rd")).Returns("hashed");

        var result = await _sut.RegisterAsync(new RegisterRequest { Username = "alice", Password = "Passw0rd" });

        Assert.NotNull(created);
        Assert.Equal(Roles.User, created!.Role);     // self-registration is never Admin
        Assert.Equal("hashed", created.PasswordHash); // never the plain text
        Assert.Equal("access-jwt", result.AccessToken);
        Assert.Equal("refresh-new", result.RefreshToken);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenPasswordTooShort()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.RegisterAsync(new RegisterRequest { Username = "alice", Password = "123" }));
    }

    // ---------- Login ----------

    [Fact]
    public async Task LoginAsync_Throws_WhenUserMissing()
    {
        _users.Setup(u => u.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Username = "ghost", Password = "whatever" }));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordWrong()
    {
        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new User { Username = "alice", PasswordHash = "hashed" });
        _hasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Username = "alice", Password = "wrong" }));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenValid()
    {
        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new User { Username = "alice", PasswordHash = "hashed", Role = Roles.User });
        _hasher.Setup(h => h.Verify("Passw0rd", "hashed")).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest { Username = "alice", Password = "Passw0rd" });

        Assert.Equal("access-jwt", result.AccessToken);
        Assert.Equal("refresh-new", result.RefreshToken);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Refresh (rotation + replay) ----------

    [Fact]
    public async Task RefreshAsync_RotatesToken_OnValidToken()
    {
        var user = new User { Id = 1, Username = "alice", Role = Roles.User };
        var active = new RefreshToken
        {
            Token = "refresh-old",
            ExpiresOn = DateTime.UtcNow.AddDays(1), // active
            User = user
        };
        user.RefreshTokens.Add(active);
        _users.Setup(u => u.GetRefreshTokenWithUserAsync("refresh-old", It.IsAny<CancellationToken>()))
              .ReturnsAsync(active);

        var result = await _sut.RefreshAsync(new RefreshRequest { RefreshToken = "refresh-old" });

        // Old token retired, pointing at its replacement; a brand-new token returned.
        Assert.NotNull(active.RevokedOn);
        Assert.Equal("refresh-new", active.ReplacedByToken);
        Assert.Equal("refresh-new", result.RefreshToken);
        Assert.Contains(user.RefreshTokens, t => t.Token == "refresh-new");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_Throws_WhenTokenUnknown()
    {
        _users.Setup(u => u.GetRefreshTokenWithUserAsync("nope", It.IsAny<CancellationToken>()))
              .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.RefreshAsync(new RefreshRequest { RefreshToken = "nope" }));
    }

    [Fact]
    public async Task RefreshAsync_DetectsReplay_RevokesWholeFamily_WhenTokenInactive()
    {
        var user = new User { Id = 1, Username = "alice" };
        var alreadyUsed = new RefreshToken
        {
            Token = "refresh-used",
            ExpiresOn = DateTime.UtcNow.AddDays(1),
            RevokedOn = DateTime.UtcNow.AddMinutes(-5), // already rotated -> inactive
            User = user
        };
        var stillActive = new RefreshToken
        {
            Token = "refresh-live",
            ExpiresOn = DateTime.UtcNow.AddDays(1) // active sibling in the family
        };
        user.RefreshTokens.Add(alreadyUsed);
        user.RefreshTokens.Add(stillActive);
        _users.Setup(u => u.GetRefreshTokenWithUserAsync("refresh-used", It.IsAny<CancellationToken>()))
              .ReturnsAsync(alreadyUsed);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.RefreshAsync(new RefreshRequest { RefreshToken = "refresh-used" }));

        // Replay of a dead token kills the live sibling too (family revocation).
        Assert.NotNull(stillActive.RevokedOn);
        Assert.False(stillActive.IsActive);
    }

    // ---------- Revoke ----------

    [Fact]
    public async Task RevokeAsync_Revokes_WhenActive()
    {
        var active = new RefreshToken { Token = "r", ExpiresOn = DateTime.UtcNow.AddDays(1), User = new User() };
        _users.Setup(u => u.GetRefreshTokenWithUserAsync("r", It.IsAny<CancellationToken>())).ReturnsAsync(active);

        await _sut.RevokeAsync(new RevokeRequest { RefreshToken = "r" });

        Assert.NotNull(active.RevokedOn);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_Throws_WhenAlreadyInactive()
    {
        var revoked = new RefreshToken
        {
            Token = "r",
            ExpiresOn = DateTime.UtcNow.AddDays(1),
            RevokedOn = DateTime.UtcNow,
            User = new User()
        };
        _users.Setup(u => u.GetRefreshTokenWithUserAsync("r", It.IsAny<CancellationToken>())).ReturnsAsync(revoked);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.RevokeAsync(new RevokeRequest { RefreshToken = "r" }));
    }
}
