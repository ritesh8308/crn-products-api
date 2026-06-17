using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

/// <summary>
/// Issues access tokens (signed JWTs) and refresh tokens (opaque random strings).
/// Owns the signing key and the configured lifetimes (JwtSettings), keeping all JWT
/// concerns out of the Application layer behind the ITokenService interface.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        // Claims = the facts about the user baked into the token. Sub/Name/Role drive
        // identity and authorization; Jti gives each token a unique id.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        // A refresh token is NOT a JWT — it's just a large, unguessable random value
        // we store and look up. 64 random bytes = 512 bits of entropy.
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays);
        return (token, expires);
    }
}
