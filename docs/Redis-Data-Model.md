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

Quotes are stored with Redis expiry.

Once a quote expires, execution should return a rejection.

Once a quote is executed, the backend deletes the quote key to prevent double execution.

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

## Resetting Redis

To clear all simulator state:

```bash
docker compose exec redis redis-cli FLUSHALL
```

This is useful after model changes, because older JSON may not match newer C# models.
