using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using JobHub.Grpc.Contracts.Catalog;
using JobHub.Grpc.Contracts.Profile;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class ApplicationGrpcContractTests
{
    [Fact]
    public void Job_without_expiration_preserves_absence()
    {
        var message = new ApplicationJobResponse
        {
            JobId = "507f1f77bcf86cd799439011",
            CompanyId = Guid.NewGuid().ToString(),
            IsActive = false
        };

        var decoded = ApplicationJobResponse.Parser.ParseFrom(message.ToByteArray());

        Assert.Equal(message.JobId, decoded.JobId);
        Assert.Equal(message.CompanyId, decoded.CompanyId);
        Assert.False(decoded.IsActive);
        Assert.Null(decoded.ExpirationDate);
    }

    [Fact]
    public void Job_expiration_preserves_utc_instant()
    {
        var expiry = new DateTimeOffset(2026, 9, 11, 12, 30, 0, TimeSpan.FromHours(2));
        var message = new ApplicationJobResponse
        {
            IsActive = true,
            ExpirationDate = Timestamp.FromDateTimeOffset(expiry)
        };

        var decoded = ApplicationJobResponse.Parser.ParseFrom(message.ToByteArray());

        Assert.True(decoded.IsActive);
        Assert.Equal(expiry.ToUniversalTime(), decoded.ExpirationDate.ToDateTimeOffset());
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://profiles.example/cv/current.pdf")]
    public void Candidate_reference_preserves_distinct_identity_and_profile_ids(string cvUrl)
    {
        var message = new ApplicationCandidateProfileResponse
        {
            ProfileId = Guid.NewGuid().ToString(),
            UserId = "identity-user-123",
            CvUrl = cvUrl
        };

        var decoded = ApplicationCandidateProfileResponse.Parser.ParseFrom(message.ToByteArray());

        Assert.Equal(message.ProfileId, decoded.ProfileId);
        Assert.Equal(message.UserId, decoded.UserId);
        Assert.Equal(cvUrl, decoded.CvUrl);
    }

    [Fact]
    public void Company_reference_preserves_profile_id_separately_from_owner()
    {
        var message = new ApplicationCompanyProfileResponse
        {
            ProfileId = Guid.NewGuid().ToString(),
            UserId = "employer-123"
        };

        var decoded = ApplicationCompanyProfileResponse.Parser.ParseFrom(message.ToByteArray());

        Assert.Equal(message.ProfileId, decoded.ProfileId);
        Assert.Equal(message.UserId, decoded.UserId);
    }
}
