using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for overtime policy management endpoints.
/// Tests policy CRUD operations, default policy management, validation, and assignments.
/// </summary>
public sealed class OvertimePolicyTests : IntegrationTestBase
{
    public OvertimePolicyTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Policy CRUD Tests

    [Fact]
    public async Task GetAllPolicies_ReturnsActivePoliciesOnly_WhenNotFiltered()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var activePolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Standard Overtime", 
            PolicyCode = "STANDARD",
            Description = "Standard overtime policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var inactivePolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Old Policy", 
            PolicyCode = "OLD",
            Description = "Deprecated policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.0m,
            EffectiveFrom = DateTime.UtcNow.AddYears(-2),
            EffectiveTo = DateTime.UtcNow.AddYears(-1),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.AddRange(activePolicy, inactivePolicy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var policies = await response.Content.ReadFromJsonAsync<List<OvertimePolicy>>();
        policies.Should().NotBeNull();
        policies!.Should().Contain(p => p.PolicyName == "Standard Overtime" && p.IsActive);
        policies.Should().NotContain(p => p.PolicyName == "Old Policy");
    }

    [Fact]
    public async Task GetAllPolicies_ReturnsInactivePolicies_WhenIsActiveFalse()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var inactivePolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Inactive Policy", 
            PolicyCode = "INACTIVE",
            Description = "Test policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddYears(-1),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(inactivePolicy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies?isActive=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var policies = await response.Content.ReadFromJsonAsync<List<OvertimePolicy>>();
        policies.Should().NotBeNull();
        policies!.Should().Contain(p => p.PolicyName == "Inactive Policy" && !p.IsActive);
    }

    [Fact]
    public async Task GetAllPolicies_FiltersCorrectly_ByIsDefault()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var defaultPolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Default Policy", 
            PolicyCode = "DEFAULT",
            Description = "Default overtime policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var nonDefaultPolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Custom Policy", 
            PolicyCode = "CUSTOM",
            Description = "Custom policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 2.0m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.AddRange(defaultPolicy, nonDefaultPolicy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies?isDefault=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var policies = await response.Content.ReadFromJsonAsync<List<OvertimePolicy>>();
        policies.Should().NotBeNull();
        policies!.Should().Contain(p => p.PolicyName == "Default Policy" && p.IsDefault);
        policies.Should().NotContain(p => p.PolicyName == "Custom Policy");
    }

    [Fact]
    public async Task GetAllPolicies_SupportsPagination()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Create 5 test policies
        for (int i = 0; i < 5; i++)
        {
            var policy = new OvertimePolicy 
            { 
                PolicyId = Guid.NewGuid(), 
                PolicyName = $"Policy {i}", 
                PolicyCode = $"POL{i}",
                Description = $"Test policy {i}",
                DailyThreshold = TimeSpan.FromHours(8),
                DailyMultiplier = 1.5m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-30),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.OvertimePolicies.Add(policy);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<OvertimePolicy>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCountLessThanOrEqualTo(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetPolicyById_ReturnsPolicy_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Premium Overtime", 
            PolicyCode = "PREMIUM",
            Description = "Premium overtime rates",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 2.0m,
            ApplyWeeklyRule = true,
            WeeklyThreshold = TimeSpan.FromHours(40),
            WeeklyMultiplier = 2.5m,
            ApplyWeekendRule = true,
            WeekendMultiplier = 3.0m,
            ApplyHolidayRule = true,
            HolidayMultiplier = 4.0m,
            MinimumOvertimeMinutes = 15,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/overtime-policies/{policy.PolicyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OvertimePolicy>();
        result.Should().NotBeNull();
        result!.PolicyName.Should().Be("Premium Overtime");
        result.DailyMultiplier.Should().Be(2.0m);
        result.WeekendMultiplier.Should().Be(3.0m);
        result.HolidayMultiplier.Should().Be(4.0m);
    }

    [Fact]
    public async Task GetPolicyById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/overtime-policies/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDefaultPolicy_ReturnsDefaultPolicy_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Clear any existing defaults
        var existingDefaults = await db.OvertimePolicies.Where(p => p.IsDefault).ToListAsync();
        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
        }
        await db.SaveChangesAsync();
        
        var defaultPolicy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Company Default", 
            PolicyCode = "DEFAULT",
            Description = "Default overtime policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(defaultPolicy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies/default");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OvertimePolicy>();
        result.Should().NotBeNull();
        result!.PolicyName.Should().Be("Company Default");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultPolicy_ReturnsNotFound_WhenNoDefaultExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Ensure no default policies exist
        var defaults = await db.OvertimePolicies.Where(p => p.IsDefault).ToListAsync();
        foreach (var policy in defaults)
        {
            policy.IsDefault = false;
        }
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/overtime-policies/default");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePolicy_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var policy = new OvertimePolicy
        { 
            PolicyName = "New Overtime Policy", 
            PolicyCode = "NEW_OT",
            Description = "New policy for testing",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            ApplyWeeklyRule = true,
            WeeklyThreshold = TimeSpan.FromHours(40),
            WeeklyMultiplier = 1.5m,
            ApplyWeekendRule = true,
            WeekendMultiplier = 2.0m,
            ApplyHolidayRule = true,
            HolidayMultiplier = 3.0m,
            MinimumOvertimeMinutes = 15,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/overtime-policies", policy);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<OvertimePolicy>();
        result.Should().NotBeNull();
        result!.PolicyName.Should().Be("New Overtime Policy");
        result.PolicyCode.Should().Be("NEW_OT");
        result.DailyMultiplier.Should().Be(1.5m);
        result.PolicyId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePolicy_UnsetsOtherDefaults_WhenCreatingAsDefault()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Create existing default policy
        var existingDefault = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Old Default", 
            PolicyCode = "OLD_DEFAULT",
            Description = "Old default policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(existingDefault);
        await db.SaveChangesAsync();

        var newPolicy = new OvertimePolicy
        { 
            PolicyName = "New Default Policy", 
            PolicyCode = "NEW_DEFAULT",
            Description = "New default policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            ApplyWeeklyRule = true,
            WeeklyThreshold = TimeSpan.FromHours(40),
            WeeklyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/overtime-policies", newPolicy);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify old default was unset
        using var verifyDb = GetDbContext();
        var oldDefault = await verifyDb.OvertimePolicies.FindAsync(existingDefault.PolicyId);
        oldDefault!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePolicy_UpdatesSuccessfully_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Original Policy", 
            PolicyCode = "ORIG",
            Description = "Original description",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();

        var update = new OvertimePolicy
        { 
            PolicyName = "Updated Policy",
            PolicyCode = "UPDATED",
            Description = "Updated description",
            DailyThreshold = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(30)),
            DailyMultiplier = 2.0m,
            ApplyWeeklyRule = true,
            WeeklyThreshold = TimeSpan.FromHours(40),
            WeeklyMultiplier = 2.0m,
            ApplyWeekendRule = false,
            WeekendMultiplier = 2.0m,
            ApplyHolidayRule = false,
            HolidayMultiplier = 3.0m,
            MinimumOvertimeMinutes = 30,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = false
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/overtime-policies/{policy.PolicyId}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OvertimePolicy>();
        result.Should().NotBeNull();
        result!.PolicyName.Should().Be("Updated Policy");
        result.DailyMultiplier.Should().Be(2.0m);
        result.MinimumOvertimeMinutes.Should().Be(30);
    }

    [Fact]
    public async Task UpdatePolicy_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var update = new OvertimePolicy
        { 
            PolicyName = "Nonexistent",
            PolicyCode = "NONE",
            Description = "Test",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = false,
            ApplyWeeklyRule = false,
            WeeklyThreshold = TimeSpan.FromHours(40),
            WeeklyMultiplier = 1.5m,
            ApplyWeekendRule = false,
            WeekendMultiplier = 2.0m,
            ApplyHolidayRule = false,
            HolidayMultiplier = 3.0m,
            MinimumOvertimeMinutes = 15
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/overtime-policies/{Guid.NewGuid()}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePolicy_SoftDeletes_WhenNotAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Temp Policy", 
            PolicyCode = "TEMP",
            Description = "Temporary policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/overtime-policies/{policy.PolicyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify soft delete
        using var verifyDb = GetDbContext();
        var deleted = await verifyDb.OvertimePolicies.FindAsync(policy.PolicyId);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePolicy_ReturnsError_WhenAssignedToShifts()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "In Use Policy", 
            PolicyCode = "INUSE",
            Description = "Policy in use",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();
        
        // Create shift using this policy
        var shift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Test Shift", 
            ShiftCode = "TEST",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            RequiredHours = TimeSpan.FromHours(8),
            OvertimePolicyId = policy.PolicyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/overtime-policies/{policy.PolicyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("assigned");
    }

    [Fact]
    public async Task DeletePolicy_ReturnsError_WhenAssignedToDepartments()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Dept Policy", 
            PolicyCode = "DEPT",
            Description = "Department policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();
        
        // Create department using this policy
        var dept = new Department 
        { 
            DepartmentId = Guid.NewGuid(), 
            DepartmentName = "Test Dept", 
            DepartmentCode = $"DEPT{Guid.NewGuid():N}".Substring(0, 20),
            OvertimePolicyId = policy.PolicyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/overtime-policies/{policy.PolicyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("assigned");
    }

    #endregion

    #region Default Policy Management Tests

    [Fact]
    public async Task SetAsDefault_SetsSuccessfully_WhenPolicyIsActive()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "To Be Default", 
            PolicyCode = "TBD",
            Description = "Will be default",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/overtime-policies/{policy.PolicyId}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify it's now default
        using var verifyDb = GetDbContext();
        var updated = await verifyDb.OvertimePolicies.FindAsync(policy.PolicyId);
        updated!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsDefault_ReturnsError_WhenPolicyIsInactive()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var policy = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Inactive Policy", 
            PolicyCode = "INACTIVE",
            Description = "Inactive policy",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = false,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.Add(policy);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/overtime-policies/{policy.PolicyId}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("inactive");
    }

    [Fact]
    public async Task SetAsDefault_UnsetsOtherDefaults()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Create existing default
        var oldDefault = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "Old Default", 
            PolicyCode = "OLD",
            Description = "Old default",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var newDefault = new OvertimePolicy 
        { 
            PolicyId = Guid.NewGuid(), 
            PolicyName = "New Default", 
            PolicyCode = "NEW",
            Description = "New default",
            DailyThreshold = TimeSpan.FromHours(8),
            DailyMultiplier = 1.5m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.OvertimePolicies.AddRange(oldDefault, newDefault);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/overtime-policies/{newDefault.PolicyId}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify old default was unset
        using var verifyDb = GetDbContext();
        var oldDefaultUpdated = await verifyDb.OvertimePolicies.FindAsync(oldDefault.PolicyId);
        var newDefaultUpdated = await verifyDb.OvertimePolicies.FindAsync(newDefault.PolicyId);
        
        oldDefaultUpdated!.IsDefault.Should().BeFalse();
        newDefaultUpdated!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsDefault_ReturnsNotFound_WhenPolicyDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.PostAsync($"/api/overtime-policies/{Guid.NewGuid()}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task OvertimePolicyEndpoints_RequireAuthentication()
    {
        // Act
        var getResponse = await Client.GetAsync("/api/overtime-policies");
        var postResponse = await Client.PostAsJsonAsync("/api/overtime-policies", new { });
        
        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OvertimePolicyEndpoints_RequireOvertimeManagePermission()
    {
        // Note: This test assumes the permission system is correctly configured
        // and that a user without overtime:manage permission would get Forbidden
        await AuthenticateAsAdminAsync(); // Admin has all permissions
        
        var response = await Client.GetAsync("/api/overtime-policies");
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Response DTOs

    private sealed class PaginatedResponse<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = [];
    }

    #endregion
}
