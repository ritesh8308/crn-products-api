namespace Domain.Entities;

/// <summary>
/// A long-lived, opaque token that can be exchanged for a fresh access token.
/// Unlike the JWT access token (stateless, never stored), refresh tokens MUST be
/// persisted so we can validate, rotate, and revoke them. Rotation works by setting
/// RevokedOn + ReplacedByToken on the old row each time it's used.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }

    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }

    // Set when the token is rotated (replaced by a new one) or explicitly revoked (logout).
    public DateTime? RevokedOn { get; set; }
    // The token that replaced this one on rotation — useful for auditing a token "family".
    public string? ReplacedByToken { get; set; }

    public User? User { get; set; }

    // Computed, not persisted (configured as Ignore in EF). Convenience for the
    // service so the "is this token still usable?" rule lives in one place.
    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public bool IsActive => RevokedOn is null && !IsExpired;
}
