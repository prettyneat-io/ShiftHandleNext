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
