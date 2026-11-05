using RepairRequests.Core.Services;

namespace RepairRequests.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsExpectedFormat()
    {
        var hash = _hasher.Hash("Admin123!");

        var parts = hash.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha256", parts[0]);
        Assert.Equal(120_000, int.Parse(parts[1]));
        Assert.NotEmpty(Convert.FromBase64String(parts[2]));
        Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
    }

    [Fact]
    public void Hash_UsesUniqueSalt_ForSamePassword()
    {
        var first = _hasher.Hash("Manager123!");
        var second = _hasher.Hash("Manager123!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Client123!");

        Assert.True(_hasher.Verify("Client123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Client123!");

        Assert.False(_hasher.Verify("Client124!", hash));
        Assert.False(_hasher.Verify("client123!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_ReturnsFalse_ForEmptyPassword(string password)
    {
        var hash = _hasher.Hash("Client123!");

        Assert.False(_hasher.Verify(password, hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("md5$1000$abc$def")]
    [InlineData("pbkdf2-sha256$1000$abc")]
    [InlineData("pbkdf2-sha256$not-a-number$abc$def")]
    [InlineData("pbkdf2-sha256$0$abc$def")]
    [InlineData("pbkdf2-sha256$1000$***$def")]
    public void Verify_ReturnsFalse_ForMalformedHash(string storedHash)
    {
        Assert.False(_hasher.Verify("Client123!", storedHash));
    }

    [Fact]
    public void Hash_Throws_ForEmptyPassword()
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Hash(string.Empty));
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Hash("   "));
    }

    [Fact]
    public void Verify_RespectsIterationCountStoredInHash()
    {
        // Хэш с нестандартным числом итераций должен проверяться именно с ним.
        var salt = Convert.ToBase64String(new byte[16]);
        var key = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "Secret123", Convert.FromBase64String(salt), 1_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        var hash = $"pbkdf2-sha256$1000${salt}${Convert.ToBase64String(key)}";

        Assert.True(_hasher.Verify("Secret123", hash));
        Assert.False(_hasher.Verify("Secret124", hash));
    }
}
