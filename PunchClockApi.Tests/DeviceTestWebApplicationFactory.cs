using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PunchClockApi.Data;
using PunchClockApi.Services;
using PyZK.DotNet;
using Python.Runtime;

namespace PunchClockApi.Tests;

/// <summary>
/// Custom WebApplicationFactory for device integration testing.
/// Initializes Python.NET and registers the real IDeviceService for testing with actual devices.
/// </summary>
public sealed class DeviceTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static bool _pythonInitialized = false;
    private static readonly object _pythonInitLock = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Initialize Python.NET once for all device tests
        lock (_pythonInitLock)
        {
            if (!_pythonInitialized)
            {
                // Set Python DLL path for Linux
                if (OperatingSystem.IsLinux())
                {
                    Runtime.PythonDLL = "/usr/lib/x86_64-linux-gnu/libpython3.13.so.1.0";
                }
                PyZKClient.InitializePython();
                _pythonInitialized = true;
            }
        }

        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Add in-memory database for testing
            services.AddDbContext<PunchClockDbContext>(options =>
            {
                options.UseInMemoryDatabase("DeviceTestDatabase");
            });
            
            // Register the REAL IDeviceService for device integration tests
            // (Program.cs skips this in Testing environment expecting a mock)
            services.AddScoped<IDeviceService, DeviceService>();
            
            // Register a hosted service to seed the database on startup
            services.AddSingleton<IHostedService, DeviceTestDatabaseSeeder>();
        });
    }
}

/// <summary>
/// Background service to seed the test database on startup for device tests
/// </summary>
internal sealed class DeviceTestDatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _services;

    public DeviceTestDatabaseSeeder(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
        
        await db.Database.EnsureCreatedAsync(cancellationToken);
        
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var seeder = new DatabaseSeeder(db, logger);
            await seeder.SeedAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
