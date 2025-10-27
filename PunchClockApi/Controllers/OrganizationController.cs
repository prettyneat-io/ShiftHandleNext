using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api")]
public sealed class OrganizationController : BaseController<object>
{
    private readonly PunchClockDbContext _db;

    public OrganizationController(PunchClockDbContext db, ILogger<OrganizationController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetAllDepartments(
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
            
            var query = _db.Departments.AsQueryable();

            // Apply default filter for active departments if not explicitly filtered
            if (isActive.HasValue)
            {
                query = query.Where(d => d.IsActive == isActive.Value);
            }
            else if (options.Where is null || !options.Where.ContainsKey("IsActive"))
            {
                query = query.Where(d => d.IsActive);
            }

            // Apply query options (filtering, sorting, pagination, includes)
            query = BaseController<Department>.ApplyQueryOptions(query, options);

            // Get total count before pagination
            var total = await _db.Departments
                .Where(d => isActive.HasValue ? d.IsActive == isActive.Value : d.IsActive)
                .CountAsync();

            var departments = await query.ToListAsync();

            // Return paginated response if page/limit specified
            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = departments
                });
            }

            return Ok(departments);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] Department department)
    {
        try
        {
            department.DepartmentId = Guid.NewGuid();
            department.CreatedAt = DateTime.UtcNow;
            department.UpdatedAt = DateTime.UtcNow;

            _db.Departments.Add(department);
            await _db.SaveChangesAsync();

            return Created($"/api/departments/{department.DepartmentId}", department);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("locations")]
    public async Task<IActionResult> GetAllLocations(
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
            
            var query = _db.Locations.AsQueryable();

            // Apply default filter for active locations if not explicitly filtered
            if (isActive.HasValue)
            {
                query = query.Where(l => l.IsActive == isActive.Value);
            }
            else if (options.Where is null || !options.Where.ContainsKey("IsActive"))
            {
                query = query.Where(l => l.IsActive);
            }

            // Apply query options (filtering, sorting, pagination, includes)
            query = BaseController<Location>.ApplyQueryOptions(query, options);

            // Get total count before pagination
            var total = await _db.Locations
                .Where(l => isActive.HasValue ? l.IsActive == isActive.Value : l.IsActive)
                .CountAsync();

            var locations = await query.ToListAsync();

            // Return paginated response if page/limit specified
            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = locations
                });
            }

            return Ok(locations);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("locations")]
    public async Task<IActionResult> CreateLocation([FromBody] Location location)
    {
        try
        {
            location.LocationId = Guid.NewGuid();
            location.CreatedAt = DateTime.UtcNow;
            location.UpdatedAt = DateTime.UtcNow;

            _db.Locations.Add(location);
            await _db.SaveChangesAsync();

            return Created($"/api/locations/{location.LocationId}", location);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
