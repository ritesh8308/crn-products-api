using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests;

/// <summary>
/// Tests the JWT/refresh token generator. JwtSettings is supplied directly via
/// Options.Create — no DI container needed.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var settings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "test-signing-key-that-is-long-enough-for-hmacsha256!!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };
        _service = new TokenService(Options.Create(settings));
    }

    [Fact]
    public void GenerateAccessToken_EmbedsUserClaims()
    {
        var user = new User { Id = 42, Username = "alice", Role = Roles.Admin };

        var (token, expiresAt) = _service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("alice", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(Roles.Admin, jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_IsRandom_AndFutureDated()
    {
        var a = _service.GenerateRefreshToken();
        var b = _service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(a.Token));
        Assert.NotEqual(a.Token, b.Token);                 // cryptographically random
        Assert.True(a.ExpiresAtUtc > DateTime.UtcNow);     // long-lived
    }
}
