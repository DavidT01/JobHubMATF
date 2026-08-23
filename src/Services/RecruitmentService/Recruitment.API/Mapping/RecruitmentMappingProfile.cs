using AutoMapper;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;
using Recruitment.API.Features.Commands.ScheduleInterview;

namespace Recruitment.API.Mapping
{
    public class RecruitmentMappingProfile : AutoMapper.Profile
    {
        public RecruitmentMappingProfile()
        {
            CreateMap<SelectionRound, SelectionRoundDto>();
            CreateMap<RecruitmentProcess, RecruitmentProcessDto>();
            CreateMap<CreateRecruitmentProcessCommand, RecruitmentProcess>();
            CreateMap<SelectionRoundDto, SelectionRound>().ForMember(dest => dest.Index, opt => opt.MapFrom(src => src.OrderIndex));

            CreateMap<ScheduleInterviewCommand, InterviewSchedule>();
            CreateMap<InterviewSchedule, InterviewScheduleDto>();
        }
    }
}
