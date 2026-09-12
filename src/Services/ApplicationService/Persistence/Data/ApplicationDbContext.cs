using ApplicationService.Domain.Entities;
using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Persistence.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
