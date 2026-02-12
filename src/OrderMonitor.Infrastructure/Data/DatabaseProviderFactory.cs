using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OrderMonitor.Infrastructure.Data;

/// <summary>
/// Supported database providers for multi-database support.
/// </summary>
public enum DatabaseProvider
{
    SqlServer,
    MySql,
    PostgreSql
}

/// <summary>
/// Factory for configuring the DbContext with the appropriate database provider.
/// </summary>
public static class DatabaseProviderFactory
{
    private static readonly HashSet<string> ValidProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "SqlServer", "MySql", "PostgreSql"
    };

    /// <summary>
    /// Parses a provider string into a DatabaseProvider enum value.
    /// </summary>
    public static DatabaseProvider ParseProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Database provider cannot be null or empty.", nameof(provider));

        return provider.Trim().ToLowerInvariant() switch
        {
            "sqlserver" => DatabaseProvider.SqlServer,
            "mysql" => DatabaseProvider.MySql,
            "postgresql" or "postgres" => DatabaseProvider.PostgreSql,
            _ => throw new ArgumentException(
                $"Invalid database provider '{provider}'. Allowed values: {string.Join(", ", ValidProviders)}.",
                nameof(provider))
        };
    }

    /// <summary>
    /// Adds the OrderMonitorDbContext to the service collection with the specified provider.
    /// </summary>
    public static IServiceCollection AddOrderMonitorDbContext(
        this IServiceCollection services,
        DatabaseProvider provider,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        services.AddDbContext<OrderMonitorDbContext>(options =>
        {
            ConfigureProvider(options, provider, connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    /// <summary>
    /// Configures the DbContextOptionsBuilder for the specified provider.
    /// </summary>
    public static void ConfigureProvider(
        DbContextOptionsBuilder options,
        DatabaseProvider provider,
        string connectionString)
    {
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                options.UseSqlServer(connectionString);
                break;

            case DatabaseProvider.MySql:
                var serverVersion = ServerVersion.AutoDetect(connectionString);
                options.UseMySql(connectionString, serverVersion);
                break;

            case DatabaseProvider.PostgreSql:
                options.UseNpgsql(connectionString);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider,
                    $"Unsupported database provider: {provider}");
        }
    }
}
