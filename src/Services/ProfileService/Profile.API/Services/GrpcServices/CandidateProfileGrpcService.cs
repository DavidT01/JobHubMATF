using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;
using AutoMapper;
using MediatR;
using Profile.API.DTOs;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfileById;

namespace Profile.API.Services.GrpcServices;

public sealed class CandidateProfileGrpcService(IMediator mediator, IMapper mapper, ILogger<CandidateProfileGrpcService> logger)
    : JobHub.Grpc.Contracts.Profile.CandidateProfileGrpcService.CandidateProfileGrpcServiceBase
{
    public override async Task<CandidateContactResponse> GetCandidateContact(GetCandidateContactRequest request, ServerCallContext context)
    {
        var profile = await GetProfileAsync(request.ProfileId, context.CancellationToken);

        return new CandidateContactResponse
        {
            ProfileId = profile.Id.ToString(),
            Email = profile.Email
        };
    }

    public override async Task<ValidateCandidateProfileResponse> ValidateCandidateProfile(
        ValidateCandidateProfileRequest request,
        ServerCallContext context)
    {
        var profileId = ParseProfileId(request.ProfileId);
        var profile = await mediator.Send(new GetCandidateProfileByIdQuery(profileId), context.CancellationToken);

        return new ValidateCandidateProfileResponse
        {
            ProfileId = profileId.ToString(),
            Exists = profile is not null
        };
    }

    public override async Task<CandidateProfileResponse> GetCandidateProfile(GetCandidateProfileRequest request, ServerCallContext context)
    {
        var profile = await GetProfileAsync(request.ProfileId, context.CancellationToken);
        return mapper.Map<CandidateProfileResponse>(profile);
    }

    private async Task<CandidateProfileDto> GetProfileAsync(string profileId, CancellationToken cancellationToken)
    {
        var parsedProfileId = ParseProfileId(profileId);
        var profile = await mediator.Send(new GetCandidateProfileByIdQuery(parsedProfileId), cancellationToken);

        if (profile is null)
        {
            logger.LogInformation("Candidate profile {ProfileId} was not found through gRPC.", parsedProfileId);
            throw new RpcException(new Status(StatusCode.NotFound, $"Candidate profile '{parsedProfileId}' was not found."));
        }

        return profile;
    }

    private static Guid ParseProfileId(string profileId)
    {
        if (!Guid.TryParse(profileId, out var parsedProfileId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "profile_id must be a valid UUID."));
        }
        return parsedProfileId;
    }
}
