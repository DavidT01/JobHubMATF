using AutoMapper;
using MediatR;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.ScheduleInterview
{
    public class ScheduleInterviewCommandHandler(RecruitmentContext context, IMeetingService meetingService, IMapper mapper) : IRequestHandler<ScheduleInterviewCommand, InterviewScheduleDto>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMeetingService _meetingService = meetingService;
        private readonly IMapper _mapper = mapper;

        public async Task<InterviewScheduleDto> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
        {
            var round = await _context.Rounds.FindAsync([request.SelectionRoundId], cancellationToken) ?? throw new RecruitmentValidationException($"Selection round {request.SelectionRoundId} not found");
            var (eventId, url) = await _meetingService.ScheduleMeetingAsync(request.Title, request.Description, request.StartTime, request.EndTime, request.AttendeeEmails);

            var schedule = _mapper.Map<InterviewSchedule>(request);
            schedule.EventId = eventId;
            schedule.GoogleMeetUrl = url;

            _context.InterviewSchedules.Add(schedule);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<InterviewScheduleDto>(schedule);
        }
    }
}
