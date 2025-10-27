using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PunchClockApi.Data;
using PunchClockApi.Models;

namespace PunchClockApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : BaseController<User>
{
    private readonly PunchClockDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(
        PunchClockDbContext db,
        IConfiguration configuration,
        ILogger<AuthController> logger)
        : base(logger)
    {
        _db = db;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
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

            // Create new user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                PasswordHash = HashPassword(request.Password),
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Logger.LogInformation("New user registered: {Username}", user.Username);

            return Ok(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, error = "Username and password are required" });
            }

            // Find user
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user is null)
            {
                return Unauthorized(new { success = false, error = "Invalid username or password" });
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { success = false, error = "Invalid username or password" });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Unauthorized(new { success = false, error = "User account is inactive" });
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token (in production, save to database)
            user.PasswordResetToken = refreshToken;
            user.PasswordResetExpires = DateTime.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
            await _db.SaveChangesAsync();

            Logger.LogInformation("User logged in: {Username}", user.Username);

            return Ok(new
            {
                accessToken,
                tokenType = "Bearer",
                refreshToken,
                expiresIn = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60) * 60,
                user = new
                {
                    userId = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { success = false, error = "Refresh token is required" });
            }

            // Find user with matching refresh token
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.PasswordResetToken == request.RefreshToken &&
                    u.PasswordResetExpires > DateTime.UtcNow);

            if (user is null)
            {
                return Unauthorized(new { success = false, error = "Invalid or expired refresh token" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { success = false, error = "User account is inactive" });
            }

            // Generate new tokens
            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token
            user.PasswordResetToken = newRefreshToken;
            user.PasswordResetExpires = DateTime.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                accessToken,
                refreshToken = newRefreshToken,
                expiresIn = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60) * 60
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = GetUserIdClaim();
            if (userId is null)
            {
                return Unauthorized(new { success = false, error = "User not authenticated" });
            }

            // Clear refresh token
            var user = await _db.Users.FindAsync(Guid.Parse(userId));
            if (user is not null)
            {
                user.PasswordResetToken = null;
                user.PasswordResetExpires = null;
                await _db.SaveChangesAsync();
            }

            Logger.LogInformation("User logged out: {UserId}", userId);

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetUserIdClaim();
            if (userId is null)
            {
                return Unauthorized(new { success = false, error = "User not authenticated" });
            }

            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == Guid.Parse(userId));

            if (user is null)
            {
                return NotFound(new { success = false, error = "User not found" });
            }

            return Ok(new
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
                roles = user.UserRoles.Select(ur => new
                {
                    roleId = ur.Role.RoleId,
                    roleName = ur.Role.RoleName,
                    permissions = ur.Role.RolePermissions.Select(rp => new
                    {
                        permissionId = rp.Permission.PermissionId,
                        resource = rp.Permission.Resource,
                        action = rp.Permission.Action
                    })
                })
            });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private string GenerateAccessToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer not configured");
        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience not configured");
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("sub", user.UserId.ToString()),
            new("username", user.Username)
        };

        // Add role claims
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashPassword(string password)
    {
        // Using BCrypt would be better in production
        // For now, using simple SHA256 (NOT recommended for production)
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == passwordHash;
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null);

public sealed record RefreshTokenRequest(string RefreshToken);
