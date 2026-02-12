using OrderMonitor.Infrastructure.Security;
using Xunit;

namespace OrderMonitor.UnitTests.Security;

public class PasswordEncryptorTests : IDisposable
{
    private const string TestKey = "TestEncryptionKey32BytesLong!!!!"; // Exactly 32 chars

    public PasswordEncryptorTests()
    {
        // Reset state before each test
        PasswordEncryptor.Reset();
    }

    public void Dispose()
    {
        PasswordEncryptor.Reset();
    }

    [Fact]
    public void Configure_SetsEncryptionKey()
    {
        PasswordEncryptor.Configure(TestKey);

        // Should not throw when encrypting without explicit key
        var encrypted = PasswordEncryptor.Encrypt("test");
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
    }

    [Fact]
    public void Configure_NullKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PasswordEncryptor.Configure(null!));
    }

    [Fact]
    public void Configure_EmptyKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PasswordEncryptor.Configure(""));
    }

    [Fact]
    public void Configure_WhitespaceKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PasswordEncryptor.Configure("   "));
    }

    [Fact]
    public void Encrypt_WithoutConfiguredKey_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PasswordEncryptor.Encrypt("test"));
        Assert.Contains("Encryption key is not configured", ex.Message);
    }

    [Fact]
    public void Decrypt_WithoutConfiguredKey_ThrowsInvalidOperationException()
    {
        // First encrypt with explicit key to get valid ciphertext
        var encrypted = PasswordEncryptor.Encrypt("test", TestKey);

        PasswordEncryptor.Reset();

        var ex = Assert.Throws<InvalidOperationException>(
            () => PasswordEncryptor.Decrypt(encrypted));
        Assert.Contains("Encryption key is not configured", ex.Message);
    }

    [Fact]
    public void Encrypt_WithConfiguredKey_ProducesDecryptableResult()
    {
        PasswordEncryptor.Configure(TestKey);

        var plainText = "MySecretPassword123!";
        var encrypted = PasswordEncryptor.Encrypt(plainText);
        var decrypted = PasswordEncryptor.Decrypt(encrypted);

        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_WithExplicitKey_OverridesConfiguredKey()
    {
        PasswordEncryptor.Configure(TestKey);
        var explicitKey = "AnotherKey32BytesLongForTesting!";

        var encrypted = PasswordEncryptor.Encrypt("test", explicitKey);
        var decrypted = PasswordEncryptor.Decrypt(encrypted, explicitKey);

        Assert.Equal("test", decrypted);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        PasswordEncryptor.Configure(TestKey);

        Assert.Equal("", PasswordEncryptor.Encrypt(""));
    }

    [Fact]
    public void Encrypt_NullString_ReturnsNull()
    {
        PasswordEncryptor.Configure(TestKey);

        Assert.Null(PasswordEncryptor.Encrypt(null!));
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmpty()
    {
        PasswordEncryptor.Configure(TestKey);

        Assert.Equal("", PasswordEncryptor.Decrypt(""));
    }

    [Fact]
    public void Encrypt_DifferentInputs_ProduceDifferentOutputs()
    {
        PasswordEncryptor.Configure(TestKey);

        var enc1 = PasswordEncryptor.Encrypt("Password1");
        var enc2 = PasswordEncryptor.Encrypt("Password2");

        Assert.NotEqual(enc1, enc2);
    }

    [Fact]
    public void Encrypt_SameInput_ProducesDifferentOutputs_DueToIV()
    {
        PasswordEncryptor.Configure(TestKey);

        var enc1 = PasswordEncryptor.Encrypt("SamePassword");
        var enc2 = PasswordEncryptor.Encrypt("SamePassword");

        // Each encryption generates a random IV, so outputs differ
        Assert.NotEqual(enc1, enc2);

        // But both decrypt to the same value
        Assert.Equal("SamePassword", PasswordEncryptor.Decrypt(enc1));
        Assert.Equal("SamePassword", PasswordEncryptor.Decrypt(enc2));
    }

    [Fact]
    public void IsEncrypted_ValidBase64WithSufficientLength_ReturnsTrue()
    {
        PasswordEncryptor.Configure(TestKey);
        var encrypted = PasswordEncryptor.Encrypt("test");

        Assert.True(PasswordEncryptor.IsEncrypted(encrypted));
    }

    [Fact]
    public void IsEncrypted_ShortString_ReturnsFalse()
    {
        Assert.False(PasswordEncryptor.IsEncrypted("short"));
    }

    [Fact]
    public void IsEncrypted_EmptyOrNull_ReturnsFalse()
    {
        Assert.False(PasswordEncryptor.IsEncrypted(""));
        Assert.False(PasswordEncryptor.IsEncrypted(null!));
    }

    [Fact]
    public void IsEncrypted_NonBase64_ReturnsFalse()
    {
        Assert.False(PasswordEncryptor.IsEncrypted("This is not base64 encoded!!!???"));
    }

    [Fact]
    public void Decrypt_NonEncryptedText_ReturnsOriginal()
    {
        PasswordEncryptor.Configure(TestKey);

        // Short text that IsEncrypted returns false for
        var plainText = "plain";
        Assert.Equal(plainText, PasswordEncryptor.Decrypt(plainText));
    }

    [Fact]
    public void Reset_ClearsConfiguredKey()
    {
        PasswordEncryptor.Configure(TestKey);
        PasswordEncryptor.Reset();

        Assert.Throws<InvalidOperationException>(
            () => PasswordEncryptor.Encrypt("test"));
    }
}
