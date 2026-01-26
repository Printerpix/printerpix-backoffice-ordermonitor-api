using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderMonitor.Core.Configuration;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Core.Services;
using OrderMonitor.Infrastructure.Data;
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
        // Register database connection factory
        var connectionString = configuration.GetConnectionString("BackofficeDb")
            ?? throw new InvalidOperationException("BackofficeDb connection string is not configured.");

        services.AddSingleton<IDbConnectionFactory>(sp => new SqlConnectionFactory(connectionString));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register services
        services.AddScoped<IStuckOrderService, StuckOrderService>();
        services.AddScoped<IAlertService, AlertService>();

        // Register scanner settings
        services.Configure<ScannerSettings>(configuration.GetSection(ScannerSettings.SectionName));

        // Register background scanner as hosted service
        services.AddHostedService<BackgroundScannerService>();

        return services;
    }
}
