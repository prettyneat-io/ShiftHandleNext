using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PunchClockApi.Services;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController : BaseController<object>
{
    private readonly PunchClockDbContext _db;
    private readonly AttendanceProcessingService _attendanceService;

    public AttendanceController(
        PunchClockDbContext db, 
        AttendanceProcessingService attendanceService,
        ILogger<AttendanceController> logger)
        : base(logger)
    {
        _db = db;
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Get punch logs with optional filtering.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:read (Admin/HR Manager) OR attendance:view_own (Staff viewing own records)
    /// </remarks>
    [HttpGet("logs")]
    [Authorize]
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
            // Check permissions
            var hasReadPermission = HasPermission("attendance", "read");
            var hasViewOwnPermission = HasPermission("attendance", "view_own");
            
            if (!hasReadPermission && !hasViewOwnPermission)
            {
                return Forbid();
            }

            var options = ParseQuery(Request.Query);

            var query = _db.PunchLogs.AsQueryable();

            // If user only has view_own permission, filter to their own records
            if (hasViewOwnPermission && !hasReadPermission)
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                var userStaff = await _db.Staff
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value);
                
                if (userStaff is null)
                {
                    return NotFound(new { error = "No staff record linked to your account" });
                }

                query = query.Where(p => p.StaffId == userStaff.StaffId);
            }

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

    /// <summary>
    /// Get attendance records with optional filtering.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:read (Admin/HR Manager) OR attendance:view_own (Staff viewing own records)
    /// </remarks>
    [HttpGet("records")]
    [Authorize]
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
            // Check permissions
            var hasReadPermission = HasPermission("attendance", "read");
            var hasViewOwnPermission = HasPermission("attendance", "view_own");
            
            if (!hasReadPermission && !hasViewOwnPermission)
            {
                return Forbid();
            }

            var options = ParseQuery(Request.Query);

            var query = _db.AttendanceRecords.AsQueryable();

            // If user only has view_own permission, filter to their own records
            if (hasViewOwnPermission && !hasReadPermission)
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                var userStaff = await _db.Staff
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value);
                
                if (userStaff is null)
                {
                    return NotFound(new { error = "No staff record linked to your account" });
                }

                query = query.Where(a => a.StaffId == userStaff.StaffId);
            }

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

    /// <summary>
    /// Create a new punch log manually.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("logs")]
    [Authorize(Policy = "attendance:update")]
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

    /// <summary>
    /// Get attendance corrections with optional filtering.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("corrections")]
    [Authorize(Policy = "attendance:read")]
    public async Task<IActionResult> GetCorrections(
        [FromQuery] Guid? staffId,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? limit)
    {
        try
        {
            var query = _db.AttendanceCorrections
                .Include(c => c.Staff)
                .Include(c => c.RequestedByUser)
                .Include(c => c.ReviewedByUser)
                .AsQueryable();

            if (staffId.HasValue)
            {
                query = query.Where(c => c.StaffId == staffId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status.ToUpper());
            }

            query = query.OrderByDescending(c => c.RequestedAt);

            var total = await query.CountAsync();

            var pageNum = page ?? 1;
            var pageSize = limit ?? 50;
            query = query.Skip((pageNum - 1) * pageSize).Take(pageSize);

            var corrections = await query.ToListAsync();

            return Ok(new { total, page = pageNum, pageSize, data = corrections });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get a specific correction by ID.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("corrections/{id:guid}")]
    [Authorize(Policy = "attendance:read")]
    public async Task<IActionResult> GetCorrectionById(Guid id)
    {
        try
        {
            var correction = await _db.AttendanceCorrections
                .Include(c => c.Staff)
                .Include(c => c.Record)
                .Include(c => c.RequestedByUser)
                .Include(c => c.ReviewedByUser)
                .FirstOrDefaultAsync(c => c.CorrectionId == id);

            if (correction == null)
            {
                return NotFound(new { message = "Correction not found" });
            }

            return Ok(correction);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new attendance correction request.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("corrections")]
    [Authorize(Policy = "attendance:update")]
    public async Task<IActionResult> CreateCorrection([FromBody] AttendanceCorrection correction)
    {
        try
        {
            // Validate the attendance record exists
            var record = await _db.AttendanceRecords
                .FirstOrDefaultAsync(r => r.RecordId == correction.RecordId);

            if (record == null)
            {
                return NotFound(new { message = "Attendance record not found" });
            }

            // Set audit fields
            correction.CorrectionId = Guid.NewGuid();
            correction.StaffId = record.StaffId;
            correction.AttendanceDate = record.AttendanceDate;
            correction.OriginalClockIn = record.ClockIn;
            correction.OriginalClockOut = record.ClockOut;
            correction.Status = "PENDING";
            correction.RequestedBy = GetUserId() ?? Guid.Empty;
            correction.RequestedAt = DateTime.UtcNow;
            correction.CreatedAt = DateTime.UtcNow;
            correction.UpdatedAt = DateTime.UtcNow;

            _db.AttendanceCorrections.Add(correction);
            await _db.SaveChangesAsync();

            return Created($"/api/attendance/corrections/{correction.CorrectionId}", correction);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Approve an attendance correction request.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("corrections/{id:guid}/approve")]
    [Authorize(Policy = "attendance:update")]
    public async Task<IActionResult> ApproveCorrection(Guid id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var correction = await _db.AttendanceCorrections
                .Include(c => c.Record)
                .Include(c => c.Staff)
                .FirstOrDefaultAsync(c => c.CorrectionId == id);

            if (correction == null)
            {
                return NotFound(new { message = "Correction not found" });
            }

            if (correction.Status != "PENDING")
            {
                return BadRequest(new { message = $"Cannot approve correction with status: {correction.Status}" });
            }

            // Update correction status
            correction.Status = "APPROVED";
            correction.ReviewedBy = GetUserId() ?? Guid.Empty;
            correction.ReviewedAt = DateTime.UtcNow;
            correction.ReviewNotes = request.Notes;
            correction.UpdatedAt = DateTime.UtcNow;

            // Apply the correction to the attendance record
            var record = correction.Record;
            record.ClockIn = correction.CorrectedClockIn ?? record.ClockIn;
            record.ClockOut = correction.CorrectedClockOut ?? record.ClockOut;
            record.UpdatedAt = DateTime.UtcNow;
            record.ModifiedBy = GetUserId();

            await _db.SaveChangesAsync();

            // Reprocess attendance with the AttendanceProcessingService
            var reprocessedRecord = await _attendanceService.ProcessDailyAttendance(
                correction.StaffId,
                correction.AttendanceDate.ToDateTime(TimeOnly.MinValue)
            );

            return Ok(new 
            { 
                message = "Correction approved and attendance reprocessed", 
                correction = correction,
                updatedRecord = reprocessedRecord
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Reject an attendance correction request.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("corrections/{id:guid}/reject")]
    [Authorize(Policy = "attendance:update")]
    public async Task<IActionResult> RejectCorrection(Guid id, [FromBody] ApprovalRequest request)
    {
        try
        {
            var correction = await _db.AttendanceCorrections
                .FirstOrDefaultAsync(c => c.CorrectionId == id);

            if (correction == null)
            {
                return NotFound(new { message = "Correction not found" });
            }

            if (correction.Status != "PENDING")
            {
                return BadRequest(new { message = $"Cannot reject correction with status: {correction.Status}" });
            }

            correction.Status = "REJECTED";
            correction.ReviewedBy = GetUserId() ?? Guid.Empty;
            correction.ReviewedAt = DateTime.UtcNow;
            correction.ReviewNotes = request.Notes ?? "No reason provided";
            correction.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Correction rejected", correction });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Bulk approve or reject multiple attendance correction requests.
    /// </summary>
    /// <remarks>
    /// Required Permission: attendance:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("corrections/bulk-approve")]
    [Authorize(Policy = "attendance:update")]
    public async Task<IActionResult> BulkApproveCorrections([FromBody] BulkCorrectionRequest request)
    {
        try
        {
            var corrections = await _db.AttendanceCorrections
                .Include(c => c.Record)
                .Include(c => c.Staff)
                .Where(c => request.CorrectionIds.Contains(c.CorrectionId))
                .ToListAsync();

            if (corrections.Count != request.CorrectionIds.Count)
            {
                var foundIds = corrections.Select(c => c.CorrectionId).ToHashSet();
                var missingIds = request.CorrectionIds.Except(foundIds).ToList();
                return BadRequest(new 
                { 
                    message = "Some corrections not found",
                    missingCorrectionIds = missingIds
                });
            }

            var results = new List<BulkCorrectionResult>();
            var reviewerId = GetUserId() ?? Guid.Empty;
            var now = DateTime.UtcNow;

            foreach (var correction in corrections)
            {
                if (correction.Status != "PENDING")
                {
                    results.Add(new BulkCorrectionResult(
                        correction.CorrectionId,
                        correction.Status,
                        false,
                        $"Cannot {request.Action} correction with status: {correction.Status}"
                    ));
                    continue;
                }

                try
                {
                    if (request.Action.ToUpper() == "APPROVE")
                    {
                        // Update correction status
                        correction.Status = "APPROVED";
                        correction.ReviewedBy = reviewerId;
                        correction.ReviewedAt = now;
                        correction.ReviewNotes = request.Notes;
                        correction.UpdatedAt = now;

                        // Apply the correction to the attendance record
                        var record = correction.Record;
                        record.ClockIn = correction.CorrectedClockIn ?? record.ClockIn;
                        record.ClockOut = correction.CorrectedClockOut ?? record.ClockOut;
                        record.UpdatedAt = now;
                        record.ModifiedBy = reviewerId;

                        await _db.SaveChangesAsync();

                        // Reprocess attendance
                        await _attendanceService.ProcessDailyAttendance(
                            correction.StaffId,
                            correction.AttendanceDate.ToDateTime(TimeOnly.MinValue)
                        );

                        results.Add(new BulkCorrectionResult(
                            correction.CorrectionId,
                            "APPROVED",
                            true,
                            "Correction approved and attendance reprocessed"
                        ));
                    }
                    else if (request.Action.ToUpper() == "REJECT")
                    {
                        correction.Status = "REJECTED";
                        correction.ReviewedBy = reviewerId;
                        correction.ReviewedAt = now;
                        correction.ReviewNotes = request.Notes ?? "No reason provided";
                        correction.UpdatedAt = now;

                        await _db.SaveChangesAsync();

                        results.Add(new BulkCorrectionResult(
                            correction.CorrectionId,
                            "REJECTED",
                            true,
                            "Correction rejected"
                        ));
                    }
                    else
                    {
                        results.Add(new BulkCorrectionResult(
                            correction.CorrectionId,
                            correction.Status,
                            false,
                            $"Invalid action: {request.Action}. Must be APPROVE or REJECT"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new BulkCorrectionResult(
                        correction.CorrectionId,
                        correction.Status,
                        false,
                        $"Error processing correction: {ex.Message}"
                    ));
                }
            }

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            return Ok(new 
            { 
                message = $"Processed {results.Count} corrections: {successCount} succeeded, {failureCount} failed",
                action = request.Action.ToUpper(),
                successCount,
                failureCount,
                results
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}

public record ApprovalRequest(string? Notes);

public record BulkCorrectionRequest(List<Guid> CorrectionIds, string Action, string? Notes);

public record BulkCorrectionResult(Guid CorrectionId, string Status, bool Success, string Message);
