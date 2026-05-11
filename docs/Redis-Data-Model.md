# Redis Data Model

Redis stores the simulator's fast-moving state.

## Key overview

```text
price:{pair}
pricehistory:{pair}
quote:{quoteId}
trade:{tradeId}
trades
position:{pair}
lock:execution:pair:{pair}
lock:execution:portfolio-risk
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

Once a quote expires, execution returns a rejection.

When a quote is executed, the backend claims it with Redis get-and-delete semantics. This makes execution single-use even if two requests submit the same quote at nearly the same time.

## Trades

```text
trade:{tradeId}
trades
```

`trade:{tradeId}` stores the serialized trade.

`trades` is a Redis list of recent trade IDs.

## Positions

```text
position:{pair}
```

Positions store:

- pair
- base currency
- quote currency
- net base amount
- average price
- current price
- unrealized P&L
- P&L currency
- update timestamp

## Execution locks

```text
lock:execution:pair:{pair}
lock:execution:portfolio-risk
```

The backend uses short-lived Redis locks during execution.

The pair lock protects position updates for one currency pair. The portfolio risk lock protects risk rules that aggregate exposure across pairs and currencies.

Locks are released after execution completes and also have an expiry so they do not remain forever if the backend stops mid-request.

## Resetting Redis

To clear all simulator state:

```bash
docker compose exec redis redis-cli FLUSHALL
```

This is useful after model changes, because older JSON may not match newer C# models.
