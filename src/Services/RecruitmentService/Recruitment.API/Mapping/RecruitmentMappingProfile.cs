using AutoMapper;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;
using Recruitment.API.Features.Commands.ScheduleInterview;
using Recruitment.API.Features.Commands.EvaluateCandidate;
using Recruitment.API.Features.Commands.AdvanceCandidate;
using Recruitment.API.Features.Commands.UpdateInterviewSchedule;

namespace Recruitment.API.Mapping
{
    public class RecruitmentMappingProfile : AutoMapper.Profile
    {
        public RecruitmentMappingProfile()
        {
            CreateMap<SelectionRound, SelectionRoundDto>()
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.Index));
            CreateMap<RecruitmentProcess, RecruitmentProcessDto>();
            CreateMap<CreateRecruitmentProcessCommand, RecruitmentProcess>();
            CreateMap<SelectionRoundDto, SelectionRound>().ForMember(dest => dest.Index, opt => opt.MapFrom(src => src.OrderIndex));
            CreateMap<SelectionRoundInsertDto, SelectionRound>().ForMember(dest => dest.Index, opt => opt.MapFrom(src => src.OrderIndex));

            CreateMap<ScheduleInterviewCommand, InterviewSchedule>()
                .ForMember(dest => dest.AdditionalAttendeeEmails, opt => opt.MapFrom(src => src.AttendeeEmails));
            CreateMap<UpdateInterviewScheduleCommand, InterviewSchedule>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SelectionRoundId, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.GoogleMeetUrl, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore());
            CreateMap<InterviewSchedule, InterviewScheduleDto>();

            CreateMap<CandidateEvaluation, CandidateEvaluationDto>();
            CreateMap<EvaluateCandidateCommand, CandidateEvaluation>();

            CreateMap<CandidateProgress, CandidateProgressDto>();
            CreateMap<AdvanceCandidateCommand, CandidateProgress>();
        }
    }
}
