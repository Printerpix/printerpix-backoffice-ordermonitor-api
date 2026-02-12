namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Database connection configuration settings.
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider: sqlserver, mysql, or postgresql.
    /// </summary>
    public string Provider { get; set; } = "sqlserver";

    /// <summary>
    /// Database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted database password (Base64-encoded AES).
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// AES encryption key for decrypting EncryptedPassword.
    /// Must be provided via environment variable — never hardcoded.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Maximum connection pool size.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
}
