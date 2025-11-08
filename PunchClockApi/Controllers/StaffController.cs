using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PunchClockApi.Services;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/staff")]
public sealed class StaffController : BaseController<Staff>
{
    private readonly PunchClockDbContext _db;
    private readonly IStaffImportExportService _importExportService;

    public StaffController(
        PunchClockDbContext db, 
        IStaffImportExportService importExportService,
        ILogger<StaffController> logger)
        : base(logger)
    {
        _db = db;
        _importExportService = importExportService;
    }

    /// <summary>
    /// Get all staff with optional filtering, sorting, and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "staff:read")]
    public async Task<IActionResult> GetAllStaff(
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
            
            var query = _db.Staff.AsQueryable();

            // Apply default filter for active staff if not explicitly filtered
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

            // Get total count before pagination
            var total = await _db.Staff
                .Where(s => isActive.HasValue ? s.IsActive == isActive.Value : s.IsActive)
                .CountAsync();

            var staff = await query.ToListAsync();

            // Return paginated response if page/limit specified
            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = staff
                });
            }

            return Ok(staff);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get a specific staff member by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "staff:read")]
    public async Task<IActionResult> GetStaffById(Guid id)
    {
        try
        {
            var staff = await _db.Staff
                .Include(s => s.Department)
                .Include(s => s.Location)
                .Include(s => s.BiometricTemplates)
                .Include(s => s.DeviceEnrollments)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            return staff is not null ? Ok(staff) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create a new staff member
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "staff:create")]
    public async Task<IActionResult> CreateStaff([FromBody] Staff staff)
    {
        try
        {
            staff.StaffId = Guid.NewGuid();
            staff.CreatedAt = DateTime.UtcNow;
            staff.UpdatedAt = DateTime.UtcNow;

            _db.Staff.Add(staff);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStaffById), new { id = staff.StaffId }, staff);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Update an existing staff member
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "staff:update")]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] Staff updatedStaff)
    {
        try
        {
            var staff = await _db.Staff.FindAsync(id);
            if (staff is null) return NotFound();

            staff.FirstName = updatedStaff.FirstName;
            staff.LastName = updatedStaff.LastName;
            staff.MiddleName = updatedStaff.MiddleName;
            staff.Email = updatedStaff.Email;
            staff.Phone = updatedStaff.Phone;
            staff.Mobile = updatedStaff.Mobile;
            staff.BadgeNumber = updatedStaff.BadgeNumber;
            staff.DepartmentId = updatedStaff.DepartmentId;
            staff.LocationId = updatedStaff.LocationId;
            staff.PositionTitle = updatedStaff.PositionTitle;
            staff.EmploymentType = updatedStaff.EmploymentType;
            staff.IsActive = updatedStaff.IsActive;
            staff.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(staff);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Soft delete a staff member (sets IsActive = false)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "staff:delete")]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        try
        {
            var staff = await _db.Staff.FindAsync(id);
            if (staff is null) return NotFound();

            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Export staff to CSV
    /// </summary>
    /// <param name="includeInactive">Include inactive staff in export</param>
    /// <returns>CSV file download</returns>
    [HttpGet("export/csv")]
    [Authorize(Policy = "staff:export")]
    public async Task<IActionResult> ExportStaffToCsv([FromQuery] bool includeInactive = false)
    {
        try
        {
            var csvBytes = await _importExportService.ExportStaffToCsvAsync(includeInactive);
            var fileName = $"staff_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            
            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Import staff from CSV file
    /// </summary>
    /// <param name="file">CSV file with staff data</param>
    /// <param name="updateExisting">Update existing staff records if EmployeeId matches</param>
    /// <returns>Import results with success/error details</returns>
    [HttpPost("import/csv")]
    [Authorize(Policy = "staff:import")]
    public async Task<IActionResult> ImportStaffFromCsv(
        IFormFile? file,
        [FromQuery] bool updateExisting = false)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV file" });
            }

            using var stream = file.OpenReadStream();
            var result = await _importExportService.ImportStaffFromCsvAsync(stream, updateExisting);

            if (result.HasErrors)
            {
                return Ok(new
                {
                    success = false,
                    message = $"Import completed with {result.ErrorCount} error(s)",
                    totalRows = result.TotalRows,
                    successCount = result.SuccessCount,
                    errorCount = result.ErrorCount,
                    errors = result.Errors,
                    successfulImports = result.SuccessfulImports
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully imported {result.SuccessCount} staff record(s)",
                totalRows = result.TotalRows,
                successCount = result.SuccessCount,
                errorCount = result.ErrorCount,
                successfulImports = result.SuccessfulImports
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Validate CSV import without saving to database
    /// </summary>
    /// <param name="file">CSV file with staff data</param>
    /// <returns>Validation results</returns>
    [HttpPost("import/csv/validate")]
    [Authorize(Policy = "staff:import")]
    public async Task<IActionResult> ValidateStaffImport(IFormFile? file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV file" });
            }

            using var stream = file.OpenReadStream();
            var result = await _importExportService.ValidateStaffImportAsync(stream);

            return Ok(new
            {
                valid = !result.HasErrors,
                message = result.HasErrors 
                    ? $"Validation failed with {result.ErrorCount} error(s)"
                    : $"All {result.SuccessCount} row(s) are valid",
                totalRows = result.TotalRows,
                validRows = result.SuccessCount,
                errorCount = result.ErrorCount,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Link a User account to a Staff record
    /// Allows HR Managers to grant system access to staff members
    /// </summary>
    /// <param name="staffId">Staff ID to link user to</param>
    /// <param name="request">Request containing UserId to link</param>
    [HttpPost("{staffId:guid}/assign-user")]
    [Authorize(Policy = "staff:assign_user")]
    public async Task<IActionResult> AssignUserToStaff(Guid staffId, [FromBody] AssignUserRequest request)
    {
        try
        {
            // Validate staff exists
            var staff = await _db.Staff.FindAsync(staffId);
            if (staff is null)
            {
                return NotFound(new { error = "Staff not found" });
            }

            // Validate user exists
            var user = await _db.Users.FindAsync(request.UserId);
            if (user is null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Check if user already linked to another staff
            var existingLink = await _db.Staff.AnyAsync(s => s.UserId == request.UserId && s.StaffId != staffId);
            if (existingLink)
            {
                return Conflict(new { error = "User already linked to a different staff record" });
            }

            // Link user to staff
            staff.UserId = request.UserId;
            staff.UpdatedAt = DateTime.UtcNow;
            staff.UpdatedBy = GetUserId();

            await _db.SaveChangesAsync();

            Logger.LogInformation("User {UserId} linked to Staff {StaffId} by {PerformedBy}", 
                request.UserId, staffId, GetUserId());

            return Ok(new 
            { 
                success = true,
                message = "User successfully linked to staff record",
                staffId = staff.StaffId,
                userId = staff.UserId
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get the linked Staff ID for the current authenticated user
    /// </summary>
    private async Task<Guid?> GetLinkedStaffIdAsync()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return null;

        var staff = await _db.Staff
            .FirstOrDefaultAsync(s => s.UserId == userId.Value);
        return staff?.StaffId;
    }
}

public sealed record AssignUserRequest(Guid UserId);

