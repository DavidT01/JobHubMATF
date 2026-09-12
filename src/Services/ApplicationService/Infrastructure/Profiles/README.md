# Profile integration migration

`ICompanyProfileReader` now uses the owner-only Profile gRPC lookup. Configure
`GrpcServices__ProfileApi` (default `https://localhost:7043`) and start Profile's
HTTPS launch profile with a trusted certificate. Both services must use the same
Identity JWT settings. This address must be HTTPS, without credentials or
path/query/fragment: bearer tokens must not cross a plaintext connection.
Container deployments need trusted TLS and HTTP/2 for this endpoint too.

The reader forwards only the current request's bearer token in per-call metadata,
uses a ten-second deadline and propagates cancellation. Missing bearer credentials
do not trigger an anonymous lookup. Profile validates the token and verifies that
its Employer subject owns the requested company profile. Returned profile ID and
user ID are validated again by Application; CompanyId remains the profile UUID.

Only NotFound maps to a missing profile. Authentication failures, permission
failures and other RPC errors become dependency-unavailable errors, never an empty
profile or HTTP fallback. Redirects and certificate bypasses are disabled.

Candidate lookups still use the existing HTTP reader, including employer access
to applicants' current CV/name after Application checks job ownership. Migrating
that distinct access case awaits the agreed internal applicant-reference security
design; the owner-only Profile RPC is not weakened to support it.
