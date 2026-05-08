# Roadmap

Potential future improvements for the simulator.

## Backend improvements

- Add unit tests for quote, trade, risk, and position logic
- Add integration tests with Redis
- Add durable trade storage with PostgreSQL
- Add Redis persistence configuration
- Add Docker health checks
- Add user-specific portfolios
- Add realized P&L
- Add more complete FX P&L accounting
- Add order types beyond RFQ execution
- Add FIX message simulation
- Add latency metrics

## Frontend improvements

- Better trade blotter filtering and sorting
- Better chart controls
- Spread chart
- P&L chart
- Exposure summary cards
- Trade direction badges
- Toast notifications
- Loading and error states
- Responsive mobile layout improvements

## Trading simulation improvements

- Configurable spreads
- Configurable volatility by currency pair
- Price replay mode
- Session reset button
- Market open/close simulation
- Risk limit admin panel

## DevOps improvements

- Add CI tests
- Publish Docker images to GHCR
- Add `docker-compose.prod.yml`
- Add deployment instructions
- Add automated dependency scanning
