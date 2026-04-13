using MediatR;

namespace Profile.API.Features.CandidateProfiles.Commands.DeleteCandidate
{
    public record DeleteCandidateProfileCommand(Guid Id) : IRequest<bool>;
}
