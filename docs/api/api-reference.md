# Punch Clock API

A .NET 9.0 Web API backend for biometric punch clock synchronization and attendance management with ZKTeco devices.

## Features

- ✅ **JWT Authentication** - Token-based authentication with role-based access control
- ✅ **Staff Management** - CRUD operations for employee records with biometric enrollment
- ✅ **Device Management** - Manage ZKTeco biometric punch clock devices across multiple locations
- ✅ **ZKTeco Integration** - Full PyZK library integration for real device communication
- ✅ **Remote Fingerprint Enrollment** - Trigger fingerprint enrollment from API
- ✅ **Device Synchronization** - Sync staff enrollments and attendance records
- ✅ **Attendance Tracking** - Punch logs and attendance records with date filtering
- ✅ **Biometric Templates** - Store and manage fingerprint/face templates
- ✅ **Department & Location Management** - Organizational hierarchy support
- ✅ **Advanced Query Options** - Pagination, sorting, filtering, and eager loading
- ✅ **EF Core with PostgreSQL** - Modern ORM with snake_case conventions
- ✅ **Comprehensive Testing** - 59 integration tests with in-memory database
- ✅ **ZK Device Simulator** - Full device emulator for testing without hardware
- ✅ **Swagger Documentation** - Interactive API documentation with JWT support

## Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose (for PostgreSQL)
- Python 3.8+ (for ZK device simulator, optional)

## Getting Started

### 1. Start the Database

```bash
docker-compose up -d
```

This will start a PostgreSQL container on port 5432.

### 2. Create Database Migration

```bash
cd PunchClockApi
dotnet ef migrations add InitialCreate
```

### 3. Apply Migration

```bash
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

The API will start on `http://localhost:5187` (or check console output for actual port)

### 5. Access Swagger UI

Navigate to: `http://localhost:5187/swagger`

### 6. Login to Get JWT Token

Use Swagger or send a POST request to `/api/auth/login`:
```json
{
  "username": "admin",
  "password": "admin123"
}
```

Copy the `accessToken` from the response and click "Authorize" in Swagger UI, then enter: `Bearer <your-token>`

### 7. (Optional) Test ZK Device Integration

Start the ZK device simulator in a separate terminal:
```bash
cd Device
python zk_simulator.py
# Simulator runs on 127.0.0.1:4370
```

Then use Swagger to test device operations:
- Connect to device
- Get device users
- Sync staff to device
- Enroll fingerprints remotely

## Project Structure

```
PunchClockApi/
├── Controllers/
│   ├── AuthController.cs           # Authentication endpoints (login, register)
│   ├── StaffController.cs          # Staff CRUD operations
│   ├── DevicesController.cs        # Device management & ZK integration
│   ├── AttendanceController.cs     # Punch logs and attendance records
│   ├── OrganizationController.cs   # Departments and locations
│   ├── UsersController.cs          # User management
│   ├── SystemController.cs         # Health check
│   └── BaseController.cs           # Shared controller logic
├── Services/
│   ├── DeviceService.cs            # ZKTeco device integration service
│   └── IDeviceService.cs           # Device service interface
├── Device/
│   ├── PyZKClient.cs               # C# wrapper for PyZK
│   ├── pyzk_wrapper.py             # Python wrapper for ZK devices
│   ├── zk_simulator.py             # ZK device simulator for testing
│   └── zk/                         # PyZK library (device protocol)
├── Data/
│   ├── PunchClockDbContext.cs      # EF Core DbContext with fluent configuration
│   └── DatabaseSeeder.cs           # Development data seeding
├── Models/
│   ├── User.cs                     # User, Role, Permission entities
│   ├── Organization.cs             # Department & Location
│   ├── Staff.cs                    # Staff & BiometricTemplate
│   ├── Device.cs                   # Device & DeviceEnrollment
│   ├── Attendance.cs               # PunchLog & AttendanceRecord
│   └── Audit.cs                    # SyncLog, AuditLog, ExportLog
├── Migrations/                     # EF Core migrations
└── Program.cs                      # API configuration and startup
```

## API Endpoints

### Authentication (Public)
- `POST /api/auth/login` - Login with username/password
- `POST /api/auth/register` - Register new user
- `GET /api/auth/me` - Get current user info (requires auth)

### Staff (Requires Authentication)
- `GET /api/staff` - Get all active staff (supports pagination, sorting, filtering, includes)
- `GET /api/staff/{id}` - Get staff by ID with relationships
- `POST /api/staff` - Create new staff
- `PUT /api/staff/{id}` - Update staff
- `DELETE /api/staff/{id}` - Soft delete staff (sets IsActive = false)

### Devices (Requires Authentication)
- `GET /api/devices` - Get all active devices (supports query options)
- `GET /api/devices/{id}` - Get device by ID
- `POST /api/devices` - Register new device
- `PUT /api/devices/{id}` - Update device
- `DELETE /api/devices/{id}` - Soft delete device
- `POST /api/devices/{id}/connect` - Connect to ZKTeco device
- `POST /api/devices/{id}/disconnect` - Disconnect from device
- `POST /api/devices/{id}/test-connection` - Test device connectivity
- `GET /api/devices/{id}/info` - Get detailed device information
- `GET /api/devices/{id}/users` - Get all users from device
- `GET /api/devices/{id}/attendance` - Get all attendance records from device
- `POST /api/devices/{id}/sync-staff` - Sync staff enrollments to device
- `POST /api/devices/{id}/sync-attendance` - Sync attendance records from device
- `POST /api/devices/{id}/staff/{staffId}/enroll` - Enroll staff on device
- `POST /api/devices/{id}/staff/{staffId}/enroll-fingerprint?fingerId={0-9}` - Remote fingerprint enrollment

### Attendance (Requires Authentication)
- `GET /api/attendance/logs` - Get punch logs with date/staff/device filters
- `GET /api/attendance/records` - Get attendance records
- `POST /api/attendance/logs` - Create punch log

### Organization (Requires Authentication)
- `GET /api/departments` - Get all departments (supports query options)
- `POST /api/departments` - Create department
- `GET /api/locations` - Get all locations (supports query options)
- `POST /api/locations` - Create location

### Users (Requires Authentication)
- `GET /api/users` - Get all users (admin only)

### System (Public)
- `GET /api/health` - Database health check

### Query Parameters
Most list endpoints support:
- `page` - Page number (default: 1)
- `limit` - Items per page (default: 50)
- `sort` - Sort field (e.g., "FirstName", "LastName")
- `order` - Sort order ("asc" or "desc")
- `include` - Eager load relationships (e.g., "Department,Location")
- Field-specific filters (e.g., `isActive=true`, `firstName=John`)

## Database Configuration

Connection string is configured in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=punchclock_db;Username=punchclock;Password=punchclock_dev_password"
  }
}
```

## EF Core Commands

### Create Migration
```bash
dotnet ef migrations add <MigrationName>
```

### Apply Migration
```bash
dotnet ef database update
```

### Remove Last Migration
```bash
dotnet ef migrations remove
```

### Generate SQL Script
```bash
dotnet ef migrations script
```

## Docker Commands

### Start Database
```bash
docker-compose up -d
```

### Stop Database
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f postgres
```

### Reset Database (Delete Volume)
```bash
docker-compose down -v
```

## Testing

The project includes comprehensive integration tests using xUnit, FluentAssertions, and in-memory database.

### Run All Tests
```bash
cd PunchClockApi.Tests
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~AuthenticationTests"
dotnet test --filter "FullyQualifiedName~QueryOptionsTests"
dotnet test --filter "FullyQualifiedName~ApiEndpointTests"
dotnet test --filter "FullyQualifiedName~DeviceIntegrationTests"
```

### Test Coverage
- **59 integration tests** covering authentication, query options, API endpoints, and device integration
- **Authentication Tests (8)** - Login, registration, protected endpoints
- **Query Options Tests (20)** - Pagination, sorting, filtering, eager loading
- **API Endpoint Tests (12)** - Full CRUD operations for all entities
- **Device Integration Tests (19)** - Real ZK simulator integration for device operations
- In-memory database for fast, isolated testing
- No external dependencies required
- See `PunchClockApi.Tests/README.md` for detailed test documentation

### ZK Device Simulator
For manual testing of device operations:
```bash
cd Device
python zk_simulator.py
# Simulator runs on 127.0.0.1:4370
# Pre-loaded with test users and attendance data
```

## Development Notes

### Controller-Based Architecture
This project uses ASP.NET Core controllers with attribute routing:
- `[ApiController]` attribute for automatic model validation
- `[Route]` attribute for endpoint routing
- Shared `BaseController<T>` class for common query parsing
- Constructor dependency injection
- JWT authentication with role-based authorization

### Database Schema
The schema includes:
- **Users & RBAC** - Role-based access control with User, Role, Permission entities
- **Staff Management** - Employee records with biometric templates and device enrollments
- **Device Management** - Punch clock device registry with location assignments and ZKTeco integration
- **Attendance Tracking** - PunchLog (raw data) and AttendanceRecord (processed daily summaries)
- **Organization** - Department hierarchy and location management
- **Audit Trail** - SyncLog, AuditLog, and ExportLog for complete traceability

### ZKTeco Device Integration
The system integrates with ZKTeco biometric devices using the PyZK Python library:
- **Full Device Communication** - Connect, disconnect, test connectivity
- **User Management** - Add/delete users on devices with biometric data
- **Attendance Synchronization** - Pull attendance records from devices
- **Staff Synchronization** - Push staff enrollments to devices
- **Remote Fingerprint Enrollment** - Trigger enrollment process from API
- **Device Simulator** - Full ZK device emulator for testing without hardware
- **Python.NET Integration** - C# service layer wrapping PyZK operations

### Conventions
- **Snake_case** for database columns (PostgreSQL convention) via `.HasColumnName()`
- **PascalCase** for C# properties
- **UUIDs** (`Guid`) for all primary keys with `gen_random_uuid()` default
- **Soft deletes** with `IsActive` flags (no hard deletes)
- **Audit fields** on most tables: `created_at`, `updated_at`, `created_by`, `updated_by`
- **Fluent API** configuration only - no data annotations on models
- **JSONB** columns for flexible data (device_config, validation_errors, anomaly_flags)

## Database Seeding

The API automatically seeds the database with sample data in Development environment:
- Controlled by `appsettings.Development.json`: `"Database": { "SeedDatabase": true }`
- Creates sample users (admin/admin123), departments, locations, staff, devices
- Generates 7 days of punch log data for testing
- Only runs if database is empty

To disable seeding, set `"SeedDatabase": false` in configuration.

## Next Steps

- 🔄 **Attendance Processing** - Background job to process PunchLogs → AttendanceRecords
- 🔄 **Background Jobs** - Hangfire/Quartz for scheduled device sync
- 🔄 **Reporting Endpoints** - Export attendance data (CSV, Excel) for payroll
- 🔄 **Enhanced Validation** - FluentValidation for input validation
- 🔄 **Global Error Handler** - Middleware for consistent error responses
- ✅ **Authentication & Authorization** - JWT with User/Role/Permission
- ✅ **Device Integration** - Full ZKTeco PyZK integration with remote enrollment
- ✅ **Comprehensive Testing** - 59 integration tests with in-memory database
- ✅ **Advanced Query Options** - Pagination, sorting, filtering, includes

## License

Proprietary
