using MediatR;

namespace Profile.API.Features.CandidateProfiles.Commands.CreateCandidate
{
    public class CreateCandidateProfileCommand : IRequest<Guid>
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Projects { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public string CvUrl { get; set; } = string.Empty;
        public string? GithubUrl { get; set; }
        public string? GitlabUrl { get; set; }
        public string? LinkedInUrl { get; set; }
    }
}
