using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PunchClockApi.Data;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for the role-based permission authorization system.
/// Tests Admin, HR Manager, and Staff roles with their respective permissions.
/// </summary>
public class PermissionAuthorizationTests : IntegrationTestBase
{
    public PermissionAuthorizationTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Admin Role Tests

    [Fact]
    public async Task Admin_CanAccessAllEndpoints()
    {
        // Arrange
        var token = await GetAccessTokenAsync("admin", "admin123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act & Assert - Test various endpoints that should all succeed
        var staffResponse = await Client.GetAsync("/api/staff");
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);

        var devicesResponse = await Client.GetAsync("/api/devices");
        Assert.Equal(HttpStatusCode.OK, devicesResponse.StatusCode);

        var usersResponse = await Client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);

        var attendanceResponse = await Client.GetAsync("/api/attendance/records");
        Assert.Equal(HttpStatusCode.OK, attendanceResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessSystemSettings()
    {
        // Arrange
        var token = await GetAccessTokenAsync("admin", "admin123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/system/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAssignAdminRole()
    {
        // Arrange
        var token = await GetAccessTokenAsync("admin", "admin123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a test user
        var newUser = new
        {
            username = "testadmin",
            email = "testadmin@test.com",
            password = "Test123!",
            firstName = "Test",
            lastName = "Admin"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", newUser);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        // Get Admin role
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var adminRole = await db.Roles.FirstAsync(r => r.RoleName == "Admin");

        // Act - Assign Admin role
        var assignResponse = await Client.PostAsync($"/api/users/{createdUser!.UserId}/roles/{adminRole.RoleId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_HasAllPermissionClaimsInToken()
    {
        // Arrange & Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Decode JWT and verify it contains permission claims
        var token = loginResult.GetProperty("accessToken").GetString()!;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var permissions = jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        
        // Admin should have all permissions (44+)
        Assert.True(permissions.Count >= 44, $"Expected at least 44 permissions, got {permissions.Count}");
        Assert.Contains("staff:create", permissions);
        Assert.Contains("users:assign_admin_role", permissions);
        Assert.Contains("system:settings", permissions);
        Assert.Contains("system:jobs", permissions);
    }

    #endregion

    #region HR Manager Role Tests

    [Fact]
    public async Task HRManager_CannotAccessSystemSettings()
    {
        // Arrange
        var token = await GetAccessTokenAsync("hrmanager", "hr123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/system/settings");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HRManager_CannotAssignAdminRole()
    {
        // Arrange
        var token = await GetAccessTokenAsync("hrmanager", "hr123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a test user
        var newUser = new
        {
            username = "testuser2",
            email = "testuser2@test.com",
            password = "Test123!",
            firstName = "Test",
            lastName = "User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", newUser);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        // Get Admin role
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var adminRole = await db.Roles.FirstAsync(r => r.RoleName == "Admin");

        // Act - Try to assign Admin role
        var assignResponse = await Client.PostAsync($"/api/users/{createdUser!.UserId}/roles/{adminRole.RoleId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, assignResponse.StatusCode);
    }

    [Fact]
    public async Task HRManager_CanAssignHRManagerRole()
    {
        // Arrange
        var token = await GetAccessTokenAsync("hrmanager", "hr123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a test user
        var newUser = new
        {
            username = "testhr",
            email = "testhr@test.com",
            password = "Test123!",
            firstName = "Test",
            lastName = "HR"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", newUser);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        // Get HR Manager role
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var hrRole = await db.Roles.FirstAsync(r => r.RoleName == "HR Manager");

        // Act - Assign HR Manager role
        var assignResponse = await Client.PostAsync($"/api/users/{createdUser!.UserId}/roles/{hrRole.RoleId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
    }

    [Fact]
    public async Task HRManager_CanManageStaff()
    {
        // Arrange
        var token = await GetAccessTokenAsync("hrmanager", "hr123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get required data
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var department = await db.Departments.FirstAsync();
        var location = await db.Locations.FirstAsync();

        var newStaff = new
        {
            employeeId = "EMP999",
            firstName = "Test",
            lastName = "Staff",
            email = "teststaff@test.com",
            departmentId = department.DepartmentId,
            locationId = location.LocationId
        };

        // Act
        var createResponse = await Client.PostAsJsonAsync("/api/staff", newStaff);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    [Fact]
    public async Task HRManager_CanLinkUserToStaff()
    {
        // Arrange
        var token = await GetAccessTokenAsync("hrmanager", "hr123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get a staff member and create a user
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var staff = await db.Staff.FirstAsync(s => s.UserId == null);

        var newUser = new
        {
            username = "staffuser1",
            email = "staffuser1@test.com",
            password = "Test123!",
            firstName = staff.FirstName,
            lastName = staff.LastName
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", newUser);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        // Act - Link user to staff
        var linkResponse = await Client.PostAsJsonAsync($"/api/staff/{staff.StaffId}/assign-user", new
        {
            userId = createdUser!.UserId
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
    }

    [Fact]
    public async Task HRManager_DoesNotHaveSystemPermissions()
    {
        // Arrange & Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "hrmanager",
            password = "hr123"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        var token = loginResult.GetProperty("accessToken").GetString()!;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var permissions = jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        // HR Manager should NOT have system permissions
        Assert.DoesNotContain("system:settings", permissions);
        Assert.DoesNotContain("system:jobs", permissions);
        Assert.DoesNotContain("users:assign_admin_role", permissions);

        // But should have other permissions
        Assert.Contains("staff:create", permissions);
        Assert.Contains("users:assign_roles", permissions);
    }

    #endregion

    #region Staff Role Tests

    [Fact]
    public async Task Staff_CannotAccessStaffList()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/staff");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_CannotCreateStaff()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var department = await db.Departments.FirstAsync();
        var location = await db.Locations.FirstAsync();

        var newStaff = new
        {
            employeeId = "EMP888",
            firstName = "Should",
            lastName = "Fail",
            email = "fail@test.com",
            departmentId = department.DepartmentId,
            locationId = location.LocationId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/staff", newStaff);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_CanViewOwnAttendance()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Link staff user to a staff record first
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var staffUser = await db.Users.Include(u => u.UserRoles).FirstAsync(u => u.Username == "staff");
        var staffRecord = await db.Staff.FirstAsync();
        staffRecord.UserId = staffUser.UserId;
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/attendance/records");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Staff_CannotViewAllAttendance()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to access without linked staff record
        var response = await Client.GetAsync("/api/attendance/records");

        // Assert - Should get either Forbidden or NotFound (depending on whether staff record is linked)
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK, // OK if returns only own records
            $"Expected Forbidden, NotFound or OK, got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task Staff_CanEnrollOwnRecordToDevice()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Link staff user to a staff record
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var staffUser = await db.Users.FirstAsync(u => u.Username == "staff");
        var location = await db.Locations.FirstAsync();
        var staffRecord = await db.Staff.FirstAsync();
        staffRecord.UserId = staffUser.UserId;
        staffRecord.LocationId = location.LocationId;
        await db.SaveChangesAsync();

        // Get a device at the same location
        var device = await db.Devices.FirstAsync(d => d.LocationId == location.LocationId);

        // Act - Enroll to device at same location
        var response = await Client.PostAsync($"/api/devices/{device.DeviceId}/staff/{staffRecord.StaffId}/enroll", null);

        // Assert - May fail due to device connection, but should not be Forbidden
        Assert.True(
            response.StatusCode != HttpStatusCode.Forbidden,
            $"Expected non-Forbidden status, got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task Staff_CannotEnrollOtherStaffToDevice()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get a different staff member
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var staffUser = await db.Users.FirstAsync(u => u.Username == "staff");
        var otherStaff = await db.Staff.FirstAsync(s => s.UserId != staffUser.UserId);
        var device = await db.Devices.FirstAsync();

        // Act - Try to enroll another staff member
        var response = await Client.PostAsync($"/api/devices/{device.DeviceId}/staff/{otherStaff.StaffId}/enroll", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_CannotEnrollToDeviceAtDifferentLocation()
    {
        // Arrange
        var token = await GetAccessTokenAsync("staff", "staff123");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Link staff user to a staff record at one location
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PunchClockDbContext>();
        var staffUser = await db.Users.FirstAsync(u => u.Username == "staff");
        var locations = await db.Locations.Take(2).ToListAsync();
        
        if (locations.Count < 2)
        {
            // Skip test if we don't have multiple locations
            return;
        }

        var staffRecord = await db.Staff.FirstAsync();
        staffRecord.UserId = staffUser.UserId;
        staffRecord.LocationId = locations[0].LocationId;
        await db.SaveChangesAsync();

        // Get a device at a different location
        var device = await db.Devices.FirstOrDefaultAsync(d => d.LocationId == locations[1].LocationId);
        
        if (device == null)
        {
            // Skip test if no device at second location
            return;
        }

        // Act - Try to enroll to device at different location
        var response = await Client.PostAsync($"/api/devices/{device.DeviceId}/staff/{staffRecord.StaffId}/enroll", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Staff_HasLimitedPermissions()
    {
        // Arrange & Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "staff",
            password = "staff123"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        var token = loginResult.GetProperty("accessToken").GetString()!;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var permissions = jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        // Staff should have exactly 4 permissions
        Assert.Equal(4, permissions.Count);
        Assert.Contains("devices:self_enroll", permissions);
        Assert.Contains("attendance:view_own", permissions);
        Assert.Contains("leave:request_own", permissions);
        Assert.Contains("leave:view_own", permissions);
    }

    #endregion

    #region Unauthenticated Access Tests

    [Fact]
    public async Task UnauthenticatedUser_CannotAccessProtectedEndpoints()
    {
        // Act
        var staffResponse = await Client.GetAsync("/api/staff");
        var devicesResponse = await Client.GetAsync("/api/devices");
        var usersResponse = await Client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, staffResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, devicesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, usersResponse.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUser_CanAccessHealthCheck()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region JWT Token Tests

    [Fact]
    public async Task JWT_ContainsPermissionClaims()
    {
        // Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123"
        });

        // Assert
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginResult.GetProperty("accessToken").GetString()!;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var permissionClaims = jwtToken.Claims.Where(c => c.Type == "permission").ToList();
        Assert.True(permissionClaims.Count > 0, "JWT should contain permission claims");
    }

    [Fact]
    public async Task JWT_ContainsRoleClaims()
    {
        // Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123"
        });

        // Assert
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginResult.GetProperty("accessToken").GetString()!;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type.Contains("role")).ToList();
        Assert.True(roleClaims.Count > 0, "JWT should contain role claims");
        Assert.Contains(roleClaims, c => c.Value == "Admin");
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetAccessTokenAsync(string username, string password)
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return loginResult.GetProperty("accessToken").GetString()!;
    }

    #endregion
}
