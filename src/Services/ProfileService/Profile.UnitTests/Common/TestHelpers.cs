using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Data;
using Profile.API.Mapping;

namespace Profile.UnitTests.Common
{
    public static class TestHelpers
    {
        public static ProfileContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ProfileContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ProfileContext(options);
        }

        public static IMapper CreateMapper()
        {
            var configuration = new MapperConfiguration(config =>
            {
                config.AddProfile<CandidateMappingProfile>();
                config.AddProfile<CompanyMappingProfile>();
            }, NullLoggerFactory.Instance);

            return configuration.CreateMapper();
        }
    }
}
