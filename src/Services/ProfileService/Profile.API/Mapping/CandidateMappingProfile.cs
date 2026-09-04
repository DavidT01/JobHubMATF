using Google.Protobuf.WellKnownTypes;
using JobHub.Grpc.Contracts.Profile;
using Profile.API.DTOs;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;
using ProfileEducation = Profile.API.Entities.Education;
using ProfileExperience = Profile.API.Entities.Experience;
using ProfileLanguage = Profile.API.Entities.Language;
using ProfileProject = Profile.API.Entities.Project;
using GrpcEducation = JobHub.Grpc.Contracts.Profile.Education;
using GrpcExperience = JobHub.Grpc.Contracts.Profile.Experience;
using GrpcLanguage = JobHub.Grpc.Contracts.Profile.Language;
using GrpcProject = JobHub.Grpc.Contracts.Profile.Project;

namespace Profile.API.Mapping
{
    public class CandidateMappingProfile : AutoMapper.Profile
    {
        public CandidateMappingProfile()
        {
            CreateMap<DateTime, Timestamp>().ConvertUsing(value =>
                Timestamp.FromDateTime(value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime()));

            CreateMap<CandidateProfile, CandidateProfileDto>().ReverseMap();
            CreateMap<ProfileEducation, EducationDto>().ReverseMap();
            CreateMap<ProfileExperience, ExperienceDto>().ReverseMap();
            CreateMap<ProfileProject, ProjectDto>().ReverseMap();
            CreateMap<ProfileLanguage, LanguageDto>().ReverseMap();

            CreateMap<EducationDto, GrpcEducation>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.Degree, opt => opt.Condition(src => src.Degree is not null));

            CreateMap<ExperienceDto, GrpcExperience>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

            CreateMap<ProjectDto, GrpcProject>()
                .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description is not null))
                .ForMember(dest => dest.RepositoryUrl, opt => opt.Condition(src => src.RepositoryUrl is not null));

            CreateMap<LanguageDto, GrpcLanguage>()
                .ForMember(dest => dest.Level, opt => opt.Condition(src => src.Level is not null));

            CreateMap<CandidateProfileDto, CandidateProfileResponse>()
                .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.PictureUrl, opt => opt.Condition(src => src.PictureUrl is not null))
                .ForMember(dest => dest.GithubUrl, opt => opt.Condition(src => src.GithubUrl is not null))
                .ForMember(dest => dest.GitlabUrl, opt => opt.Condition(src => src.GitlabUrl is not null))
                .ForMember(dest => dest.LinkedInUrl, opt => opt.Condition(src => src.LinkedInUrl is not null));

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
