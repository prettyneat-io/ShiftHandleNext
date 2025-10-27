# Punch Clock API

A .NET 9.0 Web API backend for biometric punch clock synchronization and attendance management with ZKTeco devices.

## Features

- ✅ **JWT Authentication** - Token-based authentication with role-based access control
- ✅ **Staff Management** - CRUD operations for employee records with biometric enrollment
- ✅ **Device Management** - Manage biometric punch clock devices across multiple locations
- ✅ **Attendance Tracking** - Punch logs and attendance records with date filtering
- ✅ **Biometric Templates** - Store and manage fingerprint/face templates
- ✅ **Department & Location Management** - Organizational hierarchy support
- ✅ **Advanced Query Options** - Pagination, sorting, filtering, and eager loading
- ✅ **EF Core with PostgreSQL** - Modern ORM with snake_case conventions
- ✅ **Comprehensive Testing** - 40 integration tests with in-memory database
- ✅ **Swagger Documentation** - Interactive API documentation with JWT support

## Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose
- PostgreSQL (via Docker)

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

## Project Structure

```
PunchClockApi/
├── Controllers/
│   ├── AuthController.cs           # Authentication endpoints (login, register)
│   ├── StaffController.cs          # Staff CRUD operations
│   ├── DevicesController.cs        # Device management
│   ├── AttendanceController.cs     # Punch logs and attendance records
│   ├── OrganizationController.cs   # Departments and locations
│   ├── UsersController.cs          # User management
│   ├── SystemController.cs         # Health check
│   └── BaseController.cs           # Shared controller logic
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
- `POST /api/devices/{id}/sync` - Trigger device sync

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
```

### Test Coverage
- **40 integration tests** covering authentication, query options, and API endpoints
- In-memory database for fast, isolated testing
- No external dependencies required
- See `PunchClockApi.Tests/README.md` for detailed test documentation

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
- **Device Management** - Punch clock device registry with location assignments
- **Attendance Tracking** - PunchLog (raw data) and AttendanceRecord (processed daily summaries)
- **Organization** - Department hierarchy and location management
- **Audit Trail** - SyncLog, AuditLog, and ExportLog for complete traceability

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

- 🔄 **Device Integration Service** - ZKTeco SDK client for real device synchronization
- 🔄 **Attendance Processing** - Background job to process PunchLogs → AttendanceRecords
- 🔄 **Reporting Endpoints** - Export attendance data (CSV, Excel) for payroll
- 🔄 **Background Jobs** - Hangfire/Quartz for scheduled device sync
- 🔄 **Enhanced Validation** - FluentValidation for input validation
- 🔄 **Global Error Handler** - Middleware for consistent error responses
- ✅ **Authentication & Authorization** - JWT with User/Role/Permission
- ✅ **Comprehensive Testing** - 40 integration tests with in-memory database
- ✅ **Advanced Query Options** - Pagination, sorting, filtering, includes

## License

Proprietary
