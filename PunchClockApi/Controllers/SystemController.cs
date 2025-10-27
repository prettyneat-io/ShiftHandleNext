using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api")]
public sealed class SystemController : BaseController<object>
{
    private readonly PunchClockDbContext _db;

    public SystemController(PunchClockDbContext db, ILogger<SystemController> logger)
        : base(logger)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            await _db.Database.CanConnectAsync();
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return Ok(new { status = "unhealthy", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }
}
