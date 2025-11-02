namespace PunchClockApi.Models;

public class Department
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? DepartmentCode { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? ManagerStaffId { get; set; }
    public Guid? OvertimePolicyId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = [];
    public ICollection<Staff> StaffMembers { get; set; } = [];
    public OvertimePolicy? OvertimePolicy { get; set; }
}

public class Location
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public string? LocationCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string Timezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Device> Devices { get; set; } = [];
    public ICollection<Staff> StaffMembers { get; set; } = [];
    public ICollection<Holiday> Holidays { get; set; } = [];
}

public class Shift
{
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = null!;
    public string? ShiftCode { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeSpan RequiredHours { get; set; }
    public int GracePeriodMinutes { get; set; } = 15;
    public int LateThresholdMinutes { get; set; } = 15;
    public int EarlyLeaveThresholdMinutes { get; set; } = 15;
    public bool HasBreak { get; set; } = true;
    public TimeSpan? BreakDuration { get; set; }
    public TimeOnly? BreakStartTime { get; set; }
    public bool AutoDeductBreak { get; set; } = true;
    public Guid? OvertimePolicyId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public OvertimePolicy? OvertimePolicy { get; set; }
    public ICollection<Staff> StaffMembers { get; set; } = [];
}
