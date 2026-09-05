using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.EvaluateCandidate
{
    public class EvaluateCandidateCommandHandler(RecruitmentContext context, IMapper mapper, IProfileServiceClient profileServiceClient)
        : IRequestHandler<EvaluateCandidateCommand, CandidateEvaluationDto>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IProfileServiceClient _profileServiceClient = profileServiceClient;

        public async Task<CandidateEvaluationDto> Handle(EvaluateCandidateCommand request, CancellationToken cancellationToken)
        {
            var round = await _context.Rounds.FindAsync([request.SelectionRoundId], cancellationToken) 
                ?? throw new RecruitmentValidationException($"Selection round {request.SelectionRoundId} not found");

            if (!await _profileServiceClient.ValidateCandidateProfileAsync(request.CandidateProfileId, cancellationToken))
            {
                throw new RecruitmentValidationException($"Candidate profile {request.CandidateProfileId} not found");
            }

            var existingEvaluation = await _context.Evaluations
                .FirstOrDefaultAsync(e => e.CandidateProfileId == request.CandidateProfileId && e.SelectionRoundId == request.SelectionRoundId, cancellationToken);
            
            CandidateEvaluation evaluation;

            if (existingEvaluation != null)
            {
                _mapper.Map(request, existingEvaluation);
                existingEvaluation.ModifiedAt = DateTime.UtcNow;
                evaluation = existingEvaluation;
                _context.Evaluations.Update(existingEvaluation);
            }
            else
            {
                evaluation = _mapper.Map<CandidateEvaluation>(request);
                _context.Evaluations.Add(evaluation);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return _mapper.Map<CandidateEvaluationDto>(evaluation);
        }
    }
}
