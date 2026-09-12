using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Queries.GetInterviewSchedule;
using Recruitment.API.Features.Commands.UpdateInterviewSchedule;
using Recruitment.API.Features.Commands.CancelInterviewSchedule;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InterviewController(IMediator mediator, ILogger<InterviewController> logger, IRecruitmentAuthorization authorization) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost("schedule")]
        [ProducesResponseType(typeof(InterviewScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewCommand command)
        {
            await authorization.EnsureRoundOwnerAsync(command.SelectionRoundId, HttpContext.RequestAborted);
            logger.LogInformation("Received ScheduleInterview request for candidate {CandidateProfileId} and round {SelectionRoundId}", command.CandidateProfileId, command.SelectionRoundId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("{candidateProfileId}/round/{selectionRoundId}")]
        [ProducesResponseType(typeof(InterviewScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInterviewSchedule(Guid candidateProfileId, Guid selectionRoundId)
        {
            if (User.IsInRole("Candidate"))
            {
                await authorization.EnsureCandidateOwnsProfileAsync(candidateProfileId, HttpContext.RequestAborted);
            }
            else
            {
                await authorization.EnsureRoundOwnerAsync(selectionRoundId, HttpContext.RequestAborted);
            }
            logger.LogInformation("Received GetInterviewSchedule request for candidate {CandidateProfileId} and round {SelectionRoundId}", candidateProfileId, selectionRoundId);
            var result = await _mediator.Send(new GetInterviewScheduleQuery(candidateProfileId, selectionRoundId));
            return result is null ? NotFound() : Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPut("{interviewScheduleId}")]
        [ProducesResponseType(typeof(InterviewScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateInterviewSchedule(Guid interviewScheduleId, [FromBody] UpdateInterviewScheduleCommand command)
        {
            await authorization.EnsureInterviewOwnerAsync(interviewScheduleId, HttpContext.RequestAborted);
            logger.LogInformation("Received UpdateInterviewSchedule request for interview {InterviewScheduleId}", interviewScheduleId);
            var result = await _mediator.Send(command with { InterviewScheduleId = interviewScheduleId });
            return Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpDelete("{interviewScheduleId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelInterviewSchedule(Guid interviewScheduleId)
        {
            await authorization.EnsureInterviewOwnerAsync(interviewScheduleId, HttpContext.RequestAborted);
            logger.LogInformation("Received CancelInterviewSchedule request for interview {InterviewScheduleId}", interviewScheduleId);
            await _mediator.Send(new CancelInterviewScheduleCommand(interviewScheduleId));
            return NoContent();
        }
    }
}
