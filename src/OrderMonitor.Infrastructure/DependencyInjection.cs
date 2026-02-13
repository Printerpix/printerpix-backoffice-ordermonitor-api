using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        // Register database context with provider factory
        services.AddDbContext<OrderMonitorDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSetup");

            // Configure encryption key from Database settings
            var encryptionKey = config["Database:EncryptionKey"];
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                PasswordEncryptor.Configure(encryptionKey);
            }

            // Resolve database provider from connection enable flags
            var provider = ResolveProviderFromConnectionFlags(config, logger);

            // Resolve connection string
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

            DatabaseProviderFactory.ConfigureProvider(options, provider, connectionString);
            options.UseQueryTrackingBehavior(Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking);
        });

        // Register repositories
        services.AddScoped<IOrderRepository, EfCoreOrderRepository>();

        // Register services
        services.AddScoped<IStuckOrderService, StuckOrderService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IDiagnosticsService, DiagnosticsService>();

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

    /// <summary>
    /// Resolves the database provider from connection enable flags.
    /// Checks environment variables first (SQL_CONNECTIONENABLE, MYSQL_CONNECTIONENABLE, POSTGRES_CONNECTIONENABLE),
    /// then falls back to configuration section (DatabaseConnections:SqlConnectionEnable, etc.),
    /// then falls back to Database:Provider setting.
    /// </summary>
    private static DatabaseProvider ResolveProviderFromConnectionFlags(
        IConfiguration config,
        ILogger logger)
    {
        // Check environment variables first, then configuration section
        var sqlEnabled = GetConnectionFlag("SQL_CONNECTIONENABLE", "DatabaseConnections:SqlConnectionEnable", config);
        var mysqlEnabled = GetConnectionFlag("MYSQL_CONNECTIONENABLE", "DatabaseConnections:MySqlConnectionEnable", config);
        var postgresEnabled = GetConnectionFlag("POSTGRES_CONNECTIONENABLE", "DatabaseConnections:PostgresConnectionEnable", config);

        // If no connection flags are set at all, fall back to Database:Provider
        if (sqlEnabled is null && mysqlEnabled is null && postgresEnabled is null)
        {
            var providerString = config["Database:Provider"] ?? "SqlServer";
            logger.LogInformation("No connection enable flags found. Falling back to Database:Provider = {Provider}", providerString);
            return DatabaseProviderFactory.ParseProvider(providerString);
        }

        // Default unset flags to false
        var sql = sqlEnabled ?? false;
        var mysql = mysqlEnabled ?? false;
        var postgres = postgresEnabled ?? false;

        var enabledCount = (sql ? 1 : 0) + (mysql ? 1 : 0) + (postgres ? 1 : 0);

        if (enabledCount == 0)
        {
            throw new InvalidOperationException(
                "No database connection is enabled. " +
                "Set one of: SQL_CONNECTIONENABLE=true, MYSQL_CONNECTIONENABLE=true, or POSTGRES_CONNECTIONENABLE=true.");
        }

        if (enabledCount > 1)
        {
            throw new InvalidOperationException(
                "Multiple database connections are enabled. Only one can be active at a time. " +
                $"Current: SQL={sql}, MySQL={mysql}, PostgreSQL={postgres}.");
        }

        if (sql)
        {
            logger.LogInformation("Database connection: SQL Server enabled");
            return DatabaseProvider.SqlServer;
        }

        if (mysql)
        {
            logger.LogInformation("Database connection: MySQL enabled");
            return DatabaseProvider.MySql;
        }

        logger.LogInformation("Database connection: PostgreSQL enabled");
        return DatabaseProvider.PostgreSql;
    }

    /// <summary>
    /// Gets a connection enable flag value. Checks environment variable first, then configuration.
    /// Returns null if neither is set.
    /// </summary>
    private static bool? GetConnectionFlag(string envVarName, string configKey, IConfiguration config)
    {
        // Check environment variable first
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return string.Equals(envValue.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        // Fall back to configuration section
        var configValue = config[configKey];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return string.Equals(configValue.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }
}
