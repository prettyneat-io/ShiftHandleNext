using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PunchClockApi.Services;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController : BaseController<Device>
{
    private readonly PunchClockDbContext _db;
    private readonly IDeviceService _deviceService;

    public DevicesController(
        PunchClockDbContext db, 
        IDeviceService deviceService,
        ILogger<DevicesController> logger)
        : base(logger)
    {
        _db = db;
        _deviceService = deviceService;
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
    public async Task<IActionResult> SyncDevice(Guid id, [FromQuery] string? type = "attendance")
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var syncLog = new SyncLog
            {
                SyncId = Guid.NewGuid(),
                DeviceId = id,
                SyncType = type?.ToUpper() ?? "ATTENDANCE",
                SyncStatus = "IN_PROGRESS",
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.SyncLogs.Add(syncLog);
            await _db.SaveChangesAsync();

            // Perform the sync based on type
            SyncResult result;
            if (type?.ToLower() == "staff")
            {
                result = await _deviceService.SyncStaffToDeviceAsync(id);
            }
            else
            {
                result = await _deviceService.SyncAttendanceFromDeviceAsync(id);
            }

            // Update sync log with results
            syncLog.SyncStatus = result.Success ? "SUCCESS" : "FAILED";
            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.RecordsProcessed = result.RecordsProcessed;
            syncLog.RecordsFailed = result.RecordsFailed;
            syncLog.ErrorDetails = result.Errors.Count > 0 
                ? string.Join("; ", result.Errors) 
                : null;
            
            await _db.SaveChangesAsync();

            return Ok(new
            {
                syncLog,
                result
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/connect")]
    public async Task<IActionResult> ConnectToDevice(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var result = await _deviceService.ConnectAsync(device);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/disconnect")]
    public async Task<IActionResult> DisconnectFromDevice(Guid id)
    {
        try
        {
            var result = await _deviceService.DisconnectAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}/users")]
    public async Task<IActionResult> GetDeviceUsers(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var result = await _deviceService.GetUsersAsync(device);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}/attendance")]
    public async Task<IActionResult> GetDeviceAttendance(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var result = await _deviceService.GetAttendanceAsync(device);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}/info")]
    public async Task<IActionResult> GetDeviceDetailedInfo(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var result = await _deviceService.GetDeviceInfoAsync(device);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:guid}/test-connection")]
    public async Task<IActionResult> TestDeviceConnection(Guid id)
    {
        try
        {
            var device = await _db.Devices.FindAsync(id);
            if (device is null) return NotFound();

            var isConnected = await _deviceService.TestConnectionAsync(device);
            return Ok(new
            {
                deviceId = id,
                isConnected,
                testedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{deviceId:guid}/staff/{staffId:guid}/enroll")]
    public async Task<IActionResult> EnrollStaffOnDevice(Guid deviceId, Guid staffId)
    {
        try
        {
            var device = await _db.Devices.FindAsync(deviceId);
            if (device is null) return NotFound("Device not found");

            var staff = await _db.Staff.FindAsync(staffId);
            if (staff is null) return NotFound("Staff not found");

            var result = await _deviceService.AddUserToDeviceAsync(device, staff);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
