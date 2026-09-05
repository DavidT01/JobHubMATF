using MediatR;
using Microsoft.AspNetCore.Mvc;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Queries.GetInterviewSchedule;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("schedule")]
        [ProducesResponseType(typeof(InterviewScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("{candidateProfileId}/round/{selectionRoundId}")]
        [ProducesResponseType(typeof(InterviewScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInterviewSchedule(Guid candidateProfileId, Guid selectionRoundId)
        {
            var result = await _mediator.Send(new GetInterviewScheduleQuery(candidateProfileId, selectionRoundId));
            return result is null ? NotFound() : Ok(result);
        }
    }
}
