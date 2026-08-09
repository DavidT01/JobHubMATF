using Microsoft.EntityFrameworkCore;
using Recruitment.API.Entities;

namespace Recruitment.API.Data
{
    public class RecruitmentContext : DbContext
    {
        public RecruitmentContext(DbContextOptions<RecruitmentContext> options) : base(options)
        {

        }

        public DbSet<RecruitmentProcess> Processes { get; set; }
        public DbSet<SelectionRound> Rounds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SelectionRound>()
                .HasOne(sr => sr.RecruitmentProcess)
                .WithMany(rp => rp.Rounds)
                .HasForeignKey(sr => sr.RecruitmentProcessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
