using Microsoft.EntityFrameworkCore;
using PunchClockApi.Models;

namespace PunchClockApi.Data;

public class PunchClockDbContext : DbContext
{
    public PunchClockDbContext(DbContextOptions<PunchClockDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<BiometricTemplate> BiometricTemplates { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceEnrollment> DeviceEnrollments { get; set; }
    public DbSet<PunchLog> PunchLogs { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<AttendanceCorrection> AttendanceCorrections { get; set; }
    public DbSet<OvertimePolicy> OvertimePolicies { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ExportLog> ExportLogs { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<Holiday> Holidays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(255);
            entity.Property(e => e.PasswordResetExpires).HasColumnName("password_reset_expires");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).HasColumnName("role_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RoleDescription).HasColumnName("role_description");
            entity.Property(e => e.IsSystemRole).HasColumnName("is_system_role").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.PermissionId).HasColumnName("permission_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.PermissionName).HasColumnName("permission_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PermissionDescription).HasColumnName("permission_description");
            entity.Property(e => e.Resource).HasColumnName("resource").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.PermissionName).IsUnique();
        });

        // UserRole (junction table)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        // RolePermission (junction table)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.GrantedAt).HasColumnName("granted_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.GrantedBy).HasColumnName("granted_by");
            entity.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.DepartmentId).HasColumnName("department_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DepartmentName).HasColumnName("department_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.DepartmentCode).HasColumnName("department_code").HasMaxLength(20);
            entity.Property(e => e.ParentDepartmentId).HasColumnName("parent_department_id");
            entity.Property(e => e.ManagerStaffId).HasColumnName("manager_staff_id");
            entity.Property(e => e.OvertimePolicyId).HasColumnName("overtime_policy_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.ParentDepartment).WithMany(d => d.SubDepartments).HasForeignKey(e => e.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OvertimePolicy).WithMany(p => p.Departments).HasForeignKey(e => e.OvertimePolicyId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.DepartmentCode).IsUnique();
        });

        // Location
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(e => e.LocationId);
            entity.Property(e => e.LocationId).HasColumnName("location_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.LocationName).HasColumnName("location_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LocationCode).HasColumnName("location_code").HasMaxLength(20);
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1").HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasColumnName("state_province").HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.Timezone).HasColumnName("timezone").HasMaxLength(50).HasDefaultValue("UTC");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.LocationCode).IsUnique();
        });

        // Shift
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("shifts");
            entity.HasKey(e => e.ShiftId);
            entity.Property(e => e.ShiftId).HasColumnName("shift_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ShiftName).HasColumnName("shift_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ShiftCode).HasColumnName("shift_code").HasMaxLength(20);
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.RequiredHours).HasColumnName("required_hours");
            entity.Property(e => e.GracePeriodMinutes).HasColumnName("grace_period_minutes").HasDefaultValue(15);
            entity.Property(e => e.LateThresholdMinutes).HasColumnName("late_threshold_minutes").HasDefaultValue(15);
            entity.Property(e => e.EarlyLeaveThresholdMinutes).HasColumnName("early_leave_threshold_minutes").HasDefaultValue(15);
            entity.Property(e => e.HasBreak).HasColumnName("has_break").HasDefaultValue(true);
            entity.Property(e => e.BreakDuration).HasColumnName("break_duration");
            entity.Property(e => e.BreakStartTime).HasColumnName("break_start_time");
            entity.Property(e => e.AutoDeductBreak).HasColumnName("auto_deduct_break").HasDefaultValue(true);
            entity.Property(e => e.OvertimePolicyId).HasColumnName("overtime_policy_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.ShiftCode).IsUnique();
            entity.HasOne(e => e.OvertimePolicy).WithMany(p => p.Shifts).HasForeignKey(e => e.OvertimePolicyId).OnDelete(DeleteBehavior.SetNull);
        });

        // Staff
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");
            entity.HasKey(e => e.StaffId);
            entity.Property(e => e.StaffId).HasColumnName("staff_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.BadgeNumber).HasColumnName("badge_number").HasMaxLength(50);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.MiddleName).HasColumnName("middle_name").HasMaxLength(100);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Mobile).HasColumnName("mobile").HasMaxLength(20);
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.PositionTitle).HasColumnName("position_title").HasMaxLength(100);
            entity.Property(e => e.EmploymentType).HasColumnName("employment_type").HasMaxLength(20);
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.TerminationDate).HasColumnName("termination_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.EnrollmentStatus).HasColumnName("enrollment_status").HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.HasOne(e => e.Department).WithMany(d => d.StaffMembers).HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Location).WithMany(l => l.StaffMembers).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Shift).WithMany(s => s.StaffMembers).HasForeignKey(e => e.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasIndex(e => e.BadgeNumber).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // BiometricTemplate
        modelBuilder.Entity<BiometricTemplate>(entity =>
        {
            entity.ToTable("biometric_templates");
            entity.HasKey(e => e.TemplateId);
            entity.Property(e => e.TemplateId).HasColumnName("template_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TemplateType).HasColumnName("template_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TemplateData).HasColumnName("template_data").IsRequired();
            entity.Property(e => e.TemplateFormat).HasColumnName("template_format").HasMaxLength(50);
            entity.Property(e => e.FingerIndex).HasColumnName("finger_index");
            entity.Property(e => e.QualityScore).HasColumnName("quality_score");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.EnrolledAt).HasColumnName("enrolled_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.EnrolledBy).HasColumnName("enrolled_by");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Staff).WithMany(s => s.BiometricTemplates).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Device).WithMany(d => d.BiometricTemplates).HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.SetNull);
        });

        // Device
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.DeviceId).HasColumnName("device_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DeviceSerial).HasColumnName("device_serial").HasMaxLength(100).IsRequired();
            entity.Property(e => e.DeviceName).HasColumnName("device_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.DeviceModel).HasColumnName("device_model").HasMaxLength(50);
            entity.Property(e => e.Manufacturer).HasColumnName("manufacturer").HasMaxLength(100).IsRequired();
            entity.Property(e => e.FirmwareVersion).HasColumnName("firmware_version").HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            entity.Property(e => e.MacAddress).HasColumnName("mac_address").HasMaxLength(17);
            entity.Property(e => e.Port).HasColumnName("port").HasDefaultValue(4370);
            entity.Property(e => e.ConnectionType).HasColumnName("connection_type").HasMaxLength(20).HasDefaultValue("TCP/IP");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.InstallationDate).HasColumnName("installation_date");
            entity.Property(e => e.UserCapacity).HasColumnName("user_capacity");
            entity.Property(e => e.LogCapacity).HasColumnName("log_capacity");
            entity.Property(e => e.FingerprintCapacity).HasColumnName("fingerprint_capacity");
            entity.Property(e => e.FaceCapacity).HasColumnName("face_capacity");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsOnline).HasColumnName("is_online").HasDefaultValue(false);
            entity.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(e => e.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
            entity.Property(e => e.DeviceConfig).HasColumnName("device_config").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.HasOne(e => e.Location).WithMany(l => l.Devices).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.DeviceSerial).IsUnique();
        });

        // DeviceEnrollment
        modelBuilder.Entity<DeviceEnrollment>(entity =>
        {
            entity.ToTable("device_enrollments");
            entity.HasKey(e => e.EnrollmentId);
            entity.Property(e => e.EnrollmentId).HasColumnName("enrollment_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.DeviceUserId).HasColumnName("device_user_id");
            entity.Property(e => e.EnrollmentStatus).HasColumnName("enrollment_status").HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.EnrolledAt).HasColumnName("enrolled_at");
            entity.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(e => e.SyncStatus).HasColumnName("sync_status").HasMaxLength(20);
            entity.Property(e => e.SyncErrorMessage).HasColumnName("sync_error_message");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Device).WithMany(d => d.DeviceEnrollments).HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Staff).WithMany(s => s.DeviceEnrollments).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DeviceId, e.StaffId }).IsUnique();
        });

        // PunchLog
        modelBuilder.Entity<PunchLog>(entity =>
        {
            entity.ToTable("punch_logs");
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.LogId).HasColumnName("log_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.PunchTime).HasColumnName("punch_time");
            entity.Property(e => e.PunchType).HasColumnName("punch_type").HasMaxLength(20);
            entity.Property(e => e.VerificationMode).HasColumnName("verification_mode").HasMaxLength(20);
            entity.Property(e => e.DeviceUserId).HasColumnName("device_user_id");
            entity.Property(e => e.DeviceLogId).HasColumnName("device_log_id");
            entity.Property(e => e.WorkCode).HasColumnName("work_code").HasMaxLength(20);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasColumnType("decimal(11,8)");
            entity.Property(e => e.IsProcessed).HasColumnName("is_processed").HasDefaultValue(false);
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.IsManualEntry).HasColumnName("is_manual_entry").HasDefaultValue(false);
            entity.Property(e => e.ManualEntryReason).HasColumnName("manual_entry_reason");
            entity.Property(e => e.IsValid).HasColumnName("is_valid").HasDefaultValue(true);
            entity.Property(e => e.ValidationErrors).HasColumnName("validation_errors").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImportedAt).HasColumnName("imported_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.HasOne(e => e.Staff).WithMany(s => s.PunchLogs).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Device).WithMany(d => d.PunchLogs).HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.SetNull);
        });

        // AttendanceRecord
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("attendance_records");
            entity.HasKey(e => e.RecordId);
            entity.Property(e => e.RecordId).HasColumnName("record_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");
            entity.Property(e => e.ClockIn).HasColumnName("clock_in");
            entity.Property(e => e.ClockOut).HasColumnName("clock_out");
            entity.Property(e => e.TotalHours).HasColumnName("total_hours");
            entity.Property(e => e.RegularHours).HasColumnName("regular_hours");
            entity.Property(e => e.OvertimeHours).HasColumnName("overtime_hours");
            entity.Property(e => e.BreakDuration).HasColumnName("break_duration");
            entity.Property(e => e.LateMinutes).HasColumnName("late_minutes");
            entity.Property(e => e.EarlyLeaveMinutes).HasColumnName("early_leave_minutes");
            entity.Property(e => e.AttendanceStatus).HasColumnName("attendance_status").HasMaxLength(20).HasDefaultValue("PRESENT");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved").HasDefaultValue(false);
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.HasAnomalies).HasColumnName("has_anomalies").HasDefaultValue(false);
            entity.Property(e => e.AnomalyFlags).HasColumnName("anomaly_flags").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.HasOne(e => e.Staff).WithMany(s => s.AttendanceRecords).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.StaffId, e.AttendanceDate }).IsUnique();
        });

        // AttendanceCorrection
        modelBuilder.Entity<AttendanceCorrection>(entity =>
        {
            entity.ToTable("attendance_corrections");
            entity.HasKey(e => e.CorrectionId);
            entity.Property(e => e.CorrectionId).HasColumnName("correction_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");
            entity.Property(e => e.CorrectionType).HasColumnName("correction_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.OriginalClockIn).HasColumnName("original_clock_in");
            entity.Property(e => e.OriginalClockOut).HasColumnName("original_clock_out");
            entity.Property(e => e.CorrectedClockIn).HasColumnName("corrected_clock_in");
            entity.Property(e => e.CorrectedClockOut).HasColumnName("corrected_clock_out");
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.SupportingDocuments).HasColumnName("supporting_documents").HasColumnType("jsonb");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewNotes).HasColumnName("review_notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Record).WithMany().HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Staff).WithMany().HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReviewedByUser).WithMany().HasForeignKey(e => e.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.RecordId);
            entity.HasIndex(e => e.StaffId);
            entity.HasIndex(e => e.Status);
        });

        // OvertimePolicy
        modelBuilder.Entity<OvertimePolicy>(entity =>
        {
            entity.ToTable("overtime_policies");
            entity.HasKey(e => e.PolicyId);
            entity.Property(e => e.PolicyId).HasColumnName("policy_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.PolicyName).HasColumnName("policy_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PolicyCode).HasColumnName("policy_code").HasMaxLength(20);
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.DailyThreshold).HasColumnName("daily_threshold").HasDefaultValue(TimeSpan.FromHours(8));
            entity.Property(e => e.DailyMultiplier).HasColumnName("daily_multiplier").HasColumnType("decimal(5,2)").HasDefaultValue(1.5m);
            entity.Property(e => e.ApplyWeeklyRule).HasColumnName("apply_weekly_rule").HasDefaultValue(true);
            entity.Property(e => e.WeeklyThreshold).HasColumnName("weekly_threshold").HasDefaultValue(TimeSpan.FromHours(40));
            entity.Property(e => e.WeeklyMultiplier).HasColumnName("weekly_multiplier").HasColumnType("decimal(5,2)").HasDefaultValue(1.5m);
            entity.Property(e => e.ApplyWeekendRule).HasColumnName("apply_weekend_rule").HasDefaultValue(true);
            entity.Property(e => e.WeekendMultiplier).HasColumnName("weekend_multiplier").HasColumnType("decimal(5,2)").HasDefaultValue(2.0m);
            entity.Property(e => e.ApplyHolidayRule).HasColumnName("apply_holiday_rule").HasDefaultValue(true);
            entity.Property(e => e.HolidayMultiplier).HasColumnName("holiday_multiplier").HasColumnType("decimal(5,2)").HasDefaultValue(3.0m);
            entity.Property(e => e.MaxDailyOvertime).HasColumnName("max_daily_overtime");
            entity.Property(e => e.MinimumOvertimeMinutes).HasColumnName("minimum_overtime_minutes").HasDefaultValue(15);
            entity.Property(e => e.AutoApprovalThreshold).HasColumnName("auto_approval_threshold");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.HasIndex(e => e.PolicyCode).IsUnique();
            entity.HasIndex(e => e.IsDefault);
        });

        // SyncLog
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.ToTable("sync_logs");
            entity.HasKey(e => e.SyncId);
            entity.Property(e => e.SyncId).HasColumnName("sync_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.SyncType).HasColumnName("sync_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SyncStatus).HasColumnName("sync_status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.RecordsProcessed).HasColumnName("records_processed");
            entity.Property(e => e.RecordsFailed).HasColumnName("records_failed");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ErrorDetails).HasColumnName("error_details");
            entity.Property(e => e.InitiatedBy).HasColumnName("initiated_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Device).WithMany(d => d.SyncLogs).HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.AuditId).HasColumnName("audit_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(20).IsRequired();
            entity.Property(e => e.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.User).WithMany(u => u.AuditLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ExportLog
        modelBuilder.Entity<ExportLog>(entity =>
        {
            entity.ToTable("export_logs");
            entity.HasKey(e => e.ExportId);
            entity.Property(e => e.ExportId).HasColumnName("export_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ExportType).HasColumnName("export_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FilterCriteria).HasColumnName("filter_criteria").HasColumnType("jsonb");
            entity.Property(e => e.RecordCount).HasColumnName("record_count");
            entity.Property(e => e.FileFormat).HasColumnName("file_format").HasMaxLength(20).IsRequired();
            entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(500);
            entity.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(500);
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.ExportedBy).HasColumnName("exported_by");
            entity.Property(e => e.ExportedAt).HasColumnName("exported_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ExportStatus).HasColumnName("export_status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ExportMetadata).HasColumnName("export_metadata").HasColumnType("jsonb");
        });

        // LeaveType
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("leave_types");
            entity.HasKey(e => e.LeaveTypeId);
            entity.Property(e => e.LeaveTypeId).HasColumnName("leave_type_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TypeName).HasColumnName("type_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.TypeCode).HasColumnName("type_code").HasMaxLength(20);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval").HasDefaultValue(true);
            entity.Property(e => e.RequiresDocumentation).HasColumnName("requires_documentation").HasDefaultValue(false);
            entity.Property(e => e.MaxDaysPerYear).HasColumnName("max_days_per_year");
            entity.Property(e => e.MinDaysNotice).HasColumnName("min_days_notice");
            entity.Property(e => e.IsPaid).HasColumnName("is_paid").HasDefaultValue(true);
            entity.Property(e => e.AllowsHalfDay).HasColumnName("allows_half_day").HasDefaultValue(true);
            entity.Property(e => e.AllowsHourly).HasColumnName("allows_hourly").HasDefaultValue(false);
            entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(7);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TypeCode).IsUnique();
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.HasKey(e => e.LeaveRequestId);
            entity.Property(e => e.LeaveRequestId).HasColumnName("leave_request_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StaffId).HasColumnName("staff_id").IsRequired();
            entity.Property(e => e.LeaveTypeId).HasColumnName("leave_type_id").IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
            entity.Property(e => e.TotalDays).HasColumnName("total_days").HasPrecision(4, 2).IsRequired();
            entity.Property(e => e.TotalHours).HasColumnName("total_hours");
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.SupportingDocuments).HasColumnName("supporting_documents").HasColumnType("jsonb");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by").IsRequired();
            entity.Property(e => e.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewNotes).HasColumnName("review_notes");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.AffectsAttendance).HasColumnName("affects_attendance").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Staff).WithMany(s => s.LeaveRequests).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LeaveType).WithMany(lt => lt.LeaveRequests).HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RequestedByUser).WithMany(u => u.RequestedLeaveRequests).HasForeignKey(e => e.RequestedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReviewedByUser).WithMany(u => u.ReviewedLeaveRequests).HasForeignKey(e => e.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CancelledByUser).WithMany(u => u.CancelledLeaveRequests).HasForeignKey(e => e.CancelledBy).OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.StaffId, e.StartDate, e.EndDate });
            entity.HasIndex(e => e.Status);
        });

        // LeaveBalance
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("leave_balances");
            entity.HasKey(e => e.LeaveBalanceId);
            entity.Property(e => e.LeaveBalanceId).HasColumnName("leave_balance_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StaffId).HasColumnName("staff_id").IsRequired();
            entity.Property(e => e.LeaveTypeId).HasColumnName("leave_type_id").IsRequired();
            entity.Property(e => e.Year).HasColumnName("year").IsRequired();
            entity.Property(e => e.TotalAllocation).HasColumnName("total_allocation").HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.CarryForward).HasColumnName("carry_forward").HasPrecision(5, 2).HasDefaultValue(0);
            entity.Property(e => e.Used).HasColumnName("used").HasPrecision(5, 2).HasDefaultValue(0);
            entity.Property(e => e.Pending).HasColumnName("pending").HasPrecision(5, 2).HasDefaultValue(0);
            entity.Property(e => e.Available).HasColumnName("available").HasPrecision(5, 2).HasDefaultValue(0);
            entity.Property(e => e.LastAccrualDate).HasColumnName("last_accrual_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Staff).WithMany(s => s.LeaveBalances).HasForeignKey(e => e.StaffId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LeaveType).WithMany(lt => lt.LeaveBalances).HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.StaffId, e.LeaveTypeId, e.Year }).IsUnique();
        });

        // Holiday
        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.ToTable("holidays");
            entity.HasKey(e => e.HolidayId);
            entity.Property(e => e.HolidayId).HasColumnName("holiday_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.HolidayName).HasColumnName("holiday_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.HolidayDate).HasColumnName("holiday_date").IsRequired();
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(false);
            entity.Property(e => e.IsMandatory).HasColumnName("is_mandatory").HasDefaultValue(true);
            entity.Property(e => e.IsPaid).HasColumnName("is_paid").HasDefaultValue(true);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Location).WithMany(l => l.Holidays).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => new { e.HolidayDate, e.LocationId });
        });
    }
}
