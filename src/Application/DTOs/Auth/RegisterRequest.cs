namespace Application.DTOs.Auth;

/// <summary>Request body for POST /auth/register. New users always get the "User" role
/// (the service ignores any client-supplied role — self-promotion to Admin is not allowed).</summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
