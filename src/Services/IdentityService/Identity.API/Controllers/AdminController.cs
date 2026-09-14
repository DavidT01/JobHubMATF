using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly NotificationService _notifications;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        NotificationService notifications)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _notifications = notifications;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.Email)
            .ToListAsync();

        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            result.Add(await ToDtoAsync(user));
        }

        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var users = await _userManager.Users.ToListAsync();
        var locked = 0;
        var confirmed = 0;
        foreach (var user in users)
        {
            if (user.EmailConfirmed)
            {
                confirmed++;
            }

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            if (lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                locked++;
            }
        }

        return Ok(new
        {
            totalUsers = users.Count,
            confirmedEmails = confirmed,
            lockedAccounts = locked
        });
    }

    [HttpPost("users/{id}/confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        if (user.EmailConfirmed)
        {
            return Ok(new { Message = "Email is already confirmed." });
        }

        user.EmailConfirmed = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = errors });
        }

        await _notifications.NotifyAsync(
            user.Id,
            "Email confirmed by admin",
            "An administrator confirmed your email. You can sign in now.");

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("users/{id}/lock")]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return BadRequest(new { Message = "Cannot lock an admin account." });
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

        await _notifications.NotifyAsync(
            user.Id,
            "Account locked",
            "An administrator has locked your account. Contact support if this is unexpected.");

        return Ok(new { Message = "User locked." });
    }

    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _notifications.NotifyAsync(
            user.Id,
            "Account unlocked",
            "An administrator has unlocked your account. You can sign in again.");

        return Ok(new { Message = "User unlocked." });
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] SetUserRoleDto model)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Candidate,
            AppRoles.Employer,
            AppRoles.Admin
        };

        if (!allowed.Contains(model.Role))
        {
            return BadRequest(new { Message = "Role must be Candidate, Employer, or Admin." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole(model.Role));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        await _notifications.NotifyAsync(
            user.Id,
            "Role updated",
            $"An administrator changed your role to {model.Role}.");

        return Ok(await ToDtoAsync(user));
    }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        var lockedOut = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow;

        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            EmailConfirmed = user.EmailConfirmed,
            LockedOut = lockedOut
        };
    }
}
