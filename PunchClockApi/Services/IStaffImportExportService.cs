namespace PunchClockApi.Services;

public interface IStaffImportExportService
{
    /// <summary>
    /// Export staff to CSV format
    /// </summary>
    Task<byte[]> ExportStaffToCsvAsync(bool includeInactive = false);

    /// <summary>
    /// Import staff from CSV with validation
    /// </summary>
    Task<StaffImportResult> ImportStaffFromCsvAsync(Stream csvStream, bool updateExisting = false);

    /// <summary>
    /// Validate CSV import without saving
    /// </summary>
    Task<StaffImportResult> ValidateStaffImportAsync(Stream csvStream);
}

public class StaffImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<StaffImportError> Errors { get; set; } = [];
    public List<StaffImportSuccess> SuccessfulImports { get; set; } = [];
    public bool HasErrors => ErrorCount > 0;
}

public class StaffImportError
{
    public int RowNumber { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public Dictionary<string, string[]> ValidationErrors { get; set; } = [];
}

public class StaffImportSuccess
{
    public int RowNumber { get; set; }
    public string EmployeeId { get; set; } = null!;
    public Guid StaffId { get; set; }
    public bool IsNew { get; set; }
}
