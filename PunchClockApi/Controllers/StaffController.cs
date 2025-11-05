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

    [HttpGet]
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

    [HttpGet("{id:guid}")]
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

    [HttpPost]
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

    [HttpPut("{id:guid}")]
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

    [HttpDelete("{id:guid}")]
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
}
