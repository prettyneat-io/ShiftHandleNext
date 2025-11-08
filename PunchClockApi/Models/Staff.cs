namespace PunchClockApi.Models;

public class Staff
{
    public Guid StaffId { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string? BadgeNumber { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? ShiftId { get; set; }
    public string? PositionTitle { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string EnrollmentStatus { get; set; } = "PENDING";
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public Location? Location { get; set; }
    public Shift? Shift { get; set; }
    public User? User { get; set; }
    public ICollection<BiometricTemplate> BiometricTemplates { get; set; } = [];
    public ICollection<DeviceEnrollment> DeviceEnrollments { get; set; } = [];
    public ICollection<PunchLog> PunchLogs { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
}

public class BiometricTemplate
{
    public Guid TemplateId { get; set; }
    public Guid StaffId { get; set; }
    public string TemplateType { get; set; } = null!;
    public string TemplateData { get; set; } = null!;
    public string? TemplateFormat { get; set; }
    public int? FingerIndex { get; set; }
    public int? QualityScore { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EnrolledAt { get; set; }
    public Guid? EnrolledBy { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Staff Staff { get; set; } = null!;
    public Device? Device { get; set; }
}
