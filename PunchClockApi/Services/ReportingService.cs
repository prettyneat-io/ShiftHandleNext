using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Services;

/// <summary>
/// Service implementation for generating attendance and payroll reports
/// </summary>
public sealed class ReportingService : IReportingService
{
    private readonly PunchClockDbContext _db;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(PunchClockDbContext db, ILogger<ReportingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DailyAttendanceReport> GenerateDailyReportAsync(
        DateOnly date, 
        Guid? locationId = null, 
        Guid? departmentId = null)
    {
        _logger.LogInformation("Generating daily attendance report for {Date}", date);

        // Get all active staff with filters
        var staffQuery = _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .Include(s => s.Shift)
            .Where(s => s.IsActive);

        if (locationId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.LocationId == locationId.Value);
        }

        if (departmentId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.DepartmentId == departmentId.Value);
        }

        var staff = await staffQuery.ToListAsync();
        var staffIds = staff.Select(s => s.StaffId).ToList();

        // Get attendance records for the day
        var attendanceRecords = await _db.AttendanceRecords
            .Where(r => r.AttendanceDate == date && staffIds.Contains(r.StaffId))
            .ToListAsync();

        // Get leave requests for the day
        var leaveRequests = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == "APPROVED" 
                      && lr.StartDate <= date 
                      && lr.EndDate >= date
                      && staffIds.Contains(lr.StaffId))
            .ToListAsync();

        var report = new DailyAttendanceReport
        {
            Date = date,
            TotalStaff = staff.Count
        };

        var entries = new List<DailyAttendanceEntry>();

        foreach (var staffMember in staff)
        {
            var attendance = attendanceRecords.FirstOrDefault(r => r.StaffId == staffMember.StaffId);
            var leave = leaveRequests.FirstOrDefault(lr => lr.StaffId == staffMember.StaffId);

            var entry = new DailyAttendanceEntry
            {
                StaffId = staffMember.StaffId,
                EmployeeId = staffMember.EmployeeId,
                FullName = $"{staffMember.FirstName} {staffMember.LastName}",
                Department = staffMember.Department?.DepartmentName,
                Location = staffMember.Location?.LocationName,
                ShiftName = staffMember.Shift?.ShiftName,
                IsOnLeave = leave != null,
                LeaveType = leave?.LeaveType?.TypeName
            };

            if (leave != null)
            {
                // Staff is on approved leave
                entry.AttendanceStatus = "ON_LEAVE";
                report.OnLeaveCount++;
            }
            else if (attendance != null)
            {
                // Staff has attendance record
                entry.ClockIn = attendance.ClockIn;
                entry.ClockOut = attendance.ClockOut;
                entry.TotalHours = attendance.TotalHours;
                entry.LateMinutes = attendance.LateMinutes;
                entry.EarlyLeaveMinutes = attendance.EarlyLeaveMinutes;
                entry.AttendanceStatus = attendance.AttendanceStatus;
                entry.HasAnomalies = attendance.HasAnomalies;
                entry.AnomalyFlags = attendance.AnomalyFlags;

                report.PresentCount++;
                if (attendance.LateMinutes > 0)
                {
                    report.LateCount++;
                }
            }
            else
            {
                // No attendance record and not on leave
                entry.AttendanceStatus = "ABSENT";
                report.AbsentCount++;
            }

            entries.Add(entry);

            // Department breakdown
            if (!string.IsNullOrEmpty(entry.Department))
            {
                if (!report.DepartmentBreakdown.ContainsKey(entry.Department))
                {
                    report.DepartmentBreakdown[entry.Department] = 0;
                }
                if (entry.AttendanceStatus == "PRESENT")
                {
                    report.DepartmentBreakdown[entry.Department]++;
                }
            }

            // Location breakdown
            if (!string.IsNullOrEmpty(entry.Location))
            {
                if (!report.LocationBreakdown.ContainsKey(entry.Location))
                {
                    report.LocationBreakdown[entry.Location] = 0;
                }
                if (entry.AttendanceStatus == "PRESENT")
                {
                    report.LocationBreakdown[entry.Location]++;
                }
            }
        }

        report.Entries = entries.OrderBy(e => e.FullName).ToList();

        _logger.LogInformation(
            "Daily report generated: {Present}/{Total} present, {Late} late, {Absent} absent, {Leave} on leave",
            report.PresentCount, report.TotalStaff, report.LateCount, report.AbsentCount, report.OnLeaveCount);

        return report;
    }

    public async Task<MonthlyAttendanceReport> GenerateMonthlyReportAsync(
        int year, 
        int month, 
        Guid? locationId = null, 
        Guid? departmentId = null)
    {
        _logger.LogInformation("Generating monthly attendance report for {Year}-{Month:D2}", year, month);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get all active staff with filters
        var staffQuery = _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .Where(s => s.IsActive);

        if (locationId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.LocationId == locationId.Value);
        }

        if (departmentId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.DepartmentId == departmentId.Value);
        }

        var staff = await staffQuery.ToListAsync();
        var staffIds = staff.Select(s => s.StaffId).ToList();

        // Get all attendance records for the month
        var attendanceRecords = await _db.AttendanceRecords
            .Where(r => r.AttendanceDate >= startDate 
                     && r.AttendanceDate <= endDate 
                     && staffIds.Contains(r.StaffId))
            .ToListAsync();

        // Get holidays for the month
        var holidays = await _db.Holidays
            .Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate && h.IsActive)
            .ToListAsync();

        var holidayDates = holidays.Select(h => h.HolidayDate).ToHashSet();

        // Calculate working days (excluding weekends and holidays)
        var totalWorkingDays = 0;
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dayOfWeek = d.DayOfWeek;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(d))
            {
                totalWorkingDays++;
            }
        }

        var report = new MonthlyAttendanceReport
        {
            Year = year,
            Month = month,
            TotalWorkingDays = totalWorkingDays,
            TotalStaff = staff.Count
        };

        var entries = new List<MonthlyAttendanceEntry>();
        var totalLateIncidents = 0;
        var totalAnomalies = 0;
        var totalOvertimeHours = TimeSpan.Zero;

        foreach (var staffMember in staff)
        {
            var staffRecords = attendanceRecords.Where(r => r.StaffId == staffMember.StaffId).ToList();

            var daysPresent = staffRecords.Count(r => r.AttendanceStatus == "PRESENT");
            var daysLate = staffRecords.Count(r => r.LateMinutes > 0);
            var totalWorkHours = staffRecords
                .Where(r => r.TotalHours.HasValue)
                .Sum(r => r.TotalHours!.Value.TotalHours);
            var overtimeHours = staffRecords
                .Where(r => r.OvertimeHours.HasValue)
                .Sum(r => r.OvertimeHours!.Value.TotalHours);
            var totalLateMinutes = staffRecords.Sum(r => r.LateMinutes ?? 0);

            var entry = new MonthlyAttendanceEntry
            {
                StaffId = staffMember.StaffId,
                EmployeeId = staffMember.EmployeeId,
                FullName = $"{staffMember.FirstName} {staffMember.LastName}",
                Department = staffMember.Department?.DepartmentName,
                Location = staffMember.Location?.LocationName,
                DaysPresent = daysPresent,
                DaysAbsent = totalWorkingDays - daysPresent,
                DaysLate = daysLate,
                TotalWorkHours = TimeSpan.FromHours(totalWorkHours),
                TotalOvertimeHours = TimeSpan.FromHours(overtimeHours),
                TotalLateMinutes = totalLateMinutes,
                AttendanceRate = totalWorkingDays > 0 ? (decimal)daysPresent / totalWorkingDays * 100 : 0
            };

            entries.Add(entry);

            totalLateIncidents += daysLate;
            totalAnomalies += staffRecords.Count(r => r.HasAnomalies);
            totalOvertimeHours += TimeSpan.FromHours(overtimeHours);
        }

        report.Entries = entries.OrderBy(e => e.FullName).ToList();
        report.Statistics = new MonthlyStatistics
        {
            AverageAttendanceRate = entries.Count > 0 ? entries.Average(e => e.AttendanceRate) : 0,
            TotalLateIncidents = totalLateIncidents,
            TotalAnomalies = totalAnomalies,
            TotalOvertimeHours = totalOvertimeHours
        };

        _logger.LogInformation(
            "Monthly report generated: {StaffCount} staff, {WorkingDays} working days, {AvgAttendance:F2}% avg attendance",
            staff.Count, totalWorkingDays, report.Statistics.AverageAttendanceRate);

        return report;
    }

    public async Task<PayrollExportReport> GeneratePayrollReportAsync(
        DateOnly startDate, 
        DateOnly endDate, 
        Guid? locationId = null, 
        Guid? departmentId = null)
    {
        _logger.LogInformation("Generating payroll report from {StartDate} to {EndDate}", startDate, endDate);

        // Get all active staff with filters
        var staffQuery = _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .Where(s => s.IsActive);

        if (locationId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.LocationId == locationId.Value);
        }

        if (departmentId.HasValue)
        {
            staffQuery = staffQuery.Where(s => s.DepartmentId == departmentId.Value);
        }

        var staff = await staffQuery.ToListAsync();
        var staffIds = staff.Select(s => s.StaffId).ToList();

        // Get all attendance records for the period
        var attendanceRecords = await _db.AttendanceRecords
            .Where(r => r.AttendanceDate >= startDate 
                     && r.AttendanceDate <= endDate 
                     && staffIds.Contains(r.StaffId))
            .ToListAsync();

        // Get leave requests for the period
        var leaveRequests = await _db.LeaveRequests
            .Where(lr => lr.Status == "APPROVED" 
                      && lr.StartDate <= endDate 
                      && lr.EndDate >= startDate
                      && staffIds.Contains(lr.StaffId))
            .ToListAsync();

        // Get holidays for the period
        var holidays = await _db.Holidays
            .Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate && h.IsActive)
            .ToListAsync();

        var holidayDates = holidays.Select(h => h.HolidayDate).ToHashSet();

        // Calculate working days
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        var workingDays = 0;
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dayOfWeek = d.DayOfWeek;
            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(d))
            {
                workingDays++;
            }
        }

        var report = new PayrollExportReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays
        };

        var entries = new List<PayrollEntry>();

        foreach (var staffMember in staff)
        {
            var staffRecords = attendanceRecords.Where(r => r.StaffId == staffMember.StaffId).ToList();
            var staffLeave = leaveRequests.Where(lr => lr.StaffId == staffMember.StaffId).ToList();

            // Calculate leave days
            var leaveDays = 0;
            foreach (var leave in staffLeave)
            {
                var leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                var leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;
                leaveDays += leaveEnd.DayNumber - leaveStart.DayNumber + 1;
            }

            var daysPresent = staffRecords.Count(r => r.AttendanceStatus == "PRESENT");
            var regularHours = staffRecords
                .Where(r => r.RegularHours.HasValue)
                .Sum(r => r.RegularHours!.Value.TotalHours);
            var overtimeHours = staffRecords
                .Where(r => r.OvertimeHours.HasValue)
                .Sum(r => r.OvertimeHours!.Value.TotalHours);

            // Separate weekend and holiday overtime (you'll need to enhance AttendanceRecord to track this)
            var weekendOvertimeHours = 0m;
            var holidayOvertimeHours = 0m;
            foreach (var record in staffRecords)
            {
                if (record.OvertimeHours.HasValue)
                {
                    var recordDate = record.AttendanceDate;
                    var dayOfWeek = recordDate.DayOfWeek;
                    
                    if (holidayDates.Contains(recordDate))
                    {
                        holidayOvertimeHours += (decimal)record.OvertimeHours.Value.TotalHours;
                    }
                    else if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        weekendOvertimeHours += (decimal)record.OvertimeHours.Value.TotalHours;
                    }
                }
            }

            var entry = new PayrollEntry
            {
                StaffId = staffMember.StaffId,
                EmployeeId = staffMember.EmployeeId,
                FullName = $"{staffMember.FirstName} {staffMember.LastName}",
                Department = staffMember.Department?.DepartmentName,
                Location = staffMember.Location?.LocationName,
                Position = staffMember.PositionTitle,
                TotalWorkingDays = workingDays,
                DaysPresent = daysPresent,
                DaysAbsent = workingDays - daysPresent - leaveDays,
                DaysOnLeave = leaveDays,
                RegularHours = (decimal)regularHours,
                OvertimeHours = (decimal)overtimeHours - weekendOvertimeHours - holidayOvertimeHours,
                WeekendOvertimeHours = weekendOvertimeHours,
                HolidayOvertimeHours = holidayOvertimeHours,
                TotalLateMinutes = staffRecords.Sum(r => r.LateMinutes ?? 0),
                TotalEarlyLeaveMinutes = staffRecords.Sum(r => r.EarlyLeaveMinutes ?? 0)
            };

            entries.Add(entry);
        }

        report.Entries = entries.OrderBy(e => e.EmployeeId).ToList();

        _logger.LogInformation(
            "Payroll report generated: {StaffCount} staff, {Days} days",
            staff.Count, totalDays);

        return report;
    }

    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string reportType) where T : class
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Write header
        csv.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p.Name))));

        // Write data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvValue(FormatCsvValue(value));
            });
            csv.AppendLine(string.Join(",", values));
        }

        _logger.LogInformation("Exported {Count} {ReportType} records to CSV", data.Count(), reportType);

        return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<Guid> LogExportAsync(
        string exportType, 
        DateOnly startDate, 
        DateOnly endDate, 
        string fileFormat, 
        int recordCount, 
        Guid exportedBy, 
        string? filterCriteria = null)
    {
        var exportLog = new ExportLog
        {
            ExportId = Guid.NewGuid(),
            ExportType = exportType,
            StartDate = startDate,
            EndDate = endDate,
            FilterCriteria = filterCriteria,
            RecordCount = recordCount,
            FileFormat = fileFormat,
            ExportedBy = exportedBy,
            ExportedAt = DateTime.UtcNow,
            ExportStatus = "SUCCESS"
        };

        _db.ExportLogs.Add(exportLog);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Export logged: {ExportType} ({RecordCount} records) by user {UserId}",
            exportType, recordCount, exportedBy);

        return exportLog.ExportId;
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Escape double quotes and wrap in quotes if contains comma, newline, or quote
        if (value.Contains(',') || value.Contains('\n') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatCsvValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeSpan ts => $"{ts.TotalHours:F2}",
            decimal dec => dec.ToString("F2", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("F2", CultureInfo.InvariantCulture),
            float flt => flt.ToString("F2", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
