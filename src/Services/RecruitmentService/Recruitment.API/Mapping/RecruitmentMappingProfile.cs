using AutoMapper;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;

namespace Recruitment.API.Mapping
{
    public class RecruitmentMappingProfile : AutoMapper.Profile
    {
        public RecruitmentMappingProfile()
        {
            CreateMap<SelectionRound, SelectionRoundDto>();
            CreateMap<RecruitmentProcess, RecruitmentProcessDto>();
            CreateMap<CreateRecruitmentProcessCommand, RecruitmentProcess>();
        }
    }
}
