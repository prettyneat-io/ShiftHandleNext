using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PunchClockApi.Services;
using Xunit;

namespace PunchClockApi.Tests;

public sealed class AttendanceProcessingTests : IAsyncLifetime
{
    private PunchClockDbContext _db = null!;
    private AttendanceProcessingService _service = null!;
    private Staff _testStaff = null!;
    private Device _testDevice = null!;
    private Department _testDepartment = null!;
    private Location _testLocation = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PunchClockDbContext>()
            .UseInMemoryDatabase($"AttendanceProcessing_{Guid.NewGuid()}")
            .Options;

        _db = new PunchClockDbContext(options);
        _service = new AttendanceProcessingService(_db);

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

        _testStaff = new Staff
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

        _testDevice = new Device
        {
            DeviceId = Guid.NewGuid(),
            DeviceName = "Main Entrance",
            DeviceSerial = "ZK123456",
            IpAddress = "192.168.1.100",
            Port = 4370,
            Manufacturer = "ZKTeco",
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Departments.Add(_testDepartment);
        _db.Locations.Add(_testLocation);
        _db.Staff.Add(_testStaff);
        _db.Devices.Add(_testDevice);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithValidPunches_CreatesAttendanceRecord()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(9), // 9:00 AM
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(17), // 5:00 PM
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.AddRange(punchIn, punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.ClockIn.Should().Be(date.AddHours(9));
        record.ClockOut.Should().Be(date.AddHours(17));
        record.TotalHours.Should().Be(TimeSpan.FromHours(7.5)); // 8 hours minus 30-minute break
        record.AttendanceStatus.Should().Be("PRESENT");
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithMultiplePunches_UsesFirstAndLast()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punches = new List<PunchLog>
        {
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(9),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            },
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(12),
                PunchType = "OUT",
                CreatedAt = DateTime.UtcNow
            },
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(13),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            },
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(18),
                PunchType = "OUT",
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.PunchLogs.AddRange(punches);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.ClockIn.Should().Be(date.AddHours(9));
        record.ClockOut.Should().Be(date.AddHours(18));
        record.TotalHours.Should().Be(TimeSpan.FromHours(8.5)); // 9 hours minus 30-minute break
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithLateArrival_CalculatesLateMinutes()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var expectedStartTime = date.AddHours(9); // Expected: 9:00 AM
        var actualCheckIn = date.AddHours(9.5); // Actual: 9:30 AM (30 minutes late)

        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = actualCheckIn,
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(17),
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.AddRange(punchIn, punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date, expectedStartTime: expectedStartTime);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.LateMinutes.Should().Be(15); // 30 minutes late minus 15-minute grace period
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithOvertime_CalculatesOvertimeHours()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var expectedEndTime = date.AddHours(17); // Expected: 5:00 PM
        var actualCheckOut = date.AddHours(20); // Actual: 8:00 PM (3 hours overtime)

        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(9),
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = actualCheckOut,
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.AddRange(punchIn, punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date, expectedEndTime: expectedEndTime);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.OvertimeHours.Should().Be(TimeSpan.FromHours(2.5)); // 11 hours minus 30-min break = 10.5 hours, minus 8 standard = 2.5 OT
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithNoCheckOut_MarksIncompleteWithAnomaly()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(9),
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.Add(punchIn);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.ClockIn.Should().Be(date.AddHours(9));
        record.ClockOut.Should().BeNull();
        record.AttendanceStatus.Should().Be("INCOMPLETE");
        record.AnomalyFlags.Should().Contain("missing_checkout");
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithNoCheckIn_MarksIncompleteWithAnomaly()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(17),
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.Add(punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.ClockIn.Should().BeNull();
        record.ClockOut.Should().Be(date.AddHours(17));
        record.AttendanceStatus.Should().Be("INCOMPLETE");
        record.AnomalyFlags.Should().Contain("missing_checkin");
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithNoPunches_MarksAbsent()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.ClockIn.Should().BeNull();
        record.ClockOut.Should().BeNull();
        record.AttendanceStatus.Should().Be("ABSENT");
        record.TotalHours.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ProcessDailyAttendance_UpdatesExistingRecord_WhenAlreadyExists()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var existingAttendanceDate = DateOnly.FromDateTime(date);
        var existingRecord = new AttendanceRecord
        {
            RecordId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            AttendanceDate = existingAttendanceDate,
            AttendanceStatus = "ABSENT",
            TotalHours = TimeSpan.Zero,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.AttendanceRecords.Add(existingRecord);
        await _db.SaveChangesAsync();

        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(9),
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(17),
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };
        _db.PunchLogs.AddRange(punchIn, punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var records = await _db.AttendanceRecords
            .Where(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate)
            .ToListAsync();

        records.Should().HaveCount(1); // Should update, not create new
        records[0].AttendanceStatus.Should().Be("PRESENT");
        records[0].TotalHours.Should().Be(TimeSpan.FromHours(7.5)); // 8 hours minus 30-minute break
    }

    [Fact]
    public async Task ProcessDateRange_ProcessesMultipleDays()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(-2);
        var endDate = DateTime.UtcNow.Date;

        // Add punches for 3 days
        for (int i = 0; i <= 2; i++)
        {
            var date = startDate.AddDays(i);
            _db.PunchLogs.AddRange(
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = _testStaff.StaffId,
                    DeviceId = _testDevice.DeviceId,
                    PunchTime = date.AddHours(9),
                    PunchType = "IN",
                    CreatedAt = DateTime.UtcNow
                },
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = _testStaff.StaffId,
                    DeviceId = _testDevice.DeviceId,
                    PunchTime = date.AddHours(17),
                    PunchType = "OUT",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDateRange(_testStaff.StaffId, startDate, endDate);

        // Assert
        var records = await _db.AttendanceRecords
            .Where(r => r.StaffId == _testStaff.StaffId)
            .OrderBy(r => r.AttendanceDate)
            .ToListAsync();

        records.Should().HaveCount(3);
        records.All(r => r.AttendanceStatus == "PRESENT").Should().BeTrue();
        records.All(r => r.TotalHours == TimeSpan.FromHours(7.5)).Should().BeTrue(); // 8 hours minus 30-minute break
    }

    [Fact]
    public async Task ProcessAllStaff_ProcessesAllActiveStaff()
    {
        // Arrange
        var staff2 = new Staff
        {
            StaffId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            EmployeeId = "EMP002",
            BadgeNumber = "BADGE002",
            DepartmentId = _testDepartment.DepartmentId,
            LocationId = _testLocation.LocationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Staff.Add(staff2);
        await _db.SaveChangesAsync();

        var date = DateTime.UtcNow.Date;

        // Add punches for both staff
        foreach (var staff in new[] { _testStaff, staff2 })
        {
            _db.PunchLogs.AddRange(
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staff.StaffId,
                    DeviceId = _testDevice.DeviceId,
                    PunchTime = date.AddHours(9),
                    PunchType = "IN",
                    CreatedAt = DateTime.UtcNow
                },
                new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staff.StaffId,
                    DeviceId = _testDevice.DeviceId,
                    PunchTime = date.AddHours(17),
                    PunchType = "OUT",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessAllStaff(date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var records = await _db.AttendanceRecords
            .Where(r => r.AttendanceDate == attendanceDate)
            .ToListAsync();

        records.Should().HaveCount(2);
        records.All(r => r.AttendanceStatus == "PRESENT").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithOddNumberOfPunches_DetectsAnomaly()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punches = new List<PunchLog>
        {
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(9),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            },
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(12),
                PunchType = "OUT",
                CreatedAt = DateTime.UtcNow
            },
            new() {
                LogId = Guid.NewGuid(),
                StaffId = _testStaff.StaffId,
                DeviceId = _testDevice.DeviceId,
                PunchTime = date.AddHours(13),
                PunchType = "IN",
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.PunchLogs.AddRange(punches);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.AnomalyFlags.Should().Contain("odd_punch_count");
    }

    [Fact]
    public async Task ProcessDailyAttendance_WithShortShift_DetectsAnomaly()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        var punchIn = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(9),
            PunchType = "IN",
            CreatedAt = DateTime.UtcNow
        };
        var punchOut = new PunchLog
        {
            LogId = Guid.NewGuid(),
            StaffId = _testStaff.StaffId,
            DeviceId = _testDevice.DeviceId,
            PunchTime = date.AddHours(11), // Only 2 hours
            PunchType = "OUT",
            CreatedAt = DateTime.UtcNow
        };

        _db.PunchLogs.AddRange(punchIn, punchOut);
        await _db.SaveChangesAsync();

        // Act
        await _service.ProcessDailyAttendance(_testStaff.StaffId, date, minimumHours: 4.0m);

        // Assert
        var attendanceDate = DateOnly.FromDateTime(date);
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == _testStaff.StaffId && r.AttendanceDate == attendanceDate);

        record.Should().NotBeNull();
        record!.TotalHours.Should().Be(TimeSpan.FromHours(2.0));
        record.AnomalyFlags.Should().Contain("short_shift");
    }
}
