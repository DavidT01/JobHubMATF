using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;
using Profile.API.Features.CompanyProfiles.Commands.DeleteCompany;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Commands
{
    public class CompanyProfileCommandHandlerTests
    {
        [Fact]
        public async Task CreateHandle_ValidCommand_PersistsProfileAndReturnsId()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new CreateCompanyProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<CreateCompanyProfileCommandHandler>.Instance);
            var command = new CreateCompanyProfileCommand
            {
                UserId = "company-1",
                CompanyName = "Company",
                ContactEmail = "contact@company.example"
            };

            var id = await handler.Handle(command, CancellationToken.None);

            var profile = await context.CompanyProfiles.FindAsync(id);
            profile.Should().NotBeNull();
            profile.UserId.Should().Be(command.UserId);
            profile.CompanyName.Should().Be(command.CompanyName);
        }

        [Fact]
        public async Task UpdateHandle_ExistingUser_UpdatesProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CompanyProfile
            {
                UserId = "company-1",
                CompanyName = "Old name",
                ContactEmail = "old@example.com"
            };
            context.CompanyProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new UpdateCompanyProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<UpdateCompanyProfileCommandHandler>.Instance);

            var result = await handler.Handle(new UpdateCompanyProfileCommand
            {
                Id = profile.Id,
                UserId = profile.UserId,
                CompanyName = "New name",
                ContactEmail = "new@example.com"
            }, CancellationToken.None);

            result.Should().BeTrue();
            var updated = await context.CompanyProfiles.FindAsync(profile.Id);
            updated!.CompanyName.Should().Be("New name");
            updated.ContactEmail.Should().Be("new@example.com");
            updated.ModifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateHandle_MissingProfile_ReturnsFalse()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new UpdateCompanyProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<UpdateCompanyProfileCommandHandler>.Instance);

            var result = await handler.Handle(new UpdateCompanyProfileCommand
            {
                Id = Guid.NewGuid(),
                UserId = "missing",
                CompanyName = "Company",
                ContactEmail = "contact@company.example"
            }, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteHandle_ExistingProfile_RemovesProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CompanyProfile { UserId = "company-1" };
            context.CompanyProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new DeleteCompanyProfileCommandHandler(
                context,
                NullLogger<DeleteCompanyProfileCommandHandler>.Instance);

            var result = await handler.Handle(new DeleteCompanyProfileCommand(profile.Id), CancellationToken.None);

            result.Should().BeTrue();
            (await context.CompanyProfiles.FindAsync(profile.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteHandle_MissingProfile_ReturnsFalse()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new DeleteCompanyProfileCommandHandler(
                context,
                NullLogger<DeleteCompanyProfileCommandHandler>.Instance);

            var result = await handler.Handle(new DeleteCompanyProfileCommand(Guid.NewGuid()), CancellationToken.None);

            result.Should().BeFalse();
        }
    }
}
