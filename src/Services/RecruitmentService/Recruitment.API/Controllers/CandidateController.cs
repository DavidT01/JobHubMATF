using MediatR;
using Microsoft.AspNetCore.Mvc;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.AdvanceCandidate;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.API.Features.Commands.UpdateCandidateStatus;
using Recruitment.API.Features.Queries.GetCandidatesInRound;
using Recruitment.API.Features.Queries.GetCandidateEvaluations;
using Recruitment.API.Features.Queries.GetCandidateProgress;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidateController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(CandidateEvaluationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EvaluateCandidate([FromBody] EvaluateCandidateCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("advance")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdvanceCandidate([FromBody] AdvanceCandidateCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reject")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectCandidate(Guid candidateProfileId, Guid recruitmentProcessId)
        {
            var result = await _mediator.Send(new UpdateCandidateStatusCommand
            {
                CandidateProfileId = candidateProfileId,
                RecruitmentProcessId = recruitmentProcessId,
                Status = Enums.CandidateProgressStatus.Rejected
            });
            return Ok(result);
        }

        [HttpPost("hire")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HireCandidate(Guid candidateProfileId, Guid recruitmentProcessId)
        {
            var result = await _mediator.Send(new UpdateCandidateStatusCommand
            {
                CandidateProfileId = candidateProfileId,
                RecruitmentProcessId = recruitmentProcessId,
                Status = Enums.CandidateProgressStatus.Hired
            });
            return Ok(result);
        }

        [HttpGet("round/{selectionRoundId}")]
        [ProducesResponseType(typeof(List<CandidateProgressDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCandidatesInRound(Guid selectionRoundId)
        {
            var result = await _mediator.Send(new GetCandidatesInRoundQuery(selectionRoundId));
            return Ok(result);
        }

        [HttpGet("{candidateId}/evaluations")]
        [ProducesResponseType(typeof(List<CandidateEvaluationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvaluations(Guid candidateId)
        {
            var result = await _mediator.Send(new GetCandidateEvaluationsQuery(candidateId));
            return Ok(result);
        }

        [HttpGet("{candidateId}/process/{processId}/progress")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProgress(Guid candidateId, Guid processId)
        {
            var result = await _mediator.Send(new GetCandidateProgressQuery(candidateId, processId));
            return Ok(result);
        }
    }
}