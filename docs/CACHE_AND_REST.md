# Cache & Third-Party REST — How SOPS Works

## API controllers (all features exposed via REST)

| Controller | Endpoint | What it does |
|------------|----------|--------------|
| **OrdersController** | `POST /api/v1/orders?dispatchToThirdParty=true` | Save SQL → **write cache** → queue → optional **HttpClient POST** to third-party |
| | `GET /api/v1/orders/{id}` | Get order (`dataSource`: cache or database) |
| | `POST /api/v1/orders/{id}/dispatch` | Dispatch existing order via third-party REST |
| **ThirdPartyController** | `GET /api/v1/third-party/info` | Active version (v1/v2) + configured URLs |
| | `GET /api/v1/third-party/health?partnerVersion=v2` | HttpClient GET `/health` |
| | `POST /api/v1/third-party/orders/{orderId}` | Uses `ThirdParty:ActiveVersion` |
| | `POST /api/v1/third-party/v1/orders/{orderId}` | Force partner API v1 |
| | `POST /api/v1/third-party/v2/orders/{orderId}` | Force partner API v2 |
| **CacheController** | `GET /api/v1/cache/orders/{id}` | Read cache only (L1/L2) |
| | `POST /api/v1/cache/orders/{id}/refresh` | Reload from DB → store in cache |
| | `DELETE /api/v1/cache/orders/{id}` | Invalidate cache |
| **SystemController** | `GET /api/v1/system/status` | Connectivity + outbox pending count |
| **BulkOrdersController** | `POST /api/v1/orders/bulk` | Bulk ingest |
| **OrdersExportController** | `GET /api/v1/orders/export` | Stream export (NDJSON) |

Application orchestration: `OrderWorkflowService`, `ThirdPartyIntegrationService` (used by controllers).  
Infrastructure: `OrderCacheService`, `ThirdPartyOrderClient` (HttpClient implementation).

## Cache (write on save, read on GET)

SOPS uses **cache-aside** with two tiers:

| Tier | Technology | TTL |
|------|------------|-----|
| L1 | `IMemoryCache` | 2 min (single order) |
| L2 | Redis `IDistributedCache` | 2 min (order) / 15 min (list) |

### Flow

```
POST /orders
  → Save to SQL
  → cache.SetOrderAsync(dto)     ← WRITE cache immediately
  → cache.InvalidateListAsync()  ← list pages are stale

GET /orders/{id}
  → cache.GetOrderAsync(id)
       → L1 hit? return
       → L2 hit? promote to L1, return
       → else load SQL → SetOrderAsync → return
```

Code: `OrderService.SubmitAsync` calls `SetOrderAsync` after `repo.AddAsync`.  
Implementation: `Infrastructure/Caching/OrderCacheService.cs`.

---

## Third-party integration (HttpClient + REST)

External fulfilment is a **separate REST API**. SOPS calls it with a typed **`HttpClient`** (not EF, not raw sockets).

| HTTP | Route | Purpose |
|------|-------|---------|
| `GET` | `/health` | Connectivity monitor |
| `POST` | `/api/v1/orders` | Submit order JSON body |

### Configuration

```json
"ThirdParty": {
  "BaseUrl": "https://your-partner-api.com/"
}
```

`HttpClient.BaseAddress` = `BaseUrl`. Paths = `ThirdPartyApiRoutes` (Application layer).

### Request / response contracts

- `ThirdPartyOrderRequest` — JSON POST body (`OrderId`, `CustomerId`, `TotalAmount`)
- `ThirdPartyOrderResponse` — JSON response (`Status`, `ExternalReference`)

### Code map

| Piece | File |
|-------|------|
| REST paths | `Application/External/ThirdPartyApiRoutes.cs` |
| Contracts | `Application/External/ThirdPartyOrderRequest.cs` |
| Interface | `Application/Abstractions/IThirdPartyOrderClient.cs` |
| HttpClient impl | `Infrastructure/External/ThirdPartyOrderClient.cs` |
| Polly retry/CB | `Infrastructure/DependencyInjection.cs` (`AddHttpClient` + `AddResilienceHandler`) |
| Background dispatch | `Infrastructure/Background/ThirdPartyDispatchService.cs` |

### Dispatch pipeline

```
Order saved → Channel<OrderMessage>
  → ThirdPartyDispatchService (background)
  → IThirdPartyOrderClient.SubmitOrderAsync()
  → HttpClient POST {BaseUrl}/api/v1/orders
  → On failure → outbox queue + Polly retries
```
