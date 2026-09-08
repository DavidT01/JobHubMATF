using Identity.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Data
{

    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserNotification> Notifications => Set<UserNotification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.UserId).HasMaxLength(450).IsRequired();
                entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
                entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();
                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });
        }
    }
}