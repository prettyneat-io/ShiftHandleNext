using Microsoft.AspNetCore.Authorization;
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

    /// <summary>
    /// Get all devices with optional filtering and pagination.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Get a specific device by ID with related data.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Create a new device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:create
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "devices:create")]
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

    /// <summary>
    /// Update an existing device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:update
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "devices:update")]
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

    /// <summary>
    /// Sync device data (attendance or staff).
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:sync
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = "devices:sync")]
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

    /// <summary>
    /// Connect to a device to test connectivity.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("{id:guid}/connect")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Disconnect from a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("{id:guid}/disconnect")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Get all users enrolled on a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("{id:guid}/users")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Get attendance records from a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("{id:guid}/attendance")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Get detailed information about a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpGet("{id:guid}/info")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Test connection to a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:read
    /// Roles: Admin, HR Manager
    /// </remarks>
    [HttpPost("{id:guid}/test-connection")]
    [Authorize(Policy = "devices:read")]
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

    /// <summary>
    /// Enroll a staff member on a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:enroll (Admin/HR Manager) OR devices:self_enroll (Staff)
    /// 
    /// Staff Self-Enrollment Rules:
    /// - Must have a linked User account (UserId not null)
    /// - Can only enroll their own Staff record
    /// - Can only enroll to devices at their assigned location
    /// - Cannot be set as device administrator
    /// </remarks>
    [HttpPost("{deviceId:guid}/staff/{staffId:guid}/enroll")]
    [Authorize] // Require authentication, check permissions manually
    public async Task<IActionResult> EnrollStaffOnDevice(Guid deviceId, Guid staffId)
    {
        try
        {
            // Check permissions: either devices:enroll OR devices:self_enroll
            var hasEnrollPermission = HasPermission("devices", "enroll");
            var hasSelfEnrollPermission = HasPermission("devices", "self_enroll");
            
            if (!hasEnrollPermission && !hasSelfEnrollPermission)
            {
                return Forbid();
            }

            var device = await _db.Devices.FindAsync(deviceId);
            if (device is null) return NotFound("Device not found");

            var staff = await _db.Staff
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);
            if (staff is null) return NotFound("Staff not found");

            // Check if user is Staff role attempting self-enrollment
            if (hasSelfEnrollPermission && !hasEnrollPermission)
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                // Business Rule 1: Staff must have linked User account
                if (!staff.UserId.HasValue || staff.UserId.Value != userId.Value)
                {
                    return Forbid(); // Can only enroll themselves
                }

                // Business Rule 2: Staff must be assigned to the device's location
                if (staff.LocationId != device.LocationId)
                {
                    return BadRequest(new 
                    { 
                        error = "Can only enroll to devices at your assigned location",
                        yourLocation = staff.LocationId,
                        deviceLocation = device.LocationId
                    });
                }

                // Business Rule 3: Staff cannot be set as device admin (pass canBeAdmin: false)
                var staffResult = await _deviceService.AddUserToDeviceAsync(device, staff, canBeAdmin: false);
                return Ok(staffResult);
            }

            // Admin and HR Manager can enroll anyone with admin privileges
            var result = await _deviceService.AddUserToDeviceAsync(device, staff, canBeAdmin: true);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Enroll a staff member's fingerprint on a device.
    /// </summary>
    /// <remarks>
    /// Required Permission: devices:enroll (Admin/HR Manager) OR devices:self_enroll (Staff)
    /// 
    /// Staff Self-Enrollment Rules (same as staff enrollment):
    /// - Must have a linked User account
    /// - Can only enroll their own fingerprint
    /// - Can only enroll to devices at their assigned location
    /// - Cannot be set as device administrator
    /// </remarks>
    [HttpPost("{deviceId:guid}/staff/{staffId:guid}/enroll-fingerprint")]
    [Authorize] // Require authentication, check permissions manually
    public async Task<IActionResult> EnrollStaffFingerprint(
        Guid deviceId, 
        Guid staffId,
        [FromQuery] int fingerId = 0)
    {
        try
        {
            if (fingerId < 0 || fingerId > 9)
            {
                return BadRequest(new { error = "Finger ID must be between 0 and 9" });
            }

            // Check permissions: either devices:enroll OR devices:self_enroll
            var hasEnrollPermission = HasPermission("devices", "enroll");
            var hasSelfEnrollPermission = HasPermission("devices", "self_enroll");
            
            if (!hasEnrollPermission && !hasSelfEnrollPermission)
            {
                return Forbid();
            }

            var device = await _db.Devices.FindAsync(deviceId);
            if (device is null) return NotFound("Device not found");

            var staff = await _db.Staff
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.StaffId == staffId);
            if (staff is null) return NotFound("Staff not found");

            // Check if user is Staff role attempting self-enrollment
            if (hasSelfEnrollPermission && !hasEnrollPermission)
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                // Business Rule 1: Staff must have linked User account
                if (!staff.UserId.HasValue || staff.UserId.Value != userId.Value)
                {
                    return Forbid(); // Can only enroll themselves
                }

                // Business Rule 2: Staff must be assigned to the device's location
                if (staff.LocationId != device.LocationId)
                {
                    return BadRequest(new 
                    { 
                        error = "Can only enroll to devices at your assigned location",
                        yourLocation = staff.LocationId,
                        deviceLocation = device.LocationId
                    });
                }

                // Business Rule 3: Staff cannot be set as device admin (pass canBeAdmin: false)
                var staffResult = await _deviceService.EnrollUserFingerprintAsync(device, staff, fingerId, canBeAdmin: false);
                
                if (staffResult.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = staffResult.Message,
                        deviceId,
                        staffId,
                        fingerId,
                        instructions = "User should scan their finger on the device 3 times when prompted"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    error = staffResult.Error,
                    deviceId,
                    staffId,
                    fingerId
                });
            }

            // Admin and HR Manager can enroll anyone with admin privileges
            var result = await _deviceService.EnrollUserFingerprintAsync(device, staff, fingerId, canBeAdmin: true);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    deviceId,
                    staffId,
                    fingerId,
                    instructions = "User should scan their finger on the device 3 times when prompted"
                });
            }

            return BadRequest(new
            {
                success = false,
                error = result.Error,
                deviceId,
                staffId,
                fingerId
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
