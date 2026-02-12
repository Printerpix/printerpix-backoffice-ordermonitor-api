using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Services;
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

            // Resolve database provider
            var providerString = config["Database:Provider"] ?? "SqlServer";
            var provider = DatabaseProviderFactory.ParseProvider(providerString);

            // Resolve connection string
            var connectionString = config["Database:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = config.GetConnectionString("BackofficeDb")
                    ?? throw new InvalidOperationException(
                        "Database connection string is not configured. " +
                        "Set Database__ConnectionString or ConnectionStrings__BackofficeDb.");
            }

            // Handle encrypted passwords
            if (connectionString.Contains("{ENCRYPTED}"))
            {
                var encryptedPassword = config["DatabaseSettings:EncryptedPassword"];
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

        // Register configuration settings
        services.Configure<ScannerSettings>(configuration.GetSection(ScannerSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<AlertSettings>(configuration.GetSection(AlertSettings.SectionName));

        // Register background scanner as hosted service
        services.AddHostedService<BackgroundScannerService>();

        return services;
    }
}
