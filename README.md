# Punch Clock API

A production-ready .NET 9.0 Web API backend for biometric punch clock synchronization and attendance management. The system centralizes staff enrollment, device management, and attendance tracking across multiple ZKTeco biometric devices with a PostgreSQL backend.

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker & Docker Compose
- Python 3.8+ (for ZK device simulator - optional)

### Start the System

1. **Start PostgreSQL database**:
   ```bash
   docker compose up -d
   ```

2. **Run the API**:
   ```bash
   cd PunchClockApi
   dotnet run
   ```

3. **Access the API**:
   - Swagger UI: http://localhost:5187/swagger
   - Hangfire Dashboard: http://localhost:5187/hangfire
   - Health Check: http://localhost:5187/api/health

4. **Login**:
   - Default credentials: `admin` / `admin123`
   - Use `/api/auth/login` to get a JWT token

## ✨ Key Features

- ✅ **JWT Authentication** - Token-based auth with role-based access control (RBAC)
- ✅ **ZKTeco Device Integration** - Full PyZK library integration for real device communication
- ✅ **Remote Fingerprint Enrollment** - Trigger enrollment from API without device access
- ✅ **Automated Background Jobs** - Hangfire-powered device sync and attendance processing
- ✅ **Attendance Processing Engine** - Transform raw punch logs into daily summaries with anomaly detection
- ✅ **Advanced Query Options** - Pagination, sorting, filtering, and eager loading on all list endpoints
- ✅ **Comprehensive Testing** - 73+ integration tests with ZK device simulator
- ✅ **PostgreSQL with EF Core** - Modern ORM with migrations and snake_case conventions
- ✅ **Swagger/OpenAPI** - Interactive API documentation with JWT support

## 📚 Documentation

### API Documentation
- **[API Reference](docs/api/api-reference.md)** - Complete API endpoint documentation with examples
- **[System Specification](docs/api/system-specification.md)** - Technical specification and data model

### Feature Guides
- **[Fingerprint Enrollment Guide](docs/guides/FINGERPRINT_ENROLLMENT_GUIDE.md)** - Remote fingerprint enrollment workflow
- **[Attendance Processing Guide](docs/guides/ATTENDANCE_PROCESSING_GUIDE.md)** - Attendance processing and background jobs
- **[Staff Enrollment Automation](docs/guides/STAFF_ENROLLMENT_AUTOMATION.md)** - Automated staff-to-device synchronization

### Development Documentation
- **[Project Summary](docs/development/project-summary.md)** - Complete project overview and implementation details
- **[Testing Guide](docs/development/testing-guide.md)** - Comprehensive testing documentation
- **[PunchClockApi README](PunchClockApi/README.md)** - API project-specific documentation
- **[PunchClockApi.Tests README](PunchClockApi.Tests/README.md)** - Test project documentation

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Punch Clock API                          │
│  (ASP.NET Core 9.0 - Controller-based with Attribute Routing)│
└──────────────────┬──────────────────────────────────────────┘
                   │
    ┌──────────────┴──────────────┬────────────────────────┐
    │                             │                        │
┌───▼────┐                  ┌────▼─────┐          ┌───────▼────────┐
│ Staff  │                  │ Devices  │          │  Attendance    │
│  CRUD  │                  │   CRUD   │          │   Tracking     │
└────────┘                  └────┬─────┘          └────────────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
              ┌─────▼──────┐          ┌──────▼──────┐
              │   PyZK     │          │  Hangfire   │
              │ Integration│          │Background   │
              │  (Python)  │          │    Jobs     │
              └────────────┘          └─────────────┘
                    │
              ┌─────▼──────┐
              │  ZKTeco    │
              │  Devices   │
              └────────────┘
```

### Technology Stack

- **Framework**: .NET 9.0 with C# 13
- **API Pattern**: Controller-based with attribute routing
- **Database**: PostgreSQL 16 with EF Core 9.0
- **Authentication**: JWT Bearer tokens with BCrypt password hashing
- **Device Integration**: PyZK (Python) via Python.NET interop
- **Background Jobs**: Hangfire with PostgreSQL storage
- **Testing**: xUnit with in-memory database
- **Documentation**: Swagger/OpenAPI
- **Containerization**: Docker Compose

## 🗄️ Database Schema

### Core Entities
- **Users & Auth**: User, Role, Permission (RBAC with M:N relationships)
- **Organization**: Department, Location (hierarchical structure)
- **Staff**: Staff, BiometricTemplate (employee records with biometric data)
- **Devices**: Device, DeviceEnrollment (punch clock devices with staff assignments)
- **Attendance**: PunchLog (raw), AttendanceRecord (processed daily summaries)
- **Audit**: SyncLog, AuditLog, ExportLog (complete traceability)

### Key Features
- UUID primary keys with `gen_random_uuid()`
- Snake_case column naming (PostgreSQL convention)
- Soft deletes with `is_active` flags
- Automatic timestamps (`created_at`, `updated_at`)
- JSONB columns for flexible metadata
- Comprehensive indexes for performance

## 🧪 Testing

Run the comprehensive test suite:

```bash
cd PunchClockApi.Tests
dotnet test
```

**Test Coverage**:
- 8 Authentication tests (JWT, login, registration)
- 20 Query options tests (pagination, sorting, filtering)
- 12 API endpoint tests (CRUD operations)
- 19 Device integration tests (real ZK simulator)
- 14 Attendance processing tests
- Background job tests

For detailed testing documentation, see [docs/development/testing-guide.md](docs/development/testing-guide.md).

## 🔧 Development

### EF Core Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName --project PunchClockApi

# Apply migration
dotnet ef database update --project PunchClockApi

# Rollback
dotnet ef database update PreviousMigrationName --project PunchClockApi
```

### Database Management

```bash
# Start PostgreSQL
docker compose up -d

# View logs
docker compose logs -f postgres

# Stop and remove (keeps data)
docker compose down

# Stop and remove all data
docker compose down -v

# Connect to database
docker exec -it punchclock_db psql -U punchclock -d punchclock_db
```

### ZK Device Simulator

For testing device integration without hardware:

```bash
cd PunchClockApi/Device
python zk_simulator.py
# Simulator runs on 127.0.0.1:4370
```

See [PunchClockApi/Device/ZK_SIMULATOR_README.md](PunchClockApi/Device/ZK_SIMULATOR_README.md) for details.

## 📦 Dependencies

### Main Project
- **Npgsql.EntityFrameworkCore.PostgreSQL** (9.0.4) - PostgreSQL provider
- **Microsoft.AspNetCore.Authentication.JwtBearer** (9.0.10) - JWT authentication
- **BCrypt.Net-Next** (4.0.3) - Password hashing
- **Python.Runtime.NETStandard** (3.0.4) - Python.NET interop for PyZK
- **Hangfire.PostgreSql** (1.20.9) - Background job processing
- **Swashbuckle.AspNetCore** (9.0.6) - OpenAPI/Swagger

### Test Project
- **Microsoft.AspNetCore.Mvc.Testing** (9.0.10) - Integration testing
- **Microsoft.EntityFrameworkCore.InMemory** (9.0.10) - In-memory database
- **FluentAssertions** (7.0.0) - Fluent assertions
- **xUnit** (2.9.2) - Test framework

## 🔐 Security

### Production Checklist
- [ ] Configure restrictive CORS for production domains
- [ ] Use environment variables or secrets manager for sensitive data
- [ ] Enable HTTPS only (disable HTTP)
- [ ] Add rate limiting on authentication endpoints
- [ ] Implement refresh token rotation
- [ ] Enable PostgreSQL SSL connections
- [ ] Configure Hangfire dashboard authentication
- [ ] Review and restrict API permissions

### Current Security Features
- ✅ JWT authentication with Bearer tokens
- ✅ BCrypt password hashing (cost factor 12)
- ✅ Role-based access control (RBAC)
- ✅ Protected endpoints with `[Authorize]` attribute
- ✅ Token expiration (24 hours)
- ⚠️ CORS allows all origins (development only)
- ⚠️ Hangfire dashboard has basic auth (configure for production)

## 🎯 Project Status

### ✅ Completed Features
- Core API with 30+ endpoints
- JWT authentication and authorization
- Full ZKTeco device integration (PyZK)
- Remote fingerprint enrollment
- Automated device synchronization
- Attendance processing engine
- Background jobs (Hangfire)
- Comprehensive testing (73+ tests)
- ZK device simulator
- Complete documentation

### 🚧 In Progress / Planned
- [ ] Shift management
- [ ] Leave/absence tracking
- [ ] Enhanced reporting (CSV/Excel exports)
- [ ] Dashboard statistics
- [ ] Real-time notifications (SignalR)
- [ ] Audit log viewer UI
- [ ] Multi-tenancy support
- [ ] CI/CD pipeline

## 📄 License

Proprietary

## 🙏 Support

For issues, questions, or contributions, please refer to the documentation in the `docs/` folder or contact the development team.

---

**Last Updated**: November 2, 2025
