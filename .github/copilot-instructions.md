# Punch Clock API - AI Agent Instructions

## Project Overview
A .NET 9.0 Web API backend for biometric punch clock synchronization and attendance management. The system centralizes staff enrollment, device management, and attendance tracking across multiple ZKTeco biometric devices with a PostgreSQL backend. Attribute-routed controllers inherit shared behavior from a custom `BaseController` to unify query parsing and error handling.

**Status**: Production-ready with JWT authentication, full ZKTeco device integration via PyZK, and comprehensive testing (59 integration tests).

## Architecture & Design Patterns

### Controller-Based API
- **Attribute Routing**: Each aggregate has its own `[ApiController]` class (e.g., `StaffController`, `DevicesController`)
- **Shared Base Class**: Controllers inherit from `BaseController<T>` for consistent query parsing, error handling, and user claim helpers
- **Constructor DI**: Dependencies injected via constructors (e.g., `StaffController(PunchClockDbContext db, ILogger<StaffController> logger)`)
- **Example Pattern**: See `Controllers/StaffController.cs` for the canonical CRUD structure
- **JWT Authentication**: Protected endpoints use `[Authorize]` attribute with role-based claims

### Entity Framework Core Patterns
- **Fluent Configuration**: All EF mappings in `PunchClockDbContext.OnModelCreating()` - NEVER use data annotations on models
- **Snake Case Convention**: Database columns use `snake_case` (e.g., `staff_id`, `created_at`) via explicit `.HasColumnName()` 
- **Navigation Properties**: Use EF Include() for eager loading: `db.Staff.Include(s => s.Department).Include(s => s.Location)`
- **Soft Deletes**: Set `IsActive = false` instead of actual deletion - applies to Staff, Devices, Departments, Locations

### Database Schema Design
- **UUIDs as PKs**: All primary keys are Guid with `gen_random_uuid()` default
- **Audit Fields**: Most tables have `created_at`, `updated_at`, `created_by`, `updated_by`
- **JSONB Support**: Used for flexible data (`device_config`, `validation_errors`, `anomaly_flags`, `export_metadata`)
- **Many-to-Many**: Junction tables like `UserRole` and `RolePermission` with composite keys
- **Self-Referencing**: Departments support hierarchy via `parent_department_id`

## Key Domain Concepts

### Data Flow Architecture
```
ZKTeco Device → PunchLog (raw) → AttendanceRecord (processed)
                      ↓
                 SyncLog tracking
```

1. **PunchLog**: Raw punch in/out from devices (immutable historical record)
2. **AttendanceRecord**: Daily summaries with calculations (total_hours, late_minutes, overtime_hours)
3. **SyncLog**: Tracks device synchronization operations (status: IN_PROGRESS, SUCCESS, FAILED)

### Staff Enrollment Lifecycle
- `enrollment_status` field: PENDING → IN_PROGRESS → COMPLETED
- Staff can have multiple `BiometricTemplate` records (different fingers, face templates)
- `DeviceEnrollment` tracks which devices each staff member is enrolled on
- `device_user_id` in DeviceEnrollment stores the device-specific internal ID

### Device Integration Architecture
- **PyZK Library**: Python ZKTeco device SDK integrated via Python.NET (pythonnet)
- **DeviceService**: C# service layer (`Services/DeviceService.cs`) wrapping PyZK operations
- **PyZKClient**: C# wrapper class for Python interop (`Device/PyZKClient.cs`)
- **pyzk_wrapper.py**: Python script providing JSON-based device operations
- **Device Operations**: Connect, disconnect, get users, get attendance, sync staff, enroll fingerprints
- Devices store connection info: `ip_address`, `port` (default 4370), `manufacturer`
- `is_online` and `last_heartbeat_at` track connectivity status
- `device_config` JSONB stores device-specific settings
- **ZK Simulator**: Full device simulator (`Device/zk_simulator.py`) for testing without hardware

## Development Workflows

### Database Operations
```bash
# Start PostgreSQL (ALWAYS required before running API)
docker compose up -d

# Create migration after model changes
dotnet ef migrations add MigrationName --project PunchClockApi

# Apply migrations
dotnet ef database update --project PunchClockApi

# Reset database (drops all data)
docker compose down -v && docker compose up -d
dotnet ef database update --project PunchClockApi
```

### Running the API
```bash
cd PunchClockApi
dotnet run
# API: http://localhost:5187
# Swagger: http://localhost:5187/swagger
```

### Database Seeding
- Controlled by `appsettings.Development.json`: `"Database": { "SeedDatabase": true }`
- `DatabaseSeeder.cs` creates sample departments, locations, staff, devices, and 7 days of punch logs
- Seeding only runs if database is empty (`AnyAsync()` check)
- Disable in production by setting to `false`

## Code Conventions & Patterns

### Model Structure
- **Pure POCOs**: Models in `Models/` have NO attributes, only properties and navigation properties
- **Required Fields**: Use `= null!;` for non-nullable reference types (e.g., `public string FirstName { get; set; } = null!;`)
- **Collections**: Initialize with `= [];` (collection expression syntax)
- **Example**: See `Staff.cs` for canonical model structure

### API Endpoint Patterns
```csharp
[ApiController]
[Route("api/staff")]
public sealed class StaffController : BaseController<Staff>
{
    private readonly PunchClockDbContext _db;

    public StaffController(PunchClockDbContext db, ILogger<StaffController> logger)
        : base(logger) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .Where(s => s.IsActive)
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Staff staff)
    {
        staff.StaffId = Guid.NewGuid();
        staff.CreatedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = staff.StaffId }, staff);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) =>
        await _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.StaffId == id)
            is Staff staff ? Ok(staff) : NotFound();
}
```

### Query Patterns
- **Filtering**: Use `.Where()` with optional parameters (see `/api/attendance/logs` for date/staff/device filters)
- **Pagination**: Return `{ total, page, pageSize, data }` structure for list endpoints
- **Ordering**: Default to most recent first: `.OrderByDescending(p => p.PunchTime)`

## Entity Relationships Reference

### Core Entities
- **Staff** ↔ Department (N:1), Location (N:1)
- **Staff** → BiometricTemplates (1:N), DeviceEnrollments (1:N), PunchLogs (1:N), AttendanceRecords (1:N)
- **Device** ↔ Location (N:1)
- **Device** → DeviceEnrollments (1:N), PunchLogs (1:N), SyncLogs (1:N), BiometricTemplates (1:N)
- **PunchLog** ↔ Staff (N:1), Device (N:1)
- **AttendanceRecord** ↔ Staff (N:1) with unique constraint on (staff_id, attendance_date)

### Authentication Entities (Fully Implemented)
- **User** ↔ UserRole ↔ Role (M:N)
- **Role** ↔ RolePermission ↔ Permission (M:N)
- Permission structure: `{ resource: "staff", action: "create" }`
- JWT tokens with BCrypt password hashing
- Claims-based authorization via `[Authorize]` attribute
- Default seeded user: `admin` / `admin123`

## Common Tasks & Examples

### Adding a New Controller or Action
1. Create a new controller in `Controllers/` inheriting from `BaseController<T>`
2. Annotate with `[ApiController]` and `[Route]` to align with existing URI patterns
3. Inject required services (typically `PunchClockDbContext` and `ILogger<TController>`)
4. Use `ParseQuery(Request.Query)` when supporting pagination/filter semantics
5. Use `HandleError(ex)` inside catch blocks for consistent error responses

### Adding a New Entity
1. Create model in `Models/` folder (use existing models as template)
2. Add `DbSet<YourEntity>` property to `PunchClockDbContext.cs`
3. Configure in `OnModelCreating()` with snake_case column names
4. Create migration: `dotnet ef migrations add AddYourEntity`
5. Apply migration: `dotnet ef database update`

### Modifying Database Schema
- **NEVER** directly edit migration files after applying
- Make changes in `PunchClockDbContext.OnModelCreating()` or model properties
- Create new migration for changes
- Use `dotnet ef migrations remove` to undo last unapplied migration

## Project File Structure
```
ShiftHandleNext/
├── README.md                     # Main project README with quick start
├── docker-compose.yml            # PostgreSQL database setup
├── docs/                         # Centralized documentation
│   ├── api/                      # API documentation
│   │   ├── api-reference.md      # Complete API endpoint reference
│   │   └── system-specification.md # Technical specification
│   ├── guides/                   # Feature guides
│   │   ├── FINGERPRINT_ENROLLMENT_GUIDE.md
│   │   ├── ATTENDANCE_PROCESSING_GUIDE.md
│   │   └── STAFF_ENROLLMENT_AUTOMATION.md
│   └── development/              # Development documentation
│       ├── project-summary.md    # Complete project overview
│       └── testing-guide.md      # Testing documentation
├── PunchClockApi/
│   ├── Program.cs                # Startup configuration and middleware wiring
│   ├── Controllers/              # Attribute-routed controllers inheriting BaseController
│   │   ├── AuthController.cs    # JWT authentication (login, register)
│   │   ├── StaffController.cs   # Staff CRUD operations
│   │   ├── DevicesController.cs # Device management & ZK integration
│   │   ├── AttendanceController.cs # Attendance tracking
│   │   ├── OrganizationController.cs # Departments & locations
│   │   ├── UsersController.cs   # User management
│   │   ├── SystemController.cs  # Health checks
│   │   └── BaseController.cs    # Shared query parsing & error handling
│   ├── Services/
│   │   ├── DeviceService.cs     # ZKTeco device integration service
│   │   ├── IDeviceService.cs    # Device service interface
│   │   ├── AttendanceProcessingService.cs # Punch log processing
│   │   ├── AttendanceProcessingJob.cs     # Background job
│   │   └── DeviceSyncJob.cs     # Device sync background job
│   ├── Device/
│   │   ├── PyZKClient.cs        # C# wrapper for PyZK
│   │   ├── pyzk_wrapper.py      # Python wrapper for ZK devices
│   │   ├── zk_simulator.py      # ZK device simulator for testing
│   │   ├── ZK_SIMULATOR_README.md # Simulator documentation
│   │   └── zk/                  # PyZK library
│   │       ├── base.py          # Core ZK protocol implementation
│   │       ├── user.py          # User management
│   │       ├── attendance.py    # Attendance records
│   │       ├── finger.py        # Fingerprint operations
│   │       └── ...
│   ├── Data/
│   │   ├── PunchClockDbContext.cs   # EF Core DbContext with all entity configs
│   │   └── DatabaseSeeder.cs        # Development data seeding
│   ├── Models/                   # 6 files grouping related entities
│   │   ├── Staff.cs             # Staff, BiometricTemplate
│   │   ├── Device.cs            # Device, DeviceEnrollment
│   │   ├── Attendance.cs        # PunchLog, AttendanceRecord
│   │   ├── Organization.cs      # Department, Location
│   │   ├── User.cs              # User, Role, Permission (auth entities)
│   │   └── Audit.cs             # SyncLog, AuditLog, ExportLog
│   ├── Migrations/              # EF Core migrations (auto-generated)
│   ├── README.md                # API-specific documentation
│   └── appsettings.Development.json # Connection string and seed config
└── PunchClockApi.Tests/
    ├── AuthenticationTests.cs   # JWT auth tests (8 tests)
    ├── QueryOptionsTests.cs     # Query parameter tests (20 tests)
    ├── ApiEndpointTests.cs      # CRUD endpoint tests (12 tests)
    ├── DeviceIntegrationTests.cs # ZKTeco device tests (19 tests)
    ├── AttendanceProcessingTests.cs # Attendance processing tests
    ├── BackgroundJobTests.cs    # Hangfire job tests
    ├── TestWebApplicationFactory.cs # Test infrastructure
    ├── IntegrationTestBase.cs   # Base test class
    └── README.md                # Test documentation
```

## Environment & Dependencies

### NuGet Packages
- `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.4) - PostgreSQL provider
- `Microsoft.EntityFrameworkCore.Design` (9.0.10) - Migration tools
- `Swashbuckle.AspNetCore` (9.0.6) - OpenAPI/Swagger
- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.10) - JWT authentication
- `System.IdentityModel.Tokens.Jwt` (8.2.1) - JWT token generation
- `BCrypt.Net-Next` (4.0.3) - Password hashing
- `Python.Runtime.NETStandard` (3.0.4) - Python.NET interop for PyZK

### Configuration
- **Connection String**: `appsettings.Development.json` → PostgreSQL localhost:5432
- **CORS**: Configured as "AllowAll" in development (restrict in production)
- **Logging**: EF Core SQL logging enabled in development (`LogLevel.Information`)

## Important Constraints & Validations

### Unique Constraints
- Staff: `employee_id`, `badge_number`, `email`
- Device: `device_serial`
- Department: `department_code`
- Location: `location_code`
- User: `username`, `email`
- AttendanceRecord: Composite unique on `(staff_id, attendance_date)`

### Delete Behavior
- **Cascade**: UserRole, RolePermission, BiometricTemplate, DeviceEnrollment, AttendanceRecord
- **Restrict**: Department relationships, Location relationships
- **SetNull**: PunchLog device/staff (preserve historical data)

## Next Development Priorities

1. ~~**Authentication/Authorization**~~ - ✅ **COMPLETED**: JWT with User/Role/Permission entities
2. ~~**Device Integration**~~ - ✅ **COMPLETED**: ZKTeco SDK client service for real device sync
3. ~~**Attendance Processing**~~ - ✅ **COMPLETED**: Background job to process PunchLogs → AttendanceRecords
4. ~~**Background Jobs**~~ - ✅ **COMPLETED**: Hangfire for scheduled device sync and attendance processing
5. **Shift Management**: Implement shift definitions and assignments
6. **Validation**: Add input validation middleware (FluentValidation recommended)
7. **Reporting**: Export endpoints for payroll (CSV/Excel)
8. **Error Handling**: Enhanced error messages and logging

## Testing & Debugging

### Health Check
- Endpoint: `GET /api/health`
- Returns: `{ status: "healthy", timestamp: "..." }` when DB is reachable

### Swagger UI
- Access at `/swagger` when running in development
- All endpoints auto-documented with tags (Staff, Devices, Attendance, Organization, System)

### Common Issues
- **"Database does not exist"**: Run `dotnet ef database update`
- **"Connection refused"**: Start PostgreSQL with `docker compose up -d`
- **"Nullable reference type"**: Use `= null!;` for required properties or `?` for optional
- **Migration conflicts**: Check if database is out of sync with `dotnet ef migrations list`

## Future Integration Points

- **Real-time Updates**: SignalR for live attendance dashboard
- **Reporting**: Separate reporting engine for payroll exports (CSV/Excel)
- **Caching**: Redis for device connection pooling and query caching
- **Notifications**: Email/SMS alerts for anomalies and missing punches
- **Multi-tenancy**: Support for multiple organizations/companies
