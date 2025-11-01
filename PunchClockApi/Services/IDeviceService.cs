using PunchClockApi.Models;
using PyZK.DotNet;

namespace PunchClockApi.Services;

/// <summary>
/// Service interface for ZKTeco device operations
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Connects to a ZKTeco device
    /// </summary>
    /// <param name="device">Device entity with connection information</param>
    /// <returns>Device information response</returns>
    Task<DeviceInfo> ConnectAsync(Device device);

    /// <summary>
    /// Disconnects from a ZKTeco device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Operation response</returns>
    Task<OperationResponse> DisconnectAsync(Guid deviceId);

    /// <summary>
    /// Gets all users from a device
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <returns>Users response with list of users</returns>
    Task<UsersResponse> GetUsersAsync(Device device);

    /// <summary>
    /// Gets all attendance records from a device
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <returns>Attendance response with list of records</returns>
    Task<AttendanceResponse> GetAttendanceAsync(Device device);

    /// <summary>
    /// Gets detailed device information
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <returns>Detailed device information</returns>
    Task<DetailedDeviceInfo> GetDeviceInfoAsync(Device device);

    /// <summary>
    /// Synchronizes staff enrollments to a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Sync result with statistics</returns>
    Task<SyncResult> SyncStaffToDeviceAsync(Guid deviceId);

    /// <summary>
    /// Synchronizes attendance records from a device
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Sync result with statistics</returns>
    Task<SyncResult> SyncAttendanceFromDeviceAsync(Guid deviceId);

    /// <summary>
    /// Adds a user to a device
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <param name="staff">Staff entity to add</param>
    /// <returns>Operation response</returns>
    Task<OperationResponse> AddUserToDeviceAsync(Device device, Staff staff);

    /// <summary>
    /// Deletes a user from a device
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <param name="deviceUserId">Device-specific user ID</param>
    /// <returns>Operation response</returns>
    Task<OperationResponse> DeleteUserFromDeviceAsync(Device device, int deviceUserId);

    /// <summary>
    /// Checks device connectivity
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <returns>True if device is reachable</returns>
    Task<bool> TestConnectionAsync(Device device);

    /// <summary>
    /// Initiates fingerprint enrollment for a user on a device
    /// </summary>
    /// <param name="device">Device entity</param>
    /// <param name="staff">Staff entity to enroll</param>
    /// <param name="fingerId">Finger index (0-9)</param>
    /// <returns>Operation response</returns>
    Task<OperationResponse> EnrollUserFingerprintAsync(Device device, Staff staff, int fingerId = 0);
}

/// <summary>
/// Result of a device synchronization operation
/// </summary>
public sealed class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsFailed { get; set; }
    public DateTime SyncStartTime { get; set; }
    public DateTime SyncEndTime { get; set; }
    public List<string> Errors { get; set; } = [];
}
