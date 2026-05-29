# Automatic API version → partner version

You do **not** pass `v2` or `partnerVersion` manually. The URL picks the integration:

| Client calls | Partner client used |
|--------------|---------------------|
| `POST /api/v1/orders` | `ThirdPartyOrderClientV1` |
| `POST /api/v2/orders` | `ThirdPartyOrderClientV2` |
| `POST /api/v1/third-party/orders/{id}` | partner v1 |
| `POST /api/v2/third-party/orders/{id}` | partner v2 |

## How it works

1. `ApiVersionPartnerMappingMiddleware` reads SOPS API version from the request (`1.0`, `2.0`).
2. `ThirdPartyOptions.SopsToPartnerVersionMap` maps `1.0` → `v1`, `2.0` → `v2`.
3. `ThirdPartyOrderClientFactory` uses that per request.
4. Background jobs (no HTTP) use `ThirdParty:ActiveVersion` from config.

## Add v3 later

1. Implement `ThirdPartyOrderClientV3`.
2. Register HttpClient in `ThirdPartyHttpClientExtensions`.
3. Extend factory `GetClient()` for `v3`.
4. Add to config:

```json
"SopsToPartnerVersionMap": {
  "1.0": "v1",
  "2.0": "v2",
  "3.0": "v3"
}
```

5. Add `OrdersV3Controller` with `[ApiVersion("3.0")]` — same route template.

## Controllers

- `OrdersController` — `[ApiVersion("1.0")]` — legacy behavior unchanged.
- `OrdersV2Controller` — `[ApiVersion("2.0")]` — same endpoint names, new logic + partner v2.

No `/third-party/v1/...` or `/third-party/v2/...` paths required.
