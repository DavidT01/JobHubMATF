# Application profile lookups

`ApplicationProfileGrpcService` exposes two owner-only RPCs for Application:
candidate lookup returns the current CV URL and identity/profile IDs; company
lookup returns the company profile ID and its owner's Identity user ID.
Existing CandidateProfileGrpcService RPCs and REST routes are unchanged.

## Configuration

Profile now validates bearer JWTs for these new RPCs. Before starting Profile,
set `JwtSettings__Secret` using the same secret as Identity and Application
(at least 32 UTF-8 bytes), through environment variables or Profile user secrets.
Do not put the secret in source control. Missing/weak secrets fail startup clearly.
`JwtSettings__Issuer` defaults to `JobHubIdentityAPI` and `JwtSettings__Audience`
to `JobHubClients`; override them if the Identity deployment uses different values.

The server verifies HS256 signature, issuer, audience and lifetime. Each lookup
also requires the token subject to equal the requested `user_id` and the matching
Candidate/Employer role. Admin and other users cannot use these owner-only RPCs.
Existing anonymous REST and older gRPC endpoints have not acquired an authorization
policy in this change; this is not a service-wide access-control audit.

Use the existing HTTPS launch profile for local HTTP/2 gRPC:

```sh
dotnet run --project src/Services/ProfileService/Profile.API --launch-profile https
```

Its gRPC address is `https://localhost:7043`; trust the local ASP.NET development
certificate first. Plain HTTP port 5213 remains for REST and is not implicitly an
HTTP/2 gRPC listener. Containers need an HTTP/2-capable internal endpoint or TLS
termination that supports gRPC; never disable certificate validation in clients.
Application's HTTP readers are still active until the client integration commit.

## Verification

```sh
dotnet test src/Services/ProfileService/Profile.UnitTests/Profile.UnitTests.csproj --configuration Release
```

Tests exercise the generated gRPC client, ASP.NET routing/authentication and actual
server implementation using TestServer and a stubbed MediatR query layer. They
cover token validation, owner/role restrictions, missing and inconsistent records,
CV/ID mapping and cancellation. They do not claim a real PostgreSQL or full
Identity/Application integration test.
