# NotificationService (member 1 follow-up)

Standalone inbox API. Today Identity still hosts in-app notifications;
this service is the next step toward moving that responsibility here.

## Run locally

```powershell
cd src\Services\NotificationService\Notification.API
dotnet run --launch-profile http
```

- API: http://localhost:5290
- Swagger: http://localhost:5290/swagger

JWT must match Identity (`JwtSettings` in `appsettings.json`).

## Tests

```powershell
dotnet test src\Services\NotificationService\Notification.API.Tests\Notification.API.Tests.csproj
```

## Next steps

1. Add RabbitMQ consumer for events from Identity / Application / Recruitment
2. Point Angular `notification.service.ts` to this API (or Gateway route)
3. Stop writing notifications inside Identity; keep Identity auth only
4. Wire service into root `docker-compose.yml` + Ocelot
