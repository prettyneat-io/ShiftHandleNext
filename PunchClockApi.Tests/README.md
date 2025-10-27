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

### Authentication (9 tests)
- ✅ Health check without authentication
- ✅ Protected endpoints require authentication
- ✅ Login with valid credentials
- ✅ Login with invalid credentials
- ✅ Get current user info
- ✅ Access endpoints with valid token
- ✅ User registration
- ✅ List users (admin only)

### Query Options (20 tests)
- ✅ Pagination structure validation
- ✅ Page and limit parameters
- ✅ Default page handling (page 0 → page 1)
- ✅ Sorting by various fields (ascending/descending)
- ✅ Invalid sort field handling
- ✅ Include/eager loading (Department, Location)
- ✅ Multiple includes
- ✅ Filtering by field values
- ✅ Empty filter results
- ✅ Combined query options
- ✅ Pagination on all endpoints (Staff, Devices, Attendance, Departments, Locations)

### API Endpoints (13 tests)
- ✅ Create Department
- ✅ Create Location
- ✅ Create Staff
- ✅ Get all Staff
- ✅ Get Staff by ID
- ✅ Create Device
- ✅ Get all Devices
- ✅ Create Punch Log
- ✅ Get Attendance Logs
- ✅ Get all Departments
- ✅ Get all Locations

**Total: 42 tests** (migrated from 3 bash scripts)

## Key Features

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

## Project Structure

```
PunchClockApi.Tests/
├── AuthenticationTests.cs      # Auth flow tests
├── QueryOptionsTests.cs        # Query parameter tests
├── ApiEndpointTests.cs         # CRUD operation tests
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

### Tests fail with "Program not found"
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
