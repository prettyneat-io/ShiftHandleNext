namespace PunchClockApi.Services;

/// <summary>
/// Service interface for generating attendance and payroll reports
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generate daily attendance report for a specific date
    /// </summary>
    Task<DailyAttendanceReport> GenerateDailyReportAsync(DateOnly date, Guid? locationId = null, Guid? departmentId = null);

    /// <summary>
    /// Generate monthly attendance summary for a specific month and year
    /// </summary>
    Task<MonthlyAttendanceReport> GenerateMonthlyReportAsync(int year, int month, Guid? locationId = null, Guid? departmentId = null);

    /// <summary>
    /// Generate payroll export data for a date range
    /// </summary>
    Task<PayrollExportReport> GeneratePayrollReportAsync(DateOnly startDate, DateOnly endDate, Guid? locationId = null, Guid? departmentId = null);

    /// <summary>
    /// Export report data as CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string reportType) where T : class;

    /// <summary>
    /// Log export operation in the database
    /// </summary>
    Task<Guid> LogExportAsync(string exportType, DateOnly startDate, DateOnly endDate, string fileFormat, int recordCount, Guid exportedBy, string? filterCriteria = null);
}

/// <summary>
/// Daily attendance report data structure
/// </summary>
public class DailyAttendanceReport
{
    public DateOnly Date { get; set; }
    public int TotalStaff { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int OnLeaveCount { get; set; }
    public List<DailyAttendanceEntry> Entries { get; set; } = [];
    public Dictionary<string, int> DepartmentBreakdown { get; set; } = new();
    public Dictionary<string, int> LocationBreakdown { get; set; } = new();
}

/// <summary>
/// Single staff attendance entry for daily report
/// </summary>
public class DailyAttendanceEntry
{
    public Guid StaffId { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? ShiftName { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public TimeSpan? TotalHours { get; set; }
    public int? LateMinutes { get; set; }
    public int? EarlyLeaveMinutes { get; set; }
    public string AttendanceStatus { get; set; } = null!;
    public bool HasAnomalies { get; set; }
    public string? AnomalyFlags { get; set; }
    public bool IsOnLeave { get; set; }
    public string? LeaveType { get; set; }
}

/// <summary>
/// Monthly attendance summary data structure
/// </summary>
public class MonthlyAttendanceReport
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalWorkingDays { get; set; }
    public int TotalStaff { get; set; }
    public List<MonthlyAttendanceEntry> Entries { get; set; } = [];
    public MonthlyStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Single staff monthly attendance summary
/// </summary>
public class MonthlyAttendanceEntry
{
    public Guid StaffId { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int DaysOnLeave { get; set; }
    public TimeSpan TotalWorkHours { get; set; }
    public TimeSpan TotalOvertimeHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Monthly statistics aggregate
/// </summary>
public class MonthlyStatistics
{
    public decimal AverageAttendanceRate { get; set; }
    public int TotalLateIncidents { get; set; }
    public int TotalAnomalies { get; set; }
    public TimeSpan TotalOvertimeHours { get; set; }
}

/// <summary>
/// Payroll export report data structure
/// </summary>
public class PayrollExportReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public List<PayrollEntry> Entries { get; set; } = [];
}

/// <summary>
/// Single staff payroll entry
/// </summary>
public class PayrollEntry
{
    public Guid StaffId { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? Position { get; set; }
    public int TotalWorkingDays { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysOnLeave { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal WeekendOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyLeaveMinutes { get; set; }
    public string? Notes { get; set; }
}
