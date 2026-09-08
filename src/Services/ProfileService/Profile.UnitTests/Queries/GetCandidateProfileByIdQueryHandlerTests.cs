using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfileById;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Queries
{
    public class GetCandidateProfileByIdQueryHandlerTests
    {
        [Fact]
        public async Task GetHandle_ExistingProfile_ReturnsMappedProfile()
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
            var handler = new GetCandidateProfileByIdQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCandidateProfileByIdQueryHandler>.Instance);

            var result = await handler.Handle(new GetCandidateProfileByIdQuery(profile.Id), CancellationToken.None);

            result.Should().NotBeNull();
            result!.FirstName.Should().Be(profile.FirstName);
            result.LastName.Should().Be(profile.LastName);
        }

        [Fact]
        public async Task GetHandle_MissingProfile_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new GetCandidateProfileByIdQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCandidateProfileByIdQueryHandler>.Instance);

            var result = await handler.Handle(new GetCandidateProfileByIdQuery(Guid.NewGuid()), CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
