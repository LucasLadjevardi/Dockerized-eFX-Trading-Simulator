# Frontend Overview

The frontend is a React application built with Vite and served by Nginx.

## Main folders

```text
frontend/
|-- src/
|   |-- api/
|   |-- components/
|   |-- App.jsx
|   |-- main.jsx
|   `-- styles.css
|-- Dockerfile
|-- nginx.conf
|-- package.json
`-- vite.config.js
```

## App.jsx

`App.jsx` owns the main application state:

- active view
- live prices
- SignalR connection status
- active quote
- positions
- trades
- user messages

It also connects to SignalR and routes between the main UI views.

## API helper

`src/api/efxApi.js` centralizes frontend API calls:

```text
requestQuote
executeTrade
getPositions
getTrades
getPriceHistory
```

This keeps components from having raw `fetch` calls scattered everywhere.

## Components

Typical components:

```text
PriceBoard.jsx      -> latest bid/ask prices
PriceChart.jsx      -> price history chart with pair, view, window, and spread controls
PortfolioPnlChart.jsx -> session P&L timeline and pair P&L chart
ExposureSummaryCards.jsx -> portfolio exposure summary cards
TradeTicket.jsx     -> quote request form
QuotePanel.jsx      -> quote details and execute button
Positions.jsx       -> open positions with unrealized and realized P&L
TradeHistory.jsx    -> trade blotter with filtering, sorting, and realized P&L
```

`PriceChart.jsx` supports mid, bid/ask, and spread views with configurable history windows.

`PortfolioPnlChart.jsx` builds a client-side session P&L series from live position updates and can also show pair-level P&L bars.

`ExposureSummaryCards.jsx` summarizes open position count, gross notional, unrealized P&L, largest pair exposure, and currency exposure.

`TradeHistory.jsx` supports searching by trade metadata, filtering by pair, side, and status, and sorting key trade columns.

## Styling

Styling is centralized in:

```text
src/styles.css
```

The UI uses card-style panels, dark theme colors, styled tables, navigation buttons, chart controls, summary cards, filter controls, and P&L coloring.

## Nginx proxying

The frontend container serves the built React app and proxies requests:

```text
/api/*  -> backend API
/hubs/* -> SignalR hub
```

This lets the frontend use relative URLs like:

```js
fetch("/api/prices")
```

and:

```js
.withUrl("/hubs/prices")
```
