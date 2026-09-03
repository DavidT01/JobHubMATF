using ApplicationService.Application.DTOs;
using ApplicationService.Application.Commands;
using ApplicationService.Application.Queries;
using ApplicationService.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationService.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Candidate)]
    [ProducesResponseType(typeof(ApplicationListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApplicationListItemDto>> Submit(
        [FromBody] SubmitApplicationCommand command, CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await sender.Send(command, cancellationToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.Candidate)]
    [ProducesResponseType(typeof(PagedResult<ApplicationListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResult<ApplicationListItemDto>>> GetMyApplications(
        CancellationToken cancellationToken, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await sender.Send(new GetCandidateApplicationsQuery(pageNumber, pageSize), cancellationToken));
    }
}
