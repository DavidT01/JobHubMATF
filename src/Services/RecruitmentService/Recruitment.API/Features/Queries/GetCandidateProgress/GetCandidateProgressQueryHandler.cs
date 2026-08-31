using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Exceptions;

namespace Recruitment.API.Features.Queries.GetCandidateProgress
{
    public class GetCandidateProgressQueryHandler(RecruitmentContext context, IMapper mapper) : IRequestHandler<GetCandidateProgressQuery, CandidateProgressDto>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMapper _mapper = mapper;

        public async Task<CandidateProgressDto> Handle(GetCandidateProgressQuery request, CancellationToken cancellationToken)
        {
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.CandidateProfileId == request.CandidateProfileId && p.RecruitmentProcessId == request.ProcessId, cancellationToken)
                ?? throw new RecruitmentValidationException("Progress for the given candidate and process not found.");

            return _mapper.Map<CandidateProgressDto>(progress);
        }
    }
}
