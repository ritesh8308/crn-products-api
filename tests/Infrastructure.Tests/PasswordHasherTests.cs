using Infrastructure.Auth;

namespace Infrastructure.Tests;

/// <summary>
/// Tests the PBKDF2 password hasher. No mocks/DB — it's pure crypto over the BCL.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Passw0rd!");
        Assert.True(_hasher.Verify("Passw0rd!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Passw0rd!");
        Assert.False(_hasher.Verify("not-it", hash));
    }

    [Fact]
    public void Hash_UsesRandomSalt_SoSamePasswordHashesDiffer()
    {
        // Different salts => different stored strings, yet both verify correctly.
        var a = _hasher.Hash("same");
        var b = _hasher.Hash("same");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify("same", a));
        Assert.True(_hasher.Verify("same", b));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.Verify("whatever", "not-a-valid-hash-format"));
    }
}
