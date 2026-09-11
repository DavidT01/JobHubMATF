# Application lifecycle events (v1)

The versioned payload contains stable references and status data only, never a
CV URL, CV bytes, cover letter, candidate name or email. CandidateProfileId is a
Profile UUID; CandidateUserId is the distinct Identity identifier and can be null
for historical applications. JobId is the Catalog ObjectId, not a company ID.

- `application.submitted.v1`: created for a newly submitted application, with no
  previous status and the application's original submission timestamp.
- `application.status-changed.v1`: created only for an actual status change,
  with old/new status names and the application's update timestamp. Accepted is
  represented by this same event with Status `Accepted`, not an extra duplicate
  lifecycle event.

SchemaVersion is 1. Preserve field meanings and status names for consumers; use
a new schema version for breaking changes. OccurredAtUtc is the business event
time, not retry time. Serialize and persist the event once: a delivery retry must
reuse the same EventId and payload. Consumers deduplicate by EventId because
publisher confirms and retries provide at-least-once, not exactly-once delivery.

This commit defines and tests payloads only. Handlers do not publish yet. The next
steps are a transactional outbox stored with Application changes, atomic status
update/outbox insertion, and a RabbitMQ publisher with confirms and retry handling.
Do not send directly after database commit and assume failures can only be logged.
Do not enable broker delivery until the durable write path is in place.
