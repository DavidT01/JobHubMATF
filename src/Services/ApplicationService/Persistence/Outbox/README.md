# Transactional outbox storage

`application_outbox` stores a serialized versioned event once. Its primary key is
the EventId, reused on retries. PostgreSQL generates a unique sequence for dispatch
ordering; a pending partial index supports due-message queries and an application
index supports per-application ordering. Sequence allocation is not commit order:
the dispatcher must also serialize delivery for each application and not bypass
an earlier failed message for the same application.

Payload is JSONB. No raw exception details or credentials are persisted. A failed
delivery increments Attempts and advances NextAttemptAtUtc without modifying the
payload. PublishedAtUtc may be set only after a broker confirmation; completion
is idempotent. Do not delete an unpublished row. Outbox retention is independent
of application deletion, so there is deliberately no cascade-delete foreign key.

The migration is additive and has not been applied automatically. Rolling it back
drops the outbox and therefore loses pending events; do not roll back a deployed
outbox before it has drained and its delivery state has been preserved.

Submission saves its application and event together; status compare-and-set and
event insertion commit or roll back in an explicit transaction. The opt-in dispatcher
uses a PostgreSQL transaction advisory lock, ordered retries and publisher confirms.
Rows accumulate while Outbox:Enabled is false; they are retained for later delivery.
Apply the migration before serving application writes, even if dispatch is disabled.
