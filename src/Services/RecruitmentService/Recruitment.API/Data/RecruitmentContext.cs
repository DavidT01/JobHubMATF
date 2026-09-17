using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data.Outbox;
using Recruitment.API.Entities;

namespace Recruitment.API.Data
{
    public class RecruitmentContext(DbContextOptions<RecruitmentContext> options) : DbContext(options)
    {
        public DbSet<RecruitmentProcess> Processes { get; set; }
        public DbSet<SelectionRound> Rounds { get; set; }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }
        public DbSet<CandidateEvaluation> Evaluations { get; set; }
        public DbSet<CandidateProgress> Progresses { get; set; }
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SelectionRound>()
                .HasOne(sr => sr.RecruitmentProcess)
                .WithMany(rp => rp.Rounds)
                .HasForeignKey(sr => sr.RecruitmentProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InterviewSchedule>()
                .HasOne(i => i.SelectionRound)
                .WithMany()
                .HasForeignKey(i => i.SelectionRoundId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateEvaluation>()
                .HasOne(e => e.SelectionRound)
                .WithMany()
                .HasForeignKey(e => e.SelectionRoundId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProgress>()
                .HasOne(cp => cp.RecruitmentProcess)
                .WithMany()
                .HasForeignKey(cp => cp.RecruitmentProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProgress>()
                .HasOne(cp => cp.CurrentSelectionRound)
                .WithMany()
                .HasForeignKey(cp => cp.CurrentSelectionRoundId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CandidateProgress>()
                .HasIndex(cp => cp.ApplicationId)
                .IsUnique();

            modelBuilder.Entity<RecruitmentProcess>()
                .Property(process => process.JobId)
                .HasMaxLength(24)
                .IsRequired();

            modelBuilder.Entity<RecruitmentProcess>()
                .HasIndex(process => process.JobId)
                .IsUnique();

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("RecruitmentOutbox");
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
    }
}
