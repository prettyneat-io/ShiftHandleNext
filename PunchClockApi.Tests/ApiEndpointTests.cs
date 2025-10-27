using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for main API endpoints (CRUD operations).
/// Migrated from test-api.sh
/// </summary>
public sealed class ApiEndpointTests : IntegrationTestBase
{
    public ApiEndpointTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task CreateDepartment_WithValidData_ReturnsDepartment()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var departmentRequest = new
        {
            departmentName = "Engineering",
            departmentCode = $"ENG{Guid.NewGuid():N}"[..10],
            isActive = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/departments", departmentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var department = await response.Content.ReadFromJsonAsync<DepartmentResponse>();
        department.Should().NotBeNull();
        department!.DepartmentId.Should().NotBeEmpty();
        department.DepartmentName.Should().Be(departmentRequest.departmentName);
        department.DepartmentCode.Should().Be(departmentRequest.departmentCode);
        department.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLocation_WithValidData_ReturnsLocation()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var locationRequest = new
        {
            locationName = "Main Office",
            locationCode = $"HQ{Guid.NewGuid():N}"[..10],
            city = "San Francisco",
            country = "USA",
            timezone = "America/Los_Angeles",
            isActive = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/locations", locationRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>();
        location.Should().NotBeNull();
        location!.LocationId.Should().NotBeEmpty();
        location.LocationName.Should().Be(locationRequest.locationName);
        location.City.Should().Be(locationRequest.city);
        location.Country.Should().Be(locationRequest.country);
    }

    [Fact]
    public async Task CreateStaff_WithValidData_ReturnsStaff()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        // Create department and location first
        var department = await CreateTestDepartmentAsync();
        var location = await CreateTestLocationAsync();

        var staffRequest = new
        {
            employeeId = $"EMP{Guid.NewGuid():N}"[..10],
            firstName = "John",
            lastName = "Doe",
            email = $"john.doe.{Guid.NewGuid():N}@example.com",
            phone = "+1234567890",
            departmentId = department.DepartmentId,
            locationId = location.LocationId,
            positionTitle = "Software Engineer",
            employmentType = "FULL_TIME",
            hireDate = DateTime.UtcNow,
            isActive = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/staff", staffRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var staff = await response.Content.ReadFromJsonAsync<StaffResponse>();
        staff.Should().NotBeNull();
        staff!.StaffId.Should().NotBeEmpty();
        staff.FirstName.Should().Be(staffRequest.firstName);
        staff.LastName.Should().Be(staffRequest.lastName);
        staff.Email.Should().Be(staffRequest.email);
    }

    [Fact]
    public async Task GetAllStaff_ReturnsStaffList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStaffById_ExistingStaff_ReturnsStaff()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        // Create a staff member first
        var department = await CreateTestDepartmentAsync();
        var location = await CreateTestLocationAsync();
        var createdStaff = await CreateTestStaffAsync(department.DepartmentId, location.LocationId);

        // Act
        var response = await Client.GetAsync($"/api/staff/{createdStaff.StaffId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<StaffResponse>();
        staff.Should().NotBeNull();
        staff!.StaffId.Should().Be(createdStaff.StaffId);
    }

    [Fact]
    public async Task CreateDevice_WithValidData_ReturnsDevice()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        var location = await CreateTestLocationAsync();

        var deviceRequest = new
        {
            deviceSerial = $"ZK{Guid.NewGuid():N}"[..10],
            deviceName = "Main Entrance",
            deviceModel = "ZKTeco F18",
            manufacturer = "ZKTeco",
            ipAddress = "192.168.1.100",
            port = 4370,
            locationId = location.LocationId,
            isActive = true,
            isOnline = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/devices", deviceRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var device = await response.Content.ReadFromJsonAsync<DeviceResponse>();
        device.Should().NotBeNull();
        device!.DeviceId.Should().NotBeEmpty();
        device.DeviceName.Should().Be(deviceRequest.deviceName);
        device.DeviceSerial.Should().Be(deviceRequest.deviceSerial);
    }

    [Fact]
    public async Task GetAllDevices_ReturnsDeviceList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceResponse>>();
        devices.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePunchLog_WithValidData_ReturnsPunchLog()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        
        // Create prerequisites
        var department = await CreateTestDepartmentAsync();
        var location = await CreateTestLocationAsync();
        var staff = await CreateTestStaffAsync(department.DepartmentId, location.LocationId);
        var device = await CreateTestDeviceAsync(location.LocationId);

        var punchLogRequest = new
        {
            staffId = staff.StaffId,
            deviceId = device.DeviceId,
            punchTime = DateTime.UtcNow,
            punchType = "IN",
            verificationMode = "FINGERPRINT",
            isValid = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/attendance/logs", punchLogRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var punchLog = await response.Content.ReadFromJsonAsync<PunchLogResponse>();
        punchLog.Should().NotBeNull();
        punchLog!.PunchLogId.Should().NotBeEmpty();
        punchLog.PunchType.Should().Be(punchLogRequest.punchType);
    }

    [Fact]
    public async Task GetAttendanceLogs_ReturnsLogsList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/attendance/logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllDepartments_ReturnsDepartmentsList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/departments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>();
        departments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllLocations_ReturnsLocationsList()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var locations = await response.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locations.Should().NotBeNull();
    }

    // Helper methods to create test data
    private async Task<DepartmentResponse> CreateTestDepartmentAsync()
    {
        var request = new
        {
            departmentName = $"Test Dept {Guid.NewGuid():N}"[..20],
            departmentCode = $"TD{Guid.NewGuid():N}"[..10],
            isActive = true
        };
        
        var response = await Client.PostAsJsonAsync("/api/departments", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DepartmentResponse>() 
            ?? throw new InvalidOperationException("Failed to create test department");
    }

    private async Task<LocationResponse> CreateTestLocationAsync()
    {
        var request = new
        {
            locationName = $"Test Location {Guid.NewGuid():N}"[..20],
            locationCode = $"TL{Guid.NewGuid():N}"[..10],
            city = "Test City",
            country = "USA",
            timezone = "America/Los_Angeles",
            isActive = true
        };
        
        var response = await Client.PostAsJsonAsync("/api/locations", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<LocationResponse>() 
            ?? throw new InvalidOperationException("Failed to create test location");
    }

    private async Task<StaffResponse> CreateTestStaffAsync(Guid departmentId, Guid locationId)
    {
        var request = new
        {
            employeeId = $"EMP{Guid.NewGuid():N}"[..10],
            firstName = "Test",
            lastName = "User",
            email = $"test.{Guid.NewGuid():N}@example.com",
            phone = "+1234567890",
            departmentId,
            locationId,
            positionTitle = "Test Position",
            employmentType = "FULL_TIME",
            hireDate = DateTime.UtcNow,
            isActive = true
        };
        
        var response = await Client.PostAsJsonAsync("/api/staff", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<StaffResponse>() 
            ?? throw new InvalidOperationException("Failed to create test staff");
    }

    private async Task<DeviceResponse> CreateTestDeviceAsync(Guid locationId)
    {
        var request = new
        {
            deviceSerial = $"ZK{Guid.NewGuid():N}"[..10],
            deviceName = $"Test Device {Guid.NewGuid():N}"[..20],
            deviceModel = "ZKTeco F18",
            manufacturer = "ZKTeco",
            ipAddress = "192.168.1.100",
            port = 4370,
            locationId,
            isActive = true,
            isOnline = true
        };
        
        var response = await Client.PostAsJsonAsync("/api/devices", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DeviceResponse>() 
            ?? throw new InvalidOperationException("Failed to create test device");
    }

    // Response DTOs
    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private sealed class DepartmentResponse
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class LocationResponse
    {
        public Guid LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
    }

    private sealed class StaffResponse
    {
        public Guid StaffId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class DeviceResponse
    {
        public Guid DeviceId { get; set; }
        public string DeviceSerial { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
    }

    private sealed class PunchLogResponse
    {
        public Guid PunchLogId { get; set; }
        public string PunchType { get; set; } = string.Empty;
        public DateTime PunchTime { get; set; }
    }
}
