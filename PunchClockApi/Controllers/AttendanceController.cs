using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController : BaseController<object>
{
    private readonly PunchClockDbContext _db;

    public AttendanceController(PunchClockDbContext db, ILogger<AttendanceController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetPunchLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? staffId,
        [FromQuery] Guid? deviceId,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] string? include)
    {
        try
        {
            var options = ParseQuery(Request.Query);

            var query = _db.PunchLogs.AsQueryable();

            // Apply domain-specific filters
            if (startDate.HasValue)
            {
                query = query.Where(p => p.PunchTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PunchTime <= endDate.Value);
            }

            if (staffId.HasValue)
            {
                query = query.Where(p => p.StaffId == staffId.Value);
            }

            if (deviceId.HasValue)
            {
                query = query.Where(p => p.DeviceId == deviceId.Value);
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply query options (includes, sorting, pagination)
            query = BaseController<PunchLog>.ApplyQueryOptions(query, options);

            var logs = await query.ToListAsync();

            var pageNum = options.Page ?? 1;
            var pageSize = options.Limit ?? 50;

            return Ok(new { total, page = pageNum, pageSize, data = logs });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetAttendanceRecords(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? staffId,
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] string? include)
    {
        try
        {
            var options = ParseQuery(Request.Query);

            var query = _db.AttendanceRecords.AsQueryable();

            // Apply domain-specific filters
            if (startDate.HasValue)
            {
                query = query.Where(a => a.AttendanceDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AttendanceDate <= endDate.Value);
            }

            if (staffId.HasValue)
            {
                query = query.Where(a => a.StaffId == staffId.Value);
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply query options (includes, sorting, pagination)
            query = BaseController<AttendanceRecord>.ApplyQueryOptions(query, options);

            var records = await query.ToListAsync();

            var pageNum = options.Page ?? 1;
            var pageSize = options.Limit ?? 50;

            return Ok(new { total, page = pageNum, pageSize, data = records });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("logs")]
    public async Task<IActionResult> CreatePunchLog([FromBody] PunchLog log)
    {
        try
        {
            log.LogId = Guid.NewGuid();
            log.CreatedAt = DateTime.UtcNow;
            log.ImportedAt = DateTime.UtcNow;

            _db.PunchLogs.Add(log);
            await _db.SaveChangesAsync();

            return Created($"/api/attendance/logs/{log.LogId}", new
            {
                punchLogId = log.LogId,
                punchType = log.PunchType,
                punchTime = log.PunchTime,
                staffId = log.StaffId,
                deviceId = log.DeviceId,
                verificationMode = log.VerificationMode,
                isValid = log.IsValid,
                createdAt = log.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
