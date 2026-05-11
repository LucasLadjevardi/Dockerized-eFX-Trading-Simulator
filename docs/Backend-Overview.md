# Backend Overview

The backend is an ASP.NET Core Web API that exposes REST endpoints and a SignalR hub.

## Main folders

```text
backend/
|-- Controllers/
|-- Hubs/
|-- Infrastructure/
|-- Models/
|-- Services/
|-- Dockerfile
|-- EfxSimulator.Api.csproj
`-- Program.cs
```

## Controllers

Controllers are the HTTP layer. They accept requests, call services, and return responses.

Current controllers:

```text
PricesController.cs
QuotesController.cs
TradesController.cs
PositionsController.cs
```

## Services

Services contain business logic.

```text
PriceGeneratorService.cs
QuoteService.cs
ExecutionService.cs
TradeService.cs
PositionService.cs
RiskService.cs
```

### PriceGeneratorService

Runs as a background service. It generates simulated FX prices every second, stores the latest price and price history in Redis, and broadcasts live price updates with SignalR.

### QuoteService

Creates temporary executable quotes from the current bid/ask price.

Quotes expire after a short server-side lifetime and are stored in Redis with the same expiry. Quotes are tagged with a normalized portfolio ID.

### ExecutionService

Owns quote execution. It validates quote requests, acquires Redis execution locks, atomically claims quotes, runs pre-trade risk, records trades, and applies position updates.

The service uses:

- a per-portfolio, per-pair execution lock to protect position updates for the same pair
- a per-portfolio risk lock to protect risk checks that aggregate across pairs
- Redis get-and-delete semantics so each quote can only be consumed once

### TradeService

Stores executed trades in PostgreSQL and returns recent trade history scoped by portfolio.

### PositionService

Maintains net positions by currency pair and portfolio. It calculates unrealized P&L and cumulative realized P&L when trades reduce, close, or flip a position.

### RiskService

Applies rule-based pre-trade risk checks against the projected post-trade portfolio.

Current rules include:

- maximum single trade size
- stale or missing market price checks
- projected pair net exposure
- projected currency exposure in USD equivalent
- projected gross notional in USD
- projected unrealized loss limit

Risk limit defaults are configured in `appsettings.json` under `RiskLimits`. They can also be overridden with environment variables such as `RiskLimits__MaxSingleTradeSize`.

## Portfolios

Portfolio-aware endpoints accept an optional `X-Portfolio-Id` header. If it is omitted, the backend uses the `default` portfolio.

Portfolio IDs are normalized to lowercase letters, digits, hyphens, underscores, and dots. Redis position keys, execution locks, risk checks, and PostgreSQL trade queries are scoped by the normalized portfolio ID.

## Infrastructure

```text
RedisStore.cs
ITradeStore.cs
PostgresTradeStore.cs
```

`RedisStore` centralizes Redis JSON serialization, Redis key naming, list operations, quote claiming, and lock helpers.

The services depend on `IRedisStore`, which allows unit tests to use an in-memory store without requiring a Redis container.

`PostgresTradeStore` persists filled trades in PostgreSQL. It creates the `trades` table lazily on first use and reads recent trades ordered by execution time.

`TradeService` depends on `ITradeStore`, which allows unit tests to use an in-memory trade store while production uses PostgreSQL.

## Tests

Backend unit tests live in:

```text
backend.Tests/
```

The unit tests cover quote creation, validation failures, trade persistence, portfolio isolation, realized P&L, position updates, risk checks, and single-use quote execution.

The test suite also includes concurrency coverage for quote execution and position accumulation.

Redis and PostgreSQL integration tests are opt-in. To run them against local containers:

```powershell
$env:EFX_REDIS_INTEGRATION = "1"
$env:EFX_REDIS_CONNECTION_STRING = "localhost:6379"
$env:EFX_POSTGRES_INTEGRATION = "1"
$env:EFX_POSTGRES_CONNECTION_STRING = "Host=localhost;Port=5432;Database=efx_simulator;Username=efx;Password=efx_dev_password"
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet test Dockerized-eFX-Trading-Simulator.sln --no-restore
```

The integration tests use Redis database `15` by default and flush that database before and after each fixture.

The PostgreSQL integration tests delete from the `trades` table before and after the fixture.

## Hubs

```text
PriceHub.cs
```

The hub is used for SignalR streaming. The server broadcasts events to connected frontend clients.

## Models

Models define the data structures used by the simulator.

Examples:

```text
FxPrice
Quote
QuoteRequest
Trade
TradeRequest
Position
RiskResult
CurrencyPairHelper
```
