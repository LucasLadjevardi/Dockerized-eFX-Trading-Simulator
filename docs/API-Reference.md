# API Reference

The backend exposes REST endpoints for prices, quotes, trades, and positions.

Base URL when running locally:

```text
http://localhost:8080
```

When calling from the frontend, use relative URLs through Nginx:

```text
/api/...
```

Portfolio-scoped endpoints accept an optional header:

```http
X-Portfolio-Id: alpha
```

If the header is omitted, the backend uses the `default` portfolio.

## Health

```http
GET /health
```

Returns backend, Redis, and PostgreSQL connectivity status.

## Prices

```http
GET /api/prices
```

Returns the latest prices for supported pairs.

```http
GET /api/prices/history/{pair}
```

Returns recent price history for charting.

Example:

```http
GET /api/prices/history/EURUSD
```

## Quotes

```http
POST /api/quotes
```

Creates a temporary quote for the requested portfolio.

Request:

```json
{
  "pair": "EURUSD",
  "side": "BUY",
  "amount": 1000000
}
```

Notes:

- `pair` is a six-letter FX pair symbol.
- `side` must be `BUY` or `SELL`.
- `amount` is interpreted as base currency amount.
- `BUY` quotes use ask price.
- `SELL` quotes use bid price.
- Quotes expire quickly and must be executed before `expiresAtUtc`.

Example response:

```json
{
  "quoteId": "q-example",
  "portfolioId": "default",
  "pair": "EURUSD",
  "side": "BUY",
  "amount": 1000000,
  "price": 1.0852,
  "createdAtUtc": "2026-05-08T12:00:00Z",
  "expiresAtUtc": "2026-05-08T12:00:05Z"
}
```

## Trades

```http
POST /api/trades
```

Executes a quote.

Request:

```json
{
  "quoteId": "q-example"
}
```

Possible outcomes:

- filled trade
- rejected because the quote expired, was already used, or does not exist
- rejected because an execution lock could not be acquired before quote expiry
- rejected by pre-trade risk rules

Quote execution is single-use. The backend atomically claims the quote before filling a trade, so retrying the same `quoteId` returns a rejection after the first successful execution.

The `X-Portfolio-Id` header must match the portfolio that created the quote.

```http
GET /api/trades
```

Returns recent trades from PostgreSQL for the requested portfolio.

Filled trades include `portfolioId` and `realizedPnl`.

## Positions

```http
GET /api/positions
```

Returns all open positions for the requested portfolio.

Positions include unrealized and cumulative realized P&L.

```http
GET /api/positions/{pair}
```

Returns the position for a specific pair in the requested portfolio.

Example:

```http
GET /api/positions/EURUSD
```

## Swagger

Swagger UI is available at:

```text
http://localhost:8080/swagger
```
