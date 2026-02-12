using System.Security.Cryptography;
using System.Text;

namespace OrderMonitor.Infrastructure.Security;

/// <summary>
/// Utility for encrypting and decrypting passwords using AES encryption.
/// The encryption key must be provided via configuration (Database:EncryptionKey)
/// and is never hardcoded.
/// </summary>
public static class PasswordEncryptor
{
    private static string? _configuredKey;

    /// <summary>
    /// Sets the encryption key from application configuration.
    /// Must be called during startup before any Encrypt/Decrypt operations.
    /// </summary>
    public static void Configure(string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be null or empty.", nameof(encryptionKey));

        _configuredKey = encryptionKey;
    }

    /// <summary>
    /// Encrypts a plain text password.
    /// </summary>
    /// <param name="plainText">The plain text password to encrypt.</param>
    /// <param name="key">Optional encryption key. If not provided, uses the configured key.</param>
    /// <returns>Base64 encoded encrypted string with IV prepended.</returns>
    public static string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var effectiveKey = key ?? _configuredKey
            ?? throw new InvalidOperationException(
                "Encryption key is not configured. Set Database__EncryptionKey environment variable " +
                "or call PasswordEncryptor.Configure() during startup.");

        var encryptionKey = GetKeyBytes(effectiveKey);

        using var aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts an encrypted password.
    /// </summary>
    /// <param name="encryptedText">The Base64 encoded encrypted string.</param>
    /// <param name="key">Optional encryption key. If not provided, uses the configured key.</param>
    /// <returns>The decrypted plain text password.</returns>
    public static string Decrypt(string encryptedText, string? key = null)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        if (!IsEncrypted(encryptedText))
            return encryptedText;

        var effectiveKey = key ?? _configuredKey
            ?? throw new InvalidOperationException(
                "Encryption key is not configured. Set Database__EncryptionKey environment variable " +
                "or call PasswordEncryptor.Configure() during startup.");

        var encryptionKey = GetKeyBytes(effectiveKey);
        var fullCipher = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = encryptionKey;

        var iv = new byte[aes.BlockSize / 8];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Checks if a string appears to be encrypted (Base64 with minimum length for IV + data).
    /// </summary>
    public static bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 24)
            return false;

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length >= 32;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the configured key. Used for testing only.
    /// </summary>
    internal static void Reset()
    {
        _configuredKey = null;
    }

    private static byte[] GetKeyBytes(string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var result = new byte[32];

        if (keyBytes.Length >= 32)
        {
            Buffer.BlockCopy(keyBytes, 0, result, 0, 32);
        }
        else
        {
            Buffer.BlockCopy(keyBytes, 0, result, 0, keyBytes.Length);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(keyBytes);
            Buffer.BlockCopy(hash, 0, result, keyBytes.Length, 32 - keyBytes.Length);
        }

        return result;
    }
}
