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
        public DbSet<Education> Educations { get; set; } = null!;
        public DbSet<Experience> Experiences { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CandidateProfile>().HasIndex(p => p.UserId).IsUnique();
            modelBuilder.Entity<CompanyProfile>().HasIndex(p => p.UserId).IsUnique();

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(c => c.Education)
                .WithOne(e => e.CandidateProfile)
                .HasForeignKey(e => e.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(c => c.Experience)
                .WithOne(e => e.CandidateProfile)
                .HasForeignKey(e => e.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(c => c.Projects)
                .WithOne(p => p.CandidateProfile)
                .HasForeignKey(p => p.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(c => c.Languages)
                .WithOne(l => l.CandidateProfile)
                .HasForeignKey(l => l.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
