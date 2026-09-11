# RabbitMQ outbox publisher

The publisher borrows a long-lived connection, owns one channel and serializes
access to that channel. It declares a durable topic exchange with a configured
non-reserved name. The proposed Application exchange is `jobhub.applications.v1`;
routing keys are the versioned event types. Consumers provision their own durable
queues/bindings before dispatch is enabled; no consumer queues are fabricated here.

Publisher confirmations and automatic confirmation tracking are both enabled.
Messages use mandatory routing, persistent delivery, JSON UTF-8, original EventId,
ApplicationId correlation and original business timestamp. Awaited publish failures
propagate; the publisher never marks the database row delivered. A ten-second bound
and caller cancellation cover waiting for channel access and confirmation.

An unroutable message, nack, timeout or broken connection must leave the outbox
pending. Reconnect with a new publisher after channel failure and retry the same
stored payload/ID. The dispatcher must record completion after confirmation;
a crash between confirmation and DB completion can cause duplicate delivery.

This component is not yet registered as a hosted service and opens no connection
at API startup. Connection configuration, dispatcher, retry/concurrency handling
and real broker tests remain before enabling it. Unit tests mock the Rabbit API;
they do not prove real broker nack/return behavior.

API/lifecycle reference: [RabbitMQ .NET client guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide).

## Dispatcher

`OutboxDispatcher` dispatches one due row per database transaction. All instances
use PostgreSQL transaction advisory lock 1801616003; a busy worker skips the tick.
The lock is held until publish confirmation and persisted delivery/retry state.
Only the earliest unpublished sequence for an application is eligible, so an older
failed event blocks later events for that application, not for other applications.
The initial dispatcher serializes throughput across instances intentionally.

Create a fresh scoped DbContext for every attempt, including after failures.
Retry delays are 5, 10, 20, 40, 80, 160 and then 300 seconds. Events are retained,
not silently discarded after a retry limit. Shutdown rolls back the transaction;
DB failure after broker confirmation can still result in a duplicate on restart.
Consumers must deduplicate. No lease timeout or SKIP LOCKED behavior is claimed.

Delivery/backoff unit tests are implemented. Real PostgreSQL lock contention,
ordering and restart tests are still required before enabling the hosted worker;
SQLite is not a substitute for the advisory-lock SQL.
