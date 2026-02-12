using OrderMonitor.Core.Interfaces;
using OrderMonitor.Infrastructure;
using OrderMonitor.Infrastructure.Configuration;
using Serilog;

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Order Monitor API");

    var builder = WebApplication.CreateBuilder(args);

    // Add YAML configuration sources (before builder.Build)
    // Override precedence: appsettings.json → YML file → environment variables
    var environment = builder.Environment.EnvironmentName;
    builder.Configuration
        .AddYamlFile("OrderMonitor_ENV.yml", optional: true)
        .AddYamlFile($"OrderMonitor_ENV.{environment.ToLowerInvariant()}.yml", optional: true)
        .AddEnvironmentVariables();

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add infrastructure services (database, repositories, config validation)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add health checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Validate configuration at startup
    var validator = app.Services.GetService<IConfigurationValidator>();
    validator?.Validate();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public for testing
public partial class Program { }
