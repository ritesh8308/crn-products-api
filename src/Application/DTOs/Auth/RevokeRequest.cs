namespace Application.DTOs.Auth;

/// <summary>Request body for POST /auth/revoke — invalidates a refresh token (logout).</summary>
public class RevokeRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
