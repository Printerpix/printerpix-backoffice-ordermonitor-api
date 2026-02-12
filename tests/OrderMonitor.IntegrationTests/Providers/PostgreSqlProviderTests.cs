using Microsoft.EntityFrameworkCore;
using OrderMonitor.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace OrderMonitor.IntegrationTests.Providers;

/// <summary>
/// Integration tests for EfCoreOrderRepository against a real PostgreSQL database via Testcontainers.
/// Validates that all LINQ queries translate correctly to PostgreSQL SQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "PostgreSQL")]
public class PostgreSqlProviderTests : DatabaseProviderTestBase, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("ordermonitor_test")
        .WithUsername("testuser")
        .WithPassword("testpass123!")
        .Build();

    protected override async Task<DbContextOptions<OrderMonitorDbContext>> CreateDbContextOptionsAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        return new DbContextOptionsBuilder<OrderMonitorDbContext>()
            .UseNpgsql(connectionString)
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
