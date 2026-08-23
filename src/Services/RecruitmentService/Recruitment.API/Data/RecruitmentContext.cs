using Microsoft.EntityFrameworkCore;
using Recruitment.API.Entities;

namespace Recruitment.API.Data
{
    public class RecruitmentContext(DbContextOptions<RecruitmentContext> options) : DbContext(options)
    {
        public DbSet<RecruitmentProcess> Processes { get; set; }
        public DbSet<SelectionRound> Rounds { get; set; }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }

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
        }
    }
}
