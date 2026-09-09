using Grpc.Core;
using MediatR;
using Recruitment.API.Enums;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Commands.ActivateCandidateProgress;
using RecruitmentContract = JobHub.Grpc.Contracts.Recruitment;

namespace Recruitment.API.Services.GrpcServices;

public sealed class RecruitmentGrpcService(IMediator mediator, ILogger<RecruitmentGrpcService> logger)
    : RecruitmentContract.RecruitmentGrpcService.RecruitmentGrpcServiceBase
{
    public override async Task<RecruitmentContract.CandidateProgressResponse> ActivateCandidateProgress(
        RecruitmentContract.ActivateCandidateProgressRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.CandidateProfileId, out var candidateProfileId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "candidate_profile_id must be a valid UUID."));
        }

        Guid? applicationId = null;
        if (request.HasApplicationId)
        {
            if (!Guid.TryParse(request.ApplicationId, out var parsedApplicationId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "application_id must be a valid UUID."));
            }

            applicationId = parsedApplicationId;
        }

        try
        {
            var progress = await mediator.Send(
                new ActivateCandidateProgressCommand(candidateProfileId, request.JobId, applicationId),
                context.CancellationToken);

            return new RecruitmentContract.CandidateProgressResponse
            {
                Id = progress.Id.ToString("D"),
                CandidateProfileId = progress.CandidateProfileId.ToString("D"),
                RecruitmentProcessId = progress.RecruitmentProcessId.ToString("D"),
                Status = MapStatus(progress.Status)
            }.WithCurrentSelectionRound(progress.CurrentSelectionRoundId);
        }
        catch (RecruitmentValidationException exception)
        {
            logger.LogWarning(exception, "Candidate progress activation failed through gRPC.");
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }

    private static RecruitmentContract.CandidateProgressStatus MapStatus(CandidateProgressStatus status) =>
        status switch
        {
            CandidateProgressStatus.InProgress => RecruitmentContract.CandidateProgressStatus.InProgress,
            CandidateProgressStatus.Completed => RecruitmentContract.CandidateProgressStatus.Completed,
            CandidateProgressStatus.Rejected => RecruitmentContract.CandidateProgressStatus.Rejected,
            CandidateProgressStatus.Hired => RecruitmentContract.CandidateProgressStatus.Hired,
            _ => RecruitmentContract.CandidateProgressStatus.Unspecified
        };
}

file static class CandidateProgressResponseExtensions
{
    public static RecruitmentContract.CandidateProgressResponse WithCurrentSelectionRound(
        this RecruitmentContract.CandidateProgressResponse response,
        Guid? currentSelectionRoundId)
    {
        if (currentSelectionRoundId.HasValue)
        {
            response.CurrentSelectionRoundId = currentSelectionRoundId.Value.ToString("D");
        }

        return response;
    }
}
