using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/leave")]
[Authorize]
public sealed class LeaveController : BaseController<LeaveRequest>
{
    private readonly PunchClockDbContext _db;

    public LeaveController(PunchClockDbContext db, ILogger<LeaveController> logger)
        : base(logger)
    {
        _db = db;
    }

    #region Leave Types

    /// <summary>
    /// Get all leave types.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read
    /// Roles: Admin, HR Manager, Staff (all can view leave types)
    /// </remarks>
    [HttpGet("types")]
    [Authorize(Policy = "leave:read")]
    public async Task<IActionResult> GetLeaveTypes([FromQuery] bool? isActive)
    {
        try
        {
            var query = _db.LeaveTypes.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(lt => lt.IsActive == isActive.Value);
            }
            else
            {
                query = query.Where(lt => lt.IsActive);
            }

            var leaveTypes = await query.OrderBy(lt => lt.TypeName).ToListAsync();
            return Ok(leaveTypes);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get a specific leave type by ID.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read
    /// Roles: Admin, HR Manager, Staff
    /// </remarks>
    [HttpGet("types/{id:guid}")]
    [Authorize(Policy = "leave:read")]
    public async Task<IActionResult> GetLeaveTypeById(Guid id)
    {
        try
        {
            var leaveType = await _db.LeaveTypes.FindAsync(id);
            return leaveType is not null ? Ok(leaveType) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new leave type.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:create
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("types")]
    [Authorize(Policy = "leave:create")]
    public async Task<IActionResult> CreateLeaveType([FromBody] LeaveType leaveType)
    {
        try
        {
            leaveType.LeaveTypeId = Guid.NewGuid();
            leaveType.CreatedAt = DateTime.UtcNow;
            leaveType.UpdatedAt = DateTime.UtcNow;

            _db.LeaveTypes.Add(leaveType);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeaveTypeById), new { id = leaveType.LeaveTypeId }, leaveType);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Update an existing leave type.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPut("types/{id:guid}")]
    [Authorize(Policy = "leave:update")]
    public async Task<IActionResult> UpdateLeaveType(Guid id, [FromBody] LeaveType updatedLeaveType)
    {
        try
        {
            var leaveType = await _db.LeaveTypes.FindAsync(id);
            if (leaveType is null) return NotFound();

            leaveType.TypeName = updatedLeaveType.TypeName;
            leaveType.TypeCode = updatedLeaveType.TypeCode;
            leaveType.Description = updatedLeaveType.Description;
            leaveType.RequiresApproval = updatedLeaveType.RequiresApproval;
            leaveType.RequiresDocumentation = updatedLeaveType.RequiresDocumentation;
            leaveType.MaxDaysPerYear = updatedLeaveType.MaxDaysPerYear;
            leaveType.MinDaysNotice = updatedLeaveType.MinDaysNotice;
            leaveType.IsPaid = updatedLeaveType.IsPaid;
            leaveType.AllowsHalfDay = updatedLeaveType.AllowsHalfDay;
            leaveType.AllowsHourly = updatedLeaveType.AllowsHourly;
            leaveType.Color = updatedLeaveType.Color;
            leaveType.IsActive = updatedLeaveType.IsActive;
            leaveType.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(leaveType);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Delete (soft delete) a leave type.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpDelete("types/{id:guid}")]
    [Authorize(Policy = "leave:update")]
    public async Task<IActionResult> DeleteLeaveType(Guid id)
    {
        try
        {
            var leaveType = await _db.LeaveTypes.FindAsync(id);
            if (leaveType is null) return NotFound();

            // Soft delete
            leaveType.IsActive = false;
            leaveType.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    #endregion

    #region Leave Requests

    /// <summary>
    /// Get leave requests with optional filtering.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read (Admin/HR Manager) OR leave:view_own (Staff viewing own requests)
    /// </remarks>
    [HttpGet("requests")]
    [Authorize]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] Guid? staffId,
        [FromQuery] Guid? leaveTypeId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? page,
        [FromQuery] int? limit)
    {
        try
        {
            // Check permissions
            var hasReadPermission = HasPermission("leave", "read");
            var hasViewOwnPermission = HasPermission("leave", "view_own");
            
            if (!hasReadPermission && !hasViewOwnPermission)
            {
                return Forbid();
            }

            var options = ParseQuery(Request.Query);
            var query = _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.RequestedByUser)
                .Include(lr => lr.ReviewedByUser)
                .AsQueryable();

            // If user only has view_own permission, filter to their own requests
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

                query = query.Where(lr => lr.StaffId == userStaff.StaffId);
            }

            // Apply filters
            if (staffId.HasValue)
            {
                query = query.Where(lr => lr.StaffId == staffId.Value);
            }

            if (leaveTypeId.HasValue)
            {
                query = query.Where(lr => lr.LeaveTypeId == leaveTypeId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(lr => lr.Status == status.ToUpper());
            }

            if (startDate.HasValue)
            {
                query = query.Where(lr => lr.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(lr => lr.EndDate <= endDate.Value);
            }

            // Default ordering: most recent first
            query = query.OrderByDescending(lr => lr.RequestedAt);

            // Get total count
            var total = await query.CountAsync();

            // Apply pagination
            if (page.HasValue && limit.HasValue)
            {
                var skip = (page.Value - 1) * limit.Value;
                query = query.Skip(skip).Take(limit.Value);
            }

            var requests = await query.ToListAsync();

            if (page.HasValue && limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = page.Value,
                    pageSize = limit.Value,
                    data = requests
                });
            }

            return Ok(requests);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get a specific leave request by ID.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read (Admin/HR Manager) OR leave:view_own (Staff viewing own request)
    /// </remarks>
    [HttpGet("requests/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetLeaveRequestById(Guid id)
    {
        try
        {
            // Check permissions
            var hasReadPermission = HasPermission("leave", "read");
            var hasViewOwnPermission = HasPermission("leave", "view_own");
            
            if (!hasReadPermission && !hasViewOwnPermission)
            {
                return Forbid();
            }

            var request = await _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.RequestedByUser)
                .Include(lr => lr.ReviewedByUser)
                .Include(lr => lr.CancelledByUser)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            if (request is null) return NotFound();

            // If user only has view_own permission, verify they own this request
            if (hasViewOwnPermission && !hasReadPermission)
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                var userStaff = await _db.Staff
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value);
                
                if (userStaff is null || request.StaffId != userStaff.StaffId)
                {
                    return Forbid(); // Can only view their own requests
                }
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Submit a new leave request.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:create (Admin/HR Manager) OR leave:request_own (Staff requesting for themselves)
    /// </remarks>
    [HttpPost("requests")]
    [Authorize]
    public async Task<IActionResult> SubmitLeaveRequest([FromBody] SubmitLeaveRequestDto dto)
    {
        try
        {
            // Get current user ID from JWT claims (needed for multiple checks)
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Check permissions
            var hasCreatePermission = HasPermission("leave", "create");
            var hasRequestOwnPermission = HasPermission("leave", "request_own");
            
            if (!hasCreatePermission && !hasRequestOwnPermission)
            {
                return Forbid();
            }

            // If user only has request_own permission, verify they're requesting for themselves
            if (hasRequestOwnPermission && !hasCreatePermission)
            {
                var userStaff = await _db.Staff
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value);
                
                if (userStaff is null || dto.StaffId != userStaff.StaffId)
                {
                    return Forbid(); // Can only request leave for themselves
                }
            }

            // Validate staff exists
            var staff = await _db.Staff.FindAsync(dto.StaffId);
            if (staff is null)
            {
                return NotFound(new { message = "Staff member not found" });
            }

            // Validate leave type exists
            var leaveType = await _db.LeaveTypes.FindAsync(dto.LeaveTypeId);
            if (leaveType is null || !leaveType.IsActive)
            {
                return NotFound(new { message = "Leave type not found or inactive" });
            }

            // Validate dates
            if (dto.EndDate < dto.StartDate)
            {
                return BadRequest(new { message = "End date cannot be before start date" });
            }

            // Calculate total days
            var daysDiff = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1;
            var totalDays = dto.IsHalfDay ? 0.5m : daysDiff;

            // Check minimum notice period
            if (leaveType.MinDaysNotice.HasValue)
            {
                var noticeGiven = dto.StartDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
                if (noticeGiven < leaveType.MinDaysNotice.Value)
                {
                    return BadRequest(new { message = $"Minimum notice period of {leaveType.MinDaysNotice.Value} days required" });
                }
            }

            // Check for overlapping leave requests
            var hasOverlap = await _db.LeaveRequests
                .Where(lr => lr.StaffId == dto.StaffId)
                .Where(lr => lr.Status == "PENDING" || lr.Status == "APPROVED")
                .Where(lr => 
                    (lr.StartDate <= dto.EndDate && lr.EndDate >= dto.StartDate))
                .AnyAsync();

            if (hasOverlap)
            {
                return BadRequest(new { message = "Leave request overlaps with existing leave" });
            }

            // Check leave balance if applicable
            var currentYear = DateTime.UtcNow.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(lb => 
                    lb.StaffId == dto.StaffId && 
                    lb.LeaveTypeId == dto.LeaveTypeId && 
                    lb.Year == currentYear);

            if (balance is not null && balance.Available < totalDays)
            {
                return BadRequest(new 
                { 
                    message = "Insufficient leave balance",
                    available = balance.Available,
                    requested = totalDays
                });
            }

            // Create leave request (userId already declared at beginning of method)
            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = Guid.NewGuid(),
                StaffId = dto.StaffId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalDays = totalDays,
                TotalHours = dto.TotalHours,
                Reason = dto.Reason,
                SupportingDocuments = dto.SupportingDocuments,
                Status = leaveType.RequiresApproval ? "PENDING" : "APPROVED",
                RequestedBy = userId.Value,
                RequestedAt = DateTime.UtcNow,
                AffectsAttendance = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.LeaveRequests.Add(leaveRequest);

            // Update leave balance pending amount
            if (balance is not null)
            {
                balance.Pending += totalDays;
                balance.Available = balance.TotalAllocation + balance.CarryForward - balance.Used - balance.Pending;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Reload with navigation properties
            var createdRequest = await _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.RequestedByUser)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == leaveRequest.LeaveRequestId);

            return CreatedAtAction(nameof(GetLeaveRequestById), new { id = leaveRequest.LeaveRequestId }, createdRequest);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Approve a leave request.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:approve
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("requests/{id:guid}/approve")]
    [Authorize(Policy = "leave:approve")]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, [FromBody] ReviewLeaveRequestDto dto)
    {
        try
        {
            var request = await _db.LeaveRequests.FindAsync(id);
            if (request is null)
            {
                return NotFound(new { message = "Leave request not found" });
            }

            if (request.Status != "PENDING")
            {
                return BadRequest(new { message = $"Cannot approve request with status '{request.Status}'" });
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Update request status
            request.Status = "APPROVED";
            request.ReviewedBy = userId.Value;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewNotes = dto.Notes;
            request.UpdatedAt = DateTime.UtcNow;

            // Update leave balance: move from pending to used
            var currentYear = DateTime.UtcNow.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(lb => 
                    lb.StaffId == request.StaffId && 
                    lb.LeaveTypeId == request.LeaveTypeId && 
                    lb.Year == currentYear);

            if (balance is not null)
            {
                balance.Pending -= request.TotalDays;
                balance.Used += request.TotalDays;
                balance.Available = balance.TotalAllocation + balance.CarryForward - balance.Used - balance.Pending;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Reload with navigation properties
            var updatedRequest = await _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.ReviewedByUser)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            return Ok(updatedRequest);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Reject a leave request.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:reject
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("requests/{id:guid}/reject")]
    [Authorize(Policy = "leave:reject")]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, [FromBody] ReviewLeaveRequestDto dto)
    {
        try
        {
            var request = await _db.LeaveRequests.FindAsync(id);
            if (request is null)
            {
                return NotFound(new { message = "Leave request not found" });
            }

            if (request.Status != "PENDING")
            {
                return BadRequest(new { message = $"Cannot reject request with status '{request.Status}'" });
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Update request status
            request.Status = "REJECTED";
            request.ReviewedBy = userId.Value;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewNotes = dto.Notes;
            request.UpdatedAt = DateTime.UtcNow;

            // Update leave balance: release pending amount
            var currentYear = DateTime.UtcNow.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(lb => 
                    lb.StaffId == request.StaffId && 
                    lb.LeaveTypeId == request.LeaveTypeId && 
                    lb.Year == currentYear);

            if (balance is not null)
            {
                balance.Pending -= request.TotalDays;
                balance.Available = balance.TotalAllocation + balance.CarryForward - balance.Used - balance.Pending;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Reload with navigation properties
            var updatedRequest = await _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.ReviewedByUser)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            return Ok(updatedRequest);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Cancel a leave request.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:cancel
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("requests/{id:guid}/cancel")]
    [Authorize(Policy = "leave:cancel")]
    public async Task<IActionResult> CancelLeaveRequest(Guid id, [FromBody] CancelLeaveRequestDto dto)
    {
        try
        {
            var request = await _db.LeaveRequests.FindAsync(id);
            if (request is null)
            {
                return NotFound(new { message = "Leave request not found" });
            }

            if (request.Status != "PENDING" && request.Status != "APPROVED")
            {
                return BadRequest(new { message = $"Cannot cancel request with status '{request.Status}'" });
            }

            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Store previous status for balance adjustment
            var wasPending = request.Status == "PENDING";
            var wasApproved = request.Status == "APPROVED";

            // Update request status
            request.Status = "CANCELLED";
            request.CancelledBy = userId.Value;
            request.CancelledAt = DateTime.UtcNow;
            request.CancellationReason = dto.Reason;
            request.UpdatedAt = DateTime.UtcNow;

            // Update leave balance
            var currentYear = DateTime.UtcNow.Year;
            var balance = await _db.LeaveBalances
                .FirstOrDefaultAsync(lb => 
                    lb.StaffId == request.StaffId && 
                    lb.LeaveTypeId == request.LeaveTypeId && 
                    lb.Year == currentYear);

            if (balance is not null)
            {
                if (wasPending)
                {
                    balance.Pending -= request.TotalDays;
                }
                else if (wasApproved)
                {
                    balance.Used -= request.TotalDays;
                }
                
                balance.Available = balance.TotalAllocation + balance.CarryForward - balance.Used - balance.Pending;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Reload with navigation properties
            var updatedRequest = await _db.LeaveRequests
                .Include(lr => lr.Staff)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.CancelledByUser)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            return Ok(updatedRequest);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    #endregion

    #region Leave Balance

    /// <summary>
    /// Get leave balance for a staff member.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read
    /// Roles: Admin, HR Manager, Staff (can view own balance)
    /// </remarks>
    [HttpGet("balance/{staffId:guid}")]
    [Authorize(Policy = "leave:read")]
    public async Task<IActionResult> GetLeaveBalance(Guid staffId, [FromQuery] int? year)
    {
        try
        {
            var targetYear = year ?? DateTime.UtcNow.Year;

            var balances = await _db.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.StaffId == staffId && lb.Year == targetYear)
                .ToListAsync();

            return Ok(balances);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create or update leave balance for a staff member.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("balance")]
    [Authorize(Policy = "leave:update")]
    public async Task<IActionResult> CreateOrUpdateLeaveBalance([FromBody] LeaveBalanceDto dto)
    {
        try
        {
            var existingBalance = await _db.LeaveBalances
                .FirstOrDefaultAsync(lb => 
                    lb.StaffId == dto.StaffId && 
                    lb.LeaveTypeId == dto.LeaveTypeId && 
                    lb.Year == dto.Year);

            if (existingBalance is not null)
            {
                // Update existing balance
                existingBalance.TotalAllocation = dto.TotalAllocation;
                existingBalance.CarryForward = dto.CarryForward;
                existingBalance.Available = existingBalance.TotalAllocation + existingBalance.CarryForward - existingBalance.Used - existingBalance.Pending;
                existingBalance.UpdatedAt = DateTime.UtcNow;
                existingBalance.Notes = dto.Notes;

                await _db.SaveChangesAsync();
                return Ok(existingBalance);
            }
            else
            {
                // Create new balance
                var balance = new LeaveBalance
                {
                    LeaveBalanceId = Guid.NewGuid(),
                    StaffId = dto.StaffId,
                    LeaveTypeId = dto.LeaveTypeId,
                    Year = dto.Year,
                    TotalAllocation = dto.TotalAllocation,
                    CarryForward = dto.CarryForward,
                    Used = 0,
                    Pending = 0,
                    Available = dto.TotalAllocation + dto.CarryForward,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.LeaveBalances.Add(balance);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetLeaveBalance), new { staffId = dto.StaffId, year = dto.Year }, balance);
            }
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    #endregion

    #region Holidays

    /// <summary>
    /// Get holidays with optional filtering.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read
    /// Roles: Admin, HR Manager, Staff (all can view holidays)
    /// </remarks>
    [HttpGet("holidays")]
    [Authorize(Policy = "leave:read")]
    public async Task<IActionResult> GetHolidays(
        [FromQuery] int? year,
        [FromQuery] Guid? locationId,
        [FromQuery] bool? isActive)
    {
        try
        {
            var query = _db.Holidays
                .Include(h => h.Location)
                .AsQueryable();

            if (year.HasValue)
            {
                var startDate = new DateOnly(year.Value, 1, 1);
                var endDate = new DateOnly(year.Value, 12, 31);
                query = query.Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate);
            }

            if (locationId.HasValue)
            {
                query = query.Where(h => h.LocationId == locationId.Value || h.LocationId == null);
            }

            if (isActive.HasValue)
            {
                query = query.Where(h => h.IsActive == isActive.Value);
            }
            else
            {
                query = query.Where(h => h.IsActive);
            }

            var holidays = await query.OrderBy(h => h.HolidayDate).ToListAsync();
            return Ok(holidays);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get a specific holiday by ID.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:read
    /// Roles: Admin, HR Manager, Staff
    /// </remarks>
    [HttpGet("holidays/{id:guid}")]
    [Authorize(Policy = "leave:read")]
    public async Task<IActionResult> GetHolidayById(Guid id)
    {
        try
        {
            var holiday = await _db.Holidays
                .Include(h => h.Location)
                .FirstOrDefaultAsync(h => h.HolidayId == id);

            return holiday is not null ? Ok(holiday) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new holiday.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:create
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("holidays")]
    [Authorize(Policy = "leave:create")]
    public async Task<IActionResult> CreateHoliday([FromBody] Holiday holiday)
    {
        try
        {
            holiday.HolidayId = Guid.NewGuid();
            holiday.CreatedAt = DateTime.UtcNow;
            holiday.UpdatedAt = DateTime.UtcNow;

            _db.Holidays.Add(holiday);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHolidayById), new { id = holiday.HolidayId }, holiday);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Update an existing holiday.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPut("holidays/{id:guid}")]
    [Authorize(Policy = "leave:update")]
    public async Task<IActionResult> UpdateHoliday(Guid id, [FromBody] Holiday updatedHoliday)
    {
        try
        {
            var holiday = await _db.Holidays.FindAsync(id);
            if (holiday is null) return NotFound();

            holiday.HolidayName = updatedHoliday.HolidayName;
            holiday.HolidayDate = updatedHoliday.HolidayDate;
            holiday.LocationId = updatedHoliday.LocationId;
            holiday.IsRecurring = updatedHoliday.IsRecurring;
            holiday.IsMandatory = updatedHoliday.IsMandatory;
            holiday.IsPaid = updatedHoliday.IsPaid;
            holiday.Description = updatedHoliday.Description;
            holiday.IsActive = updatedHoliday.IsActive;
            holiday.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(holiday);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Delete (soft delete) a holiday.
    /// </summary>
    /// <remarks>
    /// Required Permission: leave:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpDelete("holidays/{id:guid}")]
    [Authorize(Policy = "leave:update")]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        try
        {
            var holiday = await _db.Holidays.FindAsync(id);
            if (holiday is null) return NotFound();

            // Soft delete
            holiday.IsActive = false;
            holiday.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    #endregion
}

#region DTOs

public record SubmitLeaveRequestDto(
    Guid StaffId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    bool IsHalfDay = false,
    TimeSpan? TotalHours = null,
    string? SupportingDocuments = null
);

public record ReviewLeaveRequestDto(string? Notes);

public record CancelLeaveRequestDto(string Reason);

public record LeaveBalanceDto(
    Guid StaffId,
    Guid LeaveTypeId,
    int Year,
    decimal TotalAllocation,
    decimal CarryForward = 0,
    string? Notes = null
);

#endregion
