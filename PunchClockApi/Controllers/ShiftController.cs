using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/shifts")]
public sealed class ShiftController : BaseController<Shift>
{
    private readonly PunchClockDbContext _db;

    public ShiftController(PunchClockDbContext db, ILogger<ShiftController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllShifts(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] string? include,
        [FromQuery] bool? isActive)
    {
        try
        {
            var options = ParseQuery(Request.Query);
            
            var query = _db.Shifts.AsQueryable();

            // Apply default filter for active shifts if not explicitly filtered
            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }
            else if (options.Where is null || !options.Where.ContainsKey("IsActive"))
            {
                query = query.Where(s => s.IsActive);
            }

            // Apply query options (filtering, sorting, pagination, includes)
            query = ApplyQueryOptions(query, options);

            // Get total count
            var total = await query.CountAsync();

            var shifts = await query.ToListAsync();

            // Return paginated response if page/limit specified
            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = shifts
                });
            }

            return Ok(shifts);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetShiftById(Guid id)
    {
        try
        {
            var shift = await _db.Shifts
                .Include(s => s.StaffMembers)
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            return shift is not null ? Ok(shift) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateShift([FromBody] Shift shift)
    {
        try
        {
            shift.ShiftId = Guid.NewGuid();
            shift.CreatedAt = DateTime.UtcNow;
            shift.UpdatedAt = DateTime.UtcNow;

            _db.Shifts.Add(shift);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShiftById), new { id = shift.ShiftId }, shift);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateShift(Guid id, [FromBody] Shift updatedShift)
    {
        try
        {
            var shift = await _db.Shifts.FindAsync(id);
            if (shift is null) return NotFound();

            shift.ShiftName = updatedShift.ShiftName;
            shift.ShiftCode = updatedShift.ShiftCode;
            shift.StartTime = updatedShift.StartTime;
            shift.EndTime = updatedShift.EndTime;
            shift.RequiredHours = updatedShift.RequiredHours;
            shift.GracePeriodMinutes = updatedShift.GracePeriodMinutes;
            shift.LateThresholdMinutes = updatedShift.LateThresholdMinutes;
            shift.EarlyLeaveThresholdMinutes = updatedShift.EarlyLeaveThresholdMinutes;
            shift.HasBreak = updatedShift.HasBreak;
            shift.BreakDuration = updatedShift.BreakDuration;
            shift.BreakStartTime = updatedShift.BreakStartTime;
            shift.AutoDeductBreak = updatedShift.AutoDeductBreak;
            shift.IsActive = updatedShift.IsActive;
            shift.Description = updatedShift.Description;
            shift.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(shift);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteShift(Guid id)
    {
        try
        {
            var shift = await _db.Shifts.FindAsync(id);
            if (shift is null) return NotFound();

            // Soft delete
            shift.IsActive = false;
            shift.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("assign-staff")]
    public async Task<IActionResult> AssignStaffToShift([FromBody] BulkShiftAssignmentRequest request)
    {
        try
        {
            // Validate shift exists
            var shift = await _db.Shifts.FindAsync(request.ShiftId);
            if (shift is null)
            {
                return NotFound(new { message = "Shift not found" });
            }

            if (!shift.IsActive)
            {
                return BadRequest(new { message = "Cannot assign staff to an inactive shift" });
            }

            // Validate all staff members exist
            var staffMembers = await _db.Staff
                .Where(s => request.StaffIds.Contains(s.StaffId))
                .ToListAsync();

            if (staffMembers.Count != request.StaffIds.Count)
            {
                var foundIds = staffMembers.Select(s => s.StaffId).ToHashSet();
                var missingIds = request.StaffIds.Except(foundIds).ToList();
                return BadRequest(new 
                { 
                    message = "Some staff members not found",
                    missingStaffIds = missingIds
                });
            }

            // Update shift assignments
            var updatedCount = 0;
            foreach (var staff in staffMembers)
            {
                staff.ShiftId = request.ShiftId;
                staff.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _db.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Successfully assigned {updatedCount} staff members to shift '{shift.ShiftName}'",
                shiftId = request.ShiftId,
                shiftName = shift.ShiftName,
                assignedStaffIds = request.StaffIds,
                count = updatedCount
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("unassign-staff/{staffId:guid}")]
    public async Task<IActionResult> UnassignStaffFromShift(Guid staffId)
    {
        try
        {
            var staff = await _db.Staff.FindAsync(staffId);
            if (staff is null)
            {
                return NotFound(new { message = "Staff member not found" });
            }

            if (staff.ShiftId is null)
            {
                return BadRequest(new { message = "Staff member is not assigned to any shift" });
            }

            staff.ShiftId = null;
            staff.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Successfully unassigned staff member from shift",
                staffId = staffId
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}

public record BulkShiftAssignmentRequest(Guid ShiftId, List<Guid> StaffIds);
