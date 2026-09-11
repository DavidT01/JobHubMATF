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

This step adds storage only. Until the subsequent handler transaction and publisher
steps are complete, no rows are enqueued and no messages are sent. A submission
must save its application and event in one transaction; status compare-and-set and
event insertion must likewise commit or roll back together. Dispatcher concurrency
control and publisher confirms remain required before enabling delivery.
