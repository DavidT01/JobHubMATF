using MediatR;

namespace Profile.API.Features.CandidateProfiles.Commands.UploadCv
{
    public class UploadCandidateCvCommand : IRequest<string?>
    {
        public Guid Id { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
