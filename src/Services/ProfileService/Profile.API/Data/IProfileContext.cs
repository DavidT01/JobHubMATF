using Microsoft.EntityFrameworkCore;
using Profile.API.Data.Outbox;
using Profile.API.Entities;

namespace Profile.API.Data
{
    public interface IProfileContext
    {
        DbSet<CandidateProfile> CandidateProfiles { get; }
        DbSet<CompanyProfile> CompanyProfiles { get; }
        DbSet<OutboxMessage> OutboxMessages { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
