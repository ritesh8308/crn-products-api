using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
// FluentValidation also defines a ValidationException; alias the DOMAIN exceptions
// so the service throws our own types (the API middleware maps them to status codes).
using ValidationException = Domain.Exceptions.ValidationException;
using ConflictException = Domain.Exceptions.ConflictException;
using UnauthorizedException = Domain.Exceptions.UnauthorizedException;

namespace Application.Services;

/// <summary>
/// Orchestrates authentication: register, login, refresh (with rotation) and revoke.
/// Depends ONLY on Application interfaces — repository, unit of work, password hasher,
/// token service, validators — so it carries no knowledge of EF Core or the JWT libs
/// and is fully unit-testable with mocks.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        if (await _users.UsernameExistsAsync(request.Username, cancellationToken))
            throw new ConflictException($"Username '{request.Username}' is already taken.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = Roles.User,            // self-registration is always a plain User
            CreatedOn = DateTime.UtcNow
        };

        await _users.AddAsync(user, cancellationToken);
        // Persist first so the identity Id exists before we mint a token that embeds it.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        var user = await _users.GetByUsernameAsync(request.Username, cancellationToken);

        // Same error whether the user is missing or the password is wrong — don't
        // leak which usernames exist. Verify() is constant-time on the hash compare.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password.");

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetRefreshTokenWithUserAsync(request.RefreshToken, cancellationToken);
        if (existing is null)
            throw new UnauthorizedException("Invalid refresh token.");

        var user = existing.User!;

        // Replay / theft detection: presenting a token that is already revoked or
        // expired (e.g. one that was rotated earlier) is a red flag. Kill every active
        // token for this user so a thief and the victim both have to log in again.
        if (!existing.IsActive)
        {
            RevokeAllActiveTokens(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token is no longer valid.");
        }

        // Rotation: retire the presented token and issue a fresh pair.
        var newRefresh = _tokenService.GenerateRefreshToken();
        existing.RevokedOn = DateTime.UtcNow;
        existing.ReplacedByToken = newRefresh.Token;

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefresh.Token,
            ExpiresOn = newRefresh.ExpiresAtUtc,
            CreatedOn = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var access = _tokenService.GenerateAccessToken(user);
        return BuildResponse(user, access, newRefresh);
    }

    public async Task RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetRefreshTokenWithUserAsync(request.RefreshToken, cancellationToken);
        if (existing is null || !existing.IsActive)
            throw new UnauthorizedException("Invalid refresh token.");

        existing.RevokedOn = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Mints an access + refresh token for an already-persisted user and stores the
    // refresh token. Used by both register and login.
    private async Task<AuthResponse> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        var access = _tokenService.GenerateAccessToken(user);
        var refresh = _tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refresh.Token,
            ExpiresOn = refresh.ExpiresAtUtc,
            CreatedOn = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return BuildResponse(user, access, refresh);
    }

    private static void RevokeAllActiveTokens(User user)
    {
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            token.RevokedOn = DateTime.UtcNow;
    }

    private static AuthResponse BuildResponse(
        User user,
        (string Token, DateTime ExpiresAtUtc) access,
        (string Token, DateTime ExpiresAtUtc) refresh) => new()
    {
        AccessToken = access.Token,
        AccessTokenExpiresAt = access.ExpiresAtUtc,
        RefreshToken = refresh.Token,
        RefreshTokenExpiresAt = refresh.ExpiresAtUtc,
        Username = user.Username,
        Role = user.Role
    };

    // Shared validation helper — mirrors ProductService: run the validator and turn
    // failures into the domain ValidationException (HTTP 400 via middleware).
    private static async Task ValidateAsync<T>(
        IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException(message);
        }
    }
}
