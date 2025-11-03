# Reporting & Export System Guide

## Overview

The Punch Clock API includes a comprehensive reporting system for generating attendance and payroll reports with export capabilities. The system provides daily, monthly, and payroll period reports with CSV export for easy integration with Excel and other tools.

## API Endpoints

### 1. Daily Attendance Report
**Endpoint**: `GET /api/reports/daily`

Generate a detailed attendance report for a specific date.

**Query Parameters**:
- `date` (optional): Date for the report (defaults to today, format: YYYY-MM-DD)
- `locationId` (optional): Filter by location UUID
- `departmentId` (optional): Filter by department UUID
- `format` (optional): Export format - `json` (default) or `csv`

**Response** (JSON format):
```json
{
  "date": "2025-11-02",
  "totalStaff": 150,
  "presentCount": 142,
  "absentCount": 5,
  "lateCount": 12,
  "onLeaveCount": 3,
  "entries": [
    {
      "staffId": "uuid",
      "employeeId": "EMP001",
      "fullName": "John Doe",
      "department": "Engineering",
      "location": "Main Office",
      "shiftName": "Day Shift",
      "clockIn": "2025-11-02T08:05:00Z",
      "clockOut": "2025-11-02T17:00:00Z",
      "totalHours": "08:55:00",
      "lateMinutes": 5,
      "earlyLeaveMinutes": 0,
      "attendanceStatus": "PRESENT",
      "hasAnomalies": false,
      "anomalyFlags": null,
      "isOnLeave": false,
      "leaveType": null
    }
  ],
  "departmentBreakdown": {
    "Engineering": 45,
    "Sales": 32,
    "Operations": 35,
    "Admin": 30
  },
  "locationBreakdown": {
    "Main Office": 100,
    "Branch A": 42
  }
}
```

**CSV Export**:
```bash
curl -H "Authorization: Bearer {token}" \
  "http://localhost:5187/api/reports/daily?date=2025-11-02&format=csv" \
  --output daily_attendance_2025-11-02.csv
```

### 2. Monthly Attendance Summary
**Endpoint**: `GET /api/reports/monthly`

Generate a monthly attendance summary with statistics.

**Query Parameters**:
- `year` (optional): Year for the report (defaults to current year)
- `month` (optional): Month for the report (defaults to current month, 1-12)
- `locationId` (optional): Filter by location UUID
- `departmentId` (optional): Filter by department UUID
- `format` (optional): Export format - `json` (default) or `csv`

**Response** (JSON format):
```json
{
  "year": 2025,
  "month": 11,
  "totalWorkingDays": 22,
  "totalStaff": 150,
  "entries": [
    {
      "staffId": "uuid",
      "employeeId": "EMP001",
      "fullName": "John Doe",
      "department": "Engineering",
      "location": "Main Office",
      "daysPresent": 20,
      "daysAbsent": 2,
      "daysLate": 3,
      "daysOnLeave": 0,
      "totalWorkHours": "160:00:00",
      "totalOvertimeHours": "5:30:00",
      "totalLateMinutes": 45,
      "attendanceRate": 90.91
    }
  ],
  "statistics": {
    "averageAttendanceRate": 94.5,
    "totalLateIncidents": 45,
    "totalAnomalies": 12,
    "totalOvertimeHours": "825:00:00"
  }
}
```

### 3. Payroll Export
**Endpoint**: `GET /api/reports/payroll`

Generate payroll export data for a date range with detailed hours breakdown.

**Query Parameters** (Required):
- `startDate`: Start date of the period (format: YYYY-MM-DD)
- `endDate`: End date of the period (format: YYYY-MM-DD)
- `locationId` (optional): Filter by location UUID
- `departmentId` (optional): Filter by department UUID
- `format` (optional): Export format - `json` (default) or `csv`

**Response** (JSON format):
```json
{
  "startDate": "2025-11-01",
  "endDate": "2025-11-15",
  "totalDays": 15,
  "entries": [
    {
      "staffId": "uuid",
      "employeeId": "EMP001",
      "fullName": "John Doe",
      "department": "Engineering",
      "location": "Main Office",
      "position": "Senior Developer",
      "totalWorkingDays": 10,
      "daysPresent": 9,
      "daysAbsent": 1,
      "daysOnLeave": 0,
      "regularHours": 72.00,
      "overtimeHours": 5.50,
      "weekendOvertimeHours": 0.00,
      "holidayOvertimeHours": 0.00,
      "totalLateMinutes": 25,
      "totalEarlyLeaveMinutes": 0,
      "notes": null
    }
  ]
}
```

**CSV Export for Payroll**:
```bash
curl -H "Authorization: Bearer {token}" \
  "http://localhost:5187/api/reports/payroll?startDate=2025-11-01&endDate=2025-11-15&format=csv" \
  --output payroll_2025-11-01_to_2025-11-15.csv
```

### 4. Summary Statistics
**Endpoint**: `GET /api/reports/summary`

Get aggregated statistics for a date range (useful for dashboards).

**Query Parameters** (Required):
- `startDate`: Start date (format: YYYY-MM-DD)
- `endDate`: End date (format: YYYY-MM-DD)
- `locationId` (optional): Filter by location UUID
- `departmentId` (optional): Filter by department UUID

**Response**:
```json
{
  "period": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-15",
    "totalDays": 15
  },
  "staff": {
    "total": 150,
    "averageAttendanceRate": 94.5
  },
  "hours": {
    "totalRegularHours": 10800.00,
    "totalOvertimeHours": 825.50,
    "totalWeekendOvertimeHours": 120.00,
    "totalHolidayOvertimeHours": 50.00
  },
  "attendance": {
    "totalPresent": 1350,
    "totalAbsent": 75,
    "totalOnLeave": 45,
    "totalLateMinutes": 3750,
    "totalEarlyLeaveMinutes": 450
  }
}
```

### 5. Department Comparison
**Endpoint**: `GET /api/reports/departments`

Compare attendance metrics across departments.

**Query Parameters** (Required):
- `startDate`: Start date (format: YYYY-MM-DD)
- `endDate`: End date (format: YYYY-MM-DD)
- `locationId` (optional): Filter by location UUID

**Response**:
```json
{
  "period": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-15"
  },
  "totalDepartments": 4,
  "departments": [
    {
      "department": "Engineering",
      "staffCount": 45,
      "averageAttendanceRate": 95.2,
      "totalRegularHours": 3240.00,
      "totalOvertimeHours": 285.50,
      "totalLateMinutes": 675,
      "averageLateMinutesPerStaff": 15.0
    },
    {
      "department": "Sales",
      "staffCount": 32,
      "averageAttendanceRate": 93.8,
      "totalRegularHours": 2304.00,
      "totalOvertimeHours": 192.00,
      "totalLateMinutes": 960,
      "averageLateMinutesPerStaff": 30.0
    }
  ]
}
```

## Report Features

### Daily Attendance Report
- **Staff presence tracking**: Present, absent, on leave counts
- **Late arrival detection**: Staff who arrived late with minutes counted
- **Department/Location breakdown**: Visual breakdown of attendance by organizational unit
- **Anomaly flags**: Highlight attendance records with issues
- **Leave integration**: Shows staff on approved leave

### Monthly Attendance Summary
- **Working days calculation**: Excludes weekends and holidays automatically
- **Attendance rate**: Percentage of days present vs. total working days
- **Overtime tracking**: Total overtime hours per staff member
- **Late incidents**: Count of days staff arrived late
- **Statistics**: Average attendance rate, total anomalies, total overtime

### Payroll Export
- **Comprehensive hours breakdown**:
  - Regular hours (within shift)
  - Overtime hours (weekday)
  - Weekend overtime hours
  - Holiday overtime hours
- **Leave integration**: Days on approved leave
- **Absence tracking**: Days absent (excluding leave)
- **Punctuality tracking**: Total late/early leave minutes
- **Position/Department info**: For payroll categorization

### CSV Export Format
- **Excel-compatible**: Opens directly in Excel, Google Sheets
- **Proper escaping**: Handles commas, quotes, newlines in data
- **Formatted values**: Dates (YYYY-MM-DD), times (HH:MM:SS), decimals (2 places)
- **Automatic download**: Content-Disposition header for file download
- **Descriptive filenames**: Includes report type and date range

### Export Logging
All report exports are logged in the `export_logs` table with:
- Export type and date range
- Record count
- File format
- User who exported
- Filter criteria applied
- Timestamp

## Use Cases

### 1. Daily Operations Monitoring
```bash
# Get today's attendance
GET /api/reports/daily

# Check yesterday's attendance
GET /api/reports/daily?date=2025-11-01

# Export for specific department
GET /api/reports/daily?departmentId=<uuid>&format=csv
```

### 2. Monthly HR Review
```bash
# Get current month summary
GET /api/reports/monthly

# Export last month's data
GET /api/reports/monthly?year=2025&month=10&format=csv

# Compare departments
GET /api/reports/departments?startDate=2025-10-01&endDate=2025-10-31
```

### 3. Payroll Processing
```bash
# Get bi-weekly payroll (Nov 1-15)
GET /api/reports/payroll?startDate=2025-11-01&endDate=2025-11-15&format=csv

# Monthly payroll export
GET /api/reports/payroll?startDate=2025-11-01&endDate=2025-11-30&format=csv

# Location-specific payroll
GET /api/reports/payroll?startDate=2025-11-01&endDate=2025-11-15&locationId=<uuid>&format=csv
```

### 4. Dashboard Statistics
```bash
# Real-time dashboard data
GET /api/reports/summary?startDate=2025-11-01&endDate=2025-11-02

# Weekly overview
GET /api/reports/summary?startDate=2025-10-27&endDate=2025-11-02

# Department performance
GET /api/reports/departments?startDate=2025-11-01&endDate=2025-11-30
```

## Integration Examples

### Excel Integration
1. Export report as CSV
2. Open in Excel
3. Use pivot tables and charts for analysis
4. Automate with Excel macros or Power Query

### Payroll System Integration
1. Schedule daily/weekly exports via API
2. Process CSV files automatically
3. Import into payroll software
4. Reconcile hours and overtime

### HR Dashboard
1. Call summary endpoint periodically
2. Display real-time metrics
3. Show department comparisons
4. Highlight anomalies and trends

## Best Practices

### Performance
- Use date range filters to limit data volume
- Export CSV for large datasets (more efficient than JSON)
- Use location/department filters when possible
- Cache summary statistics for dashboards

### Data Accuracy
- Run payroll exports after attendance processing completes
- Review anomaly flags before finalizing payroll
- Verify overtime calculations against policies
- Cross-check leave balances with attendance

### Security
- All endpoints require authentication
- Export logs track who accessed what data
- Role-based access can be added for sensitive reports
- Consider additional authorization for payroll exports

## Error Handling

### Common Errors
- **400 Bad Request**: Invalid date format or date range
- **401 Unauthorized**: Missing or invalid JWT token
- **404 Not Found**: Invalid location/department UUID
- **500 Internal Server Error**: Database or processing error

### Validation Rules
- End date must be after start date
- Date range cannot exceed 1 year (payroll export)
- Month must be between 1 and 12
- Valid UUID format for location/department filters

## Future Enhancements

### Planned Features
- **Excel export** (XLSX format with formatting)
- **Custom report builder** (user-defined columns and filters)
- **Scheduled exports** (email reports automatically)
- **Report templates** (save favorite report configurations)
- **Real-time monitoring** (WebSocket updates for live dashboard)

---

*Last Updated: November 2, 2025*
