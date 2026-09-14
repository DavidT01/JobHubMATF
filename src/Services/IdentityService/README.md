# Identity service

## Structure

- `Identity.API` — ASP.NET Identity + JWT API
- `Identity.API.Tests` — xUnit / WebApplicationFactory tests

## Run API

```powershell
cd Identity.API
dotnet run --launch-profile http
```

## Run tests

```powershell
dotnet test Identity.API.Tests/Identity.API.Tests.csproj
```
