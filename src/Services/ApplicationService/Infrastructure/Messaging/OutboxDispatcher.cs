using ApplicationService.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Infrastructure.Messaging;

public enum OutboxDispatchResult { NoWork, Busy, Published, RetryScheduled }

public sealed class OutboxDispatcher(ApplicationDbContext db, OutboxDelivery delivery, TimeProvider clock)
{
    // All Application dispatchers share this transaction-scoped PostgreSQL lock.
    // Serial delivery is deliberate for the initial, low-volume implementation.
    private const long LockKey = 1801616003;

    public async Task<OutboxDispatchResult> DispatchOneAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var locked = await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({LockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!locked) return OutboxDispatchResult.Busy;

        var now = clock.GetUtcNow();
        var message = await db.OutboxMessages
            .Where(row => row.PublishedAtUtc == null && row.NextAttemptAtUtc <= now
                && !db.OutboxMessages.Any(earlier => earlier.ApplicationId == row.ApplicationId
                    && earlier.PublishedAtUtc == null && earlier.Sequence < row.Sequence))
            .OrderBy(row => row.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (message is null) return OutboxDispatchResult.NoWork;

        var published = await delivery.DeliverAsync(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return published ? OutboxDispatchResult.Published : OutboxDispatchResult.RetryScheduled;
    }
}
