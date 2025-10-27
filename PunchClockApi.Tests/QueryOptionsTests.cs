using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for query options (pagination, sorting, filtering, includes).
/// Migrated from test-query-options.sh
/// </summary>
public sealed class QueryOptionsTests : IntegrationTestBase
{
    public QueryOptionsTests(TestWebApplicationFactory factory) : base(factory) { }

    #region Pagination Tests

    [Fact]
    public async Task StaffPagination_WithPageAndLimit_ReturnsPaginatedStructure()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffResponse>>();
        result.Should().NotBeNull();
        result!.Total.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task StaffPagination_LimitRespected_ReturnsCorrectPageSize()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffResponse>>();
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(2);
        result.Data.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task StaffPagination_Page2_ReturnsCorrectPageNumber()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=2&limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(2);
    }

    [Fact]
    public async Task StaffPagination_Page0_DefaultsToPage1()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=0&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task StaffSorting_ByFirstNameAscending_ReturnsSortedData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?sort=FirstName&order=asc&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
        
        // Verify sorted order
        if (staff!.Count > 1)
        {
            for (int i = 0; i < staff.Count - 1; i++)
            {
                var current = staff[i].FirstName;
                var next = staff[i + 1].FirstName;
                string.Compare(current, next, StringComparison.OrdinalIgnoreCase)
                    .Should().BeLessThanOrEqualTo(0, "items should be in ascending order");
            }
        }
    }

    [Fact]
    public async Task StaffSorting_ByLastNameDescending_ReturnsData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?sort=LastName&order=desc&limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task StaffSorting_InvalidField_ReturnsDataWithoutCrashing()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?sort=NonExistentField&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
    }

    #endregion

    #region Include (Eager Loading) Tests

    [Fact]
    public async Task StaffInclude_Department_LoadsDepartmentData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?include=Department&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffWithRelationsResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
        
        var firstStaff = staff![0];
        firstStaff.Department.Should().NotBeNull("Department should be loaded");
        firstStaff.Department!.DepartmentName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StaffInclude_Location_LoadsLocationData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?include=Location&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffWithRelationsResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
        
        var firstStaff = staff![0];
        firstStaff.Location.Should().NotBeNull("Location should be loaded");
        firstStaff.Location!.LocationName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StaffInclude_MultipleRelations_LoadsAllData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?include=Department,Location&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffWithRelationsResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
        
        var firstStaff = staff![0];
        firstStaff.Department.Should().NotBeNull("Department should be loaded");
        firstStaff.Department!.DepartmentName.Should().NotBeNullOrEmpty();
        firstStaff.Location.Should().NotBeNull("Location should be loaded");
        firstStaff.Location!.LocationName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StaffInclude_EmptyParameter_ReturnsData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?include=&limit=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task StaffFilter_ByFirstName_ReturnsMatchingResults()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?FirstName=a&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        
        // All results should contain 'a' in first name
        foreach (var s in staff!)
        {
            s.FirstName.Should().ContainEquivalentOf("a");
        }
    }

    [Fact]
    public async Task StaffFilter_NonExistentName_ReturnsEmptyArray()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?FirstName=ZZZZNONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        staff.Should().BeEmpty();
    }

    #endregion

    #region Combined Query Options Tests

    [Fact]
    public async Task Staff_PaginationAndSorting_WorksTogether()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=1&limit=3&sort=FirstName&order=asc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Staff_PaginationAndInclude_WorksTogether()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?page=1&limit=2&include=Department");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<StaffWithRelationsResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCountGreaterThan(0);
        result.Data[0].Department.Should().NotBeNull();
    }

    [Fact]
    public async Task Staff_FilterSortLimit_WorksTogether()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/staff?FirstName=a&sort=LastName&order=asc&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var staff = await response.Content.ReadFromJsonAsync<List<StaffResponse>>();
        staff.Should().NotBeNull();
        staff.Should().HaveCountLessThanOrEqualTo(5);
        
        // All should match filter
        foreach (var s in staff!)
        {
            s.FirstName.Should().ContainEquivalentOf("a");
        }
    }

    #endregion

    #region Other Endpoints Tests

    [Fact]
    public async Task Devices_PaginationStructure_Works()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/devices?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<DeviceResponse>>();
        result.Should().NotBeNull();
        result!.Total.Should().BeGreaterThanOrEqualTo(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task AttendanceLogs_PaginationStructure_Works()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/attendance/logs?page=1&limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PunchLogResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task Departments_PaginationStructure_Works()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/departments?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<DepartmentResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Locations_PaginationStructure_Works()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/locations?page=1&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<LocationResponse>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    #endregion

    // Response DTOs
    private sealed class PaginatedResponse<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = [];
    }

    private class StaffResponse
    {
        public Guid StaffId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class StaffWithRelationsResponse : StaffResponse
    {
        public DepartmentResponse? Department { get; set; }
        public LocationResponse? Location { get; set; }
    }

    private sealed class DepartmentResponse
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
    }

    private sealed class LocationResponse
    {
        public Guid LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
    }

    private sealed class DeviceResponse
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
    }

    private sealed class PunchLogResponse
    {
        public Guid PunchLogId { get; set; }
        public DateTime PunchTime { get; set; }
    }
}
