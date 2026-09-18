using Microsoft.EntityFrameworkCore;
using Notification.API.Models;

namespace Notification.API.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<ProcessedBrokerEvent> ProcessedBrokerEvents => Set<ProcessedBrokerEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<ProcessedBrokerEvent>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.ProcessedAtUtc);
        });
    }
}
