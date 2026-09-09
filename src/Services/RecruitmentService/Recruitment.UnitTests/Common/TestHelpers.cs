using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Recruitment.API.Data;
using Recruitment.API.Mapping;

namespace Recruitment.UnitTests.Common
{
    public static class TestHelpers
    {
        public static RecruitmentContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<RecruitmentContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new RecruitmentContext(options);
        }

        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<RecruitmentMappingProfile>(), NullLoggerFactory.Instance);
            return config.CreateMapper();
        }
    }
}
