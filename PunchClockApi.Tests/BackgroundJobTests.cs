using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PunchClockApi.Services;
using Xunit;

namespace PunchClockApi.Tests;

public sealed class BackgroundJobTests : IAsyncLifetime
{
    private PunchClockDbContext _db = null!;
    private Mock<IDeviceService> _mockDeviceService = null!;
    private Mock<ILogger<DeviceSyncJob>> _mockLogger = null!;
    private DeviceSyncJob _deviceSyncJob = null!;
    private AttendanceProcessingJob _attendanceProcessingJob = null!;
    private Department _testDepartment = null!;
    private Location _testLocation = null!;

    // Helper method to create dynamic object for mocking
    private static dynamic CreateDynamicResult(int recordsSynced = 0, int staffSynced = 0, bool success = true)
    {
        dynamic result = new ExpandoObject();
        result.Success = success;
        result.RecordsSynced = recordsSynced;
        result.StaffSynced = staffSynced;
        return result;
    }

    // Helper to properly return Task<dynamic> for Moq
    private static Task<dynamic> CreateDynamicResultTask(int recordsSynced = 0, int staffSynced = 0, bool success = true)
    {
        dynamic result = CreateDynamicResult(recordsSynced, staffSynced, success);
        return Task.FromResult<dynamic>(result);
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PunchClockDbContext>()
            .UseInMemoryDatabase($"BackgroundJobs_{Guid.NewGuid()}")
            .Options;

        _db = new PunchClockDbContext(options);
        _mockDeviceService = new Mock<IDeviceService>();
        _mockLogger = new Mock<ILogger<DeviceSyncJob>>();
        _deviceSyncJob = new DeviceSyncJob(_db, _mockDeviceService.Object, _mockLogger.Object);
        _attendanceProcessingJob = new AttendanceProcessingJob(_db, new AttendanceProcessingService(_db));

        // Setup test data
        _testDepartment = new Department
        {
            DepartmentId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            DepartmentCode = "ENG",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _testLocation = new Location
        {
            LocationId = Guid.NewGuid(),
            LocationName = "Main Office",
            LocationCode = "MAIN",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(_testDepartment);
        _db.Locations.Add(_testLocation);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DeviceSyncJob_SyncsAllActiveDevices()
    {
        // Arrange
        var device1 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var device2 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 2",
            DeviceSerial = "ZK002",
            IpAddress = "192.168.1.101",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var device3 = new Device // Inactive device - should not sync
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 3",
            DeviceSerial = "ZK003",
            IpAddress = "192.168.1.102",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(device1, device2, device3);
        await _db.SaveChangesAsync();

        _mockDeviceService
            .Setup(s => s.SyncAttendanceAsync(It.IsAny<Guid>()))
            .Returns((Guid deviceId) => CreateDynamicResultTask(recordsSynced: 10));

        // Act
        await _deviceSyncJob.SyncAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.SyncAttendanceAsync(device1.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.SyncAttendanceAsync(device2.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.SyncAttendanceAsync(device3.DeviceId), Times.Never);

        var syncLogs = await _db.SyncLogs
            .Where(l => l.SyncType == "ATTENDANCE")
            .ToListAsync();

        syncLogs.Should().HaveCount(2);
        syncLogs.All(l => l.Status == "SUCCESS").Should().BeTrue();
    }

    [Fact]
    public async Task DeviceSyncJob_HandlesDeviceErrors_ContinuesWithOthers()
    {
        // Arrange
        var device1 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var device2 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 2",
            DeviceSerial = "ZK002",
            IpAddress = "192.168.1.101",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(device1, device2);
        await _db.SaveChangesAsync();

        // Device 1 fails, Device 2 succeeds
        _mockDeviceService
            .Setup(s => s.SyncAttendanceAsync(device1.DeviceId))
            .ThrowsAsync(new Exception("Connection timeout"));

        _mockDeviceService
            .Setup(s => s.SyncAttendanceAsync(device2.DeviceId))
            .Returns(CreateDynamicResultTask(recordsSynced: 5));

        // Act
        await _deviceSyncJob.SyncAllDevicesAsync();

        // Assert
        var syncLogs = await _db.SyncLogs
            .Where(l => l.SyncType == "ATTENDANCE")
            .OrderBy(l => l.DeviceId)
            .ToListAsync();

        syncLogs.Should().HaveCount(2);
        
        var failedLog = syncLogs.First(l => l.DeviceId == device1.DeviceId);
        failedLog.Status.Should().Be("FAILED");
        failedLog.ErrorMessage.Should().Contain("Connection timeout");

        var successLog = syncLogs.First(l => l.DeviceId == device2.DeviceId);
        successLog.Status.Should().Be("SUCCESS");
    }

    [Fact]
    public async Task DeviceSyncJob_SkipsOfflineDevices()
    {
        // Arrange
        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = false, // Offline
            LastHeartbeatAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        // Act
        await _deviceSyncJob.SyncAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.SyncAttendanceAsync(It.IsAny<Guid>()), Times.Never);

        var syncLogs = await _db.SyncLogs.ToListAsync();
        syncLogs.Should().HaveCount(1);
        syncLogs[0].Status.Should().Be("SKIPPED");
        syncLogs[0].ErrorMessage.Should().Contain("offline");
    }

    [Fact]
    public async Task DeviceSyncJob_LogsStartAndEndTime()
    {
        // Arrange
        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        _mockDeviceService
            .Setup(s => s.SyncAttendanceAsync(device.DeviceId))
            .Returns(CreateDynamicResultTask(recordsSynced: 10));

        // Act
        var startTime = DateTime.UtcNow;
        await _deviceSyncJob.SyncAllDevicesAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        var syncLog = await _db.SyncLogs.FirstAsync();
        syncLog.StartedAt.Should().BeOnOrAfter(startTime);
        syncLog.CompletedAt.Should().NotBeNull();
        syncLog.CompletedAt!.Value.Should().BeOnOrBefore(endTime);
        syncLog.CompletedAt.Value.Should().BeOnOrAfter(syncLog.StartedAt);
    }

    [Fact]
    public async Task AttendanceProcessingJob_ProcessesYesterdayByDefault()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var staff = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            EmployeeId = "EMP001",
            BadgeNumber = "BADGE001",
            DepartmentId = _testDepartment.DepartmentId,
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(staff);
        _db.Devices.Add(device);

        // Add punches for yesterday
        _db.PunchLogs.AddRange(
            new PunchLog
            {
                LogId = Guid.NewGuid(),
                StaffId = staff.StaffId,
                DeviceId = device.DeviceId,
                PunchTime = yesterday.AddHours(9),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            },
            new PunchLog
            {
                LogId = Guid.NewGuid(),
                StaffId = staff.StaffId,
                DeviceId = device.DeviceId,
                PunchTime = yesterday.AddHours(17),
                PunchType = "OUT",
                CreatedAt = DateTime.UtcNow
            }
        );

        await _db.SaveChangesAsync();

        // Act
        await _attendanceProcessingJob.ProcessYesterdayAttendanceAsync();

        // Assert
        var yesterdayDate = DateOnly.FromDateTime(yesterday);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == staff.StaffId && r.AttendanceDate == yesterdayDate);

        record.Should().NotBeNull();
        record!.AttendanceStatus.Should().Be("PRESENT");
        record.TotalHours.Should().Be(TimeSpan.FromHours(7.5)); // 8 hours minus 30-minute break
    }

    [Fact]
    public async Task AttendanceProcessingJob_ProcessesSpecificDate()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.Date.AddDays(-3);
        var staff = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            EmployeeId = "EMP001",
            BadgeNumber = "BADGE001",
            DepartmentId = _testDepartment.DepartmentId,
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(staff);
        _db.Devices.Add(device);

        _db.PunchLogs.AddRange(
            new PunchLog
            {
                LogId = Guid.NewGuid(),
                StaffId = staff.StaffId,
                DeviceId = device.DeviceId,
                PunchTime = targetDate.AddHours(9),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            },
            new PunchLog
            {
                LogId = Guid.NewGuid(),
                StaffId = staff.StaffId,
                DeviceId = device.DeviceId,
                PunchTime = targetDate.AddHours(17),
                PunchType = "OUT",
                CreatedAt = DateTime.UtcNow
            }
        );

        await _db.SaveChangesAsync();

        // Act
        await _attendanceProcessingJob.ProcessDateAsync(targetDate);

        // Assert
        var targetDateOnly = DateOnly.FromDateTime(targetDate);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == staff.StaffId && r.AttendanceDate == targetDateOnly);

        record.Should().NotBeNull();
        record!.AttendanceStatus.Should().Be("PRESENT");
    }

    [Fact]
    public async Task AttendanceProcessingJob_ProcessesDateRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date.AddDays(-5);

        var staff = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            EmployeeId = "EMP001",
            BadgeNumber = "BADGE001",
            DepartmentId = _testDepartment.DepartmentId,
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(staff);
        _db.Devices.Add(device);

        // Add punches for 3 days
        for (int i = 0; i <= 2; i++)
        {
            var date = startDate.AddDays(i);
            _db.PunchLogs.AddRange(
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staff.StaffId,
                    DeviceId = device.DeviceId,
                    PunchTime = date.AddHours(9),
                    PunchType = "IN",
                    CreatedAt = DateTime.UtcNow
                },
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staff.StaffId,
                    DeviceId = device.DeviceId,
                    PunchTime = date.AddHours(17),
                    PunchType = "OUT",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        await _db.SaveChangesAsync();

        // Act
        await _attendanceProcessingJob.ProcessDateRangeAsync(startDate, endDate);

        // Assert
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);
        var records = await _db.AttendanceRecords
            .Where(r => r.StaffId == staff.StaffId && r.AttendanceDate >= startDateOnly && r.AttendanceDate <= endDateOnly)
            .OrderBy(r => r.AttendanceDate)
            .ToListAsync();

        records.Should().HaveCount(3);
        records.All(r => r.AttendanceStatus == "PRESENT").Should().BeTrue();
    }

    [Fact]
    public async Task AttendanceProcessingJob_HandlesEmptyPunchLogs()
    {
        // Arrange
        var staff = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            EmployeeId = "EMP001",
            BadgeNumber = "BADGE001",
            DepartmentId = _testDepartment.DepartmentId,
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        // Act
        await _attendanceProcessingJob.ProcessDateAsync(yesterday);

        // Assert
        var yesterdayDate = DateOnly.FromDateTime(yesterday);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == staff.StaffId && r.AttendanceDate == yesterdayDate);

        record.Should().NotBeNull();
        record!.AttendanceStatus.Should().Be("ABSENT");
    }

    [Fact]
    public async Task DeviceSyncJob_SupportsManualTrigger_ForSpecificDevice()
    {
        // Arrange
        var device = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 1",
            DeviceSerial = "ZK001",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        _mockDeviceService
            .Setup(s => s.SyncAttendanceAsync(device.DeviceId))
            .Returns(CreateDynamicResultTask(recordsSynced: 15));

        // Act
        await _deviceSyncJob.SyncDeviceAsync(device.DeviceId);

        // Assert
        _mockDeviceService.Verify(s => s.SyncAttendanceAsync(device.DeviceId), Times.Once);

        var syncLog = await _db.SyncLogs
            .FirstAsync(l => l.DeviceId == device.DeviceId);

        syncLog.Status.Should().Be("SUCCESS");
        syncLog.RecordsSynced.Should().Be(15);
    }
}
