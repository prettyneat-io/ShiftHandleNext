using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // All endpoints require authentication
public sealed class UsersController : BaseController<User>
{
    private readonly PunchClockDbContext _db;

    public UsersController(PunchClockDbContext db, ILogger<UsersController> logger)
        : base(logger)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = "users:read")]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? limit)
    {
        try
        {
            var query = _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt);

            var pageNum = page ?? 1;
            var pageSize = limit ?? 50;
            var skip = (pageNum - 1) * pageSize;

            var total = await query.CountAsync();
            var users = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    email = u.Email,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    phone = u.Phone,
                    isActive = u.IsActive,
                    isVerified = u.IsVerified,
                    lastLogin = u.LastLogin,
                    createdAt = u.CreatedAt,
                    roles = u.UserRoles.Select(ur => ur.Role.RoleName)
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "users:read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            return Ok(new
            {
                success = true,
                user = new
                {
                    userId = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    phone = user.Phone,
                    isActive = user.IsActive,
                    isVerified = user.IsVerified,
                    lastLogin = user.LastLogin,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt,
                    roles = user.UserRoles.Select(ur => new
                    {
                        roleId = ur.Role.RoleId,
                        roleName = ur.Role.RoleName,
                        assignedAt = ur.AssignedAt,
                        expiresAt = ur.ExpiresAt,
                        permissions = ur.Role.RolePermissions.Select(rp => new
                        {
                            permissionId = rp.Permission.PermissionId,
                            resource = rp.Permission.Resource,
                            action = rp.Permission.Action
                        })
                    })
                }
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [Authorize(Policy = "users:create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, error = "Username, email, and password are required" });
            }

            // Check if user already exists
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser is not null)
            {
                return Conflict(new { success = false, error = "Username or email already exists" });
            }

            var currentUserId = GetUserIdClaim();

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                PasswordHash = HashPassword(request.Password),
                IsActive = request.IsActive ?? true,
                IsVerified = request.IsVerified ?? false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId is not null ? Guid.Parse(currentUserId) : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Logger.LogInformation("User created: {Username} by {CurrentUserId}", user.Username, currentUserId);

            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, new
            {
                success = true,
                userId = user.UserId,
                username = user.Username
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "users:update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            var currentUserId = GetUserIdClaim();

            // Users can only update themselves unless they're admin
            if (currentUserId != id.ToString() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already taken by another user
                var emailExists = await _db.Users
                    .AnyAsync(u => u.Email == request.Email && u.UserId != id);
                if (emailExists)
                {
                    return Conflict(new { success = false, error = "Email already in use" });
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            if (request.Phone is not null)
                user.Phone = request.Phone;

            // Only admins can change active/verified status
            if (User.IsInRole("Admin"))
            {
                if (request.IsActive.HasValue)
                    user.IsActive = request.IsActive.Value;

                if (request.IsVerified.HasValue)
                    user.IsVerified = request.IsVerified.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId is not null ? Guid.Parse(currentUserId) : null;

            await _db.SaveChangesAsync();

            Logger.LogInformation("User updated: {UserId} by {CurrentUserId}", id, currentUserId);

            return Ok(new { success = true, message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "users:delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            var currentUserId = GetUserIdClaim();

            // Prevent self-deletion
            if (currentUserId == id.ToString())
            {
                return BadRequest(new { success = false, error = "Cannot delete your own account" });
            }

            // Soft delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId is not null ? Guid.Parse(currentUserId) : null;

            await _db.SaveChangesAsync();

            Logger.LogInformation("User deleted: {UserId} by {CurrentUserId}", id, currentUserId);

            return Ok(new { success = true, message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Assign a role to a user
    /// HR Managers cannot assign the Admin role - only Admins can
    /// </summary>
    [HttpPost("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = "users:assign_roles")]
    public async Task<IActionResult> AssignRole(Guid id, Guid roleId, [FromBody] AssignRoleRequest? request = null)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            var role = await _db.Roles.FindAsync(roleId);
            if (role is null)
            {
                return NotFound(new { success = false, error = "Role not found" });
            }

            // Prevent HR Managers from assigning Admin role
            // Only Admins can assign the Admin role
            if (role.RoleName == "Admin" && !User.IsInRole("Admin"))
            {
                Logger.LogWarning("User {CurrentUser} attempted to assign Admin role - denied", 
                    GetUserIdClaim());
                return StatusCode(403, new 
                { 
                    success = false, 
                    error = "Only Admins can assign the Admin role" 
                });
            }

            // Check if already assigned
            var existingAssignment = await _db.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == roleId);

            if (existingAssignment is not null)
            {
                return Conflict(new { success = false, error = "Role already assigned to user" });
            }

            var currentUserId = GetUserIdClaim();

            var userRole = new UserRole
            {
                UserId = id,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = currentUserId is not null ? Guid.Parse(currentUserId) : null,
                ExpiresAt = request?.ExpiresAt
            };

            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

            Logger.LogInformation("Role {RoleName} ({RoleId}) assigned to user {UserId} by {CurrentUserId}",
                role.RoleName, roleId, id, currentUserId);

            return Ok(new { success = true, message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = "users:assign_roles")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId)
    {
        try
        {
            var userRole = await _db.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == roleId);

            if (userRole is null)
            {
                return NotFound(new { success = false, error = "Role assignment not found" });
            }

            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync();

            var currentUserId = GetUserIdClaim();
            Logger.LogInformation("Role {RoleId} removed from user {UserId} by {CurrentUserId}",
                roleId, id, currentUserId);

            return Ok(new { success = true, message = "Role removed successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var currentUserId = GetUserIdClaim();

            // Users can only change their own password unless they're admin
            if (currentUserId != id.ToString() && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var user = await _db.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            // Verify current password if not admin
            if (!User.IsInRole("Admin"))
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return BadRequest(new { success = false, error = "Current password is required" });
                }

                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return Unauthorized(new { success = false, error = "Current password is incorrect" });
                }
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, error = "New password is required" });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId is not null ? Guid.Parse(currentUserId) : null;

            // Clear refresh tokens to force re-login
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;

            await _db.SaveChangesAsync();

            Logger.LogInformation("Password changed for user {UserId} by {CurrentUserId}", id, currentUserId);

            return Ok(new { success = true, message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private static string HashPassword(string password) 
        => BCrypt.Net.BCrypt.HashPassword(password);

    private static bool VerifyPassword(string password, string passwordHash) 
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null,
    bool? IsActive = null,
    bool? IsVerified = null);

public sealed record UpdateUserRequest(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    bool? IsActive = null,
    bool? IsVerified = null);

public sealed record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword);

public sealed record AssignRoleRequest(DateTime? ExpiresAt = null);
