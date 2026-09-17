# NotificationService

Standalone in-app notification inbox for JobHub.

## Responsibilities

- Store user notifications (SQLite)
- User APIs (JWT): list, unread count, mark read / read-all
- Service APIs (X-Api-Key): create / batch create (used by Identity over HTTP)
- RabbitMQ consumer: Application + Recruitment lifecycle events → inbox

## Event flow (RabbitMQ)

```
ApplicationService / RecruitmentService
  → outbox → exchange (topic)
  → queues notification.applications / notification.recruitment
  → Notification.API consumer → UserNotification
```

| Event | Inbox title (example) |
|-------|------------------------|
| `application.submitted.v1` | Application submitted |
| `application.status-changed.v1` | Application status updated |
| `interview.scheduled.v1` / `rescheduled` / `cancelled` | Interview … |
| `candidate.round-advanced.v1` / `hired` / `rejected` | Moved to next round / hired / closed |

Identity welcome / email-confirm notifications still use **HTTP + X-Api-Key** (not Rabbit).

## Run locally

1. Start RabbitMQ (Docker): `docker compose up -d rabbitmq`  
   UI: http://localhost:15672 (guest/guest)
2. Notification (Development enables consumer):

```powershell
dotnet run --project src\Services\NotificationService\Notification.API --launch-profile http
```

3. Enable Application / Recruitment outbox (`appsettings.Development.json` already has `Outbox:Enabled=true` when you run those services in Development).

- API / Swagger: http://localhost:5290/swagger
- If you upgrade an old SQLite file and broker events fail, delete `notification.db` once so `EnsureCreated` rebuilds tables.

## Angular

`environment.notificationApiUrl` → `http://localhost:5290/api/notifications`

## Docker

`notification-api` is in root `docker-compose.yml` (port **5290**).  
Rabbit consumer + Application/Recruitment outbox are **enabled** in compose.

## Tests

```powershell
dotnet test src\Services\NotificationService\Notification.API.Tests\Notification.API.Tests.csproj
```
