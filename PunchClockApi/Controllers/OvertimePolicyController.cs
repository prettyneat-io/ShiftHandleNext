using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[Authorize]
[ApiController]
[Route("api/overtime-policies")]
public sealed class OvertimePolicyController : BaseController<OvertimePolicy>
{
    private readonly PunchClockDbContext _db;

    public OvertimePolicyController(PunchClockDbContext db, ILogger<OvertimePolicyController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPolicies(
        [FromQuery] int? page,
        [FromQuery] int? limit,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDefault)
    {
        try
        {
            var options = ParseQuery(Request.Query);
            var query = _db.OvertimePolicies.AsQueryable();

            // Apply filters
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }
            else if (options.Where is null || !options.Where.ContainsKey("IsActive"))
            {
                query = query.Where(p => p.IsActive);
            }

            if (isDefault.HasValue)
            {
                query = query.Where(p => p.IsDefault == isDefault.Value);
            }

            // Filter by effective dates
            var now = DateTime.UtcNow;
            query = query.Where(p => p.EffectiveFrom <= now && (p.EffectiveTo == null || p.EffectiveTo >= now));

            // Apply query options
            query = ApplyQueryOptions(query, options);

            var total = await query.CountAsync();
            var policies = await query.ToListAsync();

            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = policies
                });
            }

            return Ok(policies);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var policy = await _db.OvertimePolicies
                .Include(p => p.Shifts)
                .Include(p => p.Departments)
                .FirstOrDefaultAsync(p => p.PolicyId == id);

            return policy is not null ? Ok(policy) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultPolicy()
    {
        try
        {
            var now = DateTime.UtcNow;
            var policy = await _db.OvertimePolicies
                .FirstOrDefaultAsync(p => 
                    p.IsDefault && 
                    p.IsActive && 
                    p.EffectiveFrom <= now && 
                    (p.EffectiveTo == null || p.EffectiveTo >= now));

            return policy is not null ? Ok(policy) : NotFound(new { message = "No default overtime policy found" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OvertimePolicy policy)
    {
        try
        {
            policy.PolicyId = Guid.NewGuid();
            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;
            policy.CreatedBy = GetUserId();

            // If this is set as default, unset other defaults
            if (policy.IsDefault)
            {
                await UnsetOtherDefaults(policy.PolicyId);
            }

            _db.OvertimePolicies.Add(policy);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = policy.PolicyId }, policy);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OvertimePolicy updatedPolicy)
    {
        try
        {
            var policy = await _db.OvertimePolicies.FindAsync(id);
            if (policy is null) return NotFound();

            // Update fields
            policy.PolicyName = updatedPolicy.PolicyName;
            policy.PolicyCode = updatedPolicy.PolicyCode;
            policy.Description = updatedPolicy.Description;
            policy.DailyThreshold = updatedPolicy.DailyThreshold;
            policy.DailyMultiplier = updatedPolicy.DailyMultiplier;
            policy.ApplyWeeklyRule = updatedPolicy.ApplyWeeklyRule;
            policy.WeeklyThreshold = updatedPolicy.WeeklyThreshold;
            policy.WeeklyMultiplier = updatedPolicy.WeeklyMultiplier;
            policy.ApplyWeekendRule = updatedPolicy.ApplyWeekendRule;
            policy.WeekendMultiplier = updatedPolicy.WeekendMultiplier;
            policy.ApplyHolidayRule = updatedPolicy.ApplyHolidayRule;
            policy.HolidayMultiplier = updatedPolicy.HolidayMultiplier;
            policy.MaxDailyOvertime = updatedPolicy.MaxDailyOvertime;
            policy.MinimumOvertimeMinutes = updatedPolicy.MinimumOvertimeMinutes;
            policy.AutoApprovalThreshold = updatedPolicy.AutoApprovalThreshold;
            policy.EffectiveFrom = updatedPolicy.EffectiveFrom;
            policy.EffectiveTo = updatedPolicy.EffectiveTo;
            policy.IsActive = updatedPolicy.IsActive;
            policy.IsDefault = updatedPolicy.IsDefault;
            policy.UpdatedAt = DateTime.UtcNow;
            policy.UpdatedBy = GetUserId();

            // If this is set as default, unset other defaults
            if (policy.IsDefault)
            {
                await UnsetOtherDefaults(policy.PolicyId);
            }

            await _db.SaveChangesAsync();

            return Ok(policy);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var policy = await _db.OvertimePolicies.FindAsync(id);
            if (policy is null) return NotFound();

            // Check if it's assigned to any shifts or departments
            var shiftsCount = await _db.Shifts.CountAsync(s => s.OvertimePolicyId == id);
            var departmentsCount = await _db.Departments.CountAsync(d => d.OvertimePolicyId == id);

            if (shiftsCount > 0 || departmentsCount > 0)
            {
                return BadRequest(new 
                { 
                    message = "Cannot delete overtime policy that is assigned to shifts or departments",
                    shiftsCount,
                    departmentsCount
                });
            }

            // Soft delete
            policy.IsActive = false;
            policy.UpdatedAt = DateTime.UtcNow;
            policy.UpdatedBy = GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        try
        {
            var policy = await _db.OvertimePolicies.FindAsync(id);
            if (policy is null) return NotFound();

            if (!policy.IsActive)
            {
                return BadRequest(new { message = "Cannot set an inactive policy as default" });
            }

            // Unset all other defaults
            await UnsetOtherDefaults(policy.PolicyId);

            policy.IsDefault = true;
            policy.UpdatedAt = DateTime.UtcNow;
            policy.UpdatedBy = GetUserId();

            await _db.SaveChangesAsync();

            return Ok(new { message = $"Policy '{policy.PolicyName}' set as default", policyId = policy.PolicyId });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private async Task UnsetOtherDefaults(Guid excludePolicyId)
    {
        var otherDefaults = await _db.OvertimePolicies
            .Where(p => p.IsDefault && p.PolicyId != excludePolicyId)
            .ToListAsync();

        foreach (var policy in otherDefaults)
        {
            policy.IsDefault = false;
            policy.UpdatedAt = DateTime.UtcNow;
        }
    }
}
