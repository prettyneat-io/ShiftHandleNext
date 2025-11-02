using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Models;

namespace PunchClockApi.Data;

public class DatabaseSeeder
{
    private readonly PunchClockDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(PunchClockDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Check if data already exists
            if (await _context.Departments.AnyAsync() || await _context.Locations.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            await SeedUsersAndRolesAsync();
            await SeedOrganizationsAsync();
            await SeedShiftsAsync();
            await SeedStaffAsync();
            await SeedDevicesAsync();
            await SeedAttendanceDataAsync();

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedUsersAndRolesAsync()
    {
        _logger.LogInformation("Seeding users, roles, and permissions...");

        // Create permissions
        var permissions = new List<Permission>
        {
            new() { PermissionId = Guid.NewGuid(), PermissionName = "View Staff", Resource = "staff", Action = "read", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "Create Staff", Resource = "staff", Action = "create", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "Update Staff", Resource = "staff", Action = "update", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "Delete Staff", Resource = "staff", Action = "delete", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "View Devices", Resource = "devices", Action = "read", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "Manage Devices", Resource = "devices", Action = "manage", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "View Attendance", Resource = "attendance", Action = "read", CreatedAt = DateTime.UtcNow },
            new() { PermissionId = Guid.NewGuid(), PermissionName = "Export Attendance", Resource = "attendance", Action = "export", CreatedAt = DateTime.UtcNow }
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Create roles
        var adminRole = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = "Admin",
            RoleDescription = "System administrator with full access",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userRole = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = "User",
            RoleDescription = "Standard user with limited access",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var managerRole = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = "Manager",
            RoleDescription = "Manager with elevated permissions",
            IsSystemRole = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Roles.AddRangeAsync(adminRole, userRole, managerRole);
        await _context.SaveChangesAsync();

        // Assign all permissions to Admin role
        var adminRolePermissions = permissions.Select(p => new RolePermission
        {
            RoleId = adminRole.RoleId,
            PermissionId = p.PermissionId,
            GrantedAt = DateTime.UtcNow
        }).ToList();

        await _context.RolePermissions.AddRangeAsync(adminRolePermissions);
        await _context.SaveChangesAsync();

        // Create default admin user (password: "admin123")
        var adminUser = new User
        {
            UserId = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@punchclock.local",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = HashPassword("admin123"),
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create default regular user (password: "user123")
        var regularUser = new User
        {
            UserId = Guid.NewGuid(),
            Username = "user",
            Email = "user@punchclock.local",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = HashPassword("user123"),
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddRangeAsync(adminUser, regularUser);
        await _context.SaveChangesAsync();

        // Assign roles to users
        var userRoles = new List<UserRole>
        {
            new() { UserId = adminUser.UserId, RoleId = adminRole.RoleId, AssignedAt = DateTime.UtcNow },
            new() { UserId = regularUser.UserId, RoleId = userRole.RoleId, AssignedAt = DateTime.UtcNow }
        };

        await _context.UserRoles.AddRangeAsync(userRoles);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {UserCount} users, {RoleCount} roles, and {PermissionCount} permissions",
            2, 3, permissions.Count);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private async Task SeedOrganizationsAsync()
    {
        _logger.LogInformation("Seeding departments and locations...");

        var departments = new List<Department>
        {
            new Department
            {
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Engineering",
                DepartmentCode = "ENG",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Department
            {
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Human Resources",
                DepartmentCode = "HR",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Department
            {
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Sales",
                DepartmentCode = "SALES",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Department
            {
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Operations",
                DepartmentCode = "OPS",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Department
            {
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Finance",
                DepartmentCode = "FIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var locations = new List<Location>
        {
            new Location
            {
                LocationId = Guid.NewGuid(),
                LocationName = "Headquarters",
                LocationCode = "HQ",
                AddressLine1 = "123 Main Street",
                City = "New York",
                StateProvince = "NY",
                PostalCode = "10001",
                Country = "USA",
                Timezone = "America/New_York",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Location
            {
                LocationId = Guid.NewGuid(),
                LocationName = "West Coast Office",
                LocationCode = "WC",
                AddressLine1 = "456 Tech Boulevard",
                City = "San Francisco",
                StateProvince = "CA",
                PostalCode = "94105",
                Country = "USA",
                Timezone = "America/Los_Angeles",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Location
            {
                LocationId = Guid.NewGuid(),
                LocationName = "Warehouse Facility",
                LocationCode = "WH",
                AddressLine1 = "789 Industrial Park",
                City = "Chicago",
                StateProvince = "IL",
                PostalCode = "60601",
                Country = "USA",
                Timezone = "America/Chicago",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Departments.AddRangeAsync(departments);
        await _context.Locations.AddRangeAsync(locations);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {departments.Count} departments and {locations.Count} locations.");
    }

    private async Task SeedShiftsAsync()
    {
        _logger.LogInformation("Seeding shifts...");

        var shifts = new List<Shift>
        {
            new Shift
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Morning Shift",
                ShiftCode = "MORNING",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                RequiredHours = TimeSpan.FromHours(8),
                GracePeriodMinutes = 15,
                LateThresholdMinutes = 15,
                EarlyLeaveThresholdMinutes = 15,
                HasBreak = true,
                BreakDuration = TimeSpan.FromHours(1),
                BreakStartTime = new TimeOnly(12, 0),
                AutoDeductBreak = true,
                IsActive = true,
                Description = "Standard morning shift: 8:00 AM - 5:00 PM with 1 hour lunch break",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Shift
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Evening Shift",
                ShiftCode = "EVENING",
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(0, 0),
                RequiredHours = TimeSpan.FromHours(8),
                GracePeriodMinutes = 15,
                LateThresholdMinutes = 15,
                EarlyLeaveThresholdMinutes = 15,
                HasBreak = true,
                BreakDuration = TimeSpan.FromMinutes(30),
                BreakStartTime = new TimeOnly(20, 0),
                AutoDeductBreak = true,
                IsActive = true,
                Description = "Evening shift: 4:00 PM - 12:00 AM with 30 minute break",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Shift
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Night Shift",
                ShiftCode = "NIGHT",
                StartTime = new TimeOnly(23, 0),
                EndTime = new TimeOnly(7, 0),
                RequiredHours = TimeSpan.FromHours(8),
                GracePeriodMinutes = 15,
                LateThresholdMinutes = 15,
                EarlyLeaveThresholdMinutes = 15,
                HasBreak = true,
                BreakDuration = TimeSpan.FromMinutes(30),
                BreakStartTime = new TimeOnly(3, 0),
                AutoDeductBreak = true,
                IsActive = true,
                Description = "Night shift: 11:00 PM - 7:00 AM with 30 minute break",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Shift
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Flexible Shift",
                ShiftCode = "FLEXIBLE",
                StartTime = new TimeOnly(0, 0),
                EndTime = new TimeOnly(23, 59),
                RequiredHours = TimeSpan.FromHours(8),
                GracePeriodMinutes = 30,
                LateThresholdMinutes = 30,
                EarlyLeaveThresholdMinutes = 30,
                HasBreak = false,
                AutoDeductBreak = false,
                IsActive = true,
                Description = "Flexible hours - staff can work any 8 hours within the day",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Shifts.AddRangeAsync(shifts);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {shifts.Count} shifts.");
    }

    private async Task SeedStaffAsync()
    {
        _logger.LogInformation("Seeding staff members...");

        var departments = await _context.Departments.ToListAsync();
        var locations = await _context.Locations.ToListAsync();
        var shifts = await _context.Shifts.ToListAsync();

        if (!departments.Any() || !locations.Any())
        {
            _logger.LogWarning("No departments or locations found. Skipping staff seeding.");
            return;
        }

        // Get shift references (with fallback if no shifts exist yet)
        var morningShift = shifts.FirstOrDefault(s => s.ShiftCode == "MORNING");
        var eveningShift = shifts.FirstOrDefault(s => s.ShiftCode == "EVENING");
        var nightShift = shifts.FirstOrDefault(s => s.ShiftCode == "NIGHT");

        var staff = new List<Staff>
        {
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP001",
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@company.com",
                Phone = "+1-555-0101",
                Mobile = "+1-555-0102",
                BadgeNumber = "EMP001",
                DepartmentId = departments.First(d => d.DepartmentCode == "ENG").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                ShiftId = morningShift?.ShiftId,
                PositionTitle = "Senior Software Engineer",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-3).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP002",
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@company.com",
                Phone = "+1-555-0201",
                Mobile = "+1-555-0202",
                BadgeNumber = "EMP002",
                DepartmentId = departments.First(d => d.DepartmentCode == "HR").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                ShiftId = morningShift?.ShiftId,
                PositionTitle = "HR Manager",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-5).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP003",
                FirstName = "Michael",
                LastName = "Chen",
                Email = "michael.chen@company.com",
                Phone = "+1-555-0301",
                Mobile = "+1-555-0302",
                BadgeNumber = "EMP003",
                DepartmentId = departments.First(d => d.DepartmentCode == "SALES").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "WC").LocationId,
                ShiftId = morningShift?.ShiftId,
                PositionTitle = "Sales Representative",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-2).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP004",
                FirstName = "Emily",
                LastName = "Davis",
                Email = "emily.davis@company.com",
                Phone = "+1-555-0401",
                Mobile = "+1-555-0402",
                BadgeNumber = "EMP004",
                DepartmentId = departments.First(d => d.DepartmentCode == "ENG").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "WC").LocationId,
                ShiftId = eveningShift?.ShiftId,
                PositionTitle = "DevOps Engineer",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-1).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP005",
                FirstName = "David",
                LastName = "Martinez",
                Email = "david.martinez@company.com",
                Phone = "+1-555-0501",
                Mobile = "+1-555-0502",
                BadgeNumber = "EMP005",
                DepartmentId = departments.First(d => d.DepartmentCode == "OPS").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "WH").LocationId,
                ShiftId = nightShift?.ShiftId,
                PositionTitle = "Warehouse Supervisor",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-4).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP006",
                FirstName = "Lisa",
                LastName = "Anderson",
                Email = "lisa.anderson@company.com",
                Phone = "+1-555-0601",
                Mobile = "+1-555-0602",
                BadgeNumber = "EMP006",
                DepartmentId = departments.First(d => d.DepartmentCode == "FIN").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                ShiftId = morningShift?.ShiftId,
                PositionTitle = "Financial Analyst",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddMonths(-8).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP007",
                FirstName = "Robert",
                LastName = "Wilson",
                Email = "robert.wilson@company.com",
                Phone = "+1-555-0701",
                BadgeNumber = "EMP007",
                DepartmentId = departments.First(d => d.DepartmentCode == "OPS").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "WH").LocationId,
                ShiftId = eveningShift?.ShiftId,
                PositionTitle = "Warehouse Associate",
                EmploymentType = "PART_TIME",
                HireDate = DateTime.UtcNow.AddMonths(-3).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "EMP008",
                FirstName = "Jennifer",
                LastName = "Taylor",
                Email = "jennifer.taylor@company.com",
                Phone = "+1-555-0801",
                Mobile = "+1-555-0802",
                BadgeNumber = "EMP008",
                DepartmentId = departments.First(d => d.DepartmentCode == "SALES").DepartmentId,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                ShiftId = morningShift?.ShiftId,
                PositionTitle = "Sales Director",
                EmploymentType = "FULL_TIME",
                HireDate = DateTime.UtcNow.AddYears(-6).Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Staff.AddRangeAsync(staff);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {staff.Count} staff members.");
    }

    private async Task SeedDevicesAsync()
    {
        _logger.LogInformation("Seeding devices...");

        var locations = await _context.Locations.ToListAsync();

        if (!locations.Any())
        {
            _logger.LogWarning("No locations found. Skipping device seeding.");
            return;
        }

        var devices = new List<Device>
        {
            new Device
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = "HQ Main Entrance",
                DeviceSerial = "ZK001-2024-001",
                DeviceModel = "ZKTeco K50",
                Manufacturer = "ZKTeco",
                IpAddress = "192.168.1.101",
                Port = 4370,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                IsActive = true,
                IsOnline = true,
                LastSyncAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Device
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = "HQ Back Office",
                DeviceSerial = "ZK001-2024-002",
                DeviceModel = "ZKTeco K40",
                Manufacturer = "ZKTeco",
                IpAddress = "192.168.1.102",
                Port = 4370,
                LocationId = locations.First(l => l.LocationCode == "HQ").LocationId,
                IsActive = true,
                IsOnline = true,
                LastSyncAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Device
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = "West Coast Entrance",
                DeviceSerial = "ZK001-2024-003",
                DeviceModel = "ZKTeco K50",
                Manufacturer = "ZKTeco",
                IpAddress = "192.168.2.101",
                Port = 4370,
                LocationId = locations.First(l => l.LocationCode == "WC").LocationId,
                IsActive = true,
                IsOnline = true,
                LastSyncAt = DateTime.UtcNow.AddMinutes(-30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Device
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = "Warehouse Entry",
                DeviceSerial = "ZK001-2024-004",
                DeviceModel = "ZKTeco K40",
                Manufacturer = "ZKTeco",
                IpAddress = "192.168.3.101",
                Port = 4370,
                LocationId = locations.First(l => l.LocationCode == "WH").LocationId,
                IsActive = true,
                IsOnline = false,
                LastSyncAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Devices.AddRangeAsync(devices);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {devices.Count} devices.");
    }

    private async Task SeedAttendanceDataAsync()
    {
        _logger.LogInformation("Seeding attendance data...");

        var staff = await _context.Staff.ToListAsync();
        var devices = await _context.Devices.ToListAsync();

        if (!staff.Any() || !devices.Any())
        {
            _logger.LogWarning("No staff or devices found. Skipping attendance seeding.");
            return;
        }

        var punchLogs = new List<PunchLog>();
        var random = new Random();

        // Generate punch logs for the last 7 days
        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-daysAgo);

            // Skip weekends
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            foreach (var staffMember in staff.Where(s => s.EmploymentType == "FULL_TIME"))
            {
                var device = devices[random.Next(devices.Count)];

                // Clock in (8:00 AM ± 30 minutes)
                var clockInTime = date.AddHours(8).AddMinutes(random.Next(-30, 31));
                punchLogs.Add(new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staffMember.StaffId,
                    DeviceId = device.DeviceId,
                    PunchTime = clockInTime,
                    PunchType = "CHECK_IN",
                    VerificationMode = "FINGERPRINT",
                    IsManualEntry = false,
                    ImportedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                // Clock out (5:00 PM ± 45 minutes)
                var clockOutTime = date.AddHours(17).AddMinutes(random.Next(-45, 46));
                punchLogs.Add(new PunchLog
                {
                    LogId = Guid.NewGuid(),
                    StaffId = staffMember.StaffId,
                    DeviceId = device.DeviceId,
                    PunchTime = clockOutTime,
                    PunchType = "CHECK_OUT",
                    VerificationMode = "FINGERPRINT",
                    IsManualEntry = false,
                    ImportedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Part-time staff (50% attendance)
            foreach (var staffMember in staff.Where(s => s.EmploymentType == "PART_TIME"))
            {
                if (random.Next(2) == 0) // 50% chance
                {
                    var device = devices[random.Next(devices.Count)];

                    // Clock in
                    var clockInTime = date.AddHours(9).AddMinutes(random.Next(-15, 16));
                    punchLogs.Add(new PunchLog
                    {
                        LogId = Guid.NewGuid(),
                        StaffId = staffMember.StaffId,
                        DeviceId = device.DeviceId,
                        PunchTime = clockInTime,
                        PunchType = "CHECK_IN",
                        VerificationMode = "BADGE",
                        IsManualEntry = false,
                        ImportedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Clock out
                    var clockOutTime = date.AddHours(14).AddMinutes(random.Next(-15, 16));
                    punchLogs.Add(new PunchLog
                    {
                        LogId = Guid.NewGuid(),
                        StaffId = staffMember.StaffId,
                        DeviceId = device.DeviceId,
                        PunchTime = clockOutTime,
                        PunchType = "CHECK_OUT",
                        VerificationMode = "BADGE",
                        IsManualEntry = false,
                        ImportedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.PunchLogs.AddRangeAsync(punchLogs);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Seeded {punchLogs.Count} punch logs.");
    }
}
