# Getting Started

This page explains how to run the simulator locally using Docker Compose.

## Prerequisites

Install:

- Docker
- Docker Compose

You do not need to install .NET, Node.js, Nginx, or Redis locally when using Docker.

## Clone the repository

```bash
git clone <repository-url>
cd Dockerized-eFX-Trading-Simulator
```

## Start the application

```bash
docker compose up --build
```

The first run may take a few minutes because Docker needs to build the frontend and backend images.

## Open the app

```text
Frontend: http://localhost:3000
Backend:  http://localhost:8080
Swagger:  http://localhost:8080/swagger
Health:   http://localhost:8080/health
```

## Stop the app

```bash
docker compose down
```

## Rebuild commands

Rebuild all services:

```bash
docker compose build --no-cache
```

Rebuild frontend only:

```bash
docker compose build frontend --no-cache
```

Rebuild backend only:

```bash
docker compose build backend --no-cache
```

## Clear Redis data

Redis stores quotes, trades, positions, prices, and price history. To reset the simulator state:

```bash
docker compose exec redis redis-cli FLUSHALL
```

## Useful logs

```bash
docker compose logs frontend
docker compose logs backend
docker compose logs redis
```
