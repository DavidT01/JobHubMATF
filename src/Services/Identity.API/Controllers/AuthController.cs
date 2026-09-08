using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly NotificationService _notifications;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            NotificationService notifications)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _environment = environment;
            _notifications = notifications;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
            {
                return Unauthorized(new { Message = "Invalid email or password!" });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Unauthorized(new { Message = "This account is locked. Contact an administrator." });
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new { Message = "Please confirm your email before signing in." });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, userRoles);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!AppRoles.Registrable.Contains(model.Role!))
            {
                return BadRequest(new { Message = "Role must be Candidate or Employer." });
            }

            var userExists = await _userManager.FindByEmailAsync(model.Email!);
            if (userExists != null)
            {
                return BadRequest(new { Message = "User with this email already exists!" });
            }

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = errors });
            }

            if (!await _roleManager.RoleExistsAsync(model.Role!))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role!));
            }

            await _userManager.AddToRoleAsync(user, model.Role!);

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmToken));
            var frontendBase = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var confirmationUrl =
                $"{frontendBase}/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={encodedToken}";

            await _notifications.NotifyAsync(
                user.Id,
                "Welcome to JobHub",
                "Your account was created. Please confirm your email to sign in.");

            var adminIds = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            await _notifications.NotifyManyAsync(
                adminIds.Select(a => a.Id),
                "New user registered",
                $"{user.Email} registered as {model.Role}.");

            // No real SMTP in this course project: return the link so the UI can show it.
            return Ok(new
            {
                Message = "User created successfully! Please confirm your email.",
                UserId = user.Id,
                ConfirmationUrl = confirmationUrl,
                EmailToken = ExposeEmailTokens() ? encodedToken : null
            });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId!);
            if (user == null)
            {
                return BadRequest(new { Message = "Invalid confirmation link." });
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token!));
            }
            catch
            {
                return BadRequest(new { Message = "Invalid confirmation token." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = errors });
            }

            await _notifications.NotifyAsync(
                user.Id,
                "Email confirmed",
                "Your email is confirmed. You can sign in now.");

            return Ok(new { Message = "Email confirmed successfully. You can sign in now." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email!);

            // Always return the same message so we do not reveal whether the email exists.
            const string genericMessage = "If an account with that email exists, a reset link is available.";

            if (user == null)
            {
                return Ok(new { Message = genericMessage });
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var frontendBase = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var resetUrl =
                $"{frontendBase}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

            return Ok(new
            {
                Message = genericMessage,
                ResetUrl = ExposeEmailTokens() ? resetUrl : null,
                EmailToken = ExposeEmailTokens() ? encodedToken : null
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null)
            {
                return BadRequest(new { Message = "Invalid password reset request." });
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token!));
            }
            catch
            {
                return BadRequest(new { Message = "Invalid reset token." });
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword!);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = errors });
            }

            await _notifications.NotifyAsync(
                user.Id,
                "Password changed",
                "Your password was reset successfully. If this was not you, contact an administrator.");

            return Ok(new { Message = "Password reset successfully. You can sign in now." });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new MeResponse
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            });
        }

        private bool ExposeEmailTokens() =>
            _environment.IsDevelopment() || _environment.IsEnvironment("Testing");

        private JwtSecurityToken GenerateJwtToken(ApplicationUser user, IList<string> userRoles)
        {
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName!),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                authClaims.Add(new Claim("role", userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));

            return new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));
        }
    }
}
