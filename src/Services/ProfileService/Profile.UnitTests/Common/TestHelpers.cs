using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Data;
using Profile.API.Mapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

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

        public static IFormFile CreateFile(string fileName, string contentType, string content)
        {
            return CreateFile(fileName, contentType, System.Text.Encoding.UTF8.GetBytes(content));
        }

        public static FormFile CreateFile(string fileName, string contentType, byte[] content)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary { ["Content-Type"] = contentType }
            };
        }

        public static IWebHostEnvironment CreateEnvironment(string? rootPath)
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.WebRootPath).Returns(rootPath!);
            return environment.Object;
        }

        public static TemporaryRoot CreateTemporaryRoot()
        {
            return new TemporaryRoot();
        }

        public sealed class TemporaryRoot : IDisposable
        {
            public TemporaryRoot()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
