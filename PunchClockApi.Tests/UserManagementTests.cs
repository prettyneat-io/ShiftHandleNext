using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for user management endpoints.
/// Tests user CRUD operations, role assignments, password changes, and authorization.
/// </summary>
public sealed class UserManagementTests : IntegrationTestBase
{
    public UserManagementTests(TestWebApplicationFactory factory) : base(factory) { }

    #region User CRUD Tests

    [Fact]
    public async Task GetAllUsers_ReturnsActiveUsersOnly_WithPagination()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Create test users
        var user1 = new User 
        { 
            UserId = Guid.NewGuid(), 
            Username = $"testuser1_{Guid.NewGuid():N}".Substring(0, 20),
            Email = $"test1_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var user2 = new User 
        { 
            UserId = Guid.NewGuid(), 
            Username = $"testuser2_{Guid.NewGuid():N}".Substring(0, 20),
            Email = $"test2_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            IsActive = false, // Inactive
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/users?page=1&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        users.Should().NotBeNull();
        users!.Should().Contain(u => u.Username == user1.Username && u.IsActive);
        users.Should().NotContain(u => u.Username == user2.Username);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser_WithRolesAndPermissions()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync();
        
        // Assign role to user
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        };
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/users/{user.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserDetailResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User.UserId.Should().Be(user.UserId);
        result.User.Username.Should().Be(user.Username);
        result.User.Roles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var createRequest = new 
        { 
            username = $"newuser_{Guid.NewGuid():N}".Substring(0, 20),
            email = $"newuser_{Guid.NewGuid():N}@test.com",
            password = "SecurePassword123!",
            firstName = "New",
            lastName = "User",
            phone = "555-1234",
            isActive = true,
            isVerified = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UserId.Should().NotBeEmpty();
        result.Username.Should().Be(createRequest.username);
        
        // Verify in database
        using var db = GetDbContext();
        var createdUser = await db.Users.FindAsync(result.UserId);
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(createRequest.email);
        createdUser.FirstName.Should().Be("New");
        createdUser.LastName.Should().Be("User");
    }

    [Fact]
    public async Task CreateUser_ReturnsError_WhenUsernameExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var existingUser = await CreateTestUser(db);
        
        var createRequest = new 
        { 
            username = existingUser.Username, // Duplicate
            email = $"different_{Guid.NewGuid():N}@test.com",
            password = "SecurePassword123!",
            firstName = "Duplicate",
            lastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_ReturnsError_WhenEmailExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var existingUser = await CreateTestUser(db);
        
        var createRequest = new 
        { 
            username = $"different_{Guid.NewGuid():N}".Substring(0, 20),
            email = existingUser.Email, // Duplicate
            password = "SecurePassword123!",
            firstName = "Duplicate",
            lastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_ReturnsError_WhenRequiredFieldsMissing()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        var createRequest = new 
        { 
            username = "", // Empty
            email = "test@test.com",
            password = "", // Empty
            firstName = "Test",
            lastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_UpdatesSuccessfully_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        
        var updateRequest = new 
        { 
            email = $"updated_{Guid.NewGuid():N}@test.com",
            firstName = "Updated",
            lastName = "Name",
            phone = "555-5678",
            isActive = true,
            isVerified = true
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        using var verifyDb = GetDbContext();
        var updatedUser = await verifyDb.Users.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.Email.Should().Be(updateRequest.email);
        updatedUser.FirstName.Should().Be("Updated");
        updatedUser.LastName.Should().Be("Name");
        updatedUser.Phone.Should().Be("555-5678");
    }

    [Fact]
    public async Task UpdateUser_ReturnsError_WhenEmailTaken()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user1 = await CreateTestUser(db, "user1");
        var user2 = await CreateTestUser(db, "user2");
        
        var updateRequest = new 
        { 
            email = user2.Email, // Try to use user2's email
            firstName = "Updated",
            lastName = "Name"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user1.UserId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteUser_SoftDeletes_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);

        // Act
        var response = await Client.DeleteAsync($"/api/users/{user.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify soft delete
        using var verifyDb = GetDbContext();
        var deletedUser = await verifyDb.Users.FindAsync(user.UserId);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_ReturnsError_WhenDeletingOwnAccount()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Get the admin user we're authenticated as
        var adminUser = await db.Users.FirstAsync(u => u.Username == "admin");

        // Act
        var response = await Client.DeleteAsync($"/api/users/{adminUser.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cannot delete your own account");
    }

    #endregion

    #region Role Assignment Tests

    [Fact]
    public async Task AssignRole_AssignsSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync(r => r.RoleName != "Admin");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{role.RoleId}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        using var verifyDb = GetDbContext();
        var userRole = await verifyDb.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);
        userRole.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRole_WithExpirationDate_AssignsSuccessfully()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync(r => r.RoleName != "Admin");
        var expiresAt = DateTime.UtcNow.AddMonths(6);
        
        var assignRequest = new { expiresAt };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{role.RoleId}", assignRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        using var verifyDb = GetDbContext();
        var userRole = await verifyDb.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);
        userRole.Should().NotBeNull();
        userRole!.ExpiresAt.Should().NotBeNull();
        userRole.ExpiresAt.Value.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AssignRole_ReturnsError_WhenRoleAlreadyAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync();
        
        // Assign role first time
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        };
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync();

        // Act - Try to assign same role again
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{role.RoleId}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already assigned");
    }

    [Fact]
    public async Task AssignAdminRole_SucceedsForAdmin()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var adminRole = await db.Roles.FirstAsync(r => r.RoleName == "Admin");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{adminRole.RoleId}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify in database
        using var verifyDb = GetDbContext();
        var userRole = await verifyDb.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == adminRole.RoleId);
        userRole.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var role = await db.Roles.FirstAsync();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{Guid.NewGuid()}/roles/{role.RoleId}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{Guid.NewGuid()}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveRole_RemovesSuccessfully_WhenAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync();
        
        // Assign role
        var userRole = new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        };
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/users/{user.UserId}/roles/{role.RoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify removal
        using var verifyDb = GetDbContext();
        var removed = await verifyDb.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);
        removed.Should().BeNull();
    }

    [Fact]
    public async Task RemoveRole_ReturnsNotFound_WhenNotAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync();

        // Act - Try to remove role that's not assigned
        var response = await Client.DeleteAsync($"/api/users/{user.UserId}/roles/{role.RoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Password Change Tests

    [Fact]
    public async Task ChangePassword_ChangesSuccessfully_WithCurrentPassword()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var currentPassword = "OldPassword123!";
        var user = await CreateTestUser(db, passwordOverride: currentPassword);
        
        // Authenticate as the test user (remove admin token first)
        Client.DefaultRequestHeaders.Authorization = null;
        var userToken = await TestAuthHelper.LoginAsync(Client, user.Username, currentPassword);
        TestAuthHelper.AddAuthHeader(Client, userToken);
        
        var changeRequest = new 
        { 
            currentPassword = currentPassword,
            newPassword = "NewPassword456!"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify new password works
        Client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await TestAuthHelper.LoginAsync(Client, user.Username, "NewPassword456!");
        loginResponse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_ReturnsError_WithIncorrectCurrentPassword()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var currentPassword = "OldPassword123!";
        var user = await CreateTestUser(db, passwordOverride: currentPassword);
        
        // Authenticate as the test user (remove admin token first)
        Client.DefaultRequestHeaders.Authorization = null;
        var userToken = await TestAuthHelper.LoginAsync(Client, user.Username, currentPassword);
        TestAuthHelper.AddAuthHeader(Client, userToken);
        
        var changeRequest = new 
        { 
            currentPassword = "WrongPassword!",
            newPassword = "NewPassword456!"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("incorrect");
    }

    [Fact]
    public async Task ChangePassword_AdminCanChangeWithoutCurrentPassword()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        
        var changeRequest = new 
        { 
            currentPassword = (string?)null, // Admin doesn't need current password
            newPassword = "NewPassword456!"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify new password works
        var loginResponse = await TestAuthHelper.LoginAsync(Client, user.Username, "NewPassword456!");
        loginResponse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_ReturnsError_WhenNewPasswordEmpty()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        
        var changeRequest = new 
        { 
            currentPassword = (string?)null,
            newPassword = ""
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}/password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task UserEndpoints_RequireAuthentication()
    {
        // Act
        var getResponse = await Client.GetAsync("/api/users");
        var postResponse = await Client.PostAsJsonAsync("/api/users", new { });
        
        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_RequiresUsersReadPermission()
    {
        await AuthenticateAsAdminAsync(); // Admin has users:read
        
        var response = await Client.GetAsync("/api/users");
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_RequiresUsersCreatePermission()
    {
        await AuthenticateAsAdminAsync(); // Admin has users:create
        
        var createRequest = new 
        { 
            username = $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
            email = $"test_{Guid.NewGuid():N}@test.com",
            password = "Password123!",
            firstName = "Test",
            lastName = "User"
        };
        
        var response = await Client.PostAsJsonAsync("/api/users", createRequest);
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateUser_RequiresUsersUpdatePermission()
    {
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        var user = await CreateTestUser(db);
        
        var response = await Client.PutAsJsonAsync($"/api/users/{user.UserId}", new { firstName = "Updated" });
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_RequiresUsersDeletePermission()
    {
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        var user = await CreateTestUser(db);
        
        var response = await Client.DeleteAsync($"/api/users/{user.UserId}");
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignRole_RequiresUsersAssignRolesPermission()
    {
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var user = await CreateTestUser(db);
        var role = await db.Roles.FirstAsync();
        
        var response = await Client.PostAsJsonAsync($"/api/users/{user.UserId}/roles/{role.RoleId}", new { });
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Helper Methods

    private static async Task<User> CreateTestUser(PunchClockDbContext db, string? suffix = null, string? passwordOverride = null)
    {
        var uniqueId = suffix ?? Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = $"testuser_{uniqueId}",
            Email = $"test_{uniqueId}@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordOverride ?? "TestPassword123!"),
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    #endregion

    #region Response DTOs

    private sealed class UserResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = [];
    }

    private sealed class UserDetailResponse
    {
        public bool Success { get; set; }
        public UserDetail User { get; set; } = null!;
    }

    private sealed class UserDetail
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<RoleDetail> Roles { get; set; } = [];
    }

    private sealed class RoleDetail
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<PermissionDetail> Permissions { get; set; } = [];
    }

    private sealed class PermissionDetail
    {
        public Guid PermissionId { get; set; }
        public string Resource { get; set; } = null!;
        public string Action { get; set; } = null!;
    }

    private sealed class CreateUserResponse
    {
        public bool Success { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
    }

    #endregion
}
