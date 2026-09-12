# Catalog gRPC reader

Production `IJobReader` now uses `CatalogGrpcJobClient` and the generated Catalog
client. Configure `GrpcServices__CatalogApi` (default `https://localhost:7018`)
and start Catalog with its HTTPS launch profile and a trusted dev certificate.
The configured URL must be an HTTP(S) origin without credentials or path/query.
For a private container network, an explicit HTTP/2-only internal HTTP endpoint
can be configured instead. Do not disable TLS certificate validation.

Calls have a ten-second UTC deadline and propagate request cancellation. Only
`NotFound` maps to a missing job; other RPC errors become the existing dependency
unavailable response. Invalid returned IDs, company profile UUIDs and timestamps
are rejected. Inactive/expired jobs remain visible to Application business rules.
The old HTTP implementation remains unregistered during this staged migration;
there is no automatic fallback that could hide a failing gRPC deployment.

Profile lookups still use HTTP in this commit. The employer applications handler
reads applicants' current CV/name after checking job ownership; that is a different
authorization case from the newly implemented owner-only Profile RPCs and must
be integrated explicitly without weakening their owner checks.
