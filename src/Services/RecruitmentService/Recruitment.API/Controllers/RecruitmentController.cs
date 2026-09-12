using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;
using Recruitment.API.Features.Queries.GetProcessByJobId;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RecruitmentController(IMediator mediator, ILogger<RecruitmentController> logger, IRecruitmentAuthorization authorization) : ControllerBase
    {
        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpGet("job/{jobId}")]
        [ProducesResponseType(typeof(RecruitmentProcessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProcessByJobId(string jobId)
        {
            await authorization.EnsureProcessOwnerByJobIdAsync(jobId, HttpContext.RequestAborted);
            logger.LogInformation("Received GetProcessByJobId request for JobId {JobId}", jobId);
            var result = await mediator.Send(new GetProcessByJobIdQuery(jobId));
            return result != null ? Ok(result) : NotFound();
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProcess([FromBody] CreateRecruitmentProcessCommand command)
        {
            await authorization.EnsureCompanyAsync(command.CompanyId, HttpContext.RequestAborted);
            logger.LogInformation("Received CreateProcess request for JobId {JobId}", command.JobId);
            var processId = await mediator.Send(command);
            return CreatedAtAction(nameof(GetProcessByJobId), new { jobId = command.JobId }, new { Id = processId });

        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPut("{processId}/rounds")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRounds(Guid processId, [FromBody] List<SelectionRoundInsertDto> rounds)
        {
            await authorization.EnsureProcessOwnerAsync(processId, HttpContext.RequestAborted);
            logger.LogInformation("Received UpdateRounds request for ProcessId {ProcessId}", processId);
            var success = await mediator.Send(new UpdateSelectionRoundsCommand { ProcessId = processId, Rounds = rounds });
            return success ? NoContent() : NotFound("Process not found");
        }
    }
}
