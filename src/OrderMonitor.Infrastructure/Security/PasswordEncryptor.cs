using System.Security.Cryptography;
using System.Text;

namespace OrderMonitor.Infrastructure.Security;

/// <summary>
/// Utility for encrypting and decrypting passwords using AES encryption.
/// </summary>
public static class PasswordEncryptor
{
    // Default key - in production, use a key from environment variable or secure storage
    private static readonly string DefaultKey = "OrderMonitor2026SecureKey32Bytes!";

    /// <summary>
    /// Encrypts a plain text password.
    /// </summary>
    /// <param name="plainText">The plain text password to encrypt.</param>
    /// <param name="key">Optional encryption key (32 characters). If not provided, uses default key.</param>
    /// <returns>Base64 encoded encrypted string with IV prepended.</returns>
    public static string Encrypt(string plainText, string? key = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var encryptionKey = GetKeyBytes(key ?? DefaultKey);

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
    /// <param name="key">Optional encryption key (32 characters). If not provided, uses default key.</param>
    /// <returns>The decrypted plain text password.</returns>
    public static string Decrypt(string encryptedText, string? key = null)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        // Check if it looks like an encrypted value (Base64 with reasonable length)
        if (!IsEncrypted(encryptedText))
            return encryptedText; // Return as-is if not encrypted

        try
        {
            var encryptionKey = GetKeyBytes(key ?? DefaultKey);
            var fullCipher = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = encryptionKey;

            // Extract IV from the beginning
            var iv = new byte[aes.BlockSize / 8];
            var cipherBytes = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If decryption fails, return original (might be plain text)
            return encryptedText;
        }
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
            return bytes.Length >= 32; // At least IV (16) + some encrypted data
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GetKeyBytes(string key)
    {
        // Ensure key is exactly 32 bytes (256 bits) for AES-256
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var result = new byte[32];

        if (keyBytes.Length >= 32)
        {
            Buffer.BlockCopy(keyBytes, 0, result, 0, 32);
        }
        else
        {
            Buffer.BlockCopy(keyBytes, 0, result, 0, keyBytes.Length);
            // Pad with derived bytes
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(keyBytes);
            Buffer.BlockCopy(hash, 0, result, keyBytes.Length, 32 - keyBytes.Length);
        }

        return result;
    }
}
