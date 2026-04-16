using Profile.API.DTOs;
using Profile.API.Entities;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;

namespace Profile.API.Mapping
{
    public class CompanyMappingProfile : AutoMapper.Profile
    {
        public CompanyMappingProfile()
        {
            CreateMap<CompanyProfile, CompanyProfileDto>();

            CreateMap<CreateCompanyProfileCommand, CompanyProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());

            CreateMap<UpdateCompanyProfileCommand, CompanyProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());
        }
    }
}
