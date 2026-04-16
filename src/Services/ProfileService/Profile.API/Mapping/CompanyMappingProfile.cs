using Profile.API.DTO;
using Profile.API.DTOs;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;

namespace Profile.API.Mapping
{
    public class CompanyMappingProfile : AutoMapper.Profile
    {
        public CompanyMappingProfile()
        {
            CreateMap<CompanyProfile, CompanyProfileDto>();
        }
    }
}
