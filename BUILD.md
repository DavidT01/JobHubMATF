# BUILD

This document describes how to build and run the whole project on your local machine: the backend services in Docker and the Angular application through the development server.

Before you start, prepare your environment by following the [SETUP](./SETUP.md) guide (.NET 10 SDK and Node.js 24).

## 1) Prerequisites

### Docker Desktop

The backend services and databases run through Docker Compose.

1. Download and install **Docker Desktop**:  
   https://www.docker.com/products/docker-desktop/
2. Start Docker Desktop and wait until it is running.
3. Verify the installation:

```powershell
docker version
docker compose version
```

Expected: both commands print a version without errors. `docker version` must also show a `Server` section; if it doesn't, Docker Desktop is not running.

### Free ports

The following ports must be free on your machine:

| Port | Used by |
|---|---|
| 4200 | Angular application |
| 5020, 5075, 5107, 5116, 5213, 5246, 5283, 5290 | backend services and gateway |
| 5672, 15672 | RabbitMQ |

---

## 2) What gets started

| Service | Address on your machine | Database / dependency |
|---|---|---|
| Identity.API | http://localhost:5283 | SQLite (in a Docker volume) |
| Catalog | http://localhost:5246 | MongoDB, Redis |
| Profile.API | http://localhost:5213 | PostgreSQL |
| Recruitment.API | http://localhost:5116 | PostgreSQL, RabbitMQ |
| ApplicationService | http://localhost:5020 | PostgreSQL, RabbitMQ |
| Chat.API | http://localhost:5075 | MongoDB |
| Notification.API | http://localhost:5290 | SQLite, RabbitMQ |
| Gateway (Ocelot) | http://localhost:5107 | forwards requests to the services |
| RabbitMQ console | http://localhost:15672 | user `guest`, password `guest` |

PostgreSQL, MongoDB and Redis are not exposed on your machine; only the services inside the Docker network can reach them.

---

## 3) Running the backend (Docker)

1. Open a terminal in the repository root (the folder that contains `docker-compose.yml`).
2. Build and start all services:

```powershell
docker compose up -d --build
```

> The first build takes a few minutes because Docker downloads the images and NuGet packages. Later builds are much faster.

3. Check that all containers are running:

```powershell
docker compose ps
```

Expected: 12 containers with the status `Up`; `postgres`, `mongo`, `redis` and `rabbitmq` are also marked `(healthy)`.

The databases are created and the migrations are applied automatically when the services start. You don't need to run migrations manually.

---

## 4) Running the frontend (Angular)

1. In a new terminal, go to the SPA folder:

```powershell
cd .\src\Web\WebSPA
```

2. Install the dependencies (only the first time and whenever `package.json` changes):

```powershell
npm install
```

3. Start the development server:

```powershell
npm start
```

4. Open http://localhost:4200

> The development server forwards some requests (`/api/company-profiles`, `/api/applications`) to the gateway through `proxy.conf.json`. The other services are called directly, at the addresses in `src\environments\environment.development.ts`.

---

## 5) First steps in the application

### Administrator

An administrator account is created automatically on the first start:

| Email | Password |
|---|---|
| `admin@jobhub.local` | `Admin123!` |

### New users

1. On the sign-in page, choose **Register** (candidate) or **Register company** (company).
2. Fill in the form and submit it.
3. The project doesn't send real emails: after registration the application opens the confirmation page itself and confirms the account, so you can sign in right away.

An administrator can also confirm an account manually on the **Users** page (the **Confirm email** button).

### Profile after registration

Registration creates only the account, not the candidate or company profile, and the application doesn't have a screen for creating a profile yet. Without a profile, a candidate has no CV to apply with, and a company can't post jobs (jobs are posted under the company profile).

Until this is added to the application, create the profile through the Profile service's API documentation:

1. Sign in to the application and read the token and your user ID in the browser console (F12 → Console):

```js
const token = localStorage.getItem('auth_token');
(await (await fetch('http://localhost:5283/api/auth/me', { headers: { Authorization: 'Bearer ' + token } })).json()).id
```

2. Open http://localhost:5213/scalar/v1 and choose `POST /api/company-profiles` (company) or `POST /api/candidate-profiles` (candidate).
3. On the **Headers** tab, add `Authorization` with the value `Bearer <token>`.
4. Enter the data in **Body**, with the `userId` from step 1, and send the request (expected response: `201`).

Company:

```json
{
  "userId": "<user id>",
  "companyName": "My Company LLC",
  "contactEmail": "contact@mycompany.com",
  "contactPhone": "+381 11 123 4567"
}
```

Candidate:

```json
{
  "userId": "<user id>",
  "firstName": "John",
  "lastName": "Smith",
  "email": "john.smith@example.com",
  "phoneNumber": "+381 64 123 4567"
}
```

After that, you can view and complete the profile in the application (user menu → **My profile** → **Edit profile**), and a company can post jobs.

---

## 6) Checking that the services work

Each service has an API documentation page where you can try out its endpoints:

| Service | Documentation |
|---|---|
| Identity.API | http://localhost:5283/swagger |
| Catalog | http://localhost:5246/swagger |
| Notification.API | http://localhost:5290/swagger |
| Profile.API | http://localhost:5213/scalar/v1 |
| Recruitment.API | http://localhost:5116/scalar/v1 |
| Chat.API | http://localhost:5075/scalar/v1 |
| ApplicationService | http://localhost:5020/health |

The gateway has no page of its own; without a token, protected routes (for example http://localhost:5107/api/applications) return `401`, which means it is working.

---

## 7) Building and testing without Docker

From the repository root:

```powershell
dotnet build JobHubMATF.slnx
dotnet test JobHubMATF.slnx
```

The Notification service isn't in the solution file yet, so run its tests separately:

```powershell
dotnet test .\src\Services\NotificationService\Notification.API.Tests\Notification.API.Tests.csproj
```

Frontend, from the `src\Web\WebSPA` folder:

```powershell
npm run build
npm test
```

---

## 8) Everyday commands

| Command | What it does |
|---|---|
| `docker compose up -d` | starts all services without rebuilding |
| `docker compose up -d --build profile-api` | rebuilds and restarts a single service (after a code change) |
| `docker compose logs -f profile-api` | follows the logs of one service |
| `docker compose stop` | stops the services; data is kept |
| `docker compose down` | removes the containers; data stays in the volumes |
| `docker compose down -v` | removes the containers **and all data** (databases, uploaded files) |

Use the service names from `docker-compose.yml` in these commands: `identity-api`, `catalog-api`, `profile-api`, `recruitment-api`, `application-api`, `chat-api`, `notification-api`, `gateway`.

### Settings through environment variables

You don't need them for local development. If you want different values, create an `.env` file next to `docker-compose.yml`:

```
JWT_SECRET=...
POSTGRES_PASSWORD=...
NOTIFICATION_API_KEY=...
```

---

## 9) Troubleshooting

**`docker compose` reports that it can't connect to Docker**  
Docker Desktop isn't running. Start it and run the command again.

**`port is already allocated`**  
Another program is using one of the ports from section 1. Stop it, then run `docker compose up -d` again.

**The candidate profile doesn't load (the browser console shows `https://localhost:7043` and `ERR_CONNECTION_REFUSED`)**  
`profileApiUrl` in `src\Web\WebSPA\src\environments\environment.development.ts` points to the HTTPS port that Profile.API uses only when it is started with `dotnet run` and the `https` profile. When the backend runs in Docker, the address must be:

```ts
profileApiUrl: 'http://localhost:5213/api',
```

**A service doesn't start or keeps restarting**  
Check its logs, for example `docker compose logs profile-api`.

**I want to start with an empty database**  
Run `docker compose down -v`, then `docker compose up -d --build`. This deletes all users, jobs and applications.

### Known limitations

- **Scheduling interviews** (Google Meet) requires a Google key that isn't in the repository (`Google:CredentialsPath` in Recruitment.API). Without it, scheduling returns an error; the rest of the application works normally.
