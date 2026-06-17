using System.Security.Cryptography;
using Application.Interfaces;

namespace Infrastructure.Auth;

/// <summary>
/// Salted PBKDF2 password hasher using the .NET BCL (no third-party package).
/// Each password gets a fresh random salt; the stored string is self-describing —
/// "{iterations}.{salt}.{hash}" — so Verify() can reproduce the exact derivation.
/// PBKDF2 is deliberately slow (many iterations) to make brute-forcing expensive.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;          // work factor
    private const int SaltSize = 16;                 // 128-bit salt
    private const int HashSize = 32;                 // 256-bit derived key
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expected = Convert.FromBase64String(parts[2]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        // Constant-time compare to avoid leaking information via timing.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
