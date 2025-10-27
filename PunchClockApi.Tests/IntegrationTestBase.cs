using Microsoft.Extensions.DependencyInjection;
using PunchClockApi.Data;

namespace PunchClockApi.Tests;

/// <summary>
/// Base class for integration tests providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected string? AccessToken;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Authenticate as admin user and store token for subsequent requests.
    /// </summary>
    protected async Task AuthenticateAsAdminAsync()
    {
        AccessToken = await TestAuthHelper.LoginAsync(Client, "admin", "admin123");
        TestAuthHelper.AddAuthHeader(Client, AccessToken);
    }

    /// <summary>
    /// Get a fresh database context from the factory.
    /// </summary>
    protected PunchClockDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
    }

    /// <summary>
    /// Clean up resources.
    /// </summary>
    public void Dispose()
    {
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
