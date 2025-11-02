using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Services;

public sealed class AttendanceProcessingService
{
    private readonly PunchClockDbContext _db;

    public AttendanceProcessingService(PunchClockDbContext db) => _db = db;

    /// <summary>
    /// Process attendance for a single staff member on a specific date
    /// </summary>
    public async Task<AttendanceRecord> ProcessDailyAttendance(
        Guid staffId,
        DateTime date,
        DateTime? expectedStartTime = null,
        DateTime? expectedEndTime = null,
        decimal minimumHours = 0)
    {
        var attendanceDate = DateOnly.FromDateTime(date.Date);

        // Load staff with shift information
        var staff = await _db.Staff
            .Include(s => s.Shift)
            .FirstOrDefaultAsync(s => s.StaffId == staffId);

        // Get all punch logs for the staff member on this date
        var punchLogs = await _db.PunchLogs
            .Where(p => p.StaffId == staffId && p.PunchTime.Date == date.Date)
            .OrderBy(p => p.PunchTime)
            .ToListAsync();

        // Check if attendance record already exists
        var existingRecord = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.StaffId == staffId && r.AttendanceDate == attendanceDate);

        var record = existingRecord ?? new AttendanceRecord
        {
            RecordId = Guid.NewGuid(),
            StaffId = staffId,
            AttendanceDate = attendanceDate,
            CreatedAt = DateTime.UtcNow
        };

        // Reset fields for reprocessing
        record.UpdatedAt = DateTime.UtcNow;
        var anomalyFlags = new Dictionary<string, object>();

        // Get shift-based parameters if available
        var shift = staff?.Shift;
        var shiftStartTime = expectedStartTime;
        var shiftEndTime = expectedEndTime;
        TimeSpan? breakDuration = null;
        bool autoDeductBreak = false;

        if (shift != null && shift.IsActive)
        {
            // Use shift times if not overridden
            if (!expectedStartTime.HasValue)
            {
                shiftStartTime = date.Date.Add(shift.StartTime.ToTimeSpan());
            }
            if (!expectedEndTime.HasValue)
            {
                shiftEndTime = date.Date.Add(shift.EndTime.ToTimeSpan());
            }
            
            breakDuration = shift.BreakDuration;
            autoDeductBreak = shift.AutoDeductBreak;
        }

        // Handle no punches - mark as absent
        if (!punchLogs.Any())
        {
            record.AttendanceStatus = "ABSENT";
            record.ClockIn = null;
            record.ClockOut = null;
            record.TotalHours = TimeSpan.Zero;
            record.RegularHours = TimeSpan.Zero;
            record.BreakDuration = TimeSpan.Zero;
            record.LateMinutes = 0;
            record.EarlyLeaveMinutes = 0;
            record.OvertimeHours = TimeSpan.Zero;
        }
        else
        {
            // Find first IN punch and last OUT punch
            var inPunches = punchLogs.Where(p => p.PunchType == "IN").ToList();
            var outPunches = punchLogs.Where(p => p.PunchType == "OUT").ToList();

            record.ClockIn = inPunches.FirstOrDefault()?.PunchTime;
            record.ClockOut = outPunches.LastOrDefault()?.PunchTime;

            // Determine status
            if (record.ClockIn == null && record.ClockOut != null)
            {
                record.AttendanceStatus = "INCOMPLETE";
                anomalyFlags["missing_checkin"] = true;
                record.TotalHours = TimeSpan.Zero;
                record.RegularHours = TimeSpan.Zero;
                record.BreakDuration = TimeSpan.Zero;
            }
            else if (record.ClockIn != null && record.ClockOut == null)
            {
                record.AttendanceStatus = "INCOMPLETE";
                anomalyFlags["missing_checkout"] = true;
                record.TotalHours = TimeSpan.Zero;
                record.RegularHours = TimeSpan.Zero;
                record.BreakDuration = TimeSpan.Zero;
            }
            else if (record.ClockIn != null && record.ClockOut != null)
            {
                record.AttendanceStatus = "PRESENT";
                
                // Calculate gross hours (before break deduction)
                var grossHours = record.ClockOut.Value - record.ClockIn.Value;
                
                // Apply break deduction logic
                TimeSpan deductedBreak = TimeSpan.Zero;
                if (autoDeductBreak && breakDuration.HasValue)
                {
                    // Auto-deduct break time
                    deductedBreak = breakDuration.Value;
                }
                else if (!autoDeductBreak && breakDuration.HasValue)
                {
                    // Only deduct break if shift is long enough
                    // Common rule: deduct break only if worked > 6 hours
                    if (grossHours.TotalHours > 6)
                    {
                        deductedBreak = breakDuration.Value;
                    }
                }
                else
                {
                    // No shift configured - use default break logic
                    // Deduct 30-minute break if worked more than 6 hours
                    if (grossHours.TotalHours > 6)
                    {
                        deductedBreak = TimeSpan.FromMinutes(30);
                    }
                }

                record.BreakDuration = deductedBreak;
                record.TotalHours = grossHours - deductedBreak;

                // Calculate regular and overtime hours
                if (shift != null)
                {
                    var requiredHours = shift.RequiredHours;
                    if (record.TotalHours > requiredHours)
                    {
                        record.RegularHours = requiredHours;
                        record.OvertimeHours = record.TotalHours - requiredHours;
                    }
                    else
                    {
                        record.RegularHours = record.TotalHours;
                        record.OvertimeHours = TimeSpan.Zero;
                    }
                }
                else
                {
                    // No shift - assume 8 hour standard
                    var standardHours = TimeSpan.FromHours(8);
                    if (record.TotalHours > standardHours)
                    {
                        record.RegularHours = standardHours;
                        record.OvertimeHours = record.TotalHours - standardHours;
                    }
                    else
                    {
                        record.RegularHours = record.TotalHours;
                        record.OvertimeHours = TimeSpan.Zero;
                    }
                }

                // Calculate late minutes
                if (shiftStartTime.HasValue && record.ClockIn > shiftStartTime.Value)
                {
                    var lateSpan = record.ClockIn.Value - shiftStartTime.Value;
                    var gracePeriod = shift?.GracePeriodMinutes ?? 15;
                    
                    if (lateSpan.TotalMinutes > gracePeriod)
                    {
                        record.LateMinutes = (int)lateSpan.TotalMinutes - gracePeriod;
                    }
                    else
                    {
                        record.LateMinutes = 0;
                    }
                }
                else
                {
                    record.LateMinutes = 0;
                }

                // Calculate early leave minutes
                if (shiftEndTime.HasValue && record.ClockOut < shiftEndTime.Value)
                {
                    var earlySpan = shiftEndTime.Value - record.ClockOut.Value;
                    var earlyThreshold = shift?.EarlyLeaveThresholdMinutes ?? 15;
                    
                    if (earlySpan.TotalMinutes > earlyThreshold)
                    {
                        record.EarlyLeaveMinutes = (int)earlySpan.TotalMinutes - earlyThreshold;
                    }
                    else
                    {
                        record.EarlyLeaveMinutes = 0;
                    }
                }
                else
                {
                    record.EarlyLeaveMinutes = 0;
                }

                // Check for short shift
                if (minimumHours > 0 && record.TotalHours!.Value.TotalHours < (double)minimumHours)
                {
                    anomalyFlags["short_shift"] = true;
                }

                // Flag if late
                if (record.LateMinutes > 0)
                {
                    anomalyFlags["late_arrival"] = record.LateMinutes;
                }

                // Flag if early leave
                if (record.EarlyLeaveMinutes > 0)
                {
                    anomalyFlags["early_departure"] = record.EarlyLeaveMinutes;
                }
            }
            else
            {
                record.AttendanceStatus = "INCOMPLETE";
                record.TotalHours = TimeSpan.Zero;
                record.RegularHours = TimeSpan.Zero;
                record.BreakDuration = TimeSpan.Zero;
            }

            // Detect odd number of punches (potential anomaly)
            if (punchLogs.Count % 2 != 0)
            {
                anomalyFlags["odd_punch_count"] = true;
            }
        }

        // Update anomaly flags
        record.HasAnomalies = anomalyFlags.Any();
        record.AnomalyFlags = anomalyFlags.Any() 
            ? JsonSerializer.Serialize(anomalyFlags)
            : null;

        // Save or update record
        if (existingRecord == null)
        {
            _db.AttendanceRecords.Add(record);
        }

        await _db.SaveChangesAsync();
        return record;
    }

    /// <summary>
    /// Process attendance for a single staff member across a date range
    /// </summary>
    public async Task<List<AttendanceRecord>> ProcessDateRange(
        Guid staffId,
        DateTime startDate,
        DateTime endDate,
        DateTime? expectedStartTime = null,
        DateTime? expectedEndTime = null,
        decimal minimumHours = 0)
    {
        var records = new List<AttendanceRecord>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var record = await ProcessDailyAttendance(
                staffId,
                currentDate,
                expectedStartTime,
                expectedEndTime,
                minimumHours);

            records.Add(record);
            currentDate = currentDate.AddDays(1);
        }

        return records;
    }

    /// <summary>
    /// Process attendance for all active staff members on a specific date
    /// </summary>
    public async Task<List<AttendanceRecord>> ProcessAllStaff(
        DateTime date,
        DateTime? expectedStartTime = null,
        DateTime? expectedEndTime = null,
        decimal minimumHours = 0)
    {
        var activeStaff = await _db.Staff
            .Where(s => s.IsActive)
            .Select(s => s.StaffId)
            .ToListAsync();

        var records = new List<AttendanceRecord>();

        foreach (var staffId in activeStaff)
        {
            var record = await ProcessDailyAttendance(
                staffId,
                date,
                expectedStartTime,
                expectedEndTime,
                minimumHours);

            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Process attendance for all active staff across a date range
    /// </summary>
    public async Task<List<AttendanceRecord>> ProcessAllStaffDateRange(
        DateTime startDate,
        DateTime endDate,
        DateTime? expectedStartTime = null,
        DateTime? expectedEndTime = null,
        decimal minimumHours = 0)
    {
        var activeStaff = await _db.Staff
            .Where(s => s.IsActive)
            .Select(s => s.StaffId)
            .ToListAsync();

        var records = new List<AttendanceRecord>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            foreach (var staffId in activeStaff)
            {
                var record = await ProcessDailyAttendance(
                    staffId,
                    currentDate,
                    expectedStartTime,
                    expectedEndTime,
                    minimumHours);

                records.Add(record);
            }

            currentDate = currentDate.AddDays(1);
        }

        return records;
    }

    /// <summary>
    /// Reprocess attendance records that have anomalies
    /// </summary>
    public async Task<int> ReprocessAnomalies(DateTime? fromDate = null)
    {
        var query = _db.AttendanceRecords.AsQueryable();

        if (fromDate.HasValue)
        {
            var dateOnly = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(r => r.AttendanceDate >= dateOnly);
        }

        var recordsWithAnomalies = await query
            .Where(r => r.HasAnomalies)
            .ToListAsync();

        foreach (var record in recordsWithAnomalies)
        {
            await ProcessDailyAttendance(record.StaffId, record.AttendanceDate.ToDateTime(TimeOnly.MinValue));
        }

        return recordsWithAnomalies.Count;
    }
}
