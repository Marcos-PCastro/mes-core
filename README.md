# MES Core

A minimal Manufacturing Execution System core: work orders with an explicit state
machine, idempotent production reporting, event-derived OEE and batch genealogy.

[![CI](https://github.com/Marcos-PCastro/mes-core/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcos-PCastro/mes-core/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<!-- TODO S12: demo.gif here — 20-30s of the dashboard moving on its own -->

## Run it in one command

```bash
git clone https://github.com/Marcos-PCastro/mes-core.git
cd mes-core
docker compose up --build
```

| What | Where |
|---|---|
| API health | http://localhost:5000/health |
| API reference | http://localhost:5000/scalar |
| Frontend | http://localhost |

<!-- TODO S4: demo credentials -->

## Why this project exists

<!-- TODO S12: the narrative — design.md §1.2, in English, no employer name -->

## What a MES does

<!-- TODO S12: the plain-language paragraph — design.md §5 -->

## Architecture

<!-- TODO S12: C4 level 2 Mermaid diagram + dependency rule -->

## The interesting parts

<!-- TODO S12: four short blocks, each linking to the code
     - Idempotent production reporting
     - OEE derived from events, never stored
     - Batch genealogy with recursive CTE
     - Optimistic concurrency with Postgres xmin -->

## Testing strategy

<!-- TODO S11/S12: the pyramid + the counterexample a property test found -->

## Decisions and tradeoffs

- [ADR-0001 — Modular monolith over microservices](docs/adr/0001-modular-monolith-over-microservices.md)
<!-- TODO: ADR-0002 (S3), 0003 (S4), 0004 (S5), 0005 (S2/S6) -->

## What's intentionally out of scope

<!-- TODO S12: the table from design.md §4.2 -->

## Tech stack

.NET 10 · ASP.NET Core Minimal API · EF Core · Dapper · PostgreSQL 17 · SignalR
React 19 · TypeScript · Vite · TanStack Query · Docker Compose · GitHub Actions