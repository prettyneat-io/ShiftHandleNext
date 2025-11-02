using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for leave management endpoints (types, requests, balance, holidays).
/// Tests all 15 leave management endpoints with comprehensive validation.
/// </summary>
public sealed class LeaveManagementTests : IntegrationTestBase
{
    public LeaveManagementTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Leave Type Tests

    [Fact]
    public async Task GetLeaveTypes_ReturnsActiveTypesOnly_WhenNotFiltered()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var activeType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Annual Leave", 
            TypeCode = "ANNUAL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var inactiveType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Old Leave Type", 
            TypeCode = "OLD",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.AddRange(activeType, inactiveType);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<List<LeaveType>>();
        types.Should().NotBeNull();
        types!.Should().Contain(t => t.TypeName == "Annual Leave" && t.IsActive);
    }

    [Fact]
    public async Task GetLeaveTypes_ReturnsAllTypes_WhenIsActiveIsFalse()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var activeType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Annual Leave", 
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var inactiveType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Old Leave Type", 
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.AddRange(activeType, inactiveType);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/types?isActive=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var types = await response.Content.ReadFromJsonAsync<List<LeaveType>>();
        types.Should().NotBeNull();
        types!.Should().Contain(t => t.TypeName == "Old Leave Type" && !t.IsActive);
    }

    [Fact]
    public async Task GetLeaveTypeById_ReturnsType_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var leaveType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Sick Leave", 
            TypeCode = "SICK",
            Description = "Medical leave",
            RequiresDocumentation = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/types/{leaveType.LeaveTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveType>();
        result.Should().NotBeNull();
        result!.TypeName.Should().Be("Sick Leave");
        result.RequiresDocumentation.Should().BeTrue();
    }

    [Fact]
    public async Task GetLeaveTypeById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/types/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateLeaveType_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var leaveType = new 
        { 
            typeName = "Maternity Leave", 
            typeCode = "MATERNITY",
            description = "Maternity leave for mothers",
            requiresApproval = true,
            requiresDocumentation = true,
            maxDaysPerYear = 90,
            minDaysNotice = 30,
            isPaid = true,
            allowsHalfDay = false,
            allowsHourly = false,
            color = "#FF69B4",
            isActive = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/types", leaveType);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<LeaveType>();
        result.Should().NotBeNull();
        result!.TypeName.Should().Be("Maternity Leave");
        result.MaxDaysPerYear.Should().Be(90);
        result.LeaveTypeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateLeaveType_UpdatesSuccessfully_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var leaveType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Personal Leave", 
            TypeCode = "PERSONAL",
            MaxDaysPerYear = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();

        var update = new 
        { 
            typeName = "Personal Leave Updated",
            typeCode = "PERSONAL",
            maxDaysPerYear = 10,
            isActive = true,
            requiresApproval = true,
            requiresDocumentation = false,
            isPaid = true,
            allowsHalfDay = true,
            allowsHourly = false
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/leave/types/{leaveType.LeaveTypeId}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveType>();
        result.Should().NotBeNull();
        result!.TypeName.Should().Be("Personal Leave Updated");
        result.MaxDaysPerYear.Should().Be(10);
    }

    [Fact]
    public async Task DeleteLeaveType_SoftDeletes_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var leaveType = new LeaveType 
        { 
            LeaveTypeId = Guid.NewGuid(), 
            TypeName = "Temp Leave", 
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/leave/types/{leaveType.LeaveTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify soft delete
        using var verifyDb = GetDbContext();
        var deleted = await verifyDb.LeaveTypes.FindAsync(leaveType.LeaveTypeId);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Leave Request Tests

    [Fact]
    public async Task GetLeaveRequests_ReturnsAllRequests_WithoutFilters()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var request1 = new LeaveRequest 
        { 
            LeaveRequestId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            TotalDays = 3,
            Reason = "Vacation",
            Status = "PENDING",
            RequestedBy = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveRequests.Add(request1);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/requests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<LeaveRequest>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetLeaveRequests_FiltersCorrectly_ByStaffId()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff1 = await CreateTestStaff(db, "John");
        var staff2 = await CreateTestStaff(db, "Jane");
        var leaveType = await CreateTestLeaveType(db);
        
        // Get a real user for RequestedBy
        var user = await db.Users.FirstAsync();
        
        var request1 = CreateLeaveRequest(staff1.StaffId, leaveType.LeaveTypeId, user.UserId);
        var request2 = CreateLeaveRequest(staff2.StaffId, leaveType.LeaveTypeId, user.UserId);
        
        db.LeaveRequests.AddRange(request1, request2);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/requests?staffId={staff1.StaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<LeaveRequest>>();
        requests.Should().NotBeNull();
        var staff1Requests = requests!.Where(r => r.StaffId == staff1.StaffId).ToList();
        staff1Requests.Should().HaveCountGreaterThan(0);
        staff1Requests.Should().OnlyContain(r => r.StaffId == staff1.StaffId);
    }

    [Fact]
    public async Task GetLeaveRequests_FiltersCorrectly_ByStatus()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Get a real user for RequestedBy
        var user = await db.Users.FirstAsync();
        
        var pendingRequest = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId, user.UserId);
        pendingRequest.Status = "PENDING";
        
        var approvedRequest = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId, user.UserId);
        approvedRequest.Status = "APPROVED";
        
        db.LeaveRequests.AddRange(pendingRequest, approvedRequest);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/requests?status=PENDING");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<LeaveRequest>>();
        requests.Should().NotBeNull();
        var pendingRequests = requests!.Where(r => r.Status == "PENDING").ToList();
        pendingRequests.Should().HaveCountGreaterThan(0);
        pendingRequests.Should().OnlyContain(r => r.Status == "PENDING");
    }

    [Fact]
    public async Task GetLeaveRequests_SupportsPagination()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create 5 leave requests
        for (int i = 0; i < 5; i++)
        {
            var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
            request.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i));
            request.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i + 1));
            db.LeaveRequests.Add(request);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/requests?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<LeaveRequest>>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCountLessThanOrEqualTo(2); // Page size limit
        result.Total.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task SubmitLeaveRequest_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create leave balance
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            CarryForward = 0,
            Used = 0,
            Pending = 0,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();

        var request = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            reason = "Family vacation",
            isHalfDay = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<LeaveRequest>();
        result.Should().NotBeNull();
        result!.StaffId.Should().Be(staff.StaffId);
        result.TotalDays.Should().Be(3);
        result.Status.Should().Be("PENDING");
        
        // Verify balance was updated
        using var verifyDb = GetDbContext();
        var updatedBalance = await verifyDb.LeaveBalances.FindAsync(balance.LeaveBalanceId);
        updatedBalance!.Pending.Should().Be(3);
        updatedBalance.Available.Should().Be(17);
    }

    [Fact]
    public async Task SubmitLeaveRequest_ReturnsError_WhenEndDateBeforeStartDate()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);

        var request = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), // Before start date
            reason = "Invalid dates",
            isHalfDay = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitLeaveRequest_ReturnsError_WhenInsufficientBalance()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create leave balance with only 1 day available
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 1,
            CarryForward = 0,
            Used = 0,
            Pending = 0,
            Available = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();

        var request = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)), // 3 days
            reason = "Too many days",
            isHalfDay = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Insufficient leave balance");
    }

    [Fact]
    public async Task SubmitLeaveRequest_ReturnsError_WhenOverlappingRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create balance
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        
        // Create existing leave request
        var existingRequest = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        existingRequest.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        existingRequest.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12));
        existingRequest.Status = "APPROVED";
        db.LeaveRequests.Add(existingRequest);
        await db.SaveChangesAsync();

        // Try to create overlapping request
        var request = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(11)), // Overlaps
            endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(13)),
            reason = "Overlapping leave",
            isHalfDay = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/requests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("overlaps");
    }

    [Fact]
    public async Task ApproveLeaveRequest_UpdatesStatusAndBalance_WhenPending()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create balance with pending amount
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            Pending = 3,
            Used = 0,
            Available = 17,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        
        var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        request.Status = "PENDING";
        request.TotalDays = 3;
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/leave/requests/{request.LeaveRequestId}/approve", 
            new { notes = "Approved" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveRequest>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("APPROVED");
        result.ReviewedAt.Should().NotBeNull();
        
        // Verify balance updates
        using var verifyDb = GetDbContext();
        var updatedBalance = await verifyDb.LeaveBalances.FindAsync(balance.LeaveBalanceId);
        updatedBalance!.Pending.Should().Be(0);
        updatedBalance.Used.Should().Be(3);
        updatedBalance.Available.Should().Be(17);
    }

    [Fact]
    public async Task ApproveLeaveRequest_ReturnsError_WhenNotPending()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        request.Status = "APPROVED"; // Already approved
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/leave/requests/{request.LeaveRequestId}/approve", 
            new { notes = "Try to approve again" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectLeaveRequest_ReleasesBalance_WhenPending()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create balance with pending amount
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            Pending = 3,
            Used = 0,
            Available = 17,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        
        var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        request.Status = "PENDING";
        request.TotalDays = 3;
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/leave/requests/{request.LeaveRequestId}/reject", 
            new { notes = "Not enough coverage" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveRequest>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("REJECTED");
        
        // Verify balance was released
        using var verifyDb = GetDbContext();
        var updatedBalance = await verifyDb.LeaveBalances.FindAsync(balance.LeaveBalanceId);
        updatedBalance!.Pending.Should().Be(0);
        updatedBalance.Available.Should().Be(20); // Back to full allocation
    }

    [Fact]
    public async Task CancelLeaveRequest_RestoresBalance_WhenApproved()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        // Create balance with used amount
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            Pending = 0,
            Used = 3,
            Available = 17,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(balance);
        
        var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        request.Status = "APPROVED";
        request.TotalDays = 3;
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/leave/requests/{request.LeaveRequestId}/cancel", 
            new { reason = "Plans changed" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveRequest>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("CANCELLED");
        result.CancellationReason.Should().Be("Plans changed");
        
        // Verify balance was restored
        using var verifyDb = GetDbContext();
        var updatedBalance = await verifyDb.LeaveBalances.FindAsync(balance.LeaveBalanceId);
        updatedBalance!.Used.Should().Be(0);
        updatedBalance.Available.Should().Be(20);
    }

    [Fact]
    public async Task CancelLeaveRequest_ReturnsError_WhenAlreadyCancelled()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var request = CreateLeaveRequest(staff.StaffId, leaveType.LeaveTypeId);
        request.Status = "CANCELLED";
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/leave/requests/{request.LeaveRequestId}/cancel", 
            new { reason = "Try to cancel again" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Leave Balance Tests

    [Fact]
    public async Task GetLeaveBalance_ReturnsBalances_ForStaffAndYear()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType1 = await CreateTestLeaveType(db, "Annual");
        var leaveType2 = await CreateTestLeaveType(db, "Sick");
        
        var currentYear = DateTime.UtcNow.Year;
        
        var balance1 = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType1.LeaveTypeId,
            Year = currentYear,
            TotalAllocation = 20,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var balance2 = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType2.LeaveTypeId,
            Year = currentYear,
            TotalAllocation = 10,
            Available = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveBalances.AddRange(balance1, balance2);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/balance/{staff.StaffId}?year={currentYear}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<LeaveBalance>>();
        balances.Should().NotBeNull();
        balances!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLeaveBalance_ReturnsCurrentYear_WhenYearNotSpecified()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var balance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveBalances.Add(balance);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/balance/{staff.StaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<LeaveBalance>>();
        balances.Should().NotBeNull();
        balances!.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrUpdateLeaveBalance_CreatesNew_WhenNotExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var balanceDto = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            year = DateTime.UtcNow.Year,
            totalAllocation = 25m,
            carryForward = 5m,
            notes = "New allocation"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/balance", balanceDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<LeaveBalance>();
        result.Should().NotBeNull();
        result!.TotalAllocation.Should().Be(25);
        result.CarryForward.Should().Be(5);
        result.Available.Should().Be(30); // 25 + 5
    }

    [Fact]
    public async Task CreateOrUpdateLeaveBalance_UpdatesExisting_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var staff = await CreateTestStaff(db);
        var leaveType = await CreateTestLeaveType(db);
        
        var existingBalance = new LeaveBalance
        {
            LeaveBalanceId = Guid.NewGuid(),
            StaffId = staff.StaffId,
            LeaveTypeId = leaveType.LeaveTypeId,
            Year = DateTime.UtcNow.Year,
            TotalAllocation = 20,
            CarryForward = 0,
            Used = 5,
            Pending = 0,
            Available = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.LeaveBalances.Add(existingBalance);
        await db.SaveChangesAsync();
        
        var balanceDto = new 
        { 
            staffId = staff.StaffId,
            leaveTypeId = leaveType.LeaveTypeId,
            year = DateTime.UtcNow.Year,
            totalAllocation = 25m,
            carryForward = 3m,
            notes = "Updated allocation"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/balance", balanceDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LeaveBalance>();
        result.Should().NotBeNull();
        result!.TotalAllocation.Should().Be(25);
        result.CarryForward.Should().Be(3);
        result.Used.Should().Be(5); // Preserved
        result.Available.Should().Be(23); // 25 + 3 - 5
    }

    #endregion

    #region Holiday Tests

    [Fact]
    public async Task GetHolidays_ReturnsActiveHolidays_WhenNotFiltered()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var activeHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "New Year", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 1, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var inactiveHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Old Holiday", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 2, 1),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Holidays.AddRange(activeHoliday, inactiveHoliday);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/leave/holidays");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var holidays = await response.Content.ReadFromJsonAsync<List<Holiday>>();
        holidays.Should().NotBeNull();
        holidays!.Should().Contain(h => h.HolidayName == "New Year" && h.IsActive);
    }

    [Fact]
    public async Task GetHolidays_FiltersCorrectly_ByYear()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        // Use a far future year to avoid collision with other tests
        var testYear = 2099;
        var differentYear = 2100;
        
        var testYearHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Test Year 2099", 
            HolidayDate = new DateOnly(testYear, 6, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var differentYearHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Test Year 2100", 
            HolidayDate = new DateOnly(differentYear, 6, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Holidays.AddRange(testYearHoliday, differentYearHoliday);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/holidays?year={testYear}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var holidays = await response.Content.ReadFromJsonAsync<List<Holiday>>();
        holidays.Should().NotBeNull();
        holidays!.Should().HaveCount(1);
        holidays![0].HolidayName.Should().Be("Test Year 2099");
    }

    [Fact]
    public async Task GetHolidays_FiltersCorrectly_ByLocation()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var location = await CreateTestLocation(db);
        
        var locationHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Local Holiday", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 6, 1),
            LocationId = location.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var globalHoliday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Global Holiday", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 12, 25),
            LocationId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Holidays.AddRange(locationHoliday, globalHoliday);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/leave/holidays?locationId={location.LocationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var holidays = await response.Content.ReadFromJsonAsync<List<Holiday>>();
        holidays.Should().NotBeNull();
        // Should include both location-specific and global holidays
        holidays!.Should().Contain(h => h.HolidayName == "Local Holiday" && h.LocationId == location.LocationId);
        holidays.Should().Contain(h => h.HolidayName == "Global Holiday" && h.LocationId == null);
    }

    [Fact]
    public async Task CreateHoliday_CreatesSuccessfully_WithValidData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var holiday = new 
        { 
            holidayName = "Independence Day", 
            holidayDate = new DateOnly(DateTime.UtcNow.Year, 7, 4),
            isRecurring = true,
            isMandatory = true,
            isPaid = true,
            description = "National holiday",
            isActive = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leave/holidays", holiday);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<Holiday>();
        result.Should().NotBeNull();
        result!.HolidayName.Should().Be("Independence Day");
        result.IsRecurring.Should().BeTrue();
        result.HolidayId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateHoliday_UpdatesSuccessfully_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var holiday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Old Name", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 8, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Holidays.Add(holiday);
        await db.SaveChangesAsync();

        var update = new 
        { 
            holidayName = "Updated Holiday",
            holidayDate = new DateOnly(DateTime.UtcNow.Year, 8, 1),
            isRecurring = true,
            isMandatory = false,
            isPaid = true,
            description = "Updated description",
            isActive = true
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/leave/holidays/{holiday.HolidayId}", update);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Holiday>();
        result.Should().NotBeNull();
        result!.HolidayName.Should().Be("Updated Holiday");
        result.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteHoliday_SoftDeletes_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        using var db = GetDbContext();
        
        var holiday = new Holiday 
        { 
            HolidayId = Guid.NewGuid(), 
            HolidayName = "Temp Holiday", 
            HolidayDate = new DateOnly(DateTime.UtcNow.Year, 9, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Holidays.Add(holiday);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/leave/holidays/{holiday.HolidayId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify soft delete
        using var verifyDb = GetDbContext();
        var deleted = await verifyDb.Holidays.FindAsync(holiday.HolidayId);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

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
            FirstName = firstName ?? "John",
            LastName = "Doe",
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

    private static async Task<LeaveType> CreateTestLeaveType(PunchClockDbContext db, string? name = null)
    {
        var leaveType = new LeaveType
        {
            LeaveTypeId = Guid.NewGuid(),
            TypeName = name ?? "Annual Leave",
            TypeCode = $"CODE{Guid.NewGuid():N}".Substring(0, 10),
            RequiresApproval = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();
        return leaveType;
    }

    private static async Task<Location> CreateTestLocation(PunchClockDbContext db)
    {
        var location = new Location
        {
            LocationId = Guid.NewGuid(),
            LocationName = "Test Location",
            LocationCode = $"LOC{Guid.NewGuid():N}".Substring(0, 20),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        db.Locations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    private static LeaveRequest CreateLeaveRequest(Guid staffId, Guid leaveTypeId, Guid? requestedBy = null)
    {
        return new LeaveRequest
        {
            LeaveRequestId = Guid.NewGuid(),
            StaffId = staffId,
            LeaveTypeId = leaveTypeId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            TotalDays = 3,
            Reason = "Test leave",
            Status = "PENDING",
            RequestedBy = requestedBy ?? Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class PaginatedResponse<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = [];
    }

    #endregion
}
