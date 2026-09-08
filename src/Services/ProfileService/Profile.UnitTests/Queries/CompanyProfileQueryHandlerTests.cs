using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Queries
{
    public class CompanyProfileQueryHandlerTests
    {
        [Fact]
        public async Task GetHandle_ExistingUser_ReturnsMappedProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CompanyProfile { UserId = "company-1", CompanyName = "Company", ContactEmail = "contact@company.example" };
            context.CompanyProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new GetCompanyProfileQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCompanyProfileQueryHandler>.Instance);

            var result = await handler.Handle(new GetCompanyProfileQuery(profile.UserId), CancellationToken.None);

            result.Should().NotBeNull();
            result!.CompanyName.Should().Be(profile.CompanyName);
        }

        [Fact]
        public async Task GetHandle_MissingUser_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new GetCompanyProfileQueryHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<GetCompanyProfileQueryHandler>.Instance);

            var result = await handler.Handle(new GetCompanyProfileQuery("missing"), CancellationToken.None);

            result.Should().BeNull();
        }
    }
}
