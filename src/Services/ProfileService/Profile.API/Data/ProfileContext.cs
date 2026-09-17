using Microsoft.EntityFrameworkCore;
using Profile.API.Data.Outbox;
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
        public DbSet<Education> Education { get; set; } = null!;
        public DbSet<Experience> Experience { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

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

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("ProfileOutbox");
                entity.HasKey(message => message.Id);
                entity.Property(message => message.Id).ValueGeneratedNever();
                entity.Property(message => message.Sequence).UseIdentityAlwaysColumn();
                entity.Property(message => message.EventType).HasMaxLength(100).IsRequired();
                entity.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
                entity.HasIndex(message => message.Sequence).IsUnique();
                entity.HasIndex(message => new { message.AggregateId, message.Sequence });
                entity.HasIndex(message => new { message.NextAttemptAtUtc, message.Sequence })
                    .HasFilter("\"PublishedAtUtc\" IS NULL");
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
