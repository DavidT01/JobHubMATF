# Application job lookup

`CatalogJobGrpcService.GetJob` reads the current job directly from the existing
repository, without the listing cache. It returns only the Catalog ObjectId,
company **profile** UUID, active flag and nullable UTC expiration. Inactive and
expired jobs are intentionally returned: Application applies submission and
ownership rules itself. There is no job mutation or new business entity here.

The lookup has the same public-read access model as existing Catalog job details;
it does not expose candidate data. Invalid ObjectIds return `InvalidArgument`,
missing jobs `NotFound`, inconsistent stored references `Internal`, and MongoDB
errors/timeouts `Unavailable`. Caller cancellation reaches the Mongo query.

For local HTTP/2 gRPC use the existing HTTPS profile with a trusted development
certificate:

```sh
dotnet run --project src/Services/Catalog --launch-profile https
```

gRPC address: `https://localhost:7018`. REST remains on `http://localhost:5246`.
Container deployments must configure an HTTP/2 listener or gRPC-capable TLS
termination. Do not disable certificate validation. Application still uses its
HTTP reader until the gRPC client integration step.

Tests use the generated client, ASP.NET TestServer and real gRPC implementation,
with only the repository stubbed (no MongoDB connection required):

```sh
dotnet test src/Services/Catalog.Tests/Catalog.Tests.csproj --configuration Release
```

The separate .NET 10 test project is outside the production Catalog directory so
its source/output cannot be included in the web project by default globs.
