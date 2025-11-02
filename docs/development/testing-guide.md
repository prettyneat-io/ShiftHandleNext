# PunchClockApi.Tests

Integration tests for the Punch Clock API using xUnit, FluentAssertions, and ASP.NET Core's `WebApplicationFactory`.

## Overview

This test project contains comprehensive integration tests that verify the API's functionality without requiring a running PostgreSQL instance. Tests use an in-memory database for isolation and speed.

## Test Structure

### Test Classes

1. **AuthenticationTests** (`AuthenticationTests.cs`)
   - Health check endpoint
   - Login with valid/invalid credentials
   - Token-based authentication
   - User registration
   - Protected endpoint access

2. **QueryOptionsTests** (`QueryOptionsTests.cs`)
   - Pagination (page, limit)
   - Sorting (sort field, order)
   - Filtering (query parameters)
   - Eager loading (include relationships)
   - Combined query options

3. **ApiEndpointTests** (`ApiEndpointTests.cs`)
   - CRUD operations for all entities
   - Staff, Departments, Locations, Devices
   - Attendance logs and punch records
   - Relationship handling

4. **DeviceIntegrationTests** (`DeviceIntegrationTests.cs`)
   - Device connection and disconnection
   - Test connection functionality
   - Get device information and capacity
   - Retrieve users from device
   - Retrieve attendance records from device
   - Staff enrollment on device
   - Sync staff to device
   - Sync attendance from device
   - Multiple connection cycles
   - Uses real ZK simulator for authentic integration testing

### Infrastructure

- **TestWebApplicationFactory**: Custom factory that configures in-memory database
- **IntegrationTestBase**: Base class with common setup and authentication helpers
- **TestAuthHelper**: Helper methods for JWT authentication in tests

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~AuthenticationTests"
dotnet test --filter "FullyQualifiedName~QueryOptionsTests"
dotnet test --filter "FullyQualifiedName~ApiEndpointTests"
dotnet test --filter "FullyQualifiedName~DeviceIntegrationTests"
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~Login_WithValidCredentials_ReturnsToken"
```

### Run with detailed output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run from solution root
```bash
cd /home/kris/Development/ShiftHandleNext
dotnet test
```

## Test Coverage

### Authentication (8 tests)
- ✅ Health check without authentication
- ✅ Protected endpoints require authentication
- ✅ Login with valid credentials
- ✅ Login with invalid credentials
- ✅ Get current user info
- ✅ Access staff endpoint with valid token
- ✅ User registration
- ✅ List users (admin only)

### Query Options (20 tests)
- ✅ Pagination structure validation (page, limit, total, data)
- ✅ Page and limit parameters respected
- ✅ Page 2 returns correct page number
- ✅ Default page handling (page 0 → page 1)
- ✅ Sorting by first name ascending
- ✅ Sorting by last name descending
- ✅ Invalid sort field handling (graceful fallback)
- ✅ Include Department relation (eager loading)
- ✅ Include Location relation (eager loading)
- ✅ Multiple includes (Department + Location)
- ✅ Empty include parameter handling
- ✅ Filter by first name (case-insensitive)
- ✅ Filter with no matches returns empty array
- ✅ Combined pagination + sorting
- ✅ Combined pagination + include
- ✅ Combined filter + sort + limit
- ✅ Pagination on Devices endpoint
- ✅ Pagination on Attendance Logs endpoint
- ✅ Pagination on Departments endpoint
- ✅ Pagination on Locations endpoint

### API Endpoints (12 tests)
- ✅ Health check returns healthy status
- ✅ Create Department with valid data
- ✅ Create Location with valid data
- ✅ Create Staff with valid data
- ✅ Get all Staff
- ✅ Get Staff by ID
- ✅ Create Device with valid data
- ✅ Get all Devices
- ✅ Create Punch Log with valid data
- ✅ Get Attendance Logs
- ✅ Get all Departments
- ✅ Get all Locations

### Device Integration (13 tests)
- ✅ Test connection with simulator returns connected
- ✅ Connect to device with simulator returns success
- ✅ Disconnect from device after connection returns success
- ✅ Get device info with simulator returns detailed info
- ✅ Get device users with simulator returns users list
- ✅ Get device attendance with simulator returns attendance records
- ✅ Enroll staff on device with valid staff returns success
- ✅ Enroll staff on device with non-existent device returns not found
- ✅ Enroll staff on device with non-existent staff returns not found
- ✅ Sync staff to device with active staff creates enrollments
- ✅ Sync attendance from device with existing data creates attendance logs
- ✅ Sync device with invalid type defaults to attendance
- ✅ Connect and disconnect sequence multiple times works correctly
- ✅ Get device users after enrolling staff shows new user
- ✅ Device info after enrollments reflects updated counts

**Total: 53 tests** (migrated from 3 bash scripts + new device integration tests)

## Key Features

### ZK Simulator for Device Tests
Device integration tests use the Python-based ZK simulator (`zk_simulator.py`) for authentic testing:
- Tests automatically start/stop the simulator process
- Simulator responds to real ZKTeco protocol commands
- Tests verify actual device communication, not mocked responses
- Simulates device features: users, attendance, device info, enrollment
- Runs on localhost:4370 (standard ZKTeco port)

### In-Memory Database
Tests use EF Core's in-memory database provider, eliminating the need for:
- Running PostgreSQL
- Database migrations during tests
- Cleanup between test runs

Each test class gets a fresh database instance via `TestWebApplicationFactory`.

### FluentAssertions
Tests use FluentAssertions for readable, expressive assertions:
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Data.Should().HaveCountLessOrEqualTo(2);
staff.FirstName.Should().ContainEquivalentOf("a");
```

### Test Isolation
- Each test class uses `IClassFixture<TestWebApplicationFactory>` for shared setup
- Tests inherit from `IntegrationTestBase` for common functionality
- Authentication handled via `AuthenticateAsAdminAsync()`

## Migration from Bash Scripts

These C# tests replace the following bash scripts:
- `test-auth.sh` → `AuthenticationTests.cs`
- `test-query-options.sh` → `QueryOptionsTests.cs`
- `test-api.sh` → `ApiEndpointTests.cs`
- New device integration tests → `DeviceIntegrationTests.cs`

Benefits of C# tests over bash:
- ✅ Type safety and compile-time checking
- ✅ Better IDE integration (debugging, test explorer)
- ✅ No external dependencies (curl, jq)
- ✅ Faster execution with in-memory database
- ✅ Better error messages and stack traces
- ✅ Easy to run in CI/CD pipelines
- ✅ Cross-platform (Windows, Linux, macOS)

## CI/CD Integration

Tests are ready for CI/CD integration:

```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
  
- name: Publish Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: '**/*.trx'
```

## Dependencies

- **Microsoft.AspNetCore.Mvc.Testing** (9.0.10) - Integration testing infrastructure
- **Microsoft.EntityFrameworkCore.InMemory** (9.0.10) - In-memory database provider
- **FluentAssertions** (8.8.0) - Fluent assertion library
- **xunit** (2.x) - Test framework
- **xunit.runner.visualstudio** - Visual Studio test adapter
- **Python 3** - Required for running ZK simulator during device integration tests

## Requirements

### For All Tests
- .NET 9.0 SDK
- No PostgreSQL required (uses in-memory database)

### For Device Integration Tests Only
- Python 3 installed and available in PATH as `python3`
- ZK simulator Python dependencies (automatically included in test binaries)

## Project Structure

```
PunchClockApi.Tests/
├── AuthenticationTests.cs      # Auth flow tests
├── QueryOptionsTests.cs        # Query parameter tests
├── ApiEndpointTests.cs         # CRUD operation tests
├── DeviceIntegrationTests.cs   # Device integration tests with ZK simulator
├── IntegrationTestBase.cs      # Base class for all tests
├── TestWebApplicationFactory.cs # Custom test factory
├── TestAuthHelper.cs           # Auth helper methods
└── README.md                   # This file
```

## Extending Tests

To add new tests:

1. **Create new test class** inheriting from `IntegrationTestBase`
   ```csharp
   public sealed class MyNewTests : IntegrationTestBase
   {
       public MyNewTests(TestWebApplicationFactory factory) : base(factory) { }
   }
   ```

2. **Add test methods** with `[Fact]` attribute
   ```csharp
   [Fact]
   public async Task MyTest_Scenario_ExpectedResult()
   {
       await AuthenticateAsAdminAsync();
       var response = await Client.GetAsync("/api/endpoint");
       response.StatusCode.Should().Be(HttpStatusCode.OK);
   }
   ```

3. **Use FluentAssertions** for readable assertions
   ```csharp
   result.Should().NotBeNull();
   result.Data.Should().HaveCount(3);
   ```

## Troubleshooting

### Device Integration Tests

#### Python not found
If device tests fail with "python3 not found":
- Install Python 3: `sudo apt-get install python3` (Linux) or download from python.org
- Ensure `python3` is in your PATH
- On Windows, you may need to use `python` instead of `python3` (update test code)

#### Simulator fails to start
If simulator process fails:
- Check that port 4370 is not already in use: `lsof -i :4370` (Linux/Mac)
- Review test output for simulator error messages
- Verify zk_simulator.py exists in Device folder
- Check Python dependencies are installed

#### Tests timeout or hang
If device tests hang:
- Simulator may not have started properly - check test output
- Port 4370 may be blocked by firewall
- Try running tests with increased verbosity: `dotnet test --logger "console;verbosity=detailed"`

### General Tests

#### Tests fail with "Program not found"
Ensure `Program.cs` has the partial class declaration:
```csharp
public partial class Program { }
```

### JWT configuration errors
The test factory uses the Testing environment. Ensure JWT settings are configured in `appsettings.Testing.json` or environment variables.

### Database seeding issues
The test factory automatically creates the in-memory database. Seeding is handled by the `DatabaseSeeder` if configured.

## Contributing

When adding new features to the API:
1. Write integration tests first (TDD approach)
2. Run tests locally before committing
3. Ensure all tests pass in CI/CD pipeline
4. Update this README if adding new test categories
