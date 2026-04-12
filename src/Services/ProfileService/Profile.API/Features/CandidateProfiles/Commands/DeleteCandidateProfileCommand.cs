using MediatR;

namespace Profile.API.Features.CandidateProfiles.Commands
{
    public record DeleteCandidateProfileCommand(Guid Id) : IRequest<bool>;
}
