using Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            
            var userExists = await _userManager.FindByEmailAsync(model.Email!);
            if (userExists != null)
                return BadRequest(new { Message = "Korisnik sa ovim email-om već postoji!" });

            
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Greška pri kreiranju korisnika." });

           
            if (!await _roleManager.RoleExistsAsync(model.Role!))
                await _roleManager.CreateAsync(new IdentityRole(model.Role!));

            
            await _userManager.AddToRoleAsync(user, model.Role!);

            return Ok(new { Message = "Korisnik je uspešno kreiran!" });
        }
    }
}