using Microsoft.Extensions.Configuration;
using OrderMonitor.Core.Interfaces;

namespace OrderMonitor.Infrastructure.Configuration;

/// <summary>
/// Validates that all required configuration values are present and valid at startup.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private static readonly string[] ValidDatabaseProviders = { "sqlserver", "mysql", "postgresql" };

    private readonly IConfiguration _configuration;

    public ConfigurationValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Validate()
    {
        var errors = new List<string>();

        // Database validation
        var provider = _configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
        {
            errors.Add("Database:Provider is required. Allowed values: sqlserver, mysql, postgresql");
        }
        else if (!ValidDatabaseProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Database:Provider '{provider}' is invalid. Allowed values: {string.Join(", ", ValidDatabaseProviders)}");
        }

        var connectionString = _configuration["Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Fall back to legacy ConnectionStrings:BackofficeDb
            connectionString = _configuration.GetConnectionString("BackofficeDb");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("Database:ConnectionString (or ConnectionStrings:BackofficeDb) is required.");
            }
        }

        // SMTP validation
        var smtpHost = _configuration["SmtpSettings:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            errors.Add("SmtpSettings:Host is required.");
        }

        // Alerts validation
        var alertRecipients = _configuration["Alerts:Recipients"];
        var alertEnabled = _configuration["Alerts:Enabled"];
        if (string.Equals(alertEnabled, "true", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(alertRecipients))
        {
            // Check for array-style binding (Alerts:Recipients:0, Alerts:Recipients:1, etc.)
            var section = _configuration.GetSection("Alerts:Recipients");
            if (!section.GetChildren().Any())
            {
                errors.Add("Alerts:Recipients is required when Alerts:Enabled is true.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration validation failed:\n" +
                string.Join("\n", errors.Select(e => $"  - {e}")));
        }
    }
}
