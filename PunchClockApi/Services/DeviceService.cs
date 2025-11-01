using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;
using PyZK.DotNet;

namespace PunchClockApi.Services;

/// <summary>
/// Service implementation for ZKTeco device operations using PyZKClient
/// </summary>
public sealed class DeviceService : IDeviceService, IDisposable
{
    private readonly PunchClockDbContext _db;
    private readonly ILogger<DeviceService> _logger;
    private readonly Dictionary<Guid, PyZKClient> _activeConnections = [];
    private bool _disposed;

    public DeviceService(PunchClockDbContext db, ILogger<DeviceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing DeviceService and cleaning up {Count} active connections", _activeConnections.Count);

        foreach (var (deviceId, client) in _activeConnections)
        {
            try
            {
                client.Disconnect();
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing client for device {DeviceId}", deviceId);
            }
        }

        _activeConnections.Clear();
        _disposed = true;
    }

    public async Task<DeviceInfo> ConnectAsync(Device device)
    {
        try
        {
            // Clean up any existing stale connection first
            if (_activeConnections.TryGetValue(device.DeviceId, out var oldClient))
            {
                _logger.LogDebug("Cleaning up existing connection for device {DeviceId} before reconnecting", device.DeviceId);
                try
                {
                    oldClient.Disconnect();
                    oldClient.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up old connection for device {DeviceId}", device.DeviceId);
                }
                _activeConnections.Remove(device.DeviceId);
            }

            var client = GetOrCreateClient(device);
            var result = await Task.Run(() => client.Connect());

            if (result.Success)
            {
                device.IsOnline = true;
                device.LastHeartbeatAt = DateTime.UtcNow;
                device.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Connected to device {DeviceId} ({DeviceName}) at {IpAddress}:{Port}",
                    device.DeviceId, device.DeviceName, device.IpAddress, device.Port);
            }
            else
            {
                // Clean up failed connection attempt
                _activeConnections.Remove(device.DeviceId);
                client.Dispose();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to device {DeviceId}", device.DeviceId);
            
            // Clean up on exception
            if (_activeConnections.Remove(device.DeviceId, out var client))
            {
                client.Dispose();
            }
            
            return new DeviceInfo
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<OperationResponse> DisconnectAsync(Guid deviceId)
    {
        try
        {
            if (_activeConnections.TryGetValue(deviceId, out var client))
            {
                var result = await Task.Run(() => client.Disconnect());
                
                // Always remove and dispose the client after disconnect
                _activeConnections.Remove(deviceId);
                client.Dispose();

                var device = await _db.Devices.FindAsync(deviceId);
                if (device != null)
                {
                    device.IsOnline = false;
                    device.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation("Disconnected from device {DeviceId}", deviceId);
                return result;
            }

            return new OperationResponse
            {
                Success = true,
                Message = "Device was not connected"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from device {DeviceId}", deviceId);
            return new OperationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<UsersResponse> GetUsersAsync(Device device)
    {
        try
        {
            var client = GetOrCreateClient(device);
            
            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                return new UsersResponse
                {
                    Success = false,
                    Error = $"Failed to connect: {error}"
                };
            }

            var result = await Task.Run(() => client.GetUsers());
            
            _logger.LogInformation("Retrieved {Count} users from device {DeviceId}",
                result.Count, device.DeviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users from device {DeviceId}", device.DeviceId);
            return new UsersResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<AttendanceResponse> GetAttendanceAsync(Device device)
    {
        try
        {
            var client = GetOrCreateClient(device);
            
            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                return new AttendanceResponse
                {
                    Success = false,
                    Error = $"Failed to connect: {error}"
                };
            }

            var result = await Task.Run(() => client.GetAttendance());
            
            _logger.LogInformation("Retrieved {Count} attendance records from device {DeviceId}",
                result.Count, device.DeviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attendance from device {DeviceId}", device.DeviceId);
            return new AttendanceResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<DetailedDeviceInfo> GetDeviceInfoAsync(Device device)
    {
        try
        {
            var client = GetOrCreateClient(device);
            
            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                return new DetailedDeviceInfo
                {
                    Success = false,
                    Error = $"Failed to connect: {error}"
                };
            }

            var result = await Task.Run(() => client.GetDeviceInfo());
            
            _logger.LogInformation("Retrieved device info from device {DeviceId}", device.DeviceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device info from device {DeviceId}", device.DeviceId);
            return new DetailedDeviceInfo
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<SyncResult> SyncStaffToDeviceAsync(Guid deviceId)
    {
        var result = new SyncResult
        {
            SyncStartTime = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var device = await _db.Devices
                .Include(d => d.Location)
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

            if (device == null)
            {
                result.Message = "Device not found";
                return result;
            }

            var client = GetOrCreateClient(device);

            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                result.Message = $"Failed to connect: {error}";
                return result;
            }

            // Get all active staff that should be enrolled on this device
            var staffToSync = await _db.Staff
                .Where(s => s.IsActive && s.LocationId == device.LocationId)
                .ToListAsync();

            foreach (var staff in staffToSync)
            {
                result.RecordsProcessed++;

                try
                {
                    // Check if enrollment exists
                    var enrollment = await _db.DeviceEnrollments
                        .FirstOrDefaultAsync(de => de.StaffId == staff.StaffId && de.DeviceId == deviceId);

                    int deviceUserId = enrollment?.DeviceUserId ?? result.RecordsProcessed;

                    var addResult = await Task.Run(() => client.AddUser(
                        uid: deviceUserId,
                        name: $"{staff.FirstName} {staff.LastName}",
                        privilege: PyZKClient.Privilege.User,
                        userId: staff.EmployeeId
                    ));

                    if (addResult.Success)
                    {
                        // Create or update enrollment record
                        if (enrollment == null)
                        {
                            enrollment = new DeviceEnrollment
                            {
                                EnrollmentId = Guid.NewGuid(),
                                DeviceId = deviceId,
                                StaffId = staff.StaffId,
                                DeviceUserId = deviceUserId,
                                EnrolledAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _db.DeviceEnrollments.Add(enrollment);
                            result.RecordsCreated++;
                        }
                        else
                        {
                            enrollment.UpdatedAt = DateTime.UtcNow;
                            result.RecordsUpdated++;
                        }
                    }
                    else
                    {
                        result.RecordsFailed++;
                        result.Errors.Add($"Failed to add {staff.EmployeeId}: {addResult.Error}");
                        _logger.LogWarning("Failed to add staff {StaffId} to device {DeviceId}: {Error}",
                            staff.StaffId, deviceId, addResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    result.RecordsFailed++;
                    result.Errors.Add($"Error processing {staff.EmployeeId}: {ex.Message}");
                    _logger.LogError(ex, "Error syncing staff {StaffId} to device {DeviceId}",
                        staff.StaffId, deviceId);
                }
            }

            await _db.SaveChangesAsync();

            result.Success = result.RecordsFailed == 0;
            result.Message = $"Synced {result.RecordsCreated + result.RecordsUpdated} staff members to device";
            result.SyncEndTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Staff sync completed for device {DeviceId}: {Created} created, {Updated} updated, {Failed} failed",
                deviceId, result.RecordsCreated, result.RecordsUpdated, result.RecordsFailed);

            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Sync failed: {ex.Message}";
            result.SyncEndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Failed to sync staff to device {DeviceId}", deviceId);
            return result;
        }
    }

    public async Task<SyncResult> SyncAttendanceFromDeviceAsync(Guid deviceId)
    {
        var result = new SyncResult
        {
            SyncStartTime = DateTime.UtcNow,
            Success = false
        };

        try
        {
            var device = await _db.Devices.FindAsync(deviceId);
            if (device == null)
            {
                result.Message = "Device not found";
                return result;
            }

            var client = GetOrCreateClient(device);

            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                result.Message = $"Failed to connect: {error}";
                return result;
            }

            // Get attendance records from device
            var attendanceResponse = await Task.Run(() => client.GetAttendance());
            if (!attendanceResponse.Success)
            {
                result.Message = $"Failed to get attendance: {attendanceResponse.Error}";
                return result;
            }

            foreach (var att in attendanceResponse.Attendances)
            {
                result.RecordsProcessed++;

                try
                {
                    var timestamp = att.GetTimestamp();
                    if (timestamp == null)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add($"Invalid timestamp for UID {att.Uid}");
                        continue;
                    }

                    // Find staff by device user ID
                    var enrollment = await _db.DeviceEnrollments
                        .Include(de => de.Staff)
                        .FirstOrDefaultAsync(de => de.DeviceId == deviceId && de.DeviceUserId == att.Uid);

                    if (enrollment == null)
                    {
                        // Try to find by employee ID
                        var staff = await _db.Staff.FirstOrDefaultAsync(s => s.EmployeeId == att.UserId);
                        if (staff != null)
                        {
                            // Create enrollment record for future syncs
                            enrollment = new DeviceEnrollment
                            {
                                EnrollmentId = Guid.NewGuid(),
                                DeviceId = deviceId,
                                StaffId = staff.StaffId,
                                DeviceUserId = att.Uid,
                                EnrolledAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _db.DeviceEnrollments.Add(enrollment);
                        }
                        else
                        {
                            result.RecordsFailed++;
                            result.Errors.Add($"Staff not found for UID {att.Uid} / UserID {att.UserId}");
                            continue;
                        }
                    }

                    // Check if punch log already exists
                    var existingLog = await _db.PunchLogs
                        .FirstOrDefaultAsync(pl =>
                            pl.StaffId == enrollment.Staff.StaffId &&
                            pl.DeviceId == deviceId &&
                            pl.PunchTime == timestamp.Value);

                    if (existingLog == null)
                    {
                        var punchLog = new PunchLog
                        {
                            LogId = Guid.NewGuid(),
                            StaffId = enrollment.Staff.StaffId,
                            DeviceId = deviceId,
                            PunchTime = timestamp.Value,
                            DeviceUserId = att.Uid,
                            PunchType = att.Punch switch
                            {
                                0 => "CHECK_IN",
                                1 => "CHECK_OUT",
                                2 => "BREAK_OUT",
                                3 => "BREAK_IN",
                                4 => "OVERTIME_IN",
                                5 => "OVERTIME_OUT",
                                _ => "UNKNOWN"
                            },
                            VerificationMode = att.Status switch
                            {
                                0 => "PASSWORD",
                                1 => "FINGERPRINT",
                                2 => "CARD",
                                3 => "FACE",
                                _ => "UNKNOWN"
                            },
                            IsProcessed = false,
                            IsManualEntry = false,
                            IsValid = true,
                            CreatedAt = DateTime.UtcNow,
                            ImportedAt = DateTime.UtcNow
                        };

                        _db.PunchLogs.Add(punchLog);
                        result.RecordsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    result.RecordsFailed++;
                    result.Errors.Add($"Error processing attendance for UID {att.Uid}: {ex.Message}");
                    _logger.LogError(ex, "Error processing attendance record for UID {Uid}", att.Uid);
                }
            }

            await _db.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Synced {result.RecordsCreated} new attendance records from device";
            result.SyncEndTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Attendance sync completed for device {DeviceId}: {Created} created, {Failed} failed",
                deviceId, result.RecordsCreated, result.RecordsFailed);

            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Sync failed: {ex.Message}";
            result.SyncEndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Failed to sync attendance from device {DeviceId}", deviceId);
            return result;
        }
    }

    public async Task<OperationResponse> AddUserToDeviceAsync(Device device, Staff staff)
    {
        try
        {
            var client = GetOrCreateClient(device);
            
            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                return new OperationResponse
                {
                    Success = false,
                    Error = $"Failed to connect: {error}"
                };
            }

            // Get or create enrollment
            var enrollment = await _db.DeviceEnrollments
                .FirstOrDefaultAsync(de => de.StaffId == staff.StaffId && de.DeviceId == device.DeviceId);

            int deviceUserId = enrollment?.DeviceUserId ?? await GetNextDeviceUserId(device.DeviceId);

            var result = await Task.Run(() => client.AddUser(
                uid: deviceUserId,
                name: $"{staff.FirstName} {staff.LastName}",
                privilege: PyZKClient.Privilege.User,
                userId: staff.EmployeeId
            ));

            if (result.Success && enrollment == null)
            {
                enrollment = new DeviceEnrollment
                {
                    EnrollmentId = Guid.NewGuid(),
                    DeviceId = device.DeviceId,
                    StaffId = staff.StaffId,
                    DeviceUserId = deviceUserId,
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.DeviceEnrollments.Add(enrollment);
                await _db.SaveChangesAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user to device {DeviceId}", device.DeviceId);
            return new OperationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<OperationResponse> DeleteUserFromDeviceAsync(Device device, int deviceUserId)
    {
        try
        {
            var client = GetOrCreateClient(device);
            
            // Ensure connected
            var (success, error) = await EnsureConnectedAsync(client, device.DeviceId);
            if (!success)
            {
                return new OperationResponse
                {
                    Success = false,
                    Error = $"Failed to connect: {error}"
                };
            }

            var result = await Task.Run(() => client.DeleteUser(deviceUserId));

            if (result.Success)
            {
                // Remove enrollment record
                var enrollment = await _db.DeviceEnrollments
                    .FirstOrDefaultAsync(de => de.DeviceId == device.DeviceId && de.DeviceUserId == deviceUserId);

                if (enrollment != null)
                {
                    _db.DeviceEnrollments.Remove(enrollment);
                    await _db.SaveChangesAsync();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user from device {DeviceId}", device.DeviceId);
            return new OperationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> TestConnectionAsync(Device device)
    {
        try
        {
            var client = GetOrCreateClient(device);
            var result = await Task.Run(() => client.Connect());
            
            if (result.Success)
            {
                await Task.Run(() => client.Disconnect());
                // Remove stale connection after test
                _activeConnections.Remove(device.DeviceId);
                client.Dispose();
                return true;
            }

            // Clean up failed client
            _activeConnections.Remove(device.DeviceId);
            client.Dispose();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for device {DeviceId}", device.DeviceId);
            // Clean up on exception
            if (_activeConnections.Remove(device.DeviceId, out var client))
            {
                client.Dispose();
            }
            return false;
        }
    }

    private PyZKClient GetOrCreateClient(Device device)
    {
        if (string.IsNullOrEmpty(device.IpAddress))
        {
            throw new InvalidOperationException($"Device {device.DeviceId} has no IP address configured");
        }

        // Check if we have an existing client
        if (_activeConnections.TryGetValue(device.DeviceId, out var existingClient))
        {
            // Verify the client is still valid and connected
            if (existingClient.IsConnected)
            {
                return existingClient;
            }
            
            // Client exists but is disconnected - clean it up and create a new one
            _logger.LogDebug("Removing stale connection for device {DeviceId}", device.DeviceId);
            _activeConnections.Remove(device.DeviceId);
            try
            {
                existingClient.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing stale client for device {DeviceId}", device.DeviceId);
            }
        }

        // Create a fresh client
        var client = new PyZKClient(
            ipAddress: device.IpAddress!,
            port: device.Port,
            timeout: 5,
            password: 0, // TODO: Get from device config
            forceUdp: false,
            ommitPing: false
        );

        _activeConnections[device.DeviceId] = client;
        return client;
    }

    /// <summary>
    /// Ensures a device client is connected, reconnecting if necessary
    /// </summary>
    private async Task<(bool Success, string? Error)> EnsureConnectedAsync(PyZKClient client, Guid deviceId)
    {
        if (client.IsConnected)
        {
            return (true, null);
        }

        _logger.LogDebug("Reconnecting to device {DeviceId}", deviceId);
        var connectResult = await Task.Run(() => client.Connect());
        
        if (!connectResult.Success)
        {
            // Connection failed - clean up this client
            _activeConnections.Remove(deviceId);
            try
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing failed client for device {DeviceId}", deviceId);
            }
            
            return (false, connectResult.Error);
        }

        return (true, null);
    }

    private async Task<int> GetNextDeviceUserId(Guid deviceId)
    {
        // First, query the device to get the max UID currently in use
        var device = await _db.Devices.FindAsync(deviceId);
        if (device == null)
        {
            throw new InvalidOperationException($"Device {deviceId} not found");
        }

        int maxDeviceUid = 0;
        try
        {
            var usersResponse = await GetUsersAsync(device);
            if (usersResponse.Success && usersResponse.Users.Any())
            {
                maxDeviceUid = usersResponse.Users.Max(u => u.Uid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query users from device {DeviceId}, falling back to database only", deviceId);
        }

        // Also check database for any enrollments we've tracked
        var maxDbUid = await _db.DeviceEnrollments
            .Where(de => de.DeviceId == deviceId)
            .MaxAsync(de => (int?)de.DeviceUserId) ?? 0;

        // Return the maximum of both plus 1
        return Math.Max(maxDeviceUid, maxDbUid) + 1;
    }
}
