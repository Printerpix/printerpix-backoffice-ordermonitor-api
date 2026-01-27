using System.Security.Cryptography;
using System.Text;

// Simple tool to encrypt passwords for Order Monitor API

var password = args.Length > 0 ? args[0] : "Pixbo@2019";

var key = "OrderMonitor2026SecureKey32Bytes!";
var keyBytes = GetKeyBytes(key);

using var aes = Aes.Create();
aes.Key = keyBytes;
aes.GenerateIV();

static byte[] GetKeyBytes(string key)
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

using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
var plainBytes = Encoding.UTF8.GetBytes(password);
var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

// Prepend IV to encrypted data
var result = new byte[aes.IV.Length + encryptedBytes.Length];
Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

var encrypted = Convert.ToBase64String(result);

Console.WriteLine($"Plain text: {password}");
Console.WriteLine($"Encrypted:  {encrypted}");
