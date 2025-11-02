# Staff Enrollment Automation

## Overview
The Punch Clock API now includes automated background jobs to manage staff enrollments across all ZKTeco biometric devices. These jobs ensure that device user lists stay synchronized with the database, automatically pushing new staff enrollments, updating existing ones, and removing inactive/deleted staff.

## Automated Background Jobs

### 1. Sync All Staff to Devices
**Job Name**: `sync-all-staff`  
**Schedule**: Every 6 hours (`0 */6 * * *`)  
**Purpose**: Synchronizes all active staff members to their assigned devices based on location

**What it does**:
- Queries all active devices in the system
- For each online device, syncs all active staff members at that device's location
- Creates or updates `DeviceEnrollment` records
- Tracks sync status and errors for each staff member
- Logs detailed information about created, updated, and failed enrollments

**Behavior**:
- Only syncs to devices that are marked as `IsOnline`
- Skips offline devices with a log message
- Updates enrollment status fields:
  - `EnrollmentStatus`: Set to "COMPLETED" on success
  - `SyncStatus`: "SUCCESS" or "FAILED"
  - `LastSyncAt`: Timestamp of the last sync attempt
  - `SyncErrorMessage`: Error details if sync failed

### 2. Remove Inactive Staff from Devices
**Job Name**: `remove-inactive-staff`  
**Schedule**: Daily at 2:00 AM (`Cron.Daily(2)`)  
**Purpose**: Removes staff who are no longer active from all devices

**What it does**:
- Queries all active devices
- For each device, finds enrollments where:
  - `Staff.IsActive = false`, OR
  - `Staff.TerminationDate != null`
- Calls the device to delete the user using their `DeviceUserId`
- Removes the `DeviceEnrollment` record from the database
- Logs successful removals and any errors

**Behavior**:
- Only processes online devices
- Gracefully handles cases where no `DeviceUserId` exists (orphaned enrollments)
- Provides detailed logging for audit purposes

### 3. Sync All Devices (Attendance)
**Job Name**: `sync-all-devices`  
**Schedule**: Every hour (`Cron.Hourly`)  
**Purpose**: Syncs attendance data from all devices (existing functionality)

### 4. Process Yesterday's Attendance
**Job Name**: `process-yesterday-attendance`  
**Schedule**: Daily at 1:00 AM (`Cron.Daily(1)`)  
**Purpose**: Processes previous day's punch logs into attendance records (existing functionality)

### 5. Process Pending Punch Logs
**Job Name**: `process-pending-punches`  
**Schedule**: Every 30 minutes (`*/30 * * * *`)  
**Purpose**: Processes any unprocessed punch logs (existing functionality)

## New Service Methods

### DeviceService Methods

#### `SyncStaffToDeviceAsync(Guid deviceId)`
Syncs all active staff at a device's location to that specific device.

**Enhanced Features**:
- Updates `DeviceEnrollment` status fields
- Tracks sync errors in the enrollment record
- Returns detailed `SyncResult` with counts of created, updated, and failed records

#### `RemoveInactiveStaffFromDeviceAsync(Guid deviceId)`
**NEW METHOD** - Removes inactive staff from a specific device.

**Parameters**:
- `deviceId`: The device to clean up

**Returns**: `SyncResult` with:
- `RecordsProcessed`: Total enrollments checked
- `RecordsDeleted`: Staff successfully removed
- `RecordsFailed`: Removal attempts that failed
- `Errors`: List of error messages

**Process**:
1. Finds all enrollments for inactive/terminated staff
2. Calls device API to delete each user
3. Removes enrollment record from database
4. Logs all operations

### DeviceSyncJob Methods

#### `SyncAllStaffAsync()`
**NEW METHOD** - Syncs staff to all active devices.

**Features**:
- Iterates through all active devices
- Skips offline devices
- Tracks total success/failure counts
- Creates `SyncLog` entries for each device sync

#### `RemoveInactiveStaffFromAllDevicesAsync()`
**NEW METHOD** - Removes inactive staff from all devices.

**Features**:
- Processes all active devices
- Aggregates total removal statistics
- Comprehensive logging for auditing

## SyncResult Model Enhancement

The `SyncResult` class now includes an additional field:

```csharp
public int RecordsDeleted { get; set; }
```

This tracks the number of records removed during cleanup operations.

## Monitoring and Management

### Hangfire Dashboard
Access the Hangfire dashboard at: `http://localhost:5187/hangfire`

**Features**:
- View scheduled jobs and their next execution time
- See job execution history
- Manually trigger jobs for immediate execution
- View detailed logs and error information
- Monitor job success/failure rates

### Manual Job Triggering
You can manually trigger any job from the Hangfire dashboard by:
1. Navigate to "Recurring jobs" tab
2. Click "Trigger now" button next to the desired job
3. Monitor execution in the "Jobs" tab

### Logging
All job operations are logged with structured information:

```
info: PunchClockApi.Services.DeviceSyncJob[0]
      Starting staff sync job for all active devices
info: PunchClockApi.Services.DeviceSyncJob[0]
      Found 3 active devices to sync staff to
info: PunchClockApi.Services.DeviceService[0]
      Staff sync completed for device abc-123: 15 created, 3 updated, 0 failed
```

## Database Schema Impact

### DeviceEnrollment Table
Enhanced tracking fields:
- `enrollment_status`: "PENDING", "IN_PROGRESS", or "COMPLETED"
- `sync_status`: "SUCCESS" or "FAILED"
- `sync_error_message`: Error details from last sync
- `last_sync_at`: Timestamp of most recent sync attempt

### SyncLog Table
Each sync operation creates a log entry:
- `sync_type`: "STAFF" or "ATTENDANCE"
- `sync_status`: "IN_PROGRESS", "SUCCESS", "FAILED", or "SKIPPED"
- `records_processed`: Number of items processed
- `records_synced`: Number successfully synced
- `error_message`: Error summary if failed

## Configuration

### Adjusting Job Schedules
To change job frequencies, edit `Program.cs`:

```csharp
// Sync staff every 4 hours instead of 6
RecurringJob.AddOrUpdate<DeviceSyncJob>(
    "sync-all-staff",
    job => job.SyncAllStaffAsync(),
    "0 */4 * * *");  // Changed from "0 */6 * * *"

// Remove inactive staff twice daily
RecurringJob.AddOrUpdate<DeviceSyncJob>(
    "remove-inactive-staff",
    job => job.RemoveInactiveStaffFromAllDevicesAsync(),
    "0 2,14 * * *");  // At 2:00 AM and 2:00 PM
```

### Cron Expression Examples
- `Cron.Hourly()` - Every hour at minute 0
- `Cron.Daily(hour)` - Every day at specified hour
- `"*/30 * * * *"` - Every 30 minutes
- `"0 */6 * * *"` - Every 6 hours
- `"0 2,14 * * *"` - At 2:00 AM and 2:00 PM
- `"0 0 * * 1"` - Every Monday at midnight

## Workflow Examples

### New Employee Onboarding
1. **Manual Step**: HR adds new staff member to database via API
2. **Automatic**: Next staff sync job (within 6 hours) pushes employee to devices at their location
3. **Automatic**: Employee appears on device ready for fingerprint enrollment
4. **Manual Step**: Security enrolls employee's fingerprint at physical device
5. **Automatic**: Employee can immediately clock in/out
6. **Automatic**: Attendance syncs hourly to database

### Employee Termination
1. **Manual Step**: HR sets `Staff.IsActive = false` or `Staff.TerminationDate`
2. **Automatic**: Daily cleanup job (next 2:00 AM) removes employee from all devices
3. **Automatic**: `DeviceEnrollment` records are deleted
4. **Result**: Employee can no longer access time clock devices

### Device Comes Online
1. Device reconnects after being offline
2. **Automatic**: Next staff sync (within 6 hours) catches up any missed staff changes
3. **Automatic**: Device user list matches current active staff

## Troubleshooting

### Job Not Running
- Check Hangfire dashboard for job status
- Verify database connection is healthy
- Check application logs for errors
- Ensure Hangfire tables exist in database

### Staff Not Syncing to Device
1. Check device `IsOnline` status
2. Verify staff `LocationId` matches device `LocationId`
3. Check staff `IsActive = true`
4. Review `DeviceEnrollment.sync_error_message` for details
5. Check `SyncLog` table for recent attempts

### Staff Not Removed from Device
1. Verify staff `IsActive = false` or has `TerminationDate`
2. Check device is online
3. Review job execution logs
4. Manually trigger `remove-inactive-staff` job from dashboard

### Viewing Job History
```sql
-- Recent sync logs for a device
SELECT * FROM sync_logs 
WHERE device_id = 'your-device-id' 
ORDER BY created_at DESC 
LIMIT 10;

-- Failed enrollments
SELECT s.employee_id, s.first_name, s.last_name, 
       de.sync_status, de.sync_error_message
FROM device_enrollments de
JOIN staff s ON de.staff_id = s.staff_id
WHERE de.sync_status = 'FAILED';
```

## Performance Considerations

- **Staff Sync**: Processing time depends on number of devices and staff per location
- **Cleanup Job**: Very fast unless many inactive enrollments exist
- **Device Timeouts**: 5-second connection timeout per device
- **Worker Threads**: 2 Hangfire workers handle concurrent job execution
- **Database Locking**: Minimal - jobs use read operations primarily

## Security Notes

- Hangfire dashboard requires admin authentication in production
- In development, anonymous access is allowed for testing
- All device operations use authenticated connections
- Job execution is logged for audit trail

## Future Enhancements

Potential improvements:
1. **Smart Sync**: Only sync staff that changed since last sync
2. **Location-Based Jobs**: Separate jobs per location for better scaling
3. **Real-time Sync**: Push staff changes immediately via SignalR
4. **Rollback Capability**: Undo failed syncs automatically
5. **Notification System**: Email/SMS alerts for sync failures
6. **Metrics Dashboard**: Visual analytics for enrollment statistics
7. **Batch Operations**: Bulk enroll/remove operations for efficiency

## API Endpoints

While jobs run automatically, you can manually trigger operations:

```http
# Sync staff to specific device
POST /api/devices/{deviceId}/sync-staff

# Remove inactive staff from specific device
POST /api/devices/{deviceId}/remove-inactive-staff

# Sync all staff to all devices (triggers job manually)
POST /api/devices/sync-all-staff

# Remove inactive staff from all devices (triggers job manually)
POST /api/devices/remove-inactive-staff
```

## Related Documentation

- [FINGERPRINT_ENROLLMENT_GUIDE.md](./FINGERPRINT_ENROLLMENT_GUIDE.md) - Device enrollment procedures
- [ATTENDANCE_PROCESSING_GUIDE.md](./ATTENDANCE_PROCESSING_GUIDE.md) - How attendance records are processed
- [PROJECT_SUMMARY.md](./PROJECT_SUMMARY.md) - Overall system architecture

## Support

For issues or questions:
1. Check Hangfire dashboard logs
2. Review application logs in console
3. Query `sync_logs` and `device_enrollments` tables
4. Consult device integration documentation
