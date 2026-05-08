# Architecture

The simulator is split into three Docker services:

```text
frontend  -> React app served by Nginx
backend   -> ASP.NET Core API and SignalR hub
redis     -> in-memory state store
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
    |-- TradeService
    |-- PositionService
    |-- RiskService
    |-- PriceHub
    v
Redis
```

## Frontend responsibilities

The frontend:

- displays live FX prices
- displays price charts
- allows users to request quotes
- allows users to execute quotes
- displays positions and unrealized P&L
- displays trade history
- connects to SignalR for live updates

## Backend responsibilities

The backend:

- generates simulated FX prices
- stores latest prices and price history in Redis
- creates temporary executable quotes
- validates quote execution
- applies risk checks
- records executed trades
- updates positions
- streams live prices and positions using SignalR

## Redis responsibilities

Redis stores fast-moving simulator state:

- latest prices
- price history
- temporary quotes with expiry
- trades
- trade ID list
- positions

Redis is being used as a simple in-memory store for the MVP. In a production-style system, trades would normally also be written to durable storage.

## Docker networking

Docker Compose creates an internal network where services can reference each other by service name.

For example, the backend connects to Redis using:

```text
redis:6379
```

Nginx proxies API calls to:

```text
backend:8080
```
