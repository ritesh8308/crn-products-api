using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Issues the two kinds of token. The implementation (Infrastructure) owns the JWT
/// signing key and the configured lifetimes, so the Application layer stays free of
/// JWT libraries and config — it just asks for tokens and gets back value + expiry.
/// </summary>
public interface ITokenService
{
    /// <summary>Builds a signed, short-lived JWT carrying the user's id, name and role claims.</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user);

    /// <summary>Generates a cryptographically-random, long-lived refresh token value.</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken();
}
