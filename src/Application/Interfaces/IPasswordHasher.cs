namespace Application.Interfaces;

/// <summary>
/// Abstraction over password hashing. Declared here so AuthService never depends on
/// a concrete crypto implementation; Infrastructure provides a salted PBKDF2 hasher.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a self-describing hash string (algorithm params + salt + hash).</summary>
    string Hash(string password);

    /// <summary>Re-derives the hash from the candidate password and compares it in constant time.</summary>
    bool Verify(string password, string passwordHash);
}
