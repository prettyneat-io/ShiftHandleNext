using Microsoft.AspNetCore.Authorization;

namespace PunchClockApi.Authorization;

/// <summary>
/// Represents a permission requirement for authorization.
/// Format: resource:action (e.g., "staff:create", "devices:read")
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Resource { get; }
    public string Action { get; }

    public PermissionRequirement(string resource, string action)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override string ToString() => $"{Resource}:{Action}";
}
