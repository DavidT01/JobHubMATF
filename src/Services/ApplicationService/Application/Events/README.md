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

Handlers persist these payloads in the transactional outbox with submission/status
changes. The opt-in worker publishes them with confirms and retries. Delivery is
disabled by default until deployment provides credentials, migrations and consumer
bindings. Notification/Chat/Recruitment consumers are separate integration work.
