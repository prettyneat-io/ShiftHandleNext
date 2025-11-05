using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Services;

public sealed class StaffImportExportService : IStaffImportExportService
{
    private readonly PunchClockDbContext _db;
    private readonly ILogger<StaffImportExportService> _logger;

    public StaffImportExportService(
        PunchClockDbContext db,
        ILogger<StaffImportExportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<byte[]> ExportStaffToCsvAsync(bool includeInactive = false)
    {
        _logger.LogInformation("Exporting staff to CSV (includeInactive: {IncludeInactive})", includeInactive);

        var query = _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Location)
            .Include(s => s.Shift)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var staff = await query
            .OrderBy(s => s.EmployeeId)
            .ToListAsync();

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Write header
        csv.WriteField("EmployeeId");
        csv.WriteField("BadgeNumber");
        csv.WriteField("FirstName");
        csv.WriteField("LastName");
        csv.WriteField("MiddleName");
        csv.WriteField("Email");
        csv.WriteField("Phone");
        csv.WriteField("Mobile");
        csv.WriteField("DepartmentCode");
        csv.WriteField("LocationCode");
        csv.WriteField("ShiftCode");
        csv.WriteField("PositionTitle");
        csv.WriteField("EmploymentType");
        csv.WriteField("HireDate");
        csv.WriteField("TerminationDate");
        csv.WriteField("IsActive");
        csv.WriteField("EnrollmentStatus");
        csv.NextRecord();

        // Write data
        foreach (var s in staff)
        {
            csv.WriteField(s.EmployeeId);
            csv.WriteField(s.BadgeNumber ?? "");
            csv.WriteField(s.FirstName);
            csv.WriteField(s.LastName);
            csv.WriteField(s.MiddleName ?? "");
            csv.WriteField(s.Email ?? "");
            csv.WriteField(s.Phone ?? "");
            csv.WriteField(s.Mobile ?? "");
            csv.WriteField(s.Department?.DepartmentCode ?? "");
            csv.WriteField(s.Location?.LocationCode ?? "");
            csv.WriteField(s.Shift?.ShiftCode ?? "");
            csv.WriteField(s.PositionTitle ?? "");
            csv.WriteField(s.EmploymentType ?? "");
            csv.WriteField(s.HireDate.ToString("yyyy-MM-dd"));
            csv.WriteField(s.TerminationDate?.ToString("yyyy-MM-dd") ?? "");
            csv.WriteField(s.IsActive ? "true" : "false");
            csv.WriteField(s.EnrollmentStatus);
            csv.NextRecord();
        }

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }

    public async Task<StaffImportResult> ImportStaffFromCsvAsync(Stream csvStream, bool updateExisting = false)
    {
        _logger.LogInformation("Importing staff from CSV (updateExisting: {UpdateExisting})", updateExisting);
        
        var result = await ValidateAndProcessImportAsync(csvStream, saveChanges: true, updateExisting);
        
        _logger.LogInformation(
            "Staff import completed: {Success} success, {Errors} errors out of {Total} rows",
            result.SuccessCount, result.ErrorCount, result.TotalRows);
        
        return result;
    }

    public async Task<StaffImportResult> ValidateStaffImportAsync(Stream csvStream)
    {
        _logger.LogInformation("Validating staff import CSV");
        
        var result = await ValidateAndProcessImportAsync(csvStream, saveChanges: false, updateExisting: false);
        
        _logger.LogInformation(
            "Staff import validation completed: {Success} valid, {Errors} errors out of {Total} rows",
            result.SuccessCount, result.ErrorCount, result.TotalRows);
        
        return result;
    }

    private async Task<StaffImportResult> ValidateAndProcessImportAsync(
        Stream csvStream, 
        bool saveChanges, 
        bool updateExisting)
    {
        var result = new StaffImportResult();
        
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();

        // Load reference data - handle potential duplicates by taking the first
        var departments = await _db.Departments
            .Where(d => d.DepartmentCode != null && d.IsActive)
            .GroupBy(d => d.DepartmentCode!)
            .Select(g => g.First())
            .ToDictionaryAsync(d => d.DepartmentCode!, d => d);
            
        var locations = await _db.Locations
            .Where(l => l.LocationCode != null && l.IsActive)
            .GroupBy(l => l.LocationCode!)
            .Select(g => g.First())
            .ToDictionaryAsync(l => l.LocationCode!, l => l);
            
        var shifts = await _db.Shifts
            .Where(s => s.ShiftCode != null && s.IsActive)
            .GroupBy(s => s.ShiftCode!)
            .Select(g => g.First())
            .ToDictionaryAsync(s => s.ShiftCode!, s => s);
            
        var existingStaff = await _db.Staff
            .GroupBy(s => s.EmployeeId)
            .Select(g => g.First())
            .ToDictionaryAsync(s => s.EmployeeId, s => s);

        int rowNumber = 1; // Header is row 0

        while (await csv.ReadAsync())
        {
            rowNumber++;
            result.TotalRows++;

            try
            {
                var employeeId = csv.GetField<string>("EmployeeId")?.Trim();
                
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    result.Errors.Add(new StaffImportError
                    {
                        RowNumber = rowNumber,
                        EmployeeId = "",
                        ErrorMessage = "EmployeeId is required"
                    });
                    result.ErrorCount++;
                    continue;
                }

                // Validate required fields
                var validationErrors = new Dictionary<string, string[]>();
                var firstName = csv.GetField<string>("FirstName")?.Trim();
                var lastName = csv.GetField<string>("LastName")?.Trim();

                if (string.IsNullOrWhiteSpace(firstName))
                    validationErrors["FirstName"] = ["First name is required"];
                
                if (string.IsNullOrWhiteSpace(lastName))
                    validationErrors["LastName"] = ["Last name is required"];

                // Validate hire date
                var hireDateStr = csv.GetField<string>("HireDate")?.Trim();
                DateTime hireDate = DateTime.UtcNow; // Default value
                if (string.IsNullOrWhiteSpace(hireDateStr) || !DateTime.TryParse(hireDateStr, out hireDate))
                {
                    validationErrors["HireDate"] = ["Valid hire date is required (yyyy-MM-dd)"];
                }

                // Validate references
                var departmentCode = csv.GetField<string>("DepartmentCode")?.Trim();
                Guid? departmentId = null;
                if (!string.IsNullOrWhiteSpace(departmentCode))
                {
                    if (departments.TryGetValue(departmentCode, out var dept))
                    {
                        departmentId = dept.DepartmentId;
                    }
                    else
                    {
                        validationErrors["DepartmentCode"] = [$"Department '{departmentCode}' not found"];
                    }
                }

                var locationCode = csv.GetField<string>("LocationCode")?.Trim();
                Guid? locationId = null;
                if (!string.IsNullOrWhiteSpace(locationCode))
                {
                    if (locations.TryGetValue(locationCode, out var loc))
                    {
                        locationId = loc.LocationId;
                    }
                    else
                    {
                        validationErrors["LocationCode"] = [$"Location '{locationCode}' not found"];
                    }
                }

                var shiftCode = csv.GetField<string>("ShiftCode")?.Trim();
                Guid? shiftId = null;
                if (!string.IsNullOrWhiteSpace(shiftCode))
                {
                    if (shifts.TryGetValue(shiftCode, out var shift))
                    {
                        shiftId = shift.ShiftId;
                    }
                    else
                    {
                        validationErrors["ShiftCode"] = [$"Shift '{shiftCode}' not found"];
                    }
                }

                // Parse optional fields
                var terminationDateStr = csv.GetField<string>("TerminationDate")?.Trim();
                DateTime? terminationDate = null;
                if (!string.IsNullOrWhiteSpace(terminationDateStr))
                {
                    if (!DateTime.TryParse(terminationDateStr, out var tempDate))
                    {
                        validationErrors["TerminationDate"] = ["Invalid termination date format (yyyy-MM-dd)"];
                    }
                    else
                    {
                        terminationDate = tempDate;
                    }
                }

                var isActiveStr = csv.GetField<string>("IsActive")?.Trim()?.ToLower();
                var isActive = isActiveStr != "false" && isActiveStr != "0";

                if (validationErrors.Any())
                {
                    result.Errors.Add(new StaffImportError
                    {
                        RowNumber = rowNumber,
                        EmployeeId = employeeId,
                        ErrorMessage = "Validation failed",
                        ValidationErrors = validationErrors
                    });
                    result.ErrorCount++;
                    continue;
                }

                // Check if staff exists
                var existingStaffRecord = existingStaff.GetValueOrDefault(employeeId);
                var isNewRecord = existingStaffRecord is null;

                if (!isNewRecord && !updateExisting)
                {
                    result.Errors.Add(new StaffImportError
                    {
                        RowNumber = rowNumber,
                        EmployeeId = employeeId,
                        ErrorMessage = "Staff already exists (use updateExisting=true to update)"
                    });
                    result.ErrorCount++;
                    continue;
                }

                if (saveChanges)
                {
                    Staff staff;
                    if (isNewRecord)
                    {
                        staff = new Staff
                        {
                            StaffId = Guid.NewGuid(),
                            EmployeeId = employeeId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Staff.Add(staff);
                    }
                    else
                    {
                        staff = existingStaffRecord!;
                        staff.UpdatedAt = DateTime.UtcNow;
                    }

                    // Update fields
                    staff.FirstName = firstName!;
                    staff.LastName = lastName!;
                    staff.MiddleName = csv.GetField<string>("MiddleName")?.Trim();
                    staff.BadgeNumber = csv.GetField<string>("BadgeNumber")?.Trim();
                    staff.Email = csv.GetField<string>("Email")?.Trim();
                    staff.Phone = csv.GetField<string>("Phone")?.Trim();
                    staff.Mobile = csv.GetField<string>("Mobile")?.Trim();
                    staff.DepartmentId = departmentId;
                    staff.LocationId = locationId;
                    staff.ShiftId = shiftId;
                    staff.PositionTitle = csv.GetField<string>("PositionTitle")?.Trim();
                    staff.EmploymentType = csv.GetField<string>("EmploymentType")?.Trim();
                    staff.HireDate = hireDate;
                    staff.TerminationDate = terminationDate;
                    staff.IsActive = isActive;
                    
                    var enrollmentStatus = csv.GetField<string>("EnrollmentStatus")?.Trim()?.ToUpper();
                    if (!string.IsNullOrWhiteSpace(enrollmentStatus))
                    {
                        staff.EnrollmentStatus = enrollmentStatus;
                    }

                    result.SuccessfulImports.Add(new StaffImportSuccess
                    {
                        RowNumber = rowNumber,
                        EmployeeId = employeeId,
                        StaffId = staff.StaffId,
                        IsNew = isNewRecord
                    });
                }

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing row {RowNumber}", rowNumber);
                result.Errors.Add(new StaffImportError
                {
                    RowNumber = rowNumber,
                    EmployeeId = csv.GetField<string>("EmployeeId") ?? "",
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                });
                result.ErrorCount++;
            }
        }

        if (saveChanges && result.SuccessCount > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} staff records to database", result.SuccessCount);
        }

        return result;
    }
}
