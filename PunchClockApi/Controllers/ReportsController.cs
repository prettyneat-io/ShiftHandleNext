using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PunchClockApi.Services;

namespace PunchClockApi.Controllers;

/// <summary>
/// Controller for generating and exporting attendance and payroll reports
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : BaseController<object>
{
    private readonly IReportingService _reportingService;

    public ReportsController(
        IReportingService reportingService,
        ILogger<ReportsController> logger)
        : base(logger)
    {
        _reportingService = reportingService;
    }

    /// <summary>
    /// Generate daily attendance report
    /// </summary>
    /// <param name="date">Date for the report (defaults to today)</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="format">Export format: json (default) or csv</param>
    /// <returns>Daily attendance report</returns>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string format = "json")
    {
        try
        {
            var reportDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await _reportingService.GenerateDailyReportAsync(reportDate, locationId, departmentId);

            if (format.ToLower() == "csv")
            {
                var csvData = await _reportingService.ExportToCsvAsync(report.Entries, "DailyAttendance");
                
                // Log the export
                var userId = GetUserId() ?? Guid.Empty;
                await _reportingService.LogExportAsync(
                    "DAILY_ATTENDANCE",
                    reportDate,
                    reportDate,
                    "CSV",
                    report.Entries.Count,
                    userId,
                    $"location={locationId},department={departmentId}"
                );

                return File(
                    csvData,
                    "text/csv",
                    $"daily_attendance_{reportDate:yyyy-MM-dd}.csv"
                );
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Generate monthly attendance summary report
    /// </summary>
    /// <param name="year">Year for the report (defaults to current year)</param>
    /// <param name="month">Month for the report (defaults to current month)</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="format">Export format: json (default) or csv</param>
    /// <returns>Monthly attendance summary</returns>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string format = "json")
    {
        try
        {
            var now = DateTime.UtcNow;
            var reportYear = year ?? now.Year;
            var reportMonth = month ?? now.Month;

            if (reportMonth < 1 || reportMonth > 12)
            {
                return BadRequest(new { message = "Month must be between 1 and 12" });
            }

            var report = await _reportingService.GenerateMonthlyReportAsync(
                reportYear, 
                reportMonth, 
                locationId, 
                departmentId
            );

            if (format.ToLower() == "csv")
            {
                var csvData = await _reportingService.ExportToCsvAsync(report.Entries, "MonthlyAttendance");
                
                // Log the export
                var userId = GetUserId() ?? Guid.Empty;
                var startDate = new DateOnly(reportYear, reportMonth, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                await _reportingService.LogExportAsync(
                    "MONTHLY_ATTENDANCE",
                    startDate,
                    endDate,
                    "CSV",
                    report.Entries.Count,
                    userId,
                    $"location={locationId},department={departmentId}"
                );

                return File(
                    csvData,
                    "text/csv",
                    $"monthly_attendance_{reportYear}-{reportMonth:D2}.csv"
                );
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Generate payroll export report for a date range
    /// </summary>
    /// <param name="startDate">Start date of the period</param>
    /// <param name="endDate">End date of the period</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="format">Export format: json (default) or csv</param>
    /// <returns>Payroll export data</returns>
    [HttpGet("payroll")]
    public async Task<IActionResult> GetPayrollReport(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string format = "json")
    {
        try
        {
            if (endDate < startDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            var daysDiff = endDate.DayNumber - startDate.DayNumber;
            if (daysDiff > 365)
            {
                return BadRequest(new { message = "Date range cannot exceed 1 year" });
            }

            var report = await _reportingService.GeneratePayrollReportAsync(
                startDate, 
                endDate, 
                locationId, 
                departmentId
            );

            if (format.ToLower() == "csv")
            {
                var csvData = await _reportingService.ExportToCsvAsync(report.Entries, "Payroll");
                
                // Log the export
                var userId = GetUserId() ?? Guid.Empty;
                await _reportingService.LogExportAsync(
                    "PAYROLL",
                    startDate,
                    endDate,
                    "CSV",
                    report.Entries.Count,
                    userId,
                    $"location={locationId},department={departmentId}"
                );

                return File(
                    csvData,
                    "text/csv",
                    $"payroll_export_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.csv"
                );
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get summary statistics for a date range (useful for dashboards)
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="departmentId">Optional department filter</param>
    /// <returns>Summary statistics</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryStatistics(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? departmentId)
    {
        try
        {
            if (endDate < startDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            // Generate payroll report which contains comprehensive data
            var report = await _reportingService.GeneratePayrollReportAsync(
                startDate, 
                endDate, 
                locationId, 
                departmentId
            );

            var summary = new
            {
                period = new
                {
                    startDate,
                    endDate,
                    totalDays = report.TotalDays
                },
                staff = new
                {
                    total = report.Entries.Count,
                    averageAttendanceRate = report.Entries.Count > 0
                        ? report.Entries.Average(e => e.DaysPresent) / (double)report.TotalDays * 100
                        : 0
                },
                hours = new
                {
                    totalRegularHours = report.Entries.Sum(e => e.RegularHours),
                    totalOvertimeHours = report.Entries.Sum(e => e.OvertimeHours),
                    totalWeekendOvertimeHours = report.Entries.Sum(e => e.WeekendOvertimeHours),
                    totalHolidayOvertimeHours = report.Entries.Sum(e => e.HolidayOvertimeHours)
                },
                attendance = new
                {
                    totalPresent = report.Entries.Sum(e => e.DaysPresent),
                    totalAbsent = report.Entries.Sum(e => e.DaysAbsent),
                    totalOnLeave = report.Entries.Sum(e => e.DaysOnLeave),
                    totalLateMinutes = report.Entries.Sum(e => e.TotalLateMinutes),
                    totalEarlyLeaveMinutes = report.Entries.Sum(e => e.TotalEarlyLeaveMinutes)
                }
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get department comparison report
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="locationId">Optional location filter</param>
    /// <returns>Department comparison data</returns>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartmentComparison(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? locationId)
    {
        try
        {
            if (endDate < startDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            var report = await _reportingService.GeneratePayrollReportAsync(
                startDate, 
                endDate, 
                locationId, 
                null
            );

            var departmentStats = report.Entries
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .GroupBy(e => e.Department)
                .Select(g => new
                {
                    department = g.Key,
                    staffCount = g.Count(),
                    averageAttendanceRate = g.Average(e => (double)e.DaysPresent / report.TotalDays * 100),
                    totalRegularHours = g.Sum(e => e.RegularHours),
                    totalOvertimeHours = g.Sum(e => e.OvertimeHours),
                    totalLateMinutes = g.Sum(e => e.TotalLateMinutes),
                    averageLateMinutesPerStaff = g.Average(e => e.TotalLateMinutes)
                })
                .OrderBy(d => d.department)
                .ToList();

            return Ok(new
            {
                period = new { startDate, endDate },
                totalDepartments = departmentStats.Count,
                departments = departmentStats
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
