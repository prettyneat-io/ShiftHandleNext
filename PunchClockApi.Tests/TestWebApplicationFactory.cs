using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PunchClockApi.Data;

namespace PunchClockApi.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures an in-memory database for isolated test execution.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Add in-memory database for testing (Program.cs won't register PostgreSQL in Testing environment)
            services.AddDbContext<PunchClockDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
            
            // Register a hosted service to seed the database on startup
            services.AddSingleton<IHostedService, TestDatabaseSeeder>();
        });
    }
}

/// <summary>
/// Background service to seed the test database on startup
/// </summary>
internal sealed class TestDatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _services;

    public TestDatabaseSeeder(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
        
        await db.Database.EnsureCreatedAsync(cancellationToken);
        
        // Seed test data
        var seeder = new DatabaseSeeder(db, logger);
        await seeder.SeedAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

