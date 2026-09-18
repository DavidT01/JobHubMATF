using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Profile.API.DTOs;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.DeleteCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UploadCv;
using Profile.API.Features.CandidateProfiles.Commands.UploadPicture;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile;
using Profile.API.Infrastructure;

namespace Profile.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/candidate-profiles")]
    public class CandidateProfileController(
        IMediator mediator,
        ILogger<CandidateProfileController> logger,
        IProfileAuthorization authorization) : ControllerBase
    {
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(CandidateProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCandidateProfile(string userId)
        {
            await authorization.EnsureCanReadCandidateProfileAsync(userId, HttpContext.RequestAborted);
            logger.LogInformation("Received GetCandidateProfile request for userId: {UserId}", userId);
            var result = await mediator.Send(new GetCandidateProfileQuery(userId));
            return result != null ? Ok(result) : NotFound();
        }

        [Authorize(Roles = "Candidate,Admin")]
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCandidateProfile([FromBody] CreateCandidateProfileCommand command)
        {
            command.UserId = authorization.GetCurrentUserId();
            logger.LogInformation("Received CreateCandidateProfile request for userId: {UserId}", command.UserId);
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetCandidateProfile), new { userId = command.UserId }, id);
        }

        [HttpPost("{id}/cv")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(typeof(UrlResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadCv(Guid id, [FromForm] IFormFile file)
        {
            await authorization.EnsureCandidateProfileAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Received CV upload request for candidate Id: {Id}", id);
            var url = await mediator.Send(new UploadCandidateCvCommand { Id = id, File = file });
            return url != null ? Ok(new UrlResponseDto(url)) : NotFound();
        }

        [HttpPost("{id}/picture")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(typeof(UrlResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadPicture(Guid id, [FromForm] IFormFile file)
        {
            await authorization.EnsureCandidateProfileAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Received picture upload request for candidate Id: {Id}", id);
            var url = await mediator.Send(new UploadCandidatePictureCommand { Id = id, File = file });
            return url != null ? Ok(new UrlResponseDto(url)) : NotFound();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCandidateProfile(Guid id, [FromBody] UpdateCandidateProfileCommand command)
        {
            await authorization.EnsureCandidateProfileAsync(id, HttpContext.RequestAborted);
            command.UserId = authorization.GetCurrentUserId();
            logger.LogInformation("Received UpdateCandidateProfile request for userId: {UserId}", command.UserId);
            command.Id = id;
            var result = await mediator.Send(command);
            return result ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCandidateProfile(Guid id)
        {
            await authorization.EnsureCandidateProfileAsync(id, HttpContext.RequestAborted);
            logger.LogInformation("Received DeleteCandidateProfile request for Id: {Id}", id);
            var result = await mediator.Send(new DeleteCandidateProfileCommand(id));
            return result ? NoContent() : NotFound();
        }
    }
}
