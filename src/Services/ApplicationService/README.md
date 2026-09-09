# Application Service

Application Service owns job applications: submission, candidate and employer views, status changes, and administrator statistics. It targets .NET 10 and stores its data in a dedicated PostgreSQL database.

The production code remains one project. `Domain`, `Application`, `Persistence`, and `Infrastructure` are folders within that project; `tests/ApplicationService.UnitTests` is the separate test assembly.

## Prerequisites

- .NET 10 SDK
- Docker Desktop with Docker Compose
- `dotnet-ef` 10.0.4 when applying migrations from the host:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.4
```

Run commands in `src/Services/ApplicationService` unless stated otherwise.

## Configuration

The service requires these settings:

| Setting | Purpose | Local example |
| --- | --- | --- |
| `ConnectionStrings__ApplicationDatabase` | Application PostgreSQL connection | `Host=localhost;Port=5433;Database=jobhub_applications;Username=postgres;Password=postgres` |
| `JwtSettings__Issuer` | Expected JWT issuer | `JobHubIdentityAPI` |
| `JwtSettings__Audience` | Expected JWT audience | `JobHubClients` |
| `JwtSettings__Secret` | Shared HMAC-SHA256 signing key, at least 32 UTF-8 bytes | Supply through an environment variable or user secrets |
| `Services__ProfileBaseUrl` | Internal Profile Service URL | `http://localhost:5213` |
| `Services__ProfilePublicBaseUrl` | Public base used to resolve CV links | `http://localhost:5213` |
| `Services__CatalogBaseUrl` | Internal Catalog Service URL | `http://localhost:5246` |

Do not commit a real JWT secret or production database password. The checked-in PostgreSQL credentials are development-only defaults.

## Database and migrations

`compose.application.yml` exposes PostgreSQL on host port `5433` and persists its data in the named volume `application-db-data`. Inside the Compose network, the API connects to `application-db:5432`.

Start only the database, override the host connection string, and apply the existing migrations:

```powershell
docker compose -f compose.application.yml up -d application-db
$env:ConnectionStrings__ApplicationDatabase = "Host=localhost;Port=5433;Database=jobhub_applications;Username=postgres;Password=postgres"
dotnet ef database update --project ApplicationService.csproj
```

The service intentionally does not apply migrations automatically at startup. Apply them before first use and whenever a new migration is introduced.

Create a migration after an intentional persistence-model change:

```powershell
dotnet ef migrations add MigrationName --project ApplicationService.csproj --output-dir Persistence/Migrations
```

## Run locally with .NET

Keep the database running, set a development signing key, and start the HTTP launch profile:

```powershell
$env:JwtSettings__Secret = "development-only-signing-key-at-least-32-bytes"
dotnet run --launch-profile http
```

The API is available at `http://localhost:5020`. Profile Service and Catalog Service must be reachable at their configured URLs for operations that use their data. The health endpoint is `GET /health`; development OpenAPI JSON is at `GET /openapi/v1.json`.

## Run with Docker Compose

The `Dockerfile` describes how to build one Application API image. The Compose file orchestrates that image together with its PostgreSQL container, network settings, ports, health dependency, and persistent volume.

After applying migrations as described above, start both containers:

```powershell
$env:JWT_SECRET = "development-only-signing-key-at-least-32-bytes"
docker compose -f compose.application.yml up --build
```

The containerized API is available at `http://localhost:5020`; PostgreSQL remains available to host tools at `localhost:5433`. Override `PROFILE_BASE_URL`, `PROFILE_PUBLIC_BASE_URL`, and `CATALOG_BASE_URL` when the other services do not use the Compose defaults.

Stop the containers without deleting database data:

```powershell
docker compose -f compose.application.yml down
```

Adding `--volumes` deletes the development database volume and its data, so use it only when a clean database is intended.

## HTTP API

All application endpoints require a bearer JWT. Enum names are serialized as strings and integer enum values are rejected.

| Method and route | Role | Purpose |
| --- | --- | --- |
| `POST /api/applications` | `Candidate` | Submit an application using `jobId` and optional `coverLetter`; a current CV is required on the candidate profile |
| `GET /api/applications/me` | `Candidate` | Read the signed-in candidate's applications |
| `GET /api/applications/jobs/{jobId}` | `Employer` | Read applications for an owned Catalog job, including current candidate/CV information |
| `PUT /api/applications/{applicationId}/status` | `Employer` | Change the status of an application for an owned job |
| `GET /api/applications/statistics` | `Admin` | Read aggregate statistics from the Application database |

Candidate and employer list query parameters:

- `pageNumber` defaults to `1`.
- `pageSize` defaults to `20` and must be between `1` and `100`.
- `status` optionally filters by `Submitted`, `InReview`, `Interview`, `Rejected`, or `Accepted`.
- `sortBy` is restricted to `SubmittedAtUtc` or `UpdatedAtUtc`.
- `sortDirection` is `Asc` or `Desc`; the default ordering is `SubmittedAtUtc Desc`.

The statistics endpoint accepts optional inclusive dates as `from=YYYY-MM-DD` and `to=YYYY-MM-DD`. It returns the total, count and percentage for every application status, and a daily submission trend. It never calls another service to calculate these values.

Allowed status transitions are:

- `Submitted` → `InReview` or `Rejected`
- `InReview` → `Interview`, `Accepted`, or `Rejected`
- `Interview` → `Accepted` or `Rejected`
- `Accepted` and `Rejected` are terminal

Repeating the current status is idempotent. Invalid transitions and concurrent status changes return a conflict response.

## Tests

```powershell
dotnet test ApplicationService.sln --configuration Release
dotnet build ApplicationService.sln --configuration Release --no-restore
dotnet format ApplicationService.sln --verify-no-changes --no-restore
```

Unit tests cover domain invariants, every status transition, application query filtering/sorting, ownership scoping, and statistics calculations.

## Integration boundaries

- Identity Service issues JWTs and owns user/role management. Application Service only validates the configured issuer, audience, signature, and `Candidate`, `Employer`, or `Admin` role.
- Profile Service resolves candidate/company profiles and the candidate's current CV.
- Catalog Service owns jobs and company ownership. Application Service stores the Catalog job ID but does not duplicate the job entity.
- API Gateway routing, shared dashboard/auth routing, gRPC contracts, RabbitMQ notifications, chat/recruitment flows, and full multi-service E2E tests are integration work outside this service's standalone scope.
