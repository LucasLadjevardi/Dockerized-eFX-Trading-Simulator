# Frontend Overview

The frontend is a React application built with Vite and served by Nginx.

## Main folders

```text
frontend/
├── src/
│   ├── api/
│   ├── components/
│   ├── App.jsx
│   ├── main.jsx
│   └── styles.css
├── Dockerfile
├── nginx.conf
├── package.json
└── vite.config.js
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
PriceChart.jsx      -> price history chart
TradeTicket.jsx     -> quote request form
QuotePanel.jsx      -> quote details and execute button
Positions.jsx       -> open positions and P&L
TradeHistory.jsx    -> recent executed trades
```

## Styling

Styling is centralized in:

```text
src/styles.css
```

The UI uses card-style panels, dark theme colors, styled tables, navigation buttons, and P&L coloring.

## Nginx proxying

The frontend container serves the built React app and proxies requests:

```text
/api/*  -> backend API
/hubs/* -> SignalR hub
```

This lets the frontend use relative URLs like:

```js
fetch('/api/prices')
```

and:

```js
.withUrl('/hubs/prices')
```
