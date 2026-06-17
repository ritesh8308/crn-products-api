namespace Application.DTOs.Auth;

/// <summary>Request body for POST /auth/refresh — exchanges a valid refresh token for a new token pair.</summary>
public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
