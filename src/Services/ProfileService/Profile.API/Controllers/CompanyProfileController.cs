using MediatR;
using Microsoft.AspNetCore.Mvc;
using Profile.API.DTOs;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;
using Profile.API.Features.CompanyProfiles.Commands.DeleteCompany;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;
using Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile;

namespace Profile.API.Controllers
{
    [ApiController]
    [Route("api/company-profiles")]
    public class CompanyProfileController(IMediator mediator, ILogger<CompanyProfileController> logger) : ControllerBase
    {
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCompanyProfile(string userId)
        {
            logger.LogInformation("Received GetCompanyProfile request for userId: {UserId}", userId);
            var result = await mediator.Send(new GetCompanyProfileQuery(userId));
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCompanyProfile([FromBody] CreateCompanyProfileCommand command)
        {
            logger.LogInformation("Received CreateCompanyProfile request for userId: {UserId}", command.UserId);
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetCompanyProfile), new { userId = command.UserId }, id);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCompanyProfile(Guid id, [FromBody] UpdateCompanyProfileCommand command)
        {
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
            logger.LogInformation("Received DeleteCompanyProfile request for Id: {Id}", id);
            var result = await mediator.Send(new DeleteCompanyProfileCommand(id));
            return result ? NoContent() : NotFound();
        }
    }
}
