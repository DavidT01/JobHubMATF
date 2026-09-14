using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Profile.API.DTOs;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;
using Profile.API.Features.CompanyProfiles.Commands.DeleteCompany;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;
using Profile.API.Features.CompanyProfiles.Commands.UploadLogo;
using Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile;
using Profile.API.Infrastructure;

namespace Profile.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/company-profiles")]
    public class CompanyProfileController(
        IMediator mediator,
        ILogger<CompanyProfileController> logger,
        IProfileAuthorization authorization) : ControllerBase
    {
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCompanyProfile(string userId)
        {
            await authorization.EnsureUserAsync(userId, HttpContext.RequestAborted);
            logger.LogInformation("Received GetCompanyProfile request for userId: {UserId}", userId);
            var result = await mediator.Send(new GetCompanyProfileQuery(userId));
            return result != null ? Ok(result) : NotFound();
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCompanyProfile([FromBody] CreateCompanyProfileCommand command)
        {
            command.UserId = authorization.GetCurrentUserId();
            logger.LogInformation("Received CreateCompanyProfile request for userId: {UserId}", command.UserId);
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetCompanyProfile), new { userId = command.UserId }, id);
        }

        [HttpPost("{id}/logo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(typeof(UrlResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadLogo(Guid id, [FromForm] IFormFile file)
        {
            await authorization.EnsureCompanyProfileAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Received Logo upload request for company Id: {Id}", id);
            var url = await mediator.Send(new UploadCompanyLogoCommand { Id = id, File = file });
            return url != null ? Ok(new UrlResponseDto(url)) : NotFound();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCompanyProfile(Guid id, [FromBody] UpdateCompanyProfileCommand command)
        {
            await authorization.EnsureCompanyProfileAsync(id, HttpContext.RequestAborted);
            command.UserId = authorization.GetCurrentUserId();
            logger.LogInformation("Received UpdateCompanyProfile request for userId: {UserId}", command.UserId);
            command.Id = id;
            var result = await mediator.Send(command);
            return result ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCompanyProfile(Guid id)
        {
            await authorization.EnsureCompanyProfileAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Received DeleteCompanyProfile request for Id: {Id}", id);
            var result = await mediator.Send(new DeleteCompanyProfileCommand(id));
            return result ? NoContent() : NotFound();
        }
    }
}
