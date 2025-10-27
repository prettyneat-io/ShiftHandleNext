using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for authentication flow.
/// Migrated from test-auth.sh
/// </summary>
public sealed class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task HealthCheck_NoAuth_ReturnsHealthy()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("healthy");
        health.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "admin123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.TokenType.Should().Be("Bearer");
        loginResponse.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "admin",
            password = "wrongpassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Username.Should().Be("admin");
        user.Email.Should().NotBeNullOrEmpty();
        user.FirstName.Should().NotBeNullOrEmpty();
        user.LastName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AccessStaffEndpoint_WithValidToken_ReturnsData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staffList = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staffList.Should().NotBeNull();
        staffList.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        // Arrange
        var registerRequest = new
        {
            username = $"testuser_{Guid.NewGuid():N}",
            email = $"test_{Guid.NewGuid():N}@example.com",
            password = "test123",
            firstName = "Test",
            lastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Username.Should().Be(registerRequest.username);
        user.Email.Should().Be(registerRequest.email);
        user.FirstName.Should().Be(registerRequest.firstName);
        user.LastName.Should().Be(registerRequest.lastName);
    }

    [Fact]
    public async Task ListUsers_AsAdmin_ReturnsUsersList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/users?limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        users.Should().NotBeNull();
        users.Should().HaveCountGreaterThan(0);
        users.Should().HaveCountLessThanOrEqualTo(5);
    }

    // Response DTOs for deserialization
    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    private sealed class UserResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private sealed class StaffResponse
    {
        public Guid StaffId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
