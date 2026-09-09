using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Queries
{
    public class CandidateProfileQueryHandlerTests
    {
        [Fact]
        public async Task GetHandle_ExistingUser_ReturnsMappedProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile
            {
                UserId = "candidate-1",
                FirstName = "Ana",
                LastName = "Peric",
                Email = "ana@example.com"
            };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new GetCandidateProfileQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCandidateProfileQueryHandler>.Instance);

            var result = await handler.Handle(new GetCandidateProfileQuery(profile.UserId), CancellationToken.None);

            result.Should().NotBeNull();
            result!.FirstName.Should().Be(profile.FirstName);
            result.LastName.Should().Be(profile.LastName);
        }

        [Fact]
        public async Task GetHandle_MissingUser_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new GetCandidateProfileQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCandidateProfileQueryHandler>.Instance);

            var result = await handler.Handle(new GetCandidateProfileQuery("missing"), CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
