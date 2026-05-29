# SOPS — Smart Order Processing System

Enterprise order management API built on **.NET 8**, aligned with the SOPS Business Requirements Document (BRD).

Handles order intake, **two-tier caching** (memory + Redis), **dual queues** (`Channel<T>` + SQL outbox), **third-party REST** integration (v1/v2), resilience (Polly), rate limiting, and horizontal-scale readiness.

## Tech stack

| Area | Technology |
|------|------------|
| API | ASP.NET Core 8, API versioning (v1 / v2) |
| Data | EF Core 8, SQL Server |
| Cache | `IMemoryCache` + Redis (`IDistributedCache`) |
| Resilience | Polly v8, `Microsoft.Extensions.Http.Resilience` |
| Queue | `System.Threading.Channels`, `BackgroundService` |
| Bulk | SqlBulkCopy / TVP stored procedures (optional) |
| Logging | Serilog |
| Tests | xUnit, Moq |
| DevOps | Docker, GitHub Actions |
| Cloud | Azure App Service, Azure SQL, Azure Redis |

## Solution structure

```
OrderingSystem.sln
src/
  SOPS.Domain/           Entities, OrderMessage, enums
  SOPS.Application/      Services, DTOs, abstractions, options
  SOPS.Infrastructure/   EF Core, Redis, Polly, HttpClient, background workers
  SOPS.Api/              Controllers, middleware, Swagger
tests/SOPS.Tests/
database/scripts/      SQL stored procedures (production)
docs/                    CACHE_AND_REST.md, THIRD_PARTY_V2.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or Azure SQL (optional for local dev — in-memory DB fallback)
- Redis (optional — uses in-memory distributed cache if empty)
- Docker (optional)

## Quick start

```bash
dotnet restore OrderingSystem.sln
dotnet run --project src/SOPS.Api
```

Open Swagger: `https://localhost:<port>/swagger` (port in `src/SOPS.Api/Properties/launchSettings.json`).

In Swagger UI, use the dropdown to switch **SOPS V1** / **SOPS V2**.

## Configuration

Edit `src/SOPS.Api/appsettings.json` or use User Secrets / Azure App Settings.

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=SOPS;Trusted_Connection=True;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  },
  "ThirdParty": {
    "ActiveVersion": "v1",
    "SopsToPartnerVersionMap": {
      "1.0": "v1",
      "2.0": "v2"
    },
    "V1": {
      "BaseUrl": "https://your-partner.com/",
      "SubmitOrderPath": "/api/v1/orders"
    },
    "V2": {
      "BaseUrl": "https://your-partner.com/",
      "SubmitOrderPath": "/api/v2/orders"
    }
  }
}
```

Leave `SqlServer` empty for in-memory database (local smoke tests only).

### Azure App Service

| Setting | Example |
|---------|---------|
| `ConnectionStrings__SqlServer` | Azure SQL connection string |
| `ConnectionStrings__Redis` | Azure Redis connection string |
| `ThirdParty__V1__BaseUrl` | Partner API base URL |
| `ThirdParty__ActiveVersion` | `v1` or `v2` |

## API versions (SOPS v1 vs v2)

Same endpoint names; version comes from the URL:

| SOPS API | Example URL |
|----------|-------------|
| v1 | `POST /api/v1/orders` |
| v2 | `POST /api/v2/orders` |

Partner integration is selected **automatically**:

- `/api/v1/...` → `ThirdPartyOrderClientV1`
- `/api/v2/...` → `ThirdPartyOrderClientV2`

No manual `?partnerVersion=v2` required. See `docs/THIRD_PARTY_V2.md`.

## Main endpoints

### Orders

| Method | v1 | v2 | Description |
|--------|----|----|-------------|
| POST | `/api/v1/orders` | `/api/v2/orders` | Create order (DB + cache + queue + optional third-party dispatch) |
| GET | `/api/v1/orders/{id}` | `/api/v2/orders/{id}` | Get order (cache-first) |
| GET | `/api/v1/orders` | `/api/v2/orders` | List orders (paginated) |
| POST | `/api/v1/orders/{id}/dispatch` | `/api/v2/orders/{id}/dispatch` | Dispatch to third-party REST API |
| POST | `/api/v1/orders/bulk` | — | Bulk ingest (NDJSON, one order per line) |
| GET | `/api/v1/orders/export` | — | Stream export (NDJSON) |

### Third-party

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/third-party/info` | Active partner version + URLs |
| GET | `/api/v1/third-party/health` | Partner health (v1 when using v1 URL) |
| POST | `/api/v1/third-party/orders/{orderId}` | Submit existing order to partner |

Use `/api/v2/third-party/...` for v2 partner client (same path names).

### Cache

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/cache/orders/{id}` | Read from cache only |
| POST | `/api/v1/cache/orders/{id}/refresh` | Reload from DB → cache |
| DELETE | `/api/v1/cache/orders/{id}` | Invalidate cache |

### System & health

| Method | Endpoint |
|--------|----------|
| GET | `/api/v1/system/status` |
| GET | `/api/v1/health` |
| GET | `/api/v1/health/ready` |

## Architecture (high level)

```
Client → Rate limiter → Controller → Application services
              ↓
         SQL (EF Core) + Redis cache
              ↓
         Channel<OrderMessage> → Background dispatch
              ↓
         HttpClient (Polly) → Third-party REST API
              ↓ (on failure)
         SQL outbox queue → drain when online
```

More detail: `docs/CACHE_AND_REST.md`.

## Database

1. Create a database on SQL Server or Azure SQL.
2. Run the API once (`EnsureCreated` creates schema) or add EF migrations.
3. Optionally run `database/scripts/001_StoredProcedures.sql` for TVP bulk insert and outbox stored procedures.

## Docker

```bash
docker compose up --build
```

API: `http://localhost:8080`

## Tests

```bash
dotnet test OrderingSystem.sln
```

## CI/CD

GitHub Actions: `.github/workflows/ci.yml` — restore, build, test; Docker image on `main`.

## Troubleshooting

| Issue | What to check |
|-------|----------------|
| **HTTP 500.30** (Azure) | Log stream; .NET 8 runtime; valid `ConnectionStrings__SqlServer`; redeploy latest build |
| Swagger shows only v1 | Select **SOPS V2** in Swagger dropdown (top right) |
| Connectivity DEGRADED | `ThirdParty:V1:BaseUrl` / health path; partner `/health` must return 2xx |
| Redis errors | Leave `Redis` empty for local in-memory cache |

## Documentation

- [Cache & REST integration](docs/CACHE_AND_REST.md)
- [Third-party v1 / v2 (automatic mapping)](docs/THIRD_PARTY_V2.md)

## License

Internal / learning project — adjust as needed for your organization.
