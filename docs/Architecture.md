# Architecture

The simulator is split into four Docker services:

```text
frontend  -> React app served by Nginx
backend   -> ASP.NET Core API and SignalR hub
redis     -> in-memory state store
postgres  -> durable trade store
```

## High-level flow

```text
React Frontend
    |
    | REST: quotes, trades, positions, price history
    | SignalR: live prices and position updates
    v
Nginx Frontend Container
    |
    | /api/*   -> backend:8080/api/*
    | /hubs/*  -> backend:8080/hubs/*
    v
ASP.NET Core Backend
    |
    |-- PriceGeneratorService
    |-- QuoteService
    |-- ExecutionService
    |-- TradeService
    |-- PositionService
    |-- RiskService
    |-- PriceHub
    |
    |-- Redis: prices, quotes, positions, locks
    `-- PostgreSQL: durable trade history
```

## Frontend responsibilities

The frontend:

- displays live FX prices
- displays price charts
- allows users to request quotes
- allows users to execute quotes
- displays positions, unrealized P&L, and realized P&L
- displays trade history
- connects to SignalR for live updates

## Backend responsibilities

The backend:

- generates simulated FX prices
- stores latest prices and price history in Redis
- creates temporary executable quotes
- atomically claims executable quotes
- applies pre-trade risk checks
- records executed trades in PostgreSQL
- updates portfolio-scoped positions
- streams live prices and positions using SignalR

## Execution flow

Quote execution is handled by `ExecutionService`.

When a user executes a quote, the backend:

- reads the quote to determine its currency pair
- acquires a Redis lock for that portfolio and pair
- acquires a portfolio-level Redis lock for cross-pair risk checks
- atomically claims the quote using Redis get-and-delete semantics
- rejects the request if the quote is expired, missing, or already used
- evaluates pre-trade risk rules against the projected portfolio
- applies the position update and calculates realized P&L
- records the trade

The per-portfolio, per-pair lock prevents lost updates for the same pair within a portfolio. The portfolio lock protects risk checks that aggregate across pairs and currencies.

## Redis responsibilities

Redis stores fast-moving simulator state:

- latest prices
- price history
- temporary quotes with expiry
- positions
- short-lived execution locks

Redis uses append-only persistence in Docker Compose so simulator state can survive container restarts.

## PostgreSQL responsibilities

PostgreSQL stores durable trade history:

- trade IDs
- portfolio IDs
- quote IDs
- pair and side
- base and quote currencies
- base and quote amounts
- execution price
- realized P&L
- status
- execution timestamp

## Docker health checks

Docker health checks are configured for all runtime services:

- frontend checks the Nginx HTTP endpoint
- backend checks `/health`
- Redis uses `redis-cli ping`
- PostgreSQL uses `pg_isready`

The frontend waits for a healthy backend, and the backend waits for healthy Redis and PostgreSQL services.

## Docker networking

Docker Compose creates an internal network where services can reference each other by service name.

For example, the backend connects to Redis using:

```text
redis:6379
```

and to PostgreSQL using:

```text
postgres:5432
```

Nginx proxies API calls to:

```text
backend:8080
```
