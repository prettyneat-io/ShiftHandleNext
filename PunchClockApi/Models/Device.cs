namespace PunchClockApi.Models;

public class Device
{
    public Guid DeviceId { get; set; }
    public string DeviceSerial { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public string? DeviceModel { get; set; }
    public string Manufacturer { get; set; } = null!;
    public string? FirmwareVersion { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int Port { get; set; } = 4370;
    public string ConnectionType { get; set; } = "TCP/IP";
    public Guid? LocationId { get; set; }
    public DateTime? InstallationDate { get; set; }
    public int? UserCapacity { get; set; }
    public int? LogCapacity { get; set; }
    public int? FingerprintCapacity { get; set; }
    public int? FaceCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public string? DeviceConfig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Location? Location { get; set; }
    public ICollection<DeviceEnrollment> DeviceEnrollments { get; set; } = [];
    public ICollection<PunchLog> PunchLogs { get; set; } = [];
    public ICollection<SyncLog> SyncLogs { get; set; } = [];
    public ICollection<BiometricTemplate> BiometricTemplates { get; set; } = [];
}

public class DeviceEnrollment
{
    public Guid EnrollmentId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid StaffId { get; set; }
    public int? DeviceUserId { get; set; }
    public string EnrollmentStatus { get; set; } = "PENDING";
    public DateTime? EnrolledAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? SyncStatus { get; set; }
    public string? SyncErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Device Device { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
