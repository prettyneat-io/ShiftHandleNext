using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Services;

public sealed class DeviceSyncJob
{
    private readonly PunchClockDbContext _db;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DeviceSyncJob> _logger;

    public DeviceSyncJob(PunchClockDbContext db, IDeviceService deviceService, ILogger<DeviceSyncJob> logger)
    {
        _db = db;
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Sync attendance data from all active and online devices
    /// </summary>
    public async Task SyncAllDevicesAsync()
    {
        _logger.LogInformation("Starting device sync job for all active devices");

        var devices = await _db.Devices
            .Where(d => d.IsActive)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active devices to sync", devices.Count);

        foreach (var device in devices)
        {
            await SyncDeviceAsync(device.DeviceId, device);
        }

        _logger.LogInformation("Device sync job completed");
    }

    /// <summary>
    /// Sync attendance data from a specific device
    /// </summary>
    public async Task SyncDeviceAsync(Guid deviceId, Device? device = null)
    {
        device ??= await _db.Devices.FindAsync(deviceId);

        if (device == null)
        {
            _logger.LogWarning("Device {DeviceId} not found", deviceId);
            return;
        }

        var syncLog = new SyncLog
        {
            SyncId = Guid.NewGuid(),
            DeviceId = deviceId,
            SyncType = "ATTENDANCE",
            SyncStatus = "IN_PROGRESS",
            Status = "IN_PROGRESS",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            // Check if device is online
            if (!device.IsOnline)
            {
                _logger.LogWarning("Device {DeviceName} ({DeviceId}) is offline, skipping sync", 
                    device.DeviceName, deviceId);
                
                syncLog.SyncStatus = "SKIPPED";
                syncLog.Status = "SKIPPED";
                syncLog.ErrorMessage = $"Device is offline. Last heartbeat: {device.LastHeartbeatAt}";
                syncLog.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return;
            }

            _logger.LogInformation("Syncing device {DeviceName} ({DeviceId})", device.DeviceName, deviceId);

            // Sync attendance from device
            var result = await _deviceService.SyncAttendanceAsync(deviceId);
            var recordsSynced = (int)result.RecordsSynced;

            syncLog.SyncStatus = "SUCCESS";
            syncLog.Status = "SUCCESS";
            syncLog.RecordsProcessed = recordsSynced;
            syncLog.RecordsSynced = recordsSynced;
            syncLog.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully synced {Count} records from device {DeviceName}", 
                recordsSynced, device.DeviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing device {DeviceName} ({DeviceId})", device.DeviceName, deviceId);

            syncLog.SyncStatus = "FAILED";
            syncLog.Status = "FAILED";
            syncLog.ErrorMessage = ex.Message;
            syncLog.ErrorDetails = ex.ToString();
            syncLog.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Sync staff enrollments to a specific device
    /// </summary>
    public async Task SyncStaffToDeviceAsync(Guid deviceId)
    {
        var device = await _db.Devices.FindAsync(deviceId);

        if (device == null)
        {
            _logger.LogWarning("Device {DeviceId} not found", deviceId);
            return;
        }

        var syncLog = new SyncLog
        {
            SyncId = Guid.NewGuid(),
            DeviceId = deviceId,
            SyncType = "STAFF",
            SyncStatus = "IN_PROGRESS",
            Status = "IN_PROGRESS",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            if (!device.IsOnline)
            {
                syncLog.SyncStatus = "SKIPPED";
                syncLog.Status = "SKIPPED";
                syncLog.ErrorMessage = "Device is offline";
                syncLog.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return;
            }

            _logger.LogInformation("Syncing staff to device {DeviceName} ({DeviceId})", device.DeviceName, deviceId);

            var result = await _deviceService.SyncStaffAsync(deviceId);
            var staffSynced = (int)result.StaffSynced;

            syncLog.SyncStatus = "SUCCESS";
            syncLog.Status = "SUCCESS";
            syncLog.RecordsProcessed = staffSynced;
            syncLog.RecordsSynced = staffSynced;
            syncLog.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully synced {Count} staff to device {DeviceName}", 
                staffSynced, device.DeviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing staff to device {DeviceName} ({DeviceId})", 
                device.DeviceName, deviceId);

            syncLog.SyncStatus = "FAILED";
            syncLog.Status = "FAILED";
            syncLog.ErrorMessage = ex.Message;
            syncLog.ErrorDetails = ex.ToString();
            syncLog.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Sync all active staff to all active and online devices
    /// </summary>
    public async Task SyncAllStaffAsync()
    {
        _logger.LogInformation("Starting staff sync job for all active devices");

        var devices = await _db.Devices
            .Where(d => d.IsActive)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active devices to sync staff to", devices.Count);

        int totalSynced = 0;
        int totalFailed = 0;

        foreach (var device in devices)
        {
            try
            {
                if (!device.IsOnline)
                {
                    _logger.LogInformation("Skipping offline device {DeviceName} ({DeviceId})", 
                        device.DeviceName, device.DeviceId);
                    continue;
                }

                await SyncStaffToDeviceAsync(device.DeviceId);
                
                // Get sync log to track results
                var lastSyncLog = await _db.SyncLogs
                    .Where(sl => sl.DeviceId == device.DeviceId && sl.SyncType == "STAFF")
                    .OrderByDescending(sl => sl.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastSyncLog?.SyncStatus == "SUCCESS")
                {
                    totalSynced += lastSyncLog.RecordsSynced ?? 0;
                }
                else
                {
                    totalFailed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing staff to device {DeviceId}", device.DeviceId);
                totalFailed++;
            }
        }

        _logger.LogInformation(
            "Staff sync job completed: {Synced} staff synced across devices, {Failed} devices failed",
            totalSynced, totalFailed);
    }

    /// <summary>
    /// Remove inactive or deleted staff from all devices
    /// </summary>
    public async Task RemoveInactiveStaffFromAllDevicesAsync()
    {
        _logger.LogInformation("Starting inactive staff removal job for all active devices");

        var devices = await _db.Devices
            .Where(d => d.IsActive)
            .ToListAsync();

        _logger.LogInformation("Found {Count} active devices to clean up", devices.Count);

        int totalRemoved = 0;
        int totalFailed = 0;

        foreach (var device in devices)
        {
            try
            {
                if (!device.IsOnline)
                {
                    _logger.LogInformation("Skipping offline device {DeviceName} ({DeviceId})", 
                        device.DeviceName, device.DeviceId);
                    continue;
                }

                var result = await _deviceService.RemoveInactiveStaffFromDeviceAsync(device.DeviceId);
                
                if (result.Success)
                {
                    totalRemoved += result.RecordsDeleted;
                }
                else
                {
                    totalFailed++;
                }

                _logger.LogInformation(
                    "Removed {Count} inactive staff from device {DeviceName}",
                    result.RecordsDeleted, device.DeviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing inactive staff from device {DeviceId}", device.DeviceId);
                totalFailed++;
            }
        }

        _logger.LogInformation(
            "Inactive staff removal job completed: {Removed} staff removed, {Failed} devices failed",
            totalRemoved, totalFailed);
    }
}
