# Redis Data Model

Redis stores the simulator's fast-moving state.

## Key overview

```text
price:{pair}
pricehistory:{pair}
quote:{quoteId}
portfolio:{portfolioId}:position:{pair}
lock:execution:portfolio:{portfolioId}:pair:{pair}
lock:execution:portfolio:{portfolioId}:risk
```

## Latest prices

```text
price:EURUSD
price:GBPUSD
price:USDJPY
price:EURGBP
```

Each key stores the latest serialized `FxPrice` object for the pair.

## Price history

```text
pricehistory:EURUSD
```

Price history is stored as a Redis list. The price generator appends new ticks and trims the list to keep only recent items.

Used by frontend charts.

## Quotes

```text
quote:{quoteId}
```

Quotes are stored with Redis expiry. The Redis TTL matches the quote's `expiresAtUtc` window.

Each quote includes the normalized portfolio ID that requested it.

Once a quote expires, execution returns a rejection.

When a quote is executed, the backend claims it with Redis get-and-delete semantics. This makes execution single-use even if two requests submit the same quote at nearly the same time.

## Positions

```text
portfolio:{portfolioId}:position:{pair}
```

Positions store:

- pair
- portfolio ID
- base currency
- quote currency
- net base amount
- average price
- current price
- unrealized P&L
- realized P&L
- P&L currency
- update timestamp

## Execution locks

```text
lock:execution:portfolio:{portfolioId}:pair:{pair}
lock:execution:portfolio:{portfolioId}:risk
```

The backend uses short-lived Redis locks during execution.

The pair lock protects position updates for one currency pair within one portfolio. The portfolio risk lock protects risk rules that aggregate exposure across pairs and currencies within that portfolio.

Locks are released after execution completes and also have an expiry so they do not remain forever if the backend stops mid-request.

## Persistence

Docker Compose starts Redis with append-only persistence:

```text
redis-server --appendonly yes --appendfsync everysec
```

Redis data is stored in the `redis_data` Docker volume.

Trades are not stored in Redis. Filled trades are stored durably in PostgreSQL.

## Resetting Redis

To clear all simulator state:

```bash
docker compose exec redis redis-cli FLUSHALL
```

This is useful after model changes, because older JSON may not match newer C# models.
