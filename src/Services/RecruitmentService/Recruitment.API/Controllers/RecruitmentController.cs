using MediatR;
using Microsoft.AspNetCore.Mvc;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;
using Recruitment.API.Features.Queries.GetProcessByJobId;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Route("api/recruitment-processes")]
    public class RecruitmentController(IMediator mediator, ILogger<RecruitmentController> logger) : ControllerBase
    {
        [HttpGet("job/{jobId}")]
        [ProducesResponseType(typeof(RecruitmentProcessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProcessByJobId(Guid jobId)
        {
            logger.LogInformation("Received GetProcessByJobId request for JobId {JobId}", jobId);
            var result = await mediator.Send(new GetProcessByJobIdQuery(jobId));
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProcess([FromBody] CreateRecruitmentProcessCommand command)
        {
            logger.LogInformation("Received CreateProcess request for JobId {JobId}", command.JobId);
            var processId = await mediator.Send(command);
            return CreatedAtAction(nameof(GetProcessByJobId), new { jobId = command.JobId }, new { Id = processId });

        }

        [HttpPut("{processId}/rounds")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRounds(Guid processId, [FromBody] List<SelectionRoundInsertDto> rounds)
        {
            logger.LogInformation("Received UpdateRounds request for ProcessId {ProcessId}", processId);
            var success = await mediator.Send(new UpdateSelectionRoundsCommand { ProcessId = processId, Rounds = rounds });
            return success ? NoContent() : NotFound("Process not found");
        }
    }
}
