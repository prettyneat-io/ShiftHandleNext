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

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_RemovesFromAllActiveDevices()
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
        var device3 = new Device // Inactive device - should not be processed
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

        // Mock successful removal with different counts
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device1.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 3,
                Message = "Removed 3 inactive staff"
            });

        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device2.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 2,
                Message = "Removed 2 inactive staff"
            });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device1.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device2.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device3.DeviceId), Times.Never);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_SkipsOfflineDevices()
    {
        // Arrange
        var onlineDevice = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Online Device",
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
        var offlineDevice = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Offline Device",
            DeviceSerial = "ZK002",
            IpAddress = "192.168.1.101",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = false, // Device is offline
            LastHeartbeatAt = DateTime.UtcNow.AddHours(-3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(onlineDevice, offlineDevice);
        await _db.SaveChangesAsync();

        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(onlineDevice.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 5,
                Message = "Removed 5 inactive staff"
            });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(onlineDevice.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(offlineDevice.DeviceId), Times.Never);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_HandlesDeviceErrors_ContinuesWithOthers()
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
        var device3 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 3",
            DeviceSerial = "ZK003",
            IpAddress = "192.168.1.102",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(device1, device2, device3);
        await _db.SaveChangesAsync();

        // Device 1 succeeds
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device1.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 2,
                Message = "Removed 2 inactive staff"
            });

        // Device 2 fails
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device2.DeviceId))
            .ThrowsAsync(new Exception("Connection timeout"));

        // Device 3 succeeds
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device3.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 1,
                Message = "Removed 1 inactive staff"
            });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert - All devices should be attempted despite failure
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device1.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device2.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device3.DeviceId), Times.Once);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_HandlesFailureResult()
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

        // Mock a failed result (Success = false)
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device.DeviceId))
            .ReturnsAsync(new SyncResult
            {
                Success = false,
                RecordsDeleted = 0,
                Message = "Failed to connect to device",
                Errors = ["Connection refused"]
            });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(device.DeviceId), Times.Once);
        
        // Verify logging occurred (check mock was called)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed 0 inactive staff")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_HandlesNoDevices()
    {
        // Arrange - No devices in database

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(It.IsAny<Guid>()), Times.Never);
        
        // Verify that the job started and completed logs were written
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting inactive staff removal job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_LogsCorrectTotals()
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
        var device3 = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Device 3",
            DeviceSerial = "ZK003",
            IpAddress = "192.168.1.102",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(device1, device2, device3);
        await _db.SaveChangesAsync();

        // Setup results: 2 successful (3 + 5 removed), 1 failed
        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device1.DeviceId))
            .ReturnsAsync(new SyncResult { Success = true, RecordsDeleted = 3 });

        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device2.DeviceId))
            .ReturnsAsync(new SyncResult { Success = false, RecordsDeleted = 0 });

        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(device3.DeviceId))
            .ReturnsAsync(new SyncResult { Success = true, RecordsDeleted = 5 });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert - Check final summary log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("8 staff removed") && v.ToString()!.Contains("1 devices failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveInactiveStaffFromAllDevices_ProcessesOnlyActiveDevicesFromMultipleLocations()
    {
        // Arrange
        var location2 = new Location
        {
            LocationId = Guid.NewGuid(),
            LocationName = "Branch Office",
            LocationCode = "BRANCH",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Locations.Add(location2);

        var deviceMainOffice = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Main Office Device",
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
        var deviceBranchOffice = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Branch Office Device",
            DeviceSerial = "ZK002",
            IpAddress = "192.168.2.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = location2.LocationId,
            IsActive = true,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Devices.AddRange(deviceMainOffice, deviceBranchOffice);
        await _db.SaveChangesAsync();

        _mockDeviceService
            .Setup(s => s.RemoveInactiveStaffFromDeviceAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SyncResult
            {
                Success = true,
                RecordsDeleted = 1,
                Message = "Removed 1 inactive staff"
            });

        // Act
        await _deviceSyncJob.RemoveInactiveStaffFromAllDevicesAsync();

        // Assert - Both devices should be processed
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(deviceMainOffice.DeviceId), Times.Once);
        _mockDeviceService.Verify(s => s.RemoveInactiveStaffFromDeviceAsync(deviceBranchOffice.DeviceId), Times.Once);
    }
}
