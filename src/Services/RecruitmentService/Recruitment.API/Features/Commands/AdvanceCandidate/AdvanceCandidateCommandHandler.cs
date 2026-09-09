using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Exceptions;
using Recruitment.API.Enums;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.AdvanceCandidate
{
    public class AdvanceCandidateCommandHandler(RecruitmentContext context, IMapper mapper, IProfileServiceClient profileServiceClient, ILogger<AdvanceCandidateCommandHandler> logger)
        : IRequestHandler<AdvanceCandidateCommand, CandidateProgressDto>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IProfileServiceClient _profileServiceClient = profileServiceClient;

        public async Task<CandidateProgressDto> Handle(AdvanceCandidateCommand request, CancellationToken cancellationToken)
        {
            var process = await _context.Processes
                .Include(p => p.Rounds)
                .FirstOrDefaultAsync(p => p.Id == request.RecruitmentProcessId, cancellationToken);
            if (process is null)
            {
                logger.LogWarning("Recruitment process {RecruitmentProcessId} not found.", request.RecruitmentProcessId);
                throw new RecruitmentValidationException($"Recruitment process {request.RecruitmentProcessId} not found");
            }

            if (!await _profileServiceClient.ValidateCandidateProfileAsync(request.CandidateProfileId, cancellationToken))
            {
                logger.LogWarning("Candidate profile {CandidateProfileId} not found while advancing candidate.", request.CandidateProfileId);
                throw new RecruitmentValidationException($"Candidate profile {request.CandidateProfileId} not found");
            }

            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.CandidateProfileId == request.CandidateProfileId && p.RecruitmentProcessId == request.RecruitmentProcessId, cancellationToken);
            
            if (progress == null)
            {
                progress = _mapper.Map<Entities.CandidateProgress>(request);
                progress.CurrentSelectionRoundId = process.Rounds.OrderBy(r => r.Index).FirstOrDefault()?.Id;
                progress.Status = CandidateProgressStatus.InProgress;
                _context.Progresses.Add(progress);
            }
            else
            {
                if (progress.Status != CandidateProgressStatus.InProgress)
                {
                     throw new RecruitmentValidationException($"Candidate process is not InProgress. Current status: {progress.Status}");
                }

                var currentRound = process.Rounds.FirstOrDefault(r => r.Id == progress.CurrentSelectionRoundId);
                var nextRound = process.Rounds
                    .Where(r => r.Index > (currentRound?.Index ?? -1))
                    .OrderBy(r => r.Index)
                    .FirstOrDefault();

                if (nextRound != null)
                {
                    progress.CurrentSelectionRoundId = nextRound.Id;
                    progress.ModifiedAt = DateTime.UtcNow;
                    _context.Progresses.Update(progress);
                }
                else
                {
                    progress.Status = CandidateProgressStatus.Completed;
                    progress.ModifiedAt = DateTime.UtcNow;
                    _context.Progresses.Update(progress);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully advanced candidate {CandidateProfileId} in process {RecruitmentProcessId} to status {Status}", request.CandidateProfileId, request.RecruitmentProcessId, progress.Status);
            return _mapper.Map<CandidateProgressDto>(progress);
        }
    }
}
