# Troubleshooting

## Frontend does not load

Check containers:

```bash
docker compose ps
```

Check frontend logs:

```bash
docker compose logs frontend
```

Check that frontend port mapping is correct:

```yaml
ports:
  - "3000:8080"
```

Check that Nginx listens on `8080`.

## Nginx permission error

If Nginx cannot write `/run/nginx.pid`, use:

```nginx
pid /tmp/nginx.pid;
```

## React changes not showing

Rebuild frontend without cache:

```bash
docker compose build frontend --no-cache
```

Check the built bundle:

```bash
docker compose exec frontend sh -c "grep -R 'Dashboard' /usr/share/nginx/html || true"
```

If the text is not found, Docker did not build the expected source.

## Vite cannot resolve a component

Example:

```text
Could not resolve './components/PositionsTable.jsx'
```

Check file names and casing. Linux containers are case-sensitive.

```text
Positions.jsx
PositionsTable.jsx
positions.jsx
```

are different file names inside Docker.

## SignalR disconnected

Check browser console and backend logs.

Confirm the hub is mapped:

```csharp
app.MapHub<PriceHub>("/hubs/prices");
```

Confirm Nginx proxies `/hubs/`.

## Trade rejected because quote expired

Quotes expire automatically. Execute quickly or increase the quote expiry time during development.

## Trade rejected by risk

Risk checks use the projected post-trade portfolio. A trade may be rejected because of trade size, pair exposure, currency exposure, gross notional, stale market prices, or the unrealized loss limit.

If you are testing locally and want a clean state, clear Redis and clear PostgreSQL trades.

## Trade rejected because execution lock was not acquired

Execution locks are short-lived and normally release automatically. This rejection can happen if a quote expires while waiting behind another execution. Request a fresh quote and try again.

## Old data after model changes

Clear Redis:

```bash
docker compose exec redis redis-cli FLUSHALL
```

Clear PostgreSQL trades:

```bash
docker compose exec postgres psql -U efx -d efx_simulator -c "TRUNCATE TABLE trades;"
```

## P&L color not changing

If `.data-table td` overrides `.pnl-positive`, make the P&L selector more specific:

```css
.data-table td.pnl-positive {
  color: #22c55e;
}

.data-table td.pnl-negative {
  color: #ef4444;
}
```
