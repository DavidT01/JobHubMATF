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

Hosted dispatch is registered only when `Outbox:Enabled` is true. The committed
default is false: no worker or broker connection is created. Unit tests mock the
Rabbit API; the opt-in broker test below verifies actual mandatory returns and confirms.

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

Delivery/backoff unit tests and an opt-in PostgreSQL test cover migration,
sequence generation, ordered retries and cross-instance advisory-lock contention.
Broker/restart tests remain required before enabling the hosted worker; SQLite
is not a substitute for the advisory-lock SQL.

Set `JOBHUB_TEST_POSTGRES` to a dedicated PostgreSQL test connection and run the
Application test executable. The test creates a unique `outbox_test_<guid>` schema,
applies real migrations, exercises dispatch and drops only that schema in finally.
It also uses PostgreSQL triggers to reject outbox insertion after the actual status
handler update, proving rollback, and to reject delivery-state persistence after a
simulated confirmation. A new dispatch scope then resends the same retained event
ID and payload. This covers the confirmation/DB failure window without claiming a
real process kill or RabbitMQ restart (the publisher in this DB test is a stub).
It never drops the database. Without this variable the PostgreSQL test explicitly
skips; do not report the ordinary unit run as a successful integration run.

## Opt-in worker configuration

After migrations, durable consumer bindings and real integration checks, set
`Outbox__Enabled=true`, `Outbox__Exchange` and `Outbox__ConnectionUri` via environment
or secrets. The URI includes Rabbit credentials and virtual host; never commit it.
AMQPS is required except for loopback-only local AMQP. Certificates are validated;
containers need trusted broker TLS. Exchange names beginning with `amq.` are reserved.

The worker creates a fresh scope per iteration, polls every two seconds while idle
and waits five seconds after infrastructure errors. A single shared connection and
confirm channel are reused; publish errors dispose the session so a later attempt
reconnects. Automatic client recovery is disabled to avoid competing recovery paths.
Graceful cancellation stops polling and disposes the owned channel/connection.
Logs exclude connection URIs and raw broker exception messages. Delivery remains
disabled in committed settings pending real PostgreSQL/Rabbit tests.

## Isolated broker test

Set `JOBHUB_TEST_RABBITMQ` to a dedicated broker URI (AMQP loopback or AMQPS) and run
the Application test executable. The test creates a unique durable topic exchange,
verifies an actual mandatory NO_ROUTE return fails publishing, then binds its own
durable queue and confirms successful delivery after the session reconnects. Two
copies preserve the same event ID/body and persistent metadata, demonstrating why
consumer deduplication is needed. The random queue/exchange are removed in finally.
No shared queue is purged. Without this variable the test explicitly skips.
This does not yet simulate broker restarts or a crash between confirm and DB commit.
