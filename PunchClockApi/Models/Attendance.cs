namespace PunchClockApi.Models;

public class PunchLog
{
    public Guid LogId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime PunchTime { get; set; }
    public string? PunchType { get; set; }
    public string? VerificationMode { get; set; }
    public int? DeviceUserId { get; set; }
    public long? DeviceLogId { get; set; }
    public string? WorkCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsManualEntry { get; set; }
    public string? ManualEntryReason { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationErrors { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? ModifiedBy { get; set; }

    // Navigation properties
    public Staff? Staff { get; set; }
    public Device? Device { get; set; }
}

public class AttendanceRecord
{
    public Guid RecordId { get; set; }
    public Guid StaffId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public TimeSpan? TotalHours { get; set; }
    public TimeSpan? RegularHours { get; set; }
    public TimeSpan? OvertimeHours { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public int? LateMinutes { get; set; }
    public int? EarlyLeaveMinutes { get; set; }
    public string AttendanceStatus { get; set; } = "PRESENT";
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public bool HasAnomalies { get; set; }
    public string? AnomalyFlags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }

    // Navigation properties
    public Staff Staff { get; set; } = null!;
}

public class AttendanceCorrection
{
    public Guid CorrectionId { get; set; }
    public Guid RecordId { get; set; }
    public Guid StaffId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string CorrectionType { get; set; } = null!; // "CLOCK_IN", "CLOCK_OUT", "BOTH", "MANUAL_ENTRY"
    public DateTime? OriginalClockIn { get; set; }
    public DateTime? OriginalClockOut { get; set; }
    public DateTime? CorrectedClockIn { get; set; }
    public DateTime? CorrectedClockOut { get; set; }
    public string Reason { get; set; } = null!;
    public string? SupportingDocuments { get; set; }
    public string Status { get; set; } = "PENDING"; // "PENDING", "APPROVED", "REJECTED"
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public AttendanceRecord Record { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}

public class OvertimePolicy
{
    public Guid PolicyId { get; set; }
    public string PolicyName { get; set; } = null!;
    public string? PolicyCode { get; set; }
    public string Description { get; set; } = null!;
    
    // Daily overtime thresholds
    public TimeSpan DailyThreshold { get; set; } = TimeSpan.FromHours(8);
    public decimal DailyMultiplier { get; set; } = 1.5m; // 1.5x for daily overtime
    
    // Weekly overtime thresholds
    public bool ApplyWeeklyRule { get; set; } = true;
    public TimeSpan WeeklyThreshold { get; set; } = TimeSpan.FromHours(40);
    public decimal WeeklyMultiplier { get; set; } = 1.5m;
    
    // Weekend overtime
    public bool ApplyWeekendRule { get; set; } = true;
    public decimal WeekendMultiplier { get; set; } = 2.0m; // 2x for weekends
    
    // Holiday overtime
    public bool ApplyHolidayRule { get; set; } = true;
    public decimal HolidayMultiplier { get; set; } = 3.0m; // 3x for holidays
    
    // Maximum daily overtime allowed
    public TimeSpan? MaxDailyOvertime { get; set; }
    
    // Minimum overtime increment (e.g., 15 minutes)
    public int MinimumOvertimeMinutes { get; set; } = 15;
    
    // Auto-approval threshold
    public TimeSpan? AutoApprovalThreshold { get; set; }
    
    // Effective dates
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<Shift> Shifts { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
}
