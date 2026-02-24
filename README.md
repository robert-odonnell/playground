# Family Chat v1

Single-organization Slack-lite chat system built with:

- ASP.NET Core (.NET 10) API + SignalR
- SQL Server (EF Core)
- Shared Contracts + typed Client SDK
- .NET MAUI app (mobile + desktop, admin section role-gated)

## Repository layout

- `src/FamilyChat.Api` - HTTP API, SignalR hub, auth middleware, controllers
- `src/FamilyChat.Application` - business logic/services
- `src/FamilyChat.Domain` - entities and enums
- `src/FamilyChat.Infrastructure` - EF Core, JWT, SMTP, ULID generation
- `src/FamilyChat.Contracts` - shared DTOs/events
- `src/FamilyChat.ClientSdk` - typed HTTP + SignalR client
- `src/FamilyChat.Maui` - single MAUI app for chat and admin
- `tests/*` - unit/integration/realtime tests
- `build/docker-compose.yml` - local SQL Server
- `build/sql/schema.sql` - full SQL schema script (from EF migration)
- `build/sql/seed.sql` - idempotent initial admin/channel seed

## Prerequisites

- .NET SDK 10.0+
- Docker (for local SQL Server)
- MAUI workloads for client build:
  - `dotnet workload restore src/FamilyChat.Maui/FamilyChat.Maui.csproj`

## Local backend setup

1. Start SQL Server:

```bash
docker compose -f build/docker-compose.yml up -d
```

2. Configure API settings in `src/FamilyChat.Api/appsettings.json` (or user-secrets):

- `ConnectionStrings:SqlServer`
- `Jwt:SigningKey`
- `Smtp:*`

3. Run API:

```bash
dotnet run --project src/FamilyChat.Api/FamilyChat.Api.csproj
```

On startup, EF migrations are applied automatically.

4. Optional manual SQL bootstrap:

```bash
# schema script already generated in build/sql/schema.sql
# apply schema + seed with sqlcmd if preferred
```

## API and realtime

- API base: `https://localhost:<port>/`
- SignalR hub: `/realtime`
- Swagger: `/swagger` (development)
- Health: `/health`

## Auth flow (magic link)

1. `POST /auth/magic-link/request`
2. `POST /auth/magic-link/verify`
3. Use bearer token for authenticated APIs.

If SMTP is not configured, magic links are logged by server logs for development.

## Tests

```bash
dotnet test tests/FamilyChat.UnitTests/FamilyChat.UnitTests.csproj
dotnet test tests/FamilyChat.IntegrationTests/FamilyChat.IntegrationTests.csproj
dotnet test tests/FamilyChat.RealtimeTests/FamilyChat.RealtimeTests.csproj
```

## MAUI app

The MAUI app implements:

- Magic link login
- Conversations list + unread badges
- Message list, send/delete, basic emoji reaction toggle
- Search
- Notification settings
- Admin user/channel management UI for admins

