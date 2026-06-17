namespace Application.DTOs.Auth;

/// <summary>
/// Returned by register / login / refresh. Carries the short-lived access token
/// (sent on every request) and the long-lived refresh token (sent only to /refresh),
/// plus their expiry instants so the client knows when to refresh.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
