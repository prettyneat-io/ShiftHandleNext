using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PunchClockApi.Data;
using PunchClockApi.Services;
using PyZK.DotNet;
using Python.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Initialize Python.NET for device integration (skip in test environment)
if (!builder.Environment.IsEnvironment("Testing"))
{
    // Set Python DLL path for Linux
    if (OperatingSystem.IsLinux())
    {
        Runtime.PythonDLL = "/usr/lib/x86_64-linux-gnu/libpython3.13.so.1.0";
    }
    PyZKClient.InitializePython();
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Punch Clock API", Version = "v1" });
    
    // Configure operation IDs to use controller + method names
    options.CustomOperationIds(apiDesc =>
    {
        var actionDescriptor = apiDesc.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (actionDescriptor != null)
        {
            var controllerName = actionDescriptor.ControllerName;
            var actionName = actionDescriptor.ActionName;
            return $"{actionName}{controllerName}";
        }
        return null;
    });
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure database - Don't register in Test environment (WebApplicationFactory will do it)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<PunchClockDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Register application services (skip IDeviceService in tests - mock will be provided)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<IDeviceService, DeviceService>();
}
builder.Services.AddScoped<AttendanceProcessingService>();
builder.Services.AddScoped<AttendanceProcessingJob>();
builder.Services.AddScoped<DeviceSyncJob>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IStaffImportExportService, StaffImportExportService>();

// Configure Hangfire for background jobs (skip in test environment)
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => 
            options.UseNpgsqlConnection(connectionString)));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
    });
}

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Configure Authorization - require authentication by default
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PunchClockApi.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PunchClockApi.Authorization.PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Seed database if configured
if (builder.Configuration.GetValue<bool>("Database:SeedDatabase"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<PunchClockDbContext>();
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
            var seeder = new DatabaseSeeder(context, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Punch Clock API v1"));
}

// Configure Hangfire dashboard (skip in test environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    // Allow anonymous access only in development, require admin in production
    var allowAnonymous = app.Environment.IsDevelopment();
    
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter(allowAnonymous)],
        DashboardTitle = "Punch Clock Background Jobs"
    });

    // Schedule recurring jobs
    RecurringJob.AddOrUpdate<DeviceSyncJob>(
        "sync-all-devices",
        job => job.SyncAllDevicesAsync(),
        Cron.Hourly);  // Run every hour

    RecurringJob.AddOrUpdate<DeviceSyncJob>(
        "sync-all-staff",
        job => job.SyncAllStaffAsync(),
        "0 */6 * * *");  // Run every 6 hours

    // RecurringJob.AddOrUpdate<DeviceSyncJob>(
    //     "remove-inactive-staff",
    //     job => job.RemoveInactiveStaffFromAllDevicesAsync(),
    //     Cron.Daily(2));  // Run daily at 2:00 AM

    RecurringJob.AddOrUpdate<AttendanceProcessingJob>(
        "process-yesterday-attendance",
        job => job.ProcessYesterdayAttendanceAsync(),
        Cron.Daily(1));  // Run daily at 1:00 AM

    RecurringJob.AddOrUpdate<AttendanceProcessingJob>(
        "process-pending-punches",
        job => job.ProcessPendingPunchLogsAsync(),
        "*/30 * * * *");  // Run every 30 minutes
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Register shutdown handler to cleanup Python.NET
// Note: Commented out due to BinaryFormatter deprecation in .NET 9
// var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
// lifetime.ApplicationStopping.Register(() =>
// {
//     PyZKClient.ShutdownPython();
// });

app.Run();

// Make Program accessible to tests
public partial class Program { }
