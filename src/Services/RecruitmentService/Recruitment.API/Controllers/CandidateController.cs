using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.AdvanceCandidate;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.API.Features.Commands.UpdateCandidateStatus;
using Recruitment.API.Features.Queries.GetCandidatesInRound;
using Recruitment.API.Features.Queries.GetCandidateEvaluations;
using Recruitment.API.Features.Queries.GetCandidateProgress;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CandidateController(IMediator mediator, ILogger<CandidateController> logger, IRecruitmentAuthorization authorization) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(CandidateEvaluationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EvaluateCandidate([FromBody] EvaluateCandidateCommand command)
        {
            await authorization.EnsureRoundOwnerAsync(command.SelectionRoundId, HttpContext.RequestAborted);
            logger.LogInformation("Received EvaluateCandidate request for candidate {CandidateProfileId} and round {SelectionRoundId}", command.CandidateProfileId, command.SelectionRoundId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost("advance")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdvanceCandidate([FromBody] AdvanceCandidateCommand command)
        {
            await authorization.EnsureProcessOwnerAsync(command.RecruitmentProcessId, HttpContext.RequestAborted);
            logger.LogInformation("Received AdvanceCandidate request for candidate {CandidateProfileId} and process {RecruitmentProcessId}", command.CandidateProfileId, command.RecruitmentProcessId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost("reject")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectCandidate(Guid candidateProfileId, Guid recruitmentProcessId)
        {
            await authorization.EnsureProcessOwnerAsync(recruitmentProcessId, HttpContext.RequestAborted);
            logger.LogInformation("Received RejectCandidate request for candidate {CandidateProfileId} and process {RecruitmentProcessId}", candidateProfileId, recruitmentProcessId);
            var result = await _mediator.Send(new UpdateCandidateStatusCommand
            {
                CandidateProfileId = candidateProfileId,
                RecruitmentProcessId = recruitmentProcessId,
                Status = Enums.CandidateProgressStatus.Rejected
            });
            return Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpPost("hire")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HireCandidate(Guid candidateProfileId, Guid recruitmentProcessId)
        {
            await authorization.EnsureProcessOwnerAsync(recruitmentProcessId, HttpContext.RequestAborted);
            logger.LogInformation("Received HireCandidate request for candidate {CandidateProfileId} and process {RecruitmentProcessId}", candidateProfileId, recruitmentProcessId);
            var result = await _mediator.Send(new UpdateCandidateStatusCommand
            {
                CandidateProfileId = candidateProfileId,
                RecruitmentProcessId = recruitmentProcessId,
                Status = Enums.CandidateProgressStatus.Hired
            });
            return Ok(result);
        }

        [Authorize(Policy = "EmployerOrAdmin")]
        [HttpGet("round/{selectionRoundId}")]
        [ProducesResponseType(typeof(List<CandidateProgressDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCandidatesInRound(Guid selectionRoundId)
        {
            await authorization.EnsureRoundOwnerAsync(selectionRoundId, HttpContext.RequestAborted);
            logger.LogInformation("Received GetCandidatesInRound request for round {SelectionRoundId}", selectionRoundId);
            var result = await _mediator.Send(new GetCandidatesInRoundQuery(selectionRoundId));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{candidateId}/evaluations")]
        [ProducesResponseType(typeof(List<CandidateEvaluationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvaluations(Guid candidateId)
        {
            await authorization.EnsureEvaluationOwnerAsync(candidateId, HttpContext.RequestAborted);
            logger.LogInformation("Received GetEvaluations request for candidate {CandidateProfileId}", candidateId);
            var result = await _mediator.Send(new GetCandidateEvaluationsQuery(candidateId));
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{candidateId}/process/{processId}/progress")]
        [ProducesResponseType(typeof(CandidateProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProgress(Guid candidateId, Guid processId)
        {
            if (User.IsInRole("Candidate"))
            {
                await authorization.EnsureCandidateOwnsProfileAsync(candidateId, HttpContext.RequestAborted);
            }
            else
            {
                await authorization.EnsureProcessOwnerAsync(processId, HttpContext.RequestAborted);
            }
            logger.LogInformation("Received GetProgress request for candidate {CandidateProfileId} and process {RecruitmentProcessId}", candidateId, processId);
            var result = await _mediator.Send(new GetCandidateProgressQuery(candidateId, processId));
            return Ok(result);
        }
    }
}