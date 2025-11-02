# Attendance Processing & Background Jobs

## Overview

This document describes the newly implemented **Attendance Processing Engine** and **Background Jobs** infrastructure for the Punch Clock API.

## Features Implemented

### 1. Attendance Processing Engine

The `AttendanceProcessingService` transforms raw punch logs into structured daily attendance records with business logic.

#### Key Capabilities

- **Daily Aggregation**: Process all punch logs for a staff member on a specific date
- **Hours Calculation**: Calculate total hours, regular hours, and overtime hours
- **Late Arrival Detection**: Track late arrivals based on expected start time
- **Overtime Calculation**: Calculate overtime based on expected end time  
- **Anomaly Detection**: Automatically flag issues like:
  - Missing check-in or check-out
  - Odd number of punches
  - Short shifts (less than minimum hours)
- **Batch Processing**: Process multiple staff members and date ranges
- **Reprocessing**: Re-run processing for records with anomalies

#### Usage Examples

```csharp
// Process single day for one staff member
await attendanceService.ProcessDailyAttendance(
    staffId, 
    date,
    expectedStartTime: date.AddHours(9),  // 9:00 AM
    expectedEndTime: date.AddHours(17),    // 5:00 PM
    minimumHours: 4.0m
);

// Process date range for one staff member
await attendanceService.ProcessDateRange(
    staffId,
    startDate,
    endDate
);

// Process all active staff for one day
await attendanceService.ProcessAllStaff(date);

// Reprocess records with anomalies
await attendanceService.ReprocessAnomalies(fromDate);
```

#### Output Structure

Each `AttendanceRecord` includes:
- `AttendanceDate`: Date of attendance
- `ClockIn`: First check-in time
- `ClockOut`: Last check-out time
- `TotalHours`: Total time between check-in and check-out
- `OvertimeHours`: Hours beyond expected end time
- `LateMinutes`: Minutes late from expected start time
- `AttendanceStatus`: PRESENT, ABSENT, or INCOMPLETE
- `HasAnomalies`: Boolean flag for issues
- `AnomalyFlags`: JSON object detailing specific issues

---

### 2. Background Jobs with Hangfire

Automated job processing using Hangfire with PostgreSQL persistence.

#### Scheduled Jobs

**Device Sync Job** - Runs **hourly**
- Syncs attendance records from all active, online devices
- Creates SyncLog entries tracking success/failure
- Continues processing even if individual devices fail
- Skips offline devices with appropriate logging

**Attendance Processing Job** - Runs **daily at 1:00 AM**
- Processes yesterday's attendance for all active staff
- Transforms punch logs into attendance records
- Applies business rules (overtime, late detection, etc.)

**Pending Punches Job** - Runs **every 30 minutes**
- Processes any unprocessed punch logs
- Marks punch logs as processed after successful attendance record creation
- Ensures real-time attendance data is always up-to-date

#### Job Monitoring

Access the Hangfire dashboard at: **http://localhost:5187/hangfire**

The dashboard provides:
- Job execution history and statistics
- Real-time job monitoring
- Failed job details and stack traces
- Ability to manually trigger jobs
- Recurring job schedule management

#### Manual Job Triggers

You can manually trigger jobs programmatically:

```csharp
// Sync specific device
await deviceSyncJob.SyncDeviceAsync(deviceId);

// Sync all devices
await deviceSyncJob.SyncAllDevicesAsync();

// Process specific date
await attendanceProcessingJob.ProcessDateAsync(date);

// Process date range
await attendanceProcessingJob.ProcessDateRangeAsync(startDate, endDate);
```

---

## Architecture

### Service Layer

```
AttendanceProcessingService
├── ProcessDailyAttendance()      // Single staff, single date
├── ProcessDateRange()              // Single staff, date range
├── ProcessAllStaff()               // All staff, single date
├── ProcessAllStaffDateRange()      // All staff, date range
└── ReprocessAnomalies()            // Re-run for flagged records

DeviceSyncJob
├── SyncAllDevicesAsync()           // Sync all active devices
├── SyncDeviceAsync()               // Sync specific device
└── SyncStaffToDeviceAsync()        // Push staff to device

AttendanceProcessingJob
├── ProcessYesterdayAttendanceAsync()   // Daily scheduled job
├── ProcessDateAsync()                  // Process specific date
├── ProcessDateRangeAsync()             // Process date range
└── ProcessPendingPunchLogsAsync()      // Process unprocessed punches
```

### Database Tables

**AttendanceRecords** - Processed daily attendance
- Unique constraint on `(staff_id, attendance_date)`
- Contains calculated fields (total_hours, overtime_hours, etc.)
- Tracks anomalies via `has_anomalies` and `anomaly_flags` (JSONB)

**SyncLogs** - Device synchronization history
- Tracks each sync operation with start/end times
- Records success/failure status
- Stores error messages for failed syncs

**PunchLogs** - Raw punch data
- `is_processed` flag tracks processing status
- `processed_at` timestamp records when processed
- Immutable historical record

---

## Configuration

### Hangfire Settings

In `Program.cs`:

```csharp
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => 
        options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;  // Number of concurrent jobs
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});
```

### Job Schedules

Modify schedules in `Program.cs`:

```csharp
// Device sync - every hour
RecurringJob.AddOrUpdate<DeviceSyncJob>(
    "sync-all-devices",
    job => job.SyncAllDevicesAsync(),
    Cron.Hourly);

// Attendance processing - daily at 1:00 AM
RecurringJob.AddOrUpdate<AttendanceProcessingJob>(
    "process-yesterday-attendance",
    job => job.ProcessYesterdayAttendanceAsync(),
    Cron.Daily(1));

// Pending punches - every 30 minutes
RecurringJob.AddOrUpdate<AttendanceProcessingJob>(
    "process-pending-punches",
    job => job.ProcessPendingPunchLogsAsync(),
    "*/30 * * * *");
```

---

## Testing

### Unit/Integration Tests

Tests are located in `PunchClockApi.Tests/`:

- `AttendanceProcessingTests.cs` - 14 tests covering:
  - Valid punch pair processing
  - Multiple punches (first/last)
  - Late arrival calculation
  - Overtime calculation
  - Missing check-in/out detection
  - Absent status
  - Anomaly detection
  - Update existing records
  - Date range processing
  
- `BackgroundJobTests.cs` - Tests for:
  - Device sync job execution
  - Error handling and recovery
  - Offline device handling
  - Attendance processing jobs
  - Manual job triggers

### Running Tests

```bash
cd PunchClockApi.Tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AttendanceProcessingTests"
dotnet test --filter "FullyQualifiedName~BackgroundJobTests"
```

**Note**: Some test fixtures may need updates due to model property changes (DateOnly, TimeSpan, etc.)

---

## Production Considerations

### Security

- [ ] Add authentication to Hangfire dashboard (currently open)
- [ ] Implement role-based access for job management
- [ ] Secure database connection strings

### Performance

- [ ] Configure worker count based on expected load
- [ ] Implement retry policies for transient failures
- [ ] Add job timeouts for long-running operations
- [ ] Consider job prioritization for critical syncs

### Monitoring

- [ ] Set up alerts for failed jobs
- [ ] Monitor job execution times
- [ ] Track sync success rates
- [ ] Implement health checks for background services

### Scaling

- [ ] Distribute workers across multiple servers
- [ ] Use Redis for distributed locking (if needed)
- [ ] Implement queue-based processing for high volumes
- [ ] Consider separate job servers for heavy workloads

---

## API Integration

While most processing happens automatically via background jobs, you can manually trigger operations via API endpoints:

```bash
# Manually sync device (existing endpoint)
POST /api/devices/{id}/sync-attendance

# Manually sync staff to device (existing endpoint)
POST /api/devices/{id}/sync-staff

# Get sync logs for monitoring
GET /api/sync-logs?deviceId={id}&status=FAILED

# Get attendance records with anomalies
GET /api/attendance/records?hasAnomalies=true
```

---

## Troubleshooting

### Jobs Not Running

1. Check Hangfire dashboard at `/hangfire`
2. Verify PostgreSQL connection
3. Check worker status in dashboard
4. Review application logs for exceptions

### Failed Device Syncs

1. Check device connectivity (`is_online` status)
2. Review SyncLog entries for error messages
3. Verify device credentials and network access
4. Test manual connection via `/api/devices/{id}/test-connection`

### Attendance Processing Issues

1. Verify punch logs exist (`is_processed = false`)
2. Check for validation errors in PunchLog records
3. Review attendance records with `has_anomalies = true`
4. Manually reprocess: `ProcessPendingPunchLogsAsync()`

### Performance Degradation

1. Monitor job execution times in dashboard
2. Check database query performance
3. Review worker count configuration
4. Consider indexing frequently queried columns

---

## Future Enhancements

- [ ] Shift management integration (match punches to shifts)
- [ ] Break time deduction logic
- [ ] Leave/absence integration
- [ ] Payroll export generation
- [ ] Email notifications for anomalies
- [ ] Approval workflow for attendance corrections
- [ ] Historical trend analysis
- [ ] Predictive anomaly detection with ML

---

## References

- [Hangfire Documentation](https://docs.hangfire.io/)
- [Cron Expression Guide](https://crontab.guru/)
- [PROJECT_SUMMARY.md](../PROJECT_SUMMARY.md) - Full project overview
- [PunchClockApi/README.md](../PunchClockApi/README.md) - API usage guide

---

*Last Updated: November 2, 2025*
