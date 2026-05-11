# Roadmap

Potential future improvements for the simulator.

## Recently completed

- Move risk limits into configuration
- Better trade blotter filtering and sorting
- Add atomic quote execution and Redis execution locks
- Add rule-based pre-trade risk checks
- Add unit tests for quote, trade, risk, and position logic
- Better chart controls
- Spread chart
- Broaden unit test coverage for edge cases and failure paths
- Add integration tests with Redis
- Add concurrency tests for quote execution and position updates
- Add durable trade storage with PostgreSQL
- Add Redis persistence configuration
- P&L chart
- Exposure summary cards
- Add Docker health checks
- Add user-specific portfolios
- Add realized P&L

## Backend improvements

- Add more complete FX P&L accounting
- Add order types beyond RFQ execution
- Add FIX message simulation
- Add latency metrics

## Frontend improvements

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
