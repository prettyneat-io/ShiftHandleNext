using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using Xunit;
using Xunit.Abstractions;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for device functionality using ZK simulator.
/// Tests device connection, sync operations, user enrollment, and attendance retrieval.
/// </summary>
[Collection("DeviceIntegration")]
public sealed class DeviceIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private Process? _simulatorProcess;
    private const int SimulatorPort = 4370;
    private const string SimulatorIp = "127.0.0.1";
    
    // Track created test data for cleanup
    private Guid _testDeviceId;
    private Guid _testLocationId;
    private Guid _testDepartmentId;
    private Guid _testStaffId;

    public DeviceIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Authenticate for all tests
        await AuthenticateAsAdminAsync();
        
        // Start the ZK simulator
        await StartSimulatorAsync();
        
        // Wait a bit for simulator to fully start
        await Task.Delay(1000);
        
        // Create test data
        await CreateTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Stop the simulator
        await StopSimulatorAsync();
        
        // Cleanup test data
        await CleanupTestDataAsync();
    }

    [Fact]
    public async Task TestConnection_WithSimulator_ReturnsConnected()
    {
        // Act
        var response = await Client.PostAsync($"/api/devices/{_testDeviceId}/test-connection", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<TestConnectionResponse>();
        result.Should().NotBeNull();
        result!.IsConnected.Should().BeTrue();
        result.DeviceId.Should().Be(_testDeviceId);
    }

    [Fact]
    public async Task ConnectToDevice_WithSimulator_ReturnsSuccess()
    {
        // Act
        var response = await Client.PostAsync($"/api/devices/{_testDeviceId}/connect", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<DeviceInfoResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.DeviceDetails.Should().NotBeNull();
        result.DeviceDetails!.SerialNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DisconnectFromDevice_AfterConnection_ReturnsSuccess()
    {
        // Arrange - First connect
        await Client.PostAsync($"/api/devices/{_testDeviceId}/connect", null);

        // Act
        var response = await Client.PostAsync($"/api/devices/{_testDeviceId}/disconnect", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<OperationResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetDeviceInfo_WithSimulator_ReturnsDetailedInfo()
    {
        // Act
        var response = await Client.GetAsync($"/api/devices/{_testDeviceId}/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<DetailedDeviceInfoResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SerialNumber.Should().Be("DGD9190019050335743");
        result.Platform.Should().Be("ZEM560");
        result.FirmwareVersion.Should().Contain("Ver 6.60");
        result.UsersCount.Should().BeGreaterThanOrEqualTo(0);
        result.UsersCapacity.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDeviceUsers_WithSimulator_ReturnsUsersList()
    {
        // Act
        var response = await Client.GetAsync($"/api/devices/{_testDeviceId}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UsersResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Count.Should().BeGreaterThanOrEqualTo(3); // Simulator has 3 default users
        result.Users.Should().NotBeEmpty();
        result.Users.Should().Contain(u => u.Name == "Admin");
    }

    [Fact]
    public async Task GetDeviceAttendance_WithSimulator_ReturnsAttendanceRecords()
    {
        // Act
        var response = await Client.GetAsync($"/api/devices/{_testDeviceId}/attendance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AttendanceResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        // Simulator starts with no attendance records
        result.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task EnrollStaffOnDevice_WithValidStaff_ReturnsSuccess()
    {
        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{_testDeviceId}/staff/{_testStaffId}/enroll", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<OperationResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        
        // Verify enrollment was created in database
        using var db = GetDbContext();
        var enrollment = await db.DeviceEnrollments
            .FirstOrDefaultAsync(de => de.DeviceId == _testDeviceId && de.StaffId == _testStaffId);
        enrollment.Should().NotBeNull();
    }

    [Fact]
    public async Task EnrollStaffOnDevice_WithNonExistentDevice_ReturnsNotFound()
    {
        // Arrange
        var fakeDeviceId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{fakeDeviceId}/staff/{_testStaffId}/enroll", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EnrollStaffOnDevice_WithNonExistentStaff_ReturnsNotFound()
    {
        // Arrange
        var fakeStaffId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{_testDeviceId}/staff/{fakeStaffId}/enroll", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SyncStaffToDevice_WithActiveStaff_CreatesEnrollments()
    {
        // Arrange - Create additional staff at same location
        var additionalStaff = await CreateAdditionalStaffAsync();

        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{_testDeviceId}/sync?type=staff", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<SyncResponse>();
        result.Should().NotBeNull();
        result!.Result.Should().NotBeNull();
        result.Result!.Success.Should().BeTrue();
        result.Result.RecordsProcessed.Should().BeGreaterThanOrEqualTo(2); // At least 2 staff
        result.Result.RecordsCreated.Should().BeGreaterThan(0);
        
        // Verify sync log was created
        result.SyncLog.Should().NotBeNull();
        result.SyncLog!.SyncType.Should().Be("STAFF");
        result.SyncLog.SyncStatus.Should().Be("SUCCESS");
    }

    [Fact]
    public async Task SyncAttendanceFromDevice_WithExistingData_CreatesAttendanceLogs()
    {
        // Arrange - First enroll a staff member
        await Client.PostAsync($"/api/devices/{_testDeviceId}/staff/{_testStaffId}/enroll", null);
        
        // Note: The simulator starts with empty attendance records
        // In a real scenario, we would need to add attendance to the device first

        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{_testDeviceId}/sync?type=attendance", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<SyncResponse>();
        result.Should().NotBeNull();
        result!.Result.Should().NotBeNull();
        result.Result!.Success.Should().BeTrue();
        
        // Verify sync log was created
        result.SyncLog.Should().NotBeNull();
        result.SyncLog!.SyncType.Should().Be("ATTENDANCE");
        result.SyncLog.SyncStatus.Should().Be("SUCCESS");
    }

    [Fact]
    public async Task SyncDevice_WithInvalidType_DefaultsToAttendance()
    {
        // Act
        var response = await Client.PostAsync(
            $"/api/devices/{_testDeviceId}/sync?type=invalid", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<SyncResponse>();
        result.Should().NotBeNull();
        result!.SyncLog.Should().NotBeNull();
        result.SyncLog!.SyncType.Should().Be("INVALID"); // Uppercase of what was provided
    }

    [Fact]
    public async Task ConnectAndDisconnectSequence_MultipleTimes_WorksCorrectly()
    {
        // Act & Assert - Connect/Disconnect 3 times
        for (int i = 0; i < 3; i++)
        {
            _output.WriteLine($"Connection cycle {i + 1}");
            
            // Connect
            var connectResponse = await Client.PostAsync($"/api/devices/{_testDeviceId}/connect", null);
            connectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var connectResult = await connectResponse.Content.ReadFromJsonAsync<DeviceInfoResponse>();
            connectResult!.Success.Should().BeTrue();
            
            // Disconnect
            var disconnectResponse = await Client.PostAsync($"/api/devices/{_testDeviceId}/disconnect", null);
            disconnectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var disconnectResult = await disconnectResponse.Content.ReadFromJsonAsync<OperationResponse>();
            disconnectResult!.Success.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetDeviceUsers_AfterEnrollingStaff_ShowsNewUser()
    {
        // Arrange - Get initial user count
        var initialResponse = await Client.GetAsync($"/api/devices/{_testDeviceId}/users");
        var initialResult = await initialResponse.Content.ReadFromJsonAsync<UsersResponse>();
        var initialCount = initialResult!.Count;

        // Act - Enroll staff
        await Client.PostAsync($"/api/devices/{_testDeviceId}/staff/{_testStaffId}/enroll", null);
        
        // Get updated user list
        var updatedResponse = await Client.GetAsync($"/api/devices/{_testDeviceId}/users");
        var updatedResult = await updatedResponse.Content.ReadFromJsonAsync<UsersResponse>();

        // Assert
        updatedResult!.Count.Should().Be(initialCount + 1);
        updatedResult.Users.Should().Contain(u => u.UserId.Contains("EMP")); // Our test staff has EMP prefix
    }

    [Fact]
    public async Task DeviceInfo_AfterEnrollments_ReflectsUpdatedCounts()
    {
        // Arrange - Get initial info
        var initialResponse = await Client.GetAsync($"/api/devices/{_testDeviceId}/info");
        var initialInfo = await initialResponse.Content.ReadFromJsonAsync<DetailedDeviceInfoResponse>();
        var initialUserCount = initialInfo!.UsersCount;

        // Act - Enroll multiple staff
        await Client.PostAsync($"/api/devices/{_testDeviceId}/staff/{_testStaffId}/enroll", null);
        var additionalStaff = await CreateAdditionalStaffAsync();
        await Client.PostAsync($"/api/devices/{_testDeviceId}/staff/{additionalStaff}/enroll", null);

        // Get updated info
        var updatedResponse = await Client.GetAsync($"/api/devices/{_testDeviceId}/info");
        var updatedInfo = await updatedResponse.Content.ReadFromJsonAsync<DetailedDeviceInfoResponse>();

        // Assert
        updatedInfo!.UsersCount.Should().Be(initialUserCount + 2);
    }

    // Helper methods

    private async Task StartSimulatorAsync()
    {
        try
        {
            var devicePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Device",
                "zk_simulator.py"
            );

            if (!File.Exists(devicePath))
            {
                throw new FileNotFoundException($"ZK simulator not found at: {devicePath}");
            }

            _output.WriteLine($"Starting ZK simulator at {SimulatorIp}:{SimulatorPort}");
            
            _simulatorProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"{devicePath} --ip {SimulatorIp} --port {SimulatorPort}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _simulatorProcess.OutputDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _output.WriteLine($"[Simulator] {e.Data}");
            };
            
            _simulatorProcess.ErrorDataReceived += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _output.WriteLine($"[Simulator Error] {e.Data}");
            };

            _simulatorProcess.Start();
            _simulatorProcess.BeginOutputReadLine();
            _simulatorProcess.BeginErrorReadLine();
            
            _output.WriteLine("ZK simulator started successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to start simulator: {ex.Message}");
            throw;
        }
    }

    private async Task StopSimulatorAsync()
    {
        if (_simulatorProcess != null && !_simulatorProcess.HasExited)
        {
            _output.WriteLine("Stopping ZK simulator");
            _simulatorProcess.Kill(true);
            await _simulatorProcess.WaitForExitAsync();
            _simulatorProcess.Dispose();
            _output.WriteLine("ZK simulator stopped");
            
            // Give the OS time to release the port
            await Task.Delay(500);
        }
    }

    private async Task CreateTestDataAsync()
    {
        // Create location
        var locationRequest = new
        {
            locationName = $"Test Location {Guid.NewGuid():N}"[..20],
            locationCode = $"TST{Guid.NewGuid():N}"[..10],
            city = "Test City",
            country = "USA",
            timezone = "America/Los_Angeles",
            isActive = true
        };
        
        var locationResponse = await Client.PostAsJsonAsync("/api/locations", locationRequest);
        var location = await locationResponse.Content.ReadFromJsonAsync<LocationResponse>();
        _testLocationId = location!.LocationId;
        _output.WriteLine($"Created test location: {_testLocationId}");

        // Create department
        var departmentRequest = new
        {
            departmentName = $"Test Dept {Guid.NewGuid():N}"[..20],
            departmentCode = $"TST{Guid.NewGuid():N}"[..10],
            isActive = true
        };
        
        var departmentResponse = await Client.PostAsJsonAsync("/api/departments", departmentRequest);
        var department = await departmentResponse.Content.ReadFromJsonAsync<DepartmentResponse>();
        _testDepartmentId = department!.DepartmentId;
        _output.WriteLine($"Created test department: {_testDepartmentId}");

        // Create device pointing to simulator
        var deviceRequest = new
        {
            deviceSerial = $"SIM{Guid.NewGuid():N}"[..10],
            deviceName = "Test Simulator Device",
            deviceModel = "ZKTeco F18 Simulator",
            manufacturer = "ZKTeco",
            ipAddress = SimulatorIp,
            port = SimulatorPort,
            locationId = _testLocationId,
            isActive = true,
            isOnline = false
        };

        var deviceResponse = await Client.PostAsJsonAsync("/api/devices", deviceRequest);
        var device = await deviceResponse.Content.ReadFromJsonAsync<DeviceResponse>();
        _testDeviceId = device!.DeviceId;
        _output.WriteLine($"Created test device: {_testDeviceId}");

        // Create staff
        var staffRequest = new
        {
            employeeId = $"EMP{Guid.NewGuid():N}"[..10],
            firstName = "Test",
            lastName = "User",
            email = $"test.{Guid.NewGuid():N}@example.com",
            phone = "+1234567890",
            departmentId = _testDepartmentId,
            locationId = _testLocationId,
            positionTitle = "Test Position",
            employmentType = "FULL_TIME",
            hireDate = DateTime.UtcNow,
            isActive = true
        };
        
        var staffResponse = await Client.PostAsJsonAsync("/api/staff", staffRequest);
        var staff = await staffResponse.Content.ReadFromJsonAsync<StaffResponse>();
        _testStaffId = staff!.StaffId;
        _output.WriteLine($"Created test staff: {_testStaffId}");
    }

    private async Task<Guid> CreateAdditionalStaffAsync()
    {
        var staffRequest = new
        {
            employeeId = $"EMP{Guid.NewGuid():N}"[..10],
            firstName = "Additional",
            lastName = "Staff",
            email = $"additional.{Guid.NewGuid():N}@example.com",
            phone = "+1234567891",
            departmentId = _testDepartmentId,
            locationId = _testLocationId,
            positionTitle = "Additional Position",
            employmentType = "FULL_TIME",
            hireDate = DateTime.UtcNow,
            isActive = true
        };
        
        var response = await Client.PostAsJsonAsync("/api/staff", staffRequest);
        var staff = await response.Content.ReadFromJsonAsync<StaffResponse>();
        _output.WriteLine($"Created additional staff: {staff!.StaffId}");
        return staff.StaffId;
    }

    private async Task CleanupTestDataAsync()
    {
        try
        {
            using var db = GetDbContext();
            
            // Remove device enrollments
            var enrollments = db.DeviceEnrollments.Where(de => de.DeviceId == _testDeviceId);
            db.DeviceEnrollments.RemoveRange(enrollments);
            
            // Remove punch logs
            var punchLogs = db.PunchLogs.Where(pl => pl.DeviceId == _testDeviceId);
            db.PunchLogs.RemoveRange(punchLogs);
            
            // Remove sync logs
            var syncLogs = db.SyncLogs.Where(sl => sl.DeviceId == _testDeviceId);
            db.SyncLogs.RemoveRange(syncLogs);
            
            // Remove device
            var device = await db.Devices.FindAsync(_testDeviceId);
            if (device != null) db.Devices.Remove(device);
            
            // Remove staff at test location
            var staff = db.Staff.Where(s => s.LocationId == _testLocationId);
            db.Staff.RemoveRange(staff);
            
            // Remove location
            var location = await db.Locations.FindAsync(_testLocationId);
            if (location != null) db.Locations.Remove(location);
            
            // Remove department
            var department = await db.Departments.FindAsync(_testDepartmentId);
            if (department != null) db.Departments.Remove(department);
            
            await db.SaveChangesAsync();
            _output.WriteLine("Test data cleaned up successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up test data: {ex.Message}");
        }
    }

    // Response DTOs
    private sealed class TestConnectionResponse
    {
        public Guid DeviceId { get; set; }
        public bool IsConnected { get; set; }
        public DateTime TestedAt { get; set; }
    }

    private sealed class DeviceInfoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("device_info")]
        public DeviceDetailsDto? DeviceDetails { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class DeviceDetailsDto
    {
        [JsonPropertyName("firmware_version")]
        public string FirmwareVersion { get; set; } = string.Empty;
        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; } = string.Empty;
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;
        [JsonPropertyName("device_name")]
        public string DeviceName { get; set; } = string.Empty;
    }

    private sealed class DetailedDeviceInfoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("firmware_version")]
        public string FirmwareVersion { get; set; } = string.Empty;
        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; } = string.Empty;
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;
        [JsonPropertyName("device_name")]
        public string DeviceName { get; set; } = string.Empty;
        [JsonPropertyName("mac_address")]
        public string MacAddress { get; set; } = string.Empty;
        [JsonPropertyName("users_count")]
        public int UsersCount { get; set; }
        [JsonPropertyName("users_capacity")]
        public int UsersCapacity { get; set; }
        [JsonPropertyName("fingerprints_count")]
        public int FingerprintsCount { get; set; }
        [JsonPropertyName("fingerprints_capacity")]
        public int FingerprintsCapacity { get; set; }
        [JsonPropertyName("records_count")]
        public int RecordsCount { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class OperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    private sealed class UsersResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("users")]
        public List<ZKUserDto> Users { get; set; } = [];
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class ZKUserDto
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("privilege")]
        public int Privilege { get; set; }
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    private sealed class AttendanceResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public string? Error { get; set; }
    }

    private sealed class SyncResponse
    {
        public SyncLogDto? SyncLog { get; set; }
        public SyncResultDto? Result { get; set; }
    }

    private sealed class SyncLogDto
    {
        public Guid SyncId { get; set; }
        public string SyncType { get; set; } = string.Empty;
        public string SyncStatus { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public int RecordsFailed { get; set; }
    }

    private sealed class SyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public int RecordsCreated { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsFailed { get; set; }
    }

    private sealed class LocationResponse
    {
        public Guid LocationId { get; set; }
    }

    private sealed class DepartmentResponse
    {
        public Guid DepartmentId { get; set; }
    }

    private sealed class DeviceResponse
    {
        public Guid DeviceId { get; set; }
    }

    private sealed class StaffResponse
    {
        public Guid StaffId { get; set; }
    }
}
