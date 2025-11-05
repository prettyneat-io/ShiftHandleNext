using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Models;
using Xunit;

namespace PunchClockApi.Tests;

public class StaffBulkOperationsTests : IntegrationTestBase
{
    public StaffBulkOperationsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ExportStaffToCsv_ShouldReturnCsvFile()
    {
        // Arrange - Authenticate and seed some staff
        await AuthenticateAsAdminAsync();
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/api/staff/export/csv");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        
        var csvContent = await response.Content.ReadAsStringAsync();
        csvContent.Should().Contain("EmployeeId");
        csvContent.Should().Contain("FirstName");
        csvContent.Should().Contain("LastName");
        csvContent.Should().Contain("TEST001"); // From seeded data
    }

    [Fact]
    public async Task ExportStaffToCsv_WithInactiveStaff_ShouldIncludeOnlyActive()
    {
        // Arrange - Authenticate and seed staff with some inactive
        await AuthenticateAsAdminAsync();
        await SeedTestDataAsync();
        
        using (var context = GetDbContext())
        {
            var inactiveStaff = await context.Staff.FirstAsync(s => s.EmployeeId == "TEST001");
            inactiveStaff.IsActive = false;
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/staff/export/csv?includeInactive=false");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvContent = await response.Content.ReadAsStringAsync();
        csvContent.Should().NotContain("TEST001"); // Inactive staff excluded
    }

    [Fact]
    public async Task ImportStaffFromCsv_WithValidData_ShouldCreateNewStaff()
    {
        // Arrange - Authenticate and create CSV content
        await AuthenticateAsAdminAsync();
        await SeedDepartmentsAndLocations();
        var csvContent = CreateValidCsvContent();
        var content = CreateMultipartFormDataContent(csvContent, "import.csv");

        // Act
        var response = await Client.PostAsync("/api/staff/import/csv", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);

        // Verify database
        using var context = GetDbContext();
        var staff = await context.Staff.Where(s => s.EmployeeId.StartsWith("IMPORT")).ToListAsync();
        staff.Should().HaveCount(2);
        staff.Should().Contain(s => s.EmployeeId == "IMPORT001" && s.FirstName == "John");
        staff.Should().Contain(s => s.EmployeeId == "IMPORT002" && s.FirstName == "Jane");
    }

    [Fact]
    public async Task ImportStaffFromCsv_WithDuplicateEmployeeId_ShouldReturnError()
    {
        // Arrange - Authenticate, seed departments, and create duplicate staff
        await AuthenticateAsAdminAsync();
        await SeedDepartmentsAndLocations();
        
        using (var context = GetDbContext())
        {
            await context.Staff.AddAsync(new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "DUPLICATE001",
                FirstName = "Existing",
                LastName = "Staff",
                HireDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var csvContent = @"EmployeeId,BadgeNumber,FirstName,LastName,MiddleName,Email,Phone,Mobile,DepartmentCode,LocationCode,ShiftCode,PositionTitle,EmploymentType,HireDate,TerminationDate,IsActive,EnrollmentStatus
DUPLICATE001,B001,John,Doe,,john@test.com,,,IT,HQ,,Developer,Full-Time,2024-01-01,,true,PENDING";
        
        var content = CreateMultipartFormDataContent(csvContent, "import.csv");

        // Act
        var response = await Client.PostAsync("/api/staff/import/csv", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task ImportStaffFromCsv_WithUpdateExisting_ShouldUpdateStaff()
    {
        // Arrange - Authenticate and create existing staff
        await AuthenticateAsAdminAsync();
        await SeedDepartmentsAndLocations();
        
        using (var context = GetDbContext())
        {
            var existingStaff = new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "UPDATE001",
                FirstName = "Old",
                LastName = "Name",
                Email = "old@test.com",
                HireDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Staff.AddAsync(existingStaff);
            await context.SaveChangesAsync();
        }

        var csvContent = @"EmployeeId,BadgeNumber,FirstName,LastName,MiddleName,Email,Phone,Mobile,DepartmentCode,LocationCode,ShiftCode,PositionTitle,EmploymentType,HireDate,TerminationDate,IsActive,EnrollmentStatus
UPDATE001,B001,Updated,Name,,updated@test.com,555-1234,,IT,HQ,,Senior Developer,Full-Time,2024-01-01,,true,COMPLETED";
        
        var content = CreateMultipartFormDataContent(csvContent, "import.csv");

        // Act
        var response = await Client.PostAsync("/api/staff/import/csv?updateExisting=true", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.SuccessCount.Should().Be(1);

        // Verify database
        using (var context2 = GetDbContext())
        {
            var updatedStaff = await context2.Staff.FirstAsync(s => s.EmployeeId == "UPDATE001");
            updatedStaff.FirstName.Should().Be("Updated");
            updatedStaff.Email.Should().Be("updated@test.com");
            updatedStaff.Phone.Should().Be("555-1234");
            updatedStaff.PositionTitle.Should().Be("Senior Developer");
            updatedStaff.EnrollmentStatus.Should().Be("COMPLETED");
        }
    }

    [Fact]
    public async Task ImportStaffFromCsv_WithMissingRequiredFields_ShouldReturnValidationErrors()
    {
        // Arrange - Authenticate and create CSV with missing FirstName
        await AuthenticateAsAdminAsync();
        await SeedDepartmentsAndLocations();
        var csvContent = @"EmployeeId,BadgeNumber,FirstName,LastName,MiddleName,Email,Phone,Mobile,DepartmentCode,LocationCode,ShiftCode,PositionTitle,EmploymentType,HireDate,TerminationDate,IsActive,EnrollmentStatus
INVALID001,B001,,Doe,,john@test.com,,,IT,HQ,,Developer,Full-Time,2024-01-01,,true,PENDING";
        
        var content = CreateMultipartFormDataContent(csvContent, "import.csv");

        // Act
        var response = await Client.PostAsync("/api/staff/import/csv", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
        result.Errors[0].ValidationErrors.Should().ContainKey("FirstName");
    }

    [Fact]
    public async Task ValidateStaffImport_WithValidData_ShouldReturnValid()
    {
        // Arrange - Authenticate and create valid CSV
        await AuthenticateAsAdminAsync();
        await SeedDepartmentsAndLocations();
        var csvContent = CreateValidCsvContent();
        var content = CreateMultipartFormDataContent(csvContent, "import.csv");

        // Act
        var response = await Client.PostAsync("/api/staff/import/csv/validate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
        result.Should().NotBeNull();
        result!.Valid.Should().BeTrue();
        result.ValidRows.Should().Be(2);
        result.ErrorCount.Should().Be(0);

        // Verify no data was saved
        using var context = GetDbContext();
        var staff = await context.Staff.Where(s => s.EmployeeId.StartsWith("IMPORT")).ToListAsync();
        staff.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportStaffFromCsv_WithNoFile_ShouldReturnBadRequest()
    {
        // Arrange - Authenticate
        await AuthenticateAsAdminAsync();

        // Act - Send multipart form with no "file" field
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("dummy"), "notfile"); // Add a field that's not named "file"
        var response = await Client.PostAsync("/api/staff/import/csv", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // The response is camelCase JSON with "error" field
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("No file uploaded");
    }

    // Helper methods
    private async Task SeedTestDataAsync()
    {
        await SeedDepartmentsAndLocations();

        using var context = GetDbContext();
        var staff = new[]
        {
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "TEST001",
                FirstName = "Test",
                LastName = "User1",
                Email = "test1@test.com",
                HireDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Staff
            {
                StaffId = Guid.NewGuid(),
                EmployeeId = "TEST002",
                FirstName = "Test",
                LastName = "User2",
                Email = "test2@test.com",
                HireDate = DateTime.UtcNow.AddYears(-2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Staff.AddRangeAsync(staff);
        await context.SaveChangesAsync();
    }

    private async Task SeedDepartmentsAndLocations()
    {
        using var context = GetDbContext();
        
        var dept = new Department
        {
            DepartmentId = Guid.NewGuid(),
            DepartmentCode = "IT",
            DepartmentName = "Information Technology",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var location = new Location
        {
            LocationId = Guid.NewGuid(),
            LocationCode = "HQ",
            LocationName = "Headquarters",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.Departments.AddAsync(dept);
        await context.Locations.AddAsync(location);
        await context.SaveChangesAsync();
    }

    private string CreateValidCsvContent()
    {
        return @"EmployeeId,BadgeNumber,FirstName,LastName,MiddleName,Email,Phone,Mobile,DepartmentCode,LocationCode,ShiftCode,PositionTitle,EmploymentType,HireDate,TerminationDate,IsActive,EnrollmentStatus
IMPORT001,B001,John,Doe,,john@test.com,,,IT,HQ,,Developer,Full-Time,2024-01-01,,true,PENDING
IMPORT002,B002,Jane,Smith,,jane@test.com,,,IT,HQ,,Designer,Full-Time,2024-02-01,,true,PENDING";
    }

    private MultipartFormDataContent CreateMultipartFormDataContent(string content, string fileName)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(fileContent, "file", fileName);
        return formData;
    }

    // Response DTOs
    private class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ErrorDetail> Errors { get; set; } = [];
        public List<SuccessDetail> SuccessfulImports { get; set; } = [];
    }

    private class ErrorDetail
    {
        public int RowNumber { get; set; }
        public string EmployeeId { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
        public Dictionary<string, string[]> ValidationErrors { get; set; } = [];
    }

    private class SuccessDetail
    {
        public int RowNumber { get; set; }
        public string EmployeeId { get; set; } = null!;
        public Guid StaffId { get; set; }
        public bool IsNew { get; set; }
    }

    private class ValidationResult
    {
        public bool Valid { get; set; }
        public string Message { get; set; } = null!;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorCount { get; set; }
        public List<ErrorDetail> Errors { get; set; } = [];
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = null!;
    }
}
