using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderMonitor.Core.Interfaces;
using OrderMonitor.Infrastructure.Data;

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

        return services;
    }
}
