using Profile.API.DTOs;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;

namespace Profile.API.Mapping
{
    public class CandidateMappingProfile : AutoMapper.Profile
    {
        public CandidateMappingProfile()
        {
            CreateMap<CandidateProfile, CandidateProfileDto>();

            CreateMap<CreateCandidateProfileCommand, CandidateProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());

            CreateMap<UpdateCandidateProfileCommand, CandidateProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());
        }
    }
}
