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

        public DbSet<CandidateProfile> CandidateProfiles { get; set; } = null!;
        public DbSet<CompanyProfile> CompanyProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CandidateProfile>().HasIndex(p => p.UserId).IsUnique();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
