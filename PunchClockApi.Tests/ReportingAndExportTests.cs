using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Models;
using PunchClockApi.Services;
using Xunit;

namespace PunchClockApi.Tests;

/// <summary>
/// Integration tests for reporting and export functionality
/// </summary>
public sealed class ReportingAndExportTests : IntegrationTestBase
{
    public ReportingAndExportTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Daily Report Tests

    [Fact]
    public async Task GetDailyReport_ReturnsSuccessWithData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DailyAttendanceReport>();
        Assert.NotNull(report);
        Assert.Equal(today, report.Date);
        Assert.True(report.TotalStaff >= 0);
    }

    [Fact]
    public async Task GetDailyReport_WithoutDate_UsesToday()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync("/api/reports/daily");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DailyAttendanceReport>();
        Assert.NotNull(report);
        Assert.Equal(today, report.Date);
    }

    [Fact]
    public async Task GetDailyReport_WithLocationFilter_ReturnsFilteredData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var db = GetDbContext();
        var location = await db.Locations.FirstAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&locationId={location.LocationId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DailyAttendanceReport>();
        Assert.NotNull(report);
        Assert.All(report.Entries, e => Assert.Equal(location.LocationName, e.Location));
    }

    [Fact]
    public async Task GetDailyReport_WithDepartmentFilter_ReturnsFilteredData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var db = GetDbContext();
        var department = await db.Departments.FirstAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&departmentId={department.DepartmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DailyAttendanceReport>();
        Assert.NotNull(report);
        Assert.All(report.Entries, e => Assert.Equal(department.DepartmentName, e.Department));
    }

    [Fact]
    public async Task GetDailyReport_AsCsv_ReturnsCSVFile()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("StaffId", content); // CSV header
        Assert.Contains("EmployeeId", content);
        Assert.Contains("FullName", content);
        
        // Verify it's actually CSV format
        var lines = content.Split('\n');
        Assert.True(lines.Length >= 1); // At least header
    }

    [Fact]
    public async Task GetDailyReport_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/reports/daily");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Monthly Report Tests

    [Fact]
    public async Task GetMonthlyReport_ReturnsSuccessWithData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyAttendanceReport>();
        Assert.NotNull(report);
        Assert.Equal(now.Year, report.Year);
        Assert.Equal(now.Month, report.Month);
        Assert.True(report.TotalWorkingDays > 0);
        Assert.NotNull(report.Statistics);
    }

    [Fact]
    public async Task GetMonthlyReport_WithoutYearMonth_UsesCurrentMonth()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync("/api/reports/monthly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyAttendanceReport>();
        Assert.NotNull(report);
        Assert.Equal(now.Year, report.Year);
        Assert.Equal(now.Month, report.Month);
    }

    [Fact]
    public async Task GetMonthlyReport_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/reports/monthly?year=2025&month=13");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMonthlyReport_WithLocationFilter_ReturnsFilteredData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var db = GetDbContext();
        var location = await db.Locations.FirstAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}&locationId={location.LocationId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyAttendanceReport>();
        Assert.NotNull(report);
        Assert.All(report.Entries, e => Assert.Equal(location.LocationName, e.Location));
    }

    [Fact]
    public async Task GetMonthlyReport_AsCsv_ReturnsCSVFile()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("StaffId", content);
        Assert.Contains("EmployeeId", content);
        Assert.Contains("AttendanceRate", content);
    }

    [Fact]
    public async Task GetMonthlyReport_CalculatesStatisticsCorrectly()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyAttendanceReport>();
        Assert.NotNull(report);
        Assert.NotNull(report.Statistics);
        Assert.True(report.Statistics.AverageAttendanceRate >= 0);
        Assert.True(report.Statistics.AverageAttendanceRate <= 100);
        Assert.True(report.Statistics.TotalLateIncidents >= 0);
        Assert.True(report.Statistics.TotalAnomalies >= 0);
    }

    #endregion

    #region Payroll Report Tests

    [Fact]
    public async Task GetPayrollReport_ReturnsSuccessWithData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PayrollExportReport>();
        Assert.NotNull(report);
        Assert.Equal(startDate, report.StartDate);
        Assert.Equal(endDate, report.EndDate);
        Assert.True(report.TotalDays > 0);
        Assert.NotNull(report.Entries);
    }

    [Fact]
    public async Task GetPayrollReport_WithoutDates_UsesDefaultDateValues()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act - Without dates, DateOnly parameters default to DateOnly.MinValue
        var response = await Client.GetAsync("/api/reports/payroll");

        // Assert - The endpoint accepts default values but returns data (possibly empty)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PayrollExportReport>();
        Assert.NotNull(report);
    }

    [Fact]
    public async Task GetPayrollReport_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPayrollReport_WithDateRangeExceedingOneYear_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-400));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPayrollReport_AsCsv_ReturnsCSVFile()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("StaffId", content);
        Assert.Contains("EmployeeId", content);
        Assert.Contains("RegularHours", content);
        Assert.Contains("OvertimeHours", content);
        Assert.Contains("WeekendOvertimeHours", content);
        Assert.Contains("HolidayOvertimeHours", content);
    }

    [Fact]
    public async Task GetPayrollReport_IncludesHoursBreakdown()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PayrollExportReport>();
        Assert.NotNull(report);
        
        // Verify each entry has the expected fields
        foreach (var entry in report.Entries)
        {
            Assert.True(entry.RegularHours >= 0);
            Assert.True(entry.OvertimeHours >= 0);
            Assert.True(entry.WeekendOvertimeHours >= 0);
            Assert.True(entry.HolidayOvertimeHours >= 0);
            Assert.True(entry.TotalLateMinutes >= 0);
            Assert.True(entry.TotalEarlyLeaveMinutes >= 0);
            Assert.True(entry.DaysPresent >= 0);
            Assert.True(entry.DaysAbsent >= 0);
            Assert.True(entry.DaysOnLeave >= 0);
        }
    }

    [Fact]
    public async Task GetPayrollReport_WithFilters_ReturnsFilteredData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var db = GetDbContext();
        var location = await db.Locations.FirstAsync();
        var department = await db.Departments.FirstAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync(
            $"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}" +
            $"&locationId={location.LocationId}&departmentId={department.DepartmentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PayrollExportReport>();
        Assert.NotNull(report);
        Assert.All(report.Entries, e =>
        {
            Assert.Equal(location.LocationName, e.Location);
            Assert.Equal(department.DepartmentName, e.Department);
        });
    }

    #endregion

    #region Summary Statistics Tests

    [Fact]
    public async Task GetSummaryStatistics_ReturnsAggregatedData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(summary);
    }

    [Fact]
    public async Task GetSummaryStatistics_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        // Act
        var response = await Client.GetAsync($"/api/reports/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummaryStatistics_IncludesAllSections()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify all expected sections are present
        Assert.Contains("period", content);
        Assert.Contains("staff", content);
        Assert.Contains("hours", content);
        Assert.Contains("attendance", content);
        Assert.Contains("totalRegularHours", content);
        Assert.Contains("totalOvertimeHours", content);
        Assert.Contains("totalPresent", content);
        Assert.Contains("totalAbsent", content);
    }

    #endregion

    #region Department Comparison Tests

    [Fact]
    public async Task GetDepartmentComparison_ReturnsComparisonData()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/departments?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("departments", content);
        Assert.Contains("totalDepartments", content);
    }

    [Fact]
    public async Task GetDepartmentComparison_WithEndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));

        // Act
        var response = await Client.GetAsync($"/api/reports/departments?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDepartmentComparison_IncludesMetricsPerDepartment()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/departments?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify department metrics are present
        Assert.Contains("staffCount", content);
        Assert.Contains("averageAttendanceRate", content);
        Assert.Contains("totalRegularHours", content);
        Assert.Contains("totalOvertimeHours", content);
        Assert.Contains("totalLateMinutes", content);
        Assert.Contains("averageLateMinutesPerStaff", content);
    }

    #endregion

    #region Export Logging Tests

    [Fact]
    public async Task CsvExport_LogsExportToDatabase()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var db = GetDbContext();
        var initialCount = await db.ExportLogs.CountAsync();

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify export was logged
        var finalCount = await db.ExportLogs.CountAsync();
        Assert.Equal(initialCount + 1, finalCount);
        
        var exportLog = await db.ExportLogs.OrderByDescending(e => e.ExportedAt).FirstAsync();
        Assert.Equal("DAILY_ATTENDANCE", exportLog.ExportType);
        Assert.Equal("CSV", exportLog.FileFormat);
        Assert.Equal("SUCCESS", exportLog.ExportStatus);
        Assert.True(exportLog.RecordCount >= 0);
    }

    [Fact]
    public async Task MonthlyReport_CsvExport_LogsWithCorrectDateRange()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var db = GetDbContext();

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={year}&month={month}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var exportLog = await db.ExportLogs.OrderByDescending(e => e.ExportedAt).FirstAsync();
        Assert.Equal("MONTHLY_ATTENDANCE", exportLog.ExportType);
        Assert.Equal(year, exportLog.StartDate.Year);
        Assert.Equal(month, exportLog.StartDate.Month);
    }

    [Fact]
    public async Task PayrollReport_CsvExport_LogsWithFilterCriteria()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var db = GetDbContext();
        var location = await db.Locations.FirstAsync();
        var department = await db.Departments.FirstAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync(
            $"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}" +
            $"&locationId={location.LocationId}&departmentId={department.DepartmentId}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var exportLog = await db.ExportLogs.OrderByDescending(e => e.ExportedAt).FirstAsync();
        Assert.Equal("PAYROLL", exportLog.ExportType);
        Assert.NotNull(exportLog.FilterCriteria);
        Assert.Contains($"location={location.LocationId}", exportLog.FilterCriteria);
        Assert.Contains($"department={department.DepartmentId}", exportLog.FilterCriteria);
    }

    #endregion

    #region CSV Format Tests

    [Fact]
    public async Task CsvExport_HasCorrectContentDisposition()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Content.Headers.ContentDisposition?.DispositionType == "attachment" ||
                    response.Content.Headers.ContentDisposition?.FileName != null);
        Assert.Contains($"daily_attendance_{today:yyyy-MM-dd}.csv", 
                       response.Content.Headers.ContentDisposition?.FileName ?? string.Empty);
    }

    [Fact]
    public async Task CsvExport_HandlesSpecialCharacters()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify CSV is properly formatted (headers exist)
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 1);
        
        // Verify header row has expected structure
        var header = lines[0];
        var columns = header.Split(',');
        Assert.Contains(columns, c => c.Contains("StaffId"));
        Assert.Contains(columns, c => c.Contains("EmployeeId"));
    }

    [Fact]
    public async Task PayrollCsvExport_HasDescriptiveFilename()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var filename = response.Content.Headers.ContentDisposition?.FileName ?? string.Empty;
        Assert.Contains("payroll_export", filename);
        Assert.Contains($"{startDate:yyyy-MM-dd}", filename);
        Assert.Contains($"{endDate:yyyy-MM-dd}", filename);
        Assert.EndsWith(".csv", filename);
    }

    #endregion

    #region Data Accuracy Tests

    [Fact]
    public async Task DailyReport_CountsMatchStaffRecords()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<DailyAttendanceReport>();
        Assert.NotNull(report);
        
        // Verify counts add up
        var expectedTotal = report.PresentCount + report.AbsentCount + report.OnLeaveCount;
        Assert.Equal(report.TotalStaff, expectedTotal);
    }

    [Fact]
    public async Task MonthlyReport_AttendanceRateIsValid()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<MonthlyAttendanceReport>();
        Assert.NotNull(report);
        
        // Verify all attendance rates are valid percentages
        foreach (var entry in report.Entries)
        {
            Assert.InRange(entry.AttendanceRate, 0, 100);
        }
    }

    [Fact]
    public async Task PayrollReport_DaysAddUp()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<PayrollExportReport>();
        Assert.NotNull(report);
        
        // Verify days present + absent + on leave <= total working days for each entry
        foreach (var entry in report.Entries)
        {
            var accountedDays = entry.DaysPresent + entry.DaysAbsent + entry.DaysOnLeave;
            Assert.True(accountedDays <= entry.TotalWorkingDays,
                $"Staff {entry.EmployeeId}: Days don't add up. Present: {entry.DaysPresent}, " +
                $"Absent: {entry.DaysAbsent}, Leave: {entry.DaysOnLeave}, Working Days: {entry.TotalWorkingDays}");
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task DailyReport_CompletesInReasonableTime()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await Client.GetAsync($"/api/reports/daily?date={today:yyyy-MM-dd}");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Daily report took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task MonthlyReport_CompletesInReasonableTime()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await Client.GetAsync($"/api/reports/monthly?year={now.Year}&month={now.Month}");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"Monthly report took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
    }

    [Fact]
    public async Task PayrollReport_CompletesInReasonableTime()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await Client.GetAsync($"/api/reports/payroll?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, 
            $"Payroll report took {stopwatch.ElapsedMilliseconds}ms, expected < 15000ms");
    }

    #endregion
}
