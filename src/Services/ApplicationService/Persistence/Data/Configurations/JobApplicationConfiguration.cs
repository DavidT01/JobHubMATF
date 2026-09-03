using ApplicationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApplicationService.Persistence.Data.Configurations;

public sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(application => application.CandidateId)
            .HasColumnName("candidate_id")
            .IsRequired();

        builder.Property(application => application.JobId)
            .HasColumnName("job_id")
            .IsRequired();

        builder.Property(application => application.CoverLetter)
            .HasColumnName("cover_letter")
            .HasMaxLength(JobApplication.MaximumCoverLetterLength);

        builder.Property(application => application.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(application => application.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(application => application.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(application => new { application.CandidateId, application.JobId })
            .IsUnique()
            .HasDatabaseName("ux_job_applications_candidate_job");

        builder.HasIndex(application => new { application.JobId, application.Status })
            .HasDatabaseName("ix_job_applications_job_status");

        builder.HasIndex(application => new { application.CandidateId, application.SubmittedAtUtc })
            .HasDatabaseName("ix_job_applications_candidate_submitted");
    }
}
