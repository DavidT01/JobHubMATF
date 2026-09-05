using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Entities;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.ScheduleInterview
{
    public class ScheduleInterviewCommandHandler(RecruitmentContext context, IMeetingService meetingService,
        IProfileServiceClient profileServiceClient, IMapper mapper) : IRequestHandler<ScheduleInterviewCommand, InterviewScheduleDto>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMeetingService _meetingService = meetingService;
        private readonly IProfileServiceClient _profileServiceClient = profileServiceClient;
        private readonly IMapper _mapper = mapper;

        public async Task<InterviewScheduleDto> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
        {
            var round = await _context.Rounds.FindAsync([request.SelectionRoundId], cancellationToken) ??
                throw new RecruitmentValidationException($"Selection round {request.SelectionRoundId} not found");

            var hasConflict = await _context.InterviewSchedules.AnyAsync(schedule =>
                schedule.CandidateProfileId == request.CandidateProfileId
                && request.StartTime < schedule.EndTime
                && request.EndTime > schedule.StartTime,
                cancellationToken);
            if (hasConflict)
            {
                throw new RecruitmentValidationException("The candidate already has an interview during this time.");
            }

            var candidateContact = await _profileServiceClient.GetCandidateContactAsync(request.CandidateProfileId, cancellationToken);
            var attendeeEmails = new[] { candidateContact.Email }
                .Concat(request.AttendeeEmails)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var (eventId, url) = await _meetingService.ScheduleMeetingAsync(request.Title, request.Description, request.StartTime, request.EndTime, attendeeEmails);

            var schedule = _mapper.Map<InterviewSchedule>(request);
            schedule.EventId = eventId;
            schedule.GoogleMeetUrl = url;

            _context.InterviewSchedules.Add(schedule);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<InterviewScheduleDto>(schedule);
        }
    }
}
