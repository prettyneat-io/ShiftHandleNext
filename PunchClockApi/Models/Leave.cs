namespace PunchClockApi.Models;

/// <summary>
/// Leave type definitions (vacation, sick leave, personal leave, etc.)
/// </summary>
public class LeaveType
{
    public Guid LeaveTypeId { get; set; }
    public string TypeName { get; set; } = null!;
    public string? TypeCode { get; set; }
    public string? Description { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool RequiresDocumentation { get; set; } = false;
    public int? MaxDaysPerYear { get; set; }
    public int? MinDaysNotice { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool AllowsHalfDay { get; set; } = true;
    public bool AllowsHourly { get; set; } = false;
    public string? Color { get; set; } // For UI display
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
}

/// <summary>
/// Leave requests submitted by staff members
/// </summary>
public class LeaveRequest
{
    public Guid LeaveRequestId { get; set; }
    public Guid StaffId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; } // Supports half-day (0.5) and full-day (1.0)
    public TimeSpan? TotalHours { get; set; } // For hourly leave types
    public string Reason { get; set; } = null!;
    public string? SupportingDocuments { get; set; } // JSON array of file paths/URLs
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED, CANCELLED
    public Guid RequestedBy { get; set; } // Usually same as StaffId, but could be HR on behalf
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public bool AffectsAttendance { get; set; } = true; // Should this mark attendance as "ON_LEAVE"?
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Staff Staff { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public User? CancelledByUser { get; set; }
}

/// <summary>
/// Leave balance tracking per staff member per leave type
/// </summary>
public class LeaveBalance
{
    public Guid LeaveBalanceId { get; set; }
    public Guid StaffId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; } // Fiscal year for the balance
    public decimal TotalAllocation { get; set; } // Total leave days allocated for the year
    public decimal CarryForward { get; set; } = 0; // Days carried from previous year
    public decimal Used { get; set; } = 0; // Days already used
    public decimal Pending { get; set; } = 0; // Days in pending requests
    public decimal Available { get; set; } = 0; // Available = TotalAllocation + CarryForward - Used - Pending
    public DateTime? LastAccrualDate { get; set; } // For accrual-based leave systems
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Staff Staff { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}

/// <summary>
/// Holiday calendar for different locations/regions
/// </summary>
public class Holiday
{
    public Guid HolidayId { get; set; }
    public string HolidayName { get; set; } = null!;
    public DateOnly HolidayDate { get; set; }
    public Guid? LocationId { get; set; } // Null = applies to all locations
    public bool IsRecurring { get; set; } = false; // Does it repeat annually?
    public bool IsMandatory { get; set; } = true; // Is attendance prohibited?
    public bool IsPaid { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Location? Location { get; set; }
}
