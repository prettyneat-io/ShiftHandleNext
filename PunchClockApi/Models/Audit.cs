namespace PunchClockApi.Models;

public class SyncLog
{
    public Guid SyncId { get; set; }
    public Guid? DeviceId { get; set; }
    public string SyncType { get; set; } = null!;
    
    private string _syncStatus = null!;
    public string SyncStatus
    {
        get => _syncStatus;
        set => _syncStatus = value;
    }
    
    // Alias for SyncStatus for convenience
    public string Status
    {
        get => SyncStatus;
        set => SyncStatus = value;
    }
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    private int? _recordsProcessed;
    public int? RecordsProcessed
    {
        get => _recordsProcessed;
        set => _recordsProcessed = value;
    }
    
    // Alias for RecordsProcessed
    public int? RecordsSynced
    {
        get => RecordsProcessed;
        set => RecordsProcessed = value;
    }
    
    public int? RecordsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public Guid? InitiatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Device? Device { get; set; }
}

public class AuditLog
{
    public Guid AuditId { get; set; }
    public string TableName { get; set; } = null!;
    public Guid? RecordId { get; set; }
    public string Action { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? UserId { get; set; }
    public DateTime PerformedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}

public class ExportLog
{
    public Guid ExportId { get; set; }
    public string ExportType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? FilterCriteria { get; set; }
    public int RecordCount { get; set; }
    public string FileFormat { get; set; } = null!;
    public string? FilePath { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public Guid ExportedBy { get; set; }
    public DateTime ExportedAt { get; set; }
    public string ExportStatus { get; set; } = null!;
    public string? ExportMetadata { get; set; }
}
