using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController : BaseController<Device>
{
    private readonly PunchClockDbContext _db;

    public DevicesController(PunchClockDbContext db, ILogger<DevicesController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDevices(
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
            
            var query = _db.Devices.AsQueryable();

            // Apply default filter for active devices if not explicitly filtered
            if (isActive.HasValue)
            {
                query = query.Where(d => d.IsActive == isActive.Value);
            }
            else if (options.Where is null || !options.Where.ContainsKey("IsActive"))
            {
                query = query.Where(d => d.IsActive);
            }

            // Apply query options (filtering, sorting, pagination, includes)
            query = ApplyQueryOptions(query, options);

            // Get total count before pagination
            var total = await _db.Devices
                .Where(d => isActive.HasValue ? d.IsActive == isActive.Value : d.IsActive)
                .CountAsync();

            var devices = await query.ToListAsync();

            // Return paginated response if page/limit specified
            if (options.Page.HasValue && options.Limit.HasValue)
            {
                return Ok(new
                {
                    total,
                    page = options.Page.Value,
                    pageSize = options.Limit.Value,
                    data = devices
                });
            }

            return Ok(devices);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeviceById(Guid id)
    {
        try
        {
            var device = await _db.Devices
                .Include(d => d.Location)
                .Include(d => d.DeviceEnrollments)
                .FirstOrDefaultAsync(d => d.DeviceId == id);

            return device is not null ? Ok(device) : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDevice([FromBody] Device device)
    {
        try
        {
            device.DeviceId = Guid.NewGuid();
            device.CreatedAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            _db.Devices.Add(device);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeviceById), new { id = device.DeviceId }, device);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] Device updatedDevice)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            device.DeviceName = updatedDevice.DeviceName;
            device.DeviceModel = updatedDevice.DeviceModel;
            device.IpAddress = updatedDevice.IpAddress;
            device.Port = updatedDevice.Port;
            device.LocationId = updatedDevice.LocationId;
            device.IsActive = updatedDevice.IsActive;
            device.IsOnline = updatedDevice.IsOnline;
            device.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(device);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> SyncDevice(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var syncLog = new SyncLog
            {
                SyncId = Guid.NewGuid(),
                DeviceId = id,
                SyncType = "MANUAL",
                SyncStatus = "IN_PROGRESS",
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.SyncLogs.Add(syncLog);
            await _db.SaveChangesAsync();

            // TODO: Implement actual device sync logic here

            return Accepted($"/api/sync-logs/{syncLog.SyncId}", syncLog);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
