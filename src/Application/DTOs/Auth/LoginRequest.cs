namespace Application.DTOs.Auth;

/// <summary>Request body for POST /auth/login.</summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
