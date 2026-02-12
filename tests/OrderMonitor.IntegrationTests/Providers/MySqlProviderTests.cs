using Microsoft.EntityFrameworkCore;
using OrderMonitor.Infrastructure.Data;
using Testcontainers.MySql;

namespace OrderMonitor.IntegrationTests.Providers;

/// <summary>
/// Integration tests for EfCoreOrderRepository against a real MySQL database via Testcontainers.
/// Validates that all LINQ queries translate correctly to MySQL SQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "MySQL")]
public class MySqlProviderTests : DatabaseProviderTestBase, IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.0")
        .WithDatabase("ordermonitor_test")
        .WithUsername("testuser")
        .WithPassword("testpass123!")
        .Build();

    protected override async Task<DbContextOptions<OrderMonitorDbContext>> CreateDbContextOptionsAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        return new DbContextOptionsBuilder<OrderMonitorDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;
    }

    protected override async Task OnDisposeAsync()
    {
        await _container.DisposeAsync();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await base.InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
