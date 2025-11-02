using Hangfire.Dashboard;
using Microsoft.Extensions.Logging;

namespace PunchClockApi.Services;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// In production, this ensures only authenticated administrators can access the dashboard.
/// Supports both role-based and permission-based authorization.
/// </summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAnonymous;
    private readonly ILogger<HangfireAuthorizationFilter>? _logger;
    private readonly string[] _allowedRoles;

    public HangfireAuthorizationFilter(
        bool allowAnonymous = false,
        ILogger<HangfireAuthorizationFilter>? logger = null,
        params string[] allowedRoles)
    {
        _allowAnonymous = allowAnonymous;
        _logger = logger;
        _allowedRoles = allowedRoles.Length > 0 ? allowedRoles : ["Admin", "System Administrator"];
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Allow anonymous access in development only
        if (_allowAnonymous)
        {
            _logger?.LogInformation("Hangfire dashboard accessed anonymously from {IpAddress}", remoteIp);
            return true;
        }

        // Require authentication
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            _logger?.LogWarning("Unauthorized Hangfire dashboard access attempt from {IpAddress} - not authenticated", remoteIp);
            return false;
        }

        var username = httpContext.User.Identity.Name ?? "unknown";

        // Check if user has any of the allowed roles
        foreach (var role in _allowedRoles)
        {
            if (httpContext.User.IsInRole(role))
            {
                _logger?.LogInformation("Hangfire dashboard accessed by user {Username} with role {Role} from {IpAddress}", 
                    username, role, remoteIp);
                return true;
            }
        }

        _logger?.LogWarning("Unauthorized Hangfire dashboard access attempt by user {Username} from {IpAddress} - insufficient permissions", 
            username, remoteIp);
        return false;
    }
}
