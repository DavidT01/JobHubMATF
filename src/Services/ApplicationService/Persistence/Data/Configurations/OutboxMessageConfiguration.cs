using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApplicationService.Persistence.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("application_outbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Sequence).HasColumnName("sequence").UseIdentityAlwaysColumn();
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Attempts).HasColumnName("attempts");
        builder.HasIndex(x => x.Sequence).IsUnique().HasDatabaseName("ux_application_outbox_sequence");
        builder.HasIndex(x => new { x.NextAttemptAtUtc, x.Sequence }).HasFilter("published_at_utc IS NULL")
            .HasDatabaseName("ix_application_outbox_pending");
        builder.HasIndex(x => new { x.ApplicationId, x.Sequence }).HasDatabaseName("ix_application_outbox_application");
    }
}
