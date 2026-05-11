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

Quotes expire after a short server-side lifetime and are stored in Redis with the same expiry.

### ExecutionService

Owns quote execution. It validates quote requests, acquires Redis execution locks, atomically claims quotes, runs pre-trade risk, records trades, and applies position updates.

The service uses:

- a per-pair execution lock to protect position updates for the same pair
- a portfolio risk lock to protect risk checks that aggregate across pairs
- Redis get-and-delete semantics so each quote can only be consumed once

### TradeService

Stores executed trades and returns recent trade history.

### PositionService

Maintains net positions by currency pair and calculates unrealized P&L.

### RiskService

Applies rule-based pre-trade risk checks against the projected post-trade portfolio.

Current rules include:

- maximum single trade size
- stale or missing market price checks
- projected pair net exposure
- projected currency exposure in USD equivalent
- projected gross notional in USD
- projected unrealized loss limit

## Infrastructure

```text
RedisStore.cs
```

`RedisStore` centralizes Redis JSON serialization, Redis key naming, list operations, quote claiming, and lock helpers.

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
