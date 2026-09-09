using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Commands.CreateCandidate
{
    public class CreateCandidateProfileCommand : IRequest<Guid>
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
