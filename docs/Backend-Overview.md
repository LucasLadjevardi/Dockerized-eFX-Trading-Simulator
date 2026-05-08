# Backend Overview

The backend is an ASP.NET Core Web API that exposes REST endpoints and a SignalR hub.

## Main folders

```text
backend/
├── Controllers/
├── Hubs/
├── Models/
├── Services/
├── Dockerfile
├── EfxSimulator.Api.csproj
└── Program.cs
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
TradeService.cs
PositionService.cs
RiskService.cs
```

### PriceGeneratorService

Runs as a background service. It generates simulated FX prices every second, stores the latest price and price history in Redis, and broadcasts live price updates with SignalR.

### QuoteService

Creates temporary executable quotes from the current bid/ask price.

### TradeService

Executes valid quotes, runs risk checks, stores trades, deletes used quotes, and updates positions.

### PositionService

Maintains net positions by currency pair and calculates unrealized P&L.

### RiskService

Applies simple risk rules such as maximum trade size and maximum net exposure.

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

## Empty placeholder files

If `RedisStore.cs` or `ExecutionService.cs` still exists but is empty, those files are placeholders and can be deleted unless you plan to refactor into them later.

The current app uses `IConnectionMultiplexer` directly inside services, and execution logic currently lives in `TradeService`.
