using Microsoft.EntityFrameworkCore;
using Profile.API.Entities;

namespace Profile.API.Data
{
    public class ProfileContext : DbContext, IProfileContext
    {
        public ProfileContext(DbContextOptions<ProfileContext> options)
            : base(options)
        {

        }

        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
