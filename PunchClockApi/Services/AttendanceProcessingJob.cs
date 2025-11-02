using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PunchClockApi.Data;

namespace PunchClockApi.Services;

public sealed class AttendanceProcessingJob
{
    private readonly PunchClockDbContext _db;
    private readonly AttendanceProcessingService _processingService;
    private readonly ILogger<AttendanceProcessingJob>? _logger;

    public AttendanceProcessingJob(
        PunchClockDbContext db, 
        AttendanceProcessingService processingService,
        ILogger<AttendanceProcessingJob>? logger = null)
    {
        _db = db;
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Process attendance for yesterday (default daily job)
    /// </summary>
    public async Task ProcessYesterdayAttendanceAsync()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        _logger?.LogInformation("Processing attendance for {Date}", yesterday);

        await ProcessDateAsync(yesterday);

        _logger?.LogInformation("Completed attendance processing for {Date}", yesterday);
    }

    /// <summary>
    /// Process attendance for a specific date
    /// </summary>
    public async Task ProcessDateAsync(DateTime date)
    {
        _logger?.LogInformation("Processing attendance for all staff on {Date}", date.Date);

        var result = await _processingService.ProcessAllStaff(date.Date);

        _logger?.LogInformation("Processed {Count} attendance records for {Date}", result.Count, date.Date);
    }

    /// <summary>
    /// Process attendance for a date range
    /// </summary>
    public async Task ProcessDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        _logger?.LogInformation("Processing attendance from {StartDate} to {EndDate}", 
            startDate.Date, endDate.Date);

        var result = await _processingService.ProcessAllStaffDateRange(startDate.Date, endDate.Date);

        _logger?.LogInformation("Processed {Count} attendance records for date range", result.Count);
    }

    /// <summary>
    /// Reprocess attendance records with anomalies
    /// </summary>
    public async Task ReprocessAnomaliesAsync(DateTime? fromDate = null)
    {
        _logger?.LogInformation("Reprocessing attendance records with anomalies from {Date}", 
            fromDate?.Date.ToString() ?? "beginning");

        var count = await _processingService.ReprocessAnomalies(fromDate);

        _logger?.LogInformation("Reprocessed {Count} attendance records with anomalies", count);
    }

    /// <summary>
    /// Process pending punch logs (those marked as not processed)
    /// </summary>
    public async Task ProcessPendingPunchLogsAsync()
    {
        _logger?.LogInformation("Processing pending punch logs");

        var pendingLogs = await _db.PunchLogs
            .Where(p => !p.IsProcessed && p.IsValid)
            .Select(p => new { p.StaffId, Date = p.PunchTime.Date })
            .Distinct()
            .ToListAsync();

        _logger?.LogInformation("Found {Count} distinct staff/date combinations to process", pendingLogs.Count);

        foreach (var log in pendingLogs)
        {
            if (log.StaffId.HasValue)
            {
                await _processingService.ProcessDailyAttendance(log.StaffId.Value, log.Date);
            }
        }

        // Mark punch logs as processed
        await _db.PunchLogs
            .Where(p => !p.IsProcessed && p.IsValid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsProcessed, true)
                .SetProperty(p => p.ProcessedAt, DateTime.UtcNow));

        _logger?.LogInformation("Completed processing pending punch logs");
    }
}
