# Punch Clock API

A C# .NET minimal API backend for punch clock and attendance synchronization with ZKTeco devices.

## Features

- ✅ **Staff Management** - CRUD operations for employee records
- ✅ **Device Management** - Manage biometric punch clock devices
- ✅ **Attendance Tracking** - Punch logs and attendance records
- ✅ **Biometric Templates** - Store and manage fingerprint/face data
- ✅ **Department & Location Management** - Organizational structure
- ✅ **Audit Logging** - Complete audit trail
- ✅ **EF Core with PostgreSQL** - Modern ORM with robust database
- ✅ **Swagger Documentation** - Interactive API documentation

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

The API will start on `https://localhost:5001` (or `http://localhost:5000`)

### 5. Access Swagger UI

Navigate to: `https://localhost:5001/swagger`

## Project Structure

```
PunchClockApi/
├── Data/
│   └── PunchClockDbContext.cs      # EF Core DbContext
├── Models/
│   ├── User.cs                     # User & Auth entities
│   ├── Organization.cs             # Department & Location
│   ├── Staff.cs                    # Staff & Biometric
│   ├── Device.cs                   # Device & Enrollment
│   ├── Attendance.cs               # Punch logs & Records
│   └── Audit.cs                    # Sync, Audit, Export logs
└── Program.cs                      # Minimal API configuration
```

## API Endpoints

### Staff
- `GET /api/staff` - Get all active staff
- `GET /api/staff/{id}` - Get staff by ID
- `POST /api/staff` - Create new staff
- `PUT /api/staff/{id}` - Update staff
- `DELETE /api/staff/{id}` - Soft delete staff

### Devices
- `GET /api/devices` - Get all active devices
- `GET /api/devices/{id}` - Get device by ID
- `POST /api/devices` - Register new device
- `PUT /api/devices/{id}` - Update device
- `POST /api/devices/{id}/sync` - Trigger device sync

### Attendance
- `GET /api/attendance/logs` - Get punch logs (with filters)
- `GET /api/attendance/records` - Get attendance records
- `POST /api/attendance/logs` - Create punch log

### Organization
- `GET /api/departments` - Get all departments
- `POST /api/departments` - Create department
- `GET /api/locations` - Get all locations
- `POST /api/locations` - Create location

### System
- `GET /api/health` - Database health check

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

## Development Notes

### Minimal API Style
This project uses .NET minimal APIs for a succinct, modern approach:
- No controllers - endpoints defined inline
- Reduced boilerplate
- Direct dependency injection in route handlers
- Automatic model binding

### Database Schema
The schema includes:
- **Users & RBAC** - Role-based access control
- **Staff Management** - Employee records with biometric data
- **Device Management** - Punch clock device registry
- **Attendance Tracking** - Raw logs and processed records
- **Audit Trail** - Complete system audit logging

### Conventions
- Snake_case for database columns (PostgreSQL convention)
- PascalCase for C# properties
- UUIDs for primary keys
- Soft deletes with `IsActive` flags
- Automatic timestamps with EF Core

## Next Steps

1. ✅ Implement authentication/authorization (JWT)
2. ✅ Add device integration service (PYZK API client)
3. ✅ Implement attendance processing logic
4. ✅ Add reporting endpoints
5. ✅ Set up background jobs for device sync
6. ✅ Add validation & error handling middleware
7. ✅ Implement unit tests

## License

MIT
