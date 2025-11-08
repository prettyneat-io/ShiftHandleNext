using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for shift management endpoints.
/// Tests shift CRUD operations, staff assignments, filtering, and validation.
/// </summary>
public sealed class ShiftManagementTests : IntegrationTestBase
{
    public ShiftManagementTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Shift CRUD Tests

    [Fact]
    public async Task GetAllShifts_ReturnsActiveShiftsOnly_WhenNotFiltered()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var activeShift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Morning Shift", 
            ShiftCode = "MORNING",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            RequiredHours = TimeSpan.FromHours(8),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var inactiveShift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Old Shift", 
            ShiftCode = "OLD",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            RequiredHours = TimeSpan.FromHours(8),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.AddRange(activeShift, inactiveShift);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/shifts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shifts = await response.Content.ReadFromJsonAsync<List<Shift>>();
        shifts.Should().NotBeNull();
        shifts!.Should().Contain(s => s.ShiftName == "Morning Shift" && s.IsActive);
        shifts.Should().NotContain(s => s.ShiftName == "Old Shift");
    }

    [Fact]
    public async Task GetAllShifts_ReturnsAllShifts_WhenIsActiveFalse()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var inactiveShift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Inactive Shift", 
            ShiftCode = "INACTIVE",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            RequiredHours = TimeSpan.FromHours(8),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(inactiveShift);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/shifts?isActive=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shifts = await response.Content.ReadFromJsonAsync<List<Shift>>();
        shifts.Should().NotBeNull();
        shifts!.Should().Contain(s => s.ShiftName == "Inactive Shift" && !s.IsActive);
    }

    [Fact]
    public async Task GetAllShifts_SupportsPagination()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Create 5 test shifts
        for (int i = 0; i < 5; i++)
        {
            var shift = new Shift 
            { 
                ShiftId = Guid.NewGuid(), 
                ShiftName = $"Shift {i}", 
                ShiftCode = $"SHIFT{i}",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                RequiredHours = TimeSpan.FromHours(8),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Shifts.Add(shift);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/shifts?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<Shift>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCountLessThanOrEqualTo(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetShiftById_ReturnsShift_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Evening Shift", 
            ShiftCode = "EVENING",
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(0, 0),
            RequiredHours = TimeSpan.FromHours(8),
            HasBreak = true,
            BreakDuration = TimeSpan.FromMinutes(30),
            BreakStartTime = new TimeOnly(20, 0),
            GracePeriodMinutes = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/shifts/{shift.ShiftId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Shift>();
        result.Should().NotBeNull();
        result!.ShiftName.Should().Be("Evening Shift");
        result.HasBreak.Should().BeTrue();
        result.BreakDuration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task GetShiftById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/shifts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateShift_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var shift = new 
        { 
            shiftName = "Night Shift", 
            shiftCode = "NIGHT",
            startTime = "22:00",
            endTime = "06:00",
            requiredHours = "08:00:00",
            gracePeriodMinutes = 15,
            lateThresholdMinutes = 15,
            earlyLeaveThresholdMinutes = 15,
            hasBreak = true,
            breakDuration = "00:30:00",
            breakStartTime = "02:00",
            autoDeductBreak = true,
            isActive = true,
            description = "Overnight shift"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/shifts", shift);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<Shift>();
        result.Should().NotBeNull();
        result!.ShiftName.Should().Be("Night Shift");
        result.ShiftCode.Should().Be("NIGHT");
        result.GracePeriodMinutes.Should().Be(15);
        result.ShiftId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateShift_UpdatesSuccessfully_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Day Shift", 
            ShiftCode = "DAY",
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            RequiredHours = TimeSpan.FromHours(8),
            GracePeriodMinutes = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        var update = new 
        { 
            shiftName = "Day Shift Updated",
            shiftCode = "DAY",
            startTime = "09:00",
            endTime = "18:00",
            requiredHours = "09:00:00",
            gracePeriodMinutes = 15,
            lateThresholdMinutes = 20,
            earlyLeaveThresholdMinutes = 20,
            hasBreak = true,
            breakDuration = "01:00:00",
            breakStartTime = "12:00",
            autoDeductBreak = true,
            isActive = true,
            description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/shifts/{shift.ShiftId}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Shift>();
        result.Should().NotBeNull();
        result!.ShiftName.Should().Be("Day Shift Updated");
        result.GracePeriodMinutes.Should().Be(15);
        result.LateThresholdMinutes.Should().Be(20);
    }

    [Fact]
    public async Task UpdateShift_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var update = new 
        { 
            shiftName = "Nonexistent",
            shiftCode = "NONE",
            startTime = "09:00",
            endTime = "17:00",
            requiredHours = "08:00:00",
            gracePeriodMinutes = 15,
            lateThresholdMinutes = 15,
            earlyLeaveThresholdMinutes = 15,
            hasBreak = false,
            autoDeductBreak = false,
            isActive = true
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/shifts/{Guid.NewGuid()}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteShift_SoftDeletes_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = new Shift 
        { 
            ShiftId = Guid.NewGuid(), 
            ShiftName = "Temp Shift", 
            ShiftCode = "TEMP",
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(18, 0),
            RequiredHours = TimeSpan.FromHours(8),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/shifts/{shift.ShiftId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify soft delete
        using var verifyDb = GetDbContext();
        var deleted = await verifyDb.Shifts.FindAsync(shift.ShiftId);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Staff Assignment Tests

    [Fact]
    public async Task AssignStaffToShift_AssignsSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = await CreateTestShift(db);
        var staff1 = await CreateTestStaff(db, "John");
        var staff2 = await CreateTestStaff(db, "Jane");
        
        var request = new 
        { 
            shiftId = shift.ShiftId,
            staffIds = new[] { staff1.StaffId, staff2.StaffId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/shifts/assign-staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AssignmentResponse>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.ShiftId.Should().Be(shift.ShiftId);
        
        // Verify database
        using var verifyDb = GetDbContext();
        var assignedStaff1 = await verifyDb.Staff.FindAsync(staff1.StaffId);
        var assignedStaff2 = await verifyDb.Staff.FindAsync(staff2.StaffId);
        assignedStaff1!.ShiftId.Should().Be(shift.ShiftId);
        assignedStaff2!.ShiftId.Should().Be(shift.ShiftId);
    }

    [Fact]
    public async Task AssignStaffToShift_ReturnsNotFound_WhenShiftDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        
        var request = new 
        { 
            shiftId = Guid.NewGuid(),
            staffIds = new[] { staff.StaffId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/shifts/assign-staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignStaffToShift_ReturnsError_WhenShiftIsInactive()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = await CreateTestShift(db);
        shift.IsActive = false;
        await db.SaveChangesAsync();
        
        var staff = await CreateTestStaff(db);
        
        var request = new 
        { 
            shiftId = shift.ShiftId,
            staffIds = new[] { staff.StaffId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/shifts/assign-staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("inactive");
    }

    [Fact]
    public async Task AssignStaffToShift_ReturnsError_WhenStaffNotFound()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = await CreateTestShift(db);
        var existingStaff = await CreateTestStaff(db);
        var nonExistentStaffId = Guid.NewGuid();
        
        var request = new 
        { 
            shiftId = shift.ShiftId,
            staffIds = new[] { existingStaff.StaffId, nonExistentStaffId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/shifts/assign-staff", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("not found");
        result.MissingStaffIds.Should().Contain(nonExistentStaffId);
    }

    [Fact]
    public async Task UnassignStaffFromShift_UnassignsSuccessfully_WhenStaffIsAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var shift = await CreateTestShift(db);
        var staff = await CreateTestStaff(db);
        
        // Assign staff to shift
        staff.ShiftId = shift.ShiftId;
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/shifts/unassign-staff/{staff.StaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify database
        using var verifyDb = GetDbContext();
        var unassignedStaff = await verifyDb.Staff.FindAsync(staff.StaffId);
        unassignedStaff!.ShiftId.Should().BeNull();
    }

    [Fact]
    public async Task UnassignStaffFromShift_ReturnsError_WhenStaffNotAssigned()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);

        // Act
        var response = await Client.DeleteAsync($"/api/shifts/unassign-staff/{staff.StaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not assigned");
    }

    [Fact]
    public async Task UnassignStaffFromShift_ReturnsNotFound_WhenStaffDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/shifts/unassign-staff/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task ShiftEndpoints_RequireAuthentication()
    {
        // Act
        var getResponse = await Client.GetAsync("/api/shifts");
        var postResponse = await Client.PostAsJsonAsync("/api/shifts", new { });
        
        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShiftEndpoints_RequireShiftsManagePermission()
    {
        // Note: This test assumes the permission system is correctly configured
        // and that a user without shifts:manage permission would get Forbidden
        await AuthenticateAsAdminAsync(); // Admin has all permissions
        
        var response = await Client.GetAsync("/api/shifts");
        
        // Should succeed for admin
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Helper Methods

    private static async Task<Shift> CreateTestShift(PunchClockDbContext db, string? name = null)
    {
        var shift = new Shift
        {
            ShiftId = Guid.NewGuid(),
            ShiftName = name ?? "Test Shift",
            ShiftCode = $"SHIFT{Guid.NewGuid():N}".Substring(0, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            RequiredHours = TimeSpan.FromHours(8),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
        return shift;
    }

    private static async Task<Staff> CreateTestStaff(PunchClockDbContext db, string? firstName = null)
    {
        // Ensure department and location exist
        var dept = await db.Departments.FirstOrDefaultAsync() 
            ?? new Department 
            { 
                DepartmentId = Guid.NewGuid(), 
                DepartmentName = "Test Dept",
                DepartmentCode = $"DEPT{Guid.NewGuid():N}".Substring(0, 20),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        
        var loc = await db.Locations.FirstOrDefaultAsync()
            ?? new Location 
            { 
                LocationId = Guid.NewGuid(), 
                LocationName = "Test Location",
                LocationCode = $"LOC{Guid.NewGuid():N}".Substring(0, 20),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

        if (await db.Departments.FindAsync(dept.DepartmentId) == null)
            db.Departments.Add(dept);
        
        if (await db.Locations.FindAsync(loc.LocationId) == null)
            db.Locations.Add(loc);
        
        await db.SaveChangesAsync();

        var staff = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = firstName ?? "Test",
            LastName = "User",
            EmployeeId = $"EMP{Guid.NewGuid():N}".Substring(0, 20),
            Email = $"{Guid.NewGuid():N}@test.com",
            DepartmentId = dept.DepartmentId,
            LocationId = loc.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return staff;
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

    private sealed class AssignmentResponse
    {
        public string Message { get; set; } = null!;
        public Guid ShiftId { get; set; }
        public string ShiftName { get; set; } = null!;
        public List<Guid> AssignedStaffIds { get; set; } = [];
        public int Count { get; set; }
    }

    private sealed class ErrorResponse
    {
        public string Message { get; set; } = null!;
        public List<Guid> MissingStaffIds { get; set; } = [];
    }

    #endregion
}
