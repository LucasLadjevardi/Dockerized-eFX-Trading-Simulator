# SignalR Streaming

SignalR is used to push live updates from the backend to the frontend.

## Hub

The hub endpoint is:

```text
/hubs/prices
```

The frontend connects using:

```js
new signalR.HubConnectionBuilder()
  .withUrl('/hubs/prices')
  .withAutomaticReconnect()
  .build();
```

## Events

### pricesUpdated

Broadcast when the backend generates new prices.

```text
pricesUpdated
```

Payload shape:

```json
{
  "EURUSD": {
    "pair": "EURUSD",
    "bid": 1.085,
    "ask": 1.0852,
    "mid": 1.0851,
    "spread": 0.0002,
    "timestampUtc": "2026-05-08T12:00:00Z"
  }
}
```

### positionsUpdated

Broadcast when positions exist and are recalculated against latest prices.

```text
positionsUpdated
```

This avoids frontend polling for positions every second.

## Nginx proxying

Nginx forwards `/hubs/` traffic to the backend:

```nginx
location /hubs/ {
    proxy_pass http://backend:8080/hubs/;
    proxy_http_version 1.1;

    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";

    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
}
```

## Common issues

### 404 on `/hubs/prices`

Check that the backend maps the hub:

```csharp
app.MapHub<PriceHub>("/hubs/prices");
```

### Negotiation succeeds but WebSocket fails

Check the Nginx WebSocket headers.

### Browser shows disconnected

Check backend logs and browser console.
