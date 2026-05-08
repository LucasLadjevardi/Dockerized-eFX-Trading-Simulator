# Dockerized eFX Trading Simulator Wiki

Welcome to the project documentation for the **Dockerized eFX Trading Simulator**.

This project is a Dockerized electronic foreign exchange trading simulator built with **ASP.NET Core**, **React**, **SignalR**, **Redis**, **Nginx**, and **Docker Compose**.

It simulates a simplified eFX workflow:

```text
simulated market prices
  -> live price streaming
  -> quote request
  -> quote expiry
  -> trade execution
  -> risk checks
  -> position updates
  -> unrealized P&L
```

The project is intended for learning, portfolio demonstration, and understanding how trading-style applications can be structured.

## Documentation pages

- [Getting Started](./Getting-Started.md)
- [Architecture](./Architecture.md)
- [Backend Overview](./Backend-Overview.md)
- [Frontend Overview](./Frontend-Overview.md)
- [Trading Concepts](./Trading-Concepts.md)
- [API Reference](./API-Reference.md)
- [Redis Data Model](./Redis-Data-Model.md)
- [SignalR Streaming](./SignalR-Streaming.md)
- [CI/CD](./CI-CD.md)
- [Troubleshooting](./Troubleshooting.md)
- [Roadmap](./Roadmap.md)

## What this project is not

This is not a production trading system. It does not connect to real markets, does not execute real trades, and should not be used for financial decisions.
