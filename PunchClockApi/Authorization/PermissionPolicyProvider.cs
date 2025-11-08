using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PunchClockApi.Authorization;

/// <summary>
/// Custom authorization policy provider that dynamically generates policies
/// from permission strings like "staff:create", "devices:read", etc.
/// This allows using [Authorize(Policy = "staff:create")] on controllers.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if policy name follows permission format (resource:action)
        if (policyName.Contains(':'))
        {
            var parts = policyName.Split(':', 2);
            if (parts.Length == 2)
            {
                var resource = parts[0];
                var action = parts[1];

                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(resource, action))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        // If not a permission policy, fall back to default provider
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
