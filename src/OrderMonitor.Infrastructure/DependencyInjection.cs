using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Services;
using OrderMonitor.Infrastructure.Configuration;
using OrderMonitor.Infrastructure.Data;
using OrderMonitor.Infrastructure.Security;
using OrderMonitor.Infrastructure.Services;

namespace OrderMonitor.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Use factory pattern to defer connection string resolution until first use.
        // This ensures all configuration sources (YAML, env vars, test overrides) are loaded.
        services.AddSingleton<IDbConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            // Configure encryption key from Database settings
            var encryptionKey = config["Database:EncryptionKey"];
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                PasswordEncryptor.Configure(encryptionKey);
            }

            // Resolve connection string: prefer new Database:ConnectionString, fall back to legacy
            var connectionString = config["Database:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = config.GetConnectionString("BackofficeDb")
                    ?? throw new InvalidOperationException(
                        "Database connection string is not configured. " +
                        "Set Database__ConnectionString or ConnectionStrings__BackofficeDb.");
            }

            // Decrypt password if needed
            if (connectionString.Contains("{ENCRYPTED}"))
            {
                var encryptedPassword = config["Database:EncryptedPassword"]
                    ?? config["DatabaseSettings:EncryptedPassword"];

                if (!string.IsNullOrEmpty(encryptedPassword))
                {
                    var decryptedPassword = PasswordEncryptor.Decrypt(encryptedPassword);
                    connectionString = connectionString.Replace("{ENCRYPTED}", decryptedPassword);
                }
            }

            return new SqlConnectionFactory(connectionString);
        });

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register services
        services.AddScoped<IStuckOrderService, StuckOrderService>();
        services.AddScoped<IAlertService, AlertService>();

        // Register configuration settings (all sections)
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<BusinessHoursSettings>(configuration.GetSection(BusinessHoursSettings.SectionName));
        services.Configure<HealthCheckSettings>(configuration.GetSection(HealthCheckSettings.SectionName));
        services.Configure<SwaggerSettings>(configuration.GetSection(SwaggerSettings.SectionName));
        services.Configure<ScannerSettings>(configuration.GetSection(ScannerSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<AlertSettings>(configuration.GetSection(AlertSettings.SectionName));

        // Register configuration validator
        services.AddSingleton<IConfigurationValidator>(sp =>
            new ConfigurationValidator(configuration));

        // Register YAML config loader
        services.AddSingleton<IYamlConfigLoader, YamlConfigLoader>();

        // Register background scanner as hosted service
        services.AddHostedService<BackgroundScannerService>();

        return services;
    }
}
