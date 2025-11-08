using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for system settings and administration endpoints.
/// Tests settings retrieval, updates, and system health checks.
/// Note: SystemSettingsController is currently a placeholder with limited implementation.
/// </summary>
public sealed class SystemSettingsTests : IntegrationTestBase
{
    public SystemSettingsTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Settings Retrieval Tests

    [Fact]
    public async Task GetSettings_ReturnsSettingsInformation_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/system/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("settings");
    }

    [Fact]
    public async Task GetSettings_RequiresAuthentication()
    {
        // Act
        var response = await Client.GetAsync("/api/system/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSettingByKey_ReturnsSettingInformation_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/system/settings/jwt_expiration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SettingResponse>();
        result.Should().NotBeNull();
        result!.Key.Should().Be("jwt_expiration");
    }

    [Fact]
    public async Task GetSettingByKey_RequiresAuthentication()
    {
        // Act
        var response = await Client.GetAsync("/api/system/settings/test_key");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Settings Update Tests

    [Fact]
    public async Task UpdateSettings_ReturnsSuccessMessage_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var settings = new 
        { 
            jwtExpiration = 3600,
            sessionTimeout = 1800,
            maxLoginAttempts = 5
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/system/settings", settings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("settings");
    }

    [Fact]
    public async Task UpdateSettings_RequiresAuthentication()
    {
        // Act
        var response = await Client.PutAsJsonAsync("/api/system/settings", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSetting_UpdatesSingleSetting_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var settingUpdate = new 
        { 
            value = "7200",
            description = "JWT token expiration in seconds"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/system/settings/jwt_expiration", settingUpdate);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SettingUpdateResponse>();
        result.Should().NotBeNull();
        result!.Key.Should().Be("jwt_expiration");
        result.Value.Should().Be("7200");
    }

    [Fact]
    public async Task UpdateSetting_RequiresAuthentication()
    {
        // Act
        var response = await Client.PutAsJsonAsync("/api/system/settings/test_key", new { value = "test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task ResetToDefaults_ReturnsSuccessMessage_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.PostAsync("/api/system/settings/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("reset");
        content.Should().Contain("default");
    }

    [Fact]
    public async Task ResetToDefaults_RequiresAuthentication()
    {
        // Act
        var response = await Client.PostAsync("/api/system/settings/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task GetDetailedHealth_ReturnsHealthInformation_WhenAuthenticated()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/system/settings/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DetailedHealthResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().NotBeNullOrEmpty();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetDetailedHealth_RequiresAuthentication()
    {
        // Act
        var response = await Client.GetAsync("/api/system/settings/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetailedHealth_ReturnsHealthyStatus()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/system/settings/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DetailedHealthResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("healthy");
        result.Database.Should().NotBeNull();
        result.Database.Connected.Should().BeTrue();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task SystemSettingsEndpoints_RequireSystemSettingsPermission()
    {
        // Note: All system settings endpoints require the system:settings permission
        // which is only granted to Admin users by default
        await AuthenticateAsAdminAsync();
        
        var getResponse = await Client.GetAsync("/api/system/settings");
        var healthResponse = await Client.GetAsync("/api/system/settings/health/detailed");
        
        // Should succeed for admin
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemSettingsEndpoints_RejectUnauthenticatedRequests()
    {
        // Act - Test multiple endpoints without authentication
        var getResponse = await Client.GetAsync("/api/system/settings");
        var getByKeyResponse = await Client.GetAsync("/api/system/settings/test_key");
        var putResponse = await Client.PutAsJsonAsync("/api/system/settings", new { });
        var putByKeyResponse = await Client.PutAsJsonAsync("/api/system/settings/test_key", new { value = "test" });
        var resetResponse = await Client.PostAsync("/api/system/settings/reset", null);
        var healthResponse = await Client.GetAsync("/api/system/settings/health/detailed");
        
        // Assert - All should require authentication
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        getByKeyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        putByKeyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resetResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SystemSettings_EndToEnd_GetUpdateAndReset()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act 1: Get initial settings
        var getResponse1 = await Client.GetAsync("/api/system/settings");
        getResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Update a setting
        var updateResponse = await Client.PutAsJsonAsync("/api/system/settings/test_setting", 
            new { value = "test_value", description = "Test setting" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 3: Get the updated setting
        var getByKeyResponse = await Client.GetAsync("/api/system/settings/test_setting");
        getByKeyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 4: Reset to defaults
        var resetResponse = await Client.PostAsync("/api/system/settings/reset", null);
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 5: Verify health check still works after operations
        var healthResponse = await Client.GetAsync("/api/system/settings/health/detailed");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var healthResult = await healthResponse.Content.ReadFromJsonAsync<DetailedHealthResponse>();
        healthResult.Should().NotBeNull();
        healthResult!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task SystemSettings_HealthCheck_ReportsCorrectInformation()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/system/settings/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DetailedHealthResponse>();
        
        result.Should().NotBeNull();
        result!.Status.Should().Be("healthy");
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Database.Should().NotBeNull();
        result.Database.Connected.Should().BeTrue();
        result.BackgroundJobs.Should().NotBeNull();
        result.Devices.Should().NotBeNull();
    }

    #endregion

    #region Response DTOs

    private sealed class SettingResponse
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    private sealed class SettingUpdateResponse
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    private sealed class DetailedHealthResponse
    {
        public string Status { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public DatabaseHealthInfo Database { get; set; } = null!;
        public BackgroundJobsHealthInfo BackgroundJobs { get; set; } = null!;
        public DevicesHealthInfo Devices { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    private sealed class DatabaseHealthInfo
    {
        public bool Connected { get; set; }
        public string ResponseTime { get; set; } = null!;
    }

    private sealed class BackgroundJobsHealthInfo
    {
        public bool Running { get; set; }
        public DateTime LastRun { get; set; }
    }

    private sealed class DevicesHealthInfo
    {
        public int Total { get; set; }
        public int Online { get; set; }
        public int Offline { get; set; }
    }

    #endregion
}
