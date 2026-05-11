# Trading Concepts

This page explains the FX concepts used in the simulator.

## Currency pair

An FX pair is made of two currencies.

Example:

```text
EURUSD = EUR / USD
```

The first currency is the **base currency**. The second currency is the **quote currency**.

```text
EURUSD
Base currency  = EUR
Quote currency = USD
```

## Base amount

The simulator treats order amount as the base currency amount.

Example:

```text
BUY 1,000,000 EURUSD
```

means:

```text
buy 1,000,000 EUR
sell USD equivalent
```

If the price is `1.0850`, the quote amount is:

```text
1,000,000 x 1.0850 = 1,085,000 USD
```

## Bid and ask

Each generated price has a bid and an ask.

```text
Bid = price you can sell at
Ask = price you can buy at
```

Example:

```text
EURUSD bid = 1.08500
EURUSD ask = 1.08520
```

If you buy, the simulator uses the ask price.

If you sell, the simulator uses the bid price.

## Spread

Spread is the difference between ask and bid.

```text
Spread = Ask - Bid
```

Example:

```text
1.08520 - 1.08500 = 0.00020
```

For most non-JPY pairs, `0.0001` is one pip, so a spread of `0.00020` is two pips.

For JPY pairs, `0.01` is typically one pip.

## Quotes

A quote is a temporary executable price.

The simulator stores quotes in Redis with an expiry time. If the user tries to execute after expiry, the trade is rejected.

Quote execution is single-use. The backend atomically claims a quote before filling a trade, so the same quote cannot be executed twice.

## Trades

A trade is created when a quote is executed successfully.

The simulator records:

- trade ID
- quote ID
- pair
- side
- base currency
- quote currency
- base amount
- quote amount
- execution price
- execution timestamp

## Positions

A position tracks net exposure for a pair.

```text
BUY EURUSD  -> positive EUR base position
SELL EURUSD -> negative EUR base position
```

Positive net base amount means long.

Negative net base amount means short.

## Unrealized P&L

The simulator uses simplified unrealized P&L:

```text
Unrealized P&L = Net Base Amount x (Current Price - Average Price)
```

P&L is shown in the quote currency.

Example:

```text
EURUSD P&L is shown in USD
USDJPY P&L is shown in JPY
EURGBP P&L is shown in GBP
```

## Shorting

The simulator allows selling even if the current position is zero. This creates a short position, which is common in institutional FX workflows.

If you want a wallet-style simulator, add a risk rule that rejects sells larger than the current long balance.

## Pre-trade risk

Before a quote becomes a trade, the simulator evaluates pre-trade risk against the projected post-trade portfolio.

The current rule set checks:

- single trade size
- stale or missing market prices
- net exposure by currency pair
- currency exposure converted to USD equivalent
- gross notional exposure in USD
- unrealized loss limit

The simulator also uses execution locks so risk checks and position updates are evaluated against a consistent state.
