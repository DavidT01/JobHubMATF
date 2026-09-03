using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.DeleteCandidate;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Commands
{
    public class CandidateProfileCommandHandlerTests
    {
        [Fact]
        public async Task CreateHandle_ValidCommand_PersistsProfileAndReturnsId()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new CreateCandidateProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<CreateCandidateProfileCommandHandler>.Instance);
            var command = new CreateCandidateProfileCommand
            {
                UserId = "candidate-1",
                FirstName = "Ana",
                LastName = "Peric",
                Email = "ana@example.com",
                PhoneNumber = "062123456"
            };

            var id = await handler.Handle(command, CancellationToken.None);

            var profile = await context.CandidateProfiles.FindAsync(id);
            profile.Should().NotBeNull();
            profile.UserId.Should().Be(command.UserId);
            profile.FirstName.Should().Be(command.FirstName);
            profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateHandle_WrongUser_ReturnsFalseAndDoesNotChangeProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile
            {
                UserId = "candidate-1",
                FirstName = "Marko",
                LastName = "Markovic",
                Email = "marko@example.com"
            };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new UpdateCandidateProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<UpdateCandidateProfileCommandHandler>.Instance);

            var result = await handler.Handle(new UpdateCandidateProfileCommand
            {
                Id = profile.Id,
                UserId = "another-user",
                FirstName = "Changed"
            }, CancellationToken.None);

            result.Should().BeFalse();
            (await context.CandidateProfiles.FindAsync(profile.Id))!.FirstName.Should().Be("Marko");
        }

        [Fact]
        public async Task UpdateHandle_ExistingUser_UpdatesProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1", FirstName = "Ana", LastName = "Peric", Email = "ana@example.com" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new UpdateCandidateProfileCommandHandler(
                context,
                TestHelpers.CreateMapper(),
                NullLogger<UpdateCandidateProfileCommandHandler>.Instance);

            var result = await handler.Handle(new UpdateCandidateProfileCommand
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FirstName = "Updated",
                LastName = profile.LastName,
                Email = profile.Email
            }, CancellationToken.None);

            result.Should().BeTrue();
            (await context.CandidateProfiles.FindAsync(profile.Id))!.FirstName.Should().Be("Updated");
        }

        [Fact]
        public async Task DeleteHandle_MissingProfile_ReturnsFalse()
        {
            using var context = TestHelpers.CreateDbContext();
            var handler = new DeleteCandidateProfileCommandHandler(
                context,
                NullLogger<DeleteCandidateProfileCommandHandler>.Instance);

            var result = await handler.Handle(new DeleteCandidateProfileCommand(Guid.NewGuid()), CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteHandle_ExistingProfile_RemovesProfile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new DeleteCandidateProfileCommandHandler(
                context,
                NullLogger<DeleteCandidateProfileCommandHandler>.Instance);

            var result = await handler.Handle(new DeleteCandidateProfileCommand(profile.Id), CancellationToken.None);

            result.Should().BeTrue();
            (await context.CandidateProfiles.FindAsync(profile.Id)).Should().BeNull();
        }
    }
}
