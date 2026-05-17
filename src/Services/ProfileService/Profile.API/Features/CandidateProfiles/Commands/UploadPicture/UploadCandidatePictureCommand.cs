using MediatR;

namespace Profile.API.Features.CandidateProfiles.Commands.UploadPicture
{
    public class UploadCandidatePictureCommand : IRequest<string?>
    {
        public Guid Id { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
