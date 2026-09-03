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
    [HttpPut("{applicationId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.Employer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ChangeStatus(
        Guid applicationId, [FromBody] ChangeApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeApplicationStatusCommand(applicationId, request.Status), cancellationToken);
        return NoContent();
    }

    [HttpGet("jobs/{jobId}")]
    [Authorize(Policy = AuthorizationPolicies.Employer)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PagedResult<EmployerApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResult<EmployerApplicationDto>>> GetForJob(
        string jobId, CancellationToken cancellationToken, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await sender.Send(new GetEmployerApplicationsQuery(jobId, pageNumber, pageSize), cancellationToken));
    }

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
