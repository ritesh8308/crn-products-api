using Domain.Constants;

namespace Domain.Entities;

/// <summary>
/// An application user that can authenticate. Passwords are NEVER stored in plain
/// text — only a salted PBKDF2 hash (computed in Infrastructure). Role drives the
/// role-based authorization checks in the API. A user owns many refresh tokens
/// over time (one active per device/login), kept for rotation and revocation.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    public DateTime CreatedOn { get; set; }

    // The history of refresh tokens issued to this user (active + rotated/revoked).
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
