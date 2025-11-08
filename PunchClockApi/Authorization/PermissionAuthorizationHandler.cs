using Microsoft.AspNetCore.Authorization;

namespace PunchClockApi.Authorization;

/// <summary>
/// Authorization handler that evaluates permission requirements.
/// Checks if the user has the required permission claim in their JWT token.
/// Format: permission claim = "resource:action" (e.g., "staff:create")
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Permission check failed: User not authenticated for {Requirement}", requirement);
            return Task.CompletedTask;
        }

        var username = context.User.Identity.Name ?? "unknown";
        var requiredPermission = $"{requirement.Resource}:{requirement.Action}";

        // Check if user has the specific permission claim
        if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requiredPermission))
        {
            _logger.LogDebug("Permission granted: User {Username} has permission {Permission}", 
                username, requiredPermission);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Special case: Admin role has all permissions
        if (context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Permission granted: User {Username} has Admin role", username);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        _logger.LogWarning("Permission denied: User {Username} lacks permission {Permission}", 
            username, requiredPermission);
        
        return Task.CompletedTask;
    }
}
