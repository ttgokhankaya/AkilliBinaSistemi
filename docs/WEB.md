# ADLE Web

A web delivery layer over the ADLE simulation core, built on the `web` branch in
parallel with the desktop WPF app. It reuses the platform-independent `net8.0`
libraries (`Adle.Analysis`, `AdleGraph`) and the Python `ml_service` unchanged —
the API is a thin HTTP surface over the same logic the desktop app and the
console benchmark use.

> This is the "as built" companion to [`WEB-PROPOSAL.md`](WEB-PROPOSAL.md) (the
> original plan). Phases P0–P3 of that proposal are implemented.

## Architecture

```
  React + Vite (adle-web)            browser SPA (TypeScript)
        │  HTTP /api  (dev: Vite proxy → :5080 · prod: same origin)
        ▼
  Adle.Api (ASP.NET Core 8)          minimal API, EF Core 8 + Npgsql
        ├── Adle.Analysis  ──────────  PredictionService (LCS + softmax)
        ├── AdleGraph      ──────────  Graph.Run (random walks)
        ├── PostgreSQL (adle_sim)      scaffolded AdleDbContext
        └── ml_service (FastAPI)       proxied: /tsne, /random-forest
```

The TypeScript types in `adle-web/src/api/schema.d.ts` are **generated** from the
API's OpenAPI document (`npm run gen:api`), so the request/response contract is
single-source: change a C# DTO → regenerate → the frontend types follow.

## Prerequisites

- Docker Desktop (PostgreSQL + `ml_service` via `docker-compose up -d`)
- .NET 8 SDK, Node.js 20+

Infra ports: PostgreSQL `localhost:5433`, `ml_service` `localhost:8000`.

## Run — development (two processes, hot reload)

```bash
# 1) API (http launch profile → http://localhost:5080)
cd Adle.Api && dotnet run

# 2) Frontend dev server (http://localhost:5173, proxies /api → :5080)
cd adle-web && npm install && npm run dev
```

Open http://localhost:5173.

## Run — production (single origin)

The frontend build outputs into `Adle.Api/wwwroot`; the API then serves the SPA
and the API from one origin (no proxy):

```bash
cd adle-web && npm run build      # → Adle.Api/wwwroot
cd ../Adle.Api && dotnet run      # SPA + API on http://localhost:5080
```

## Configuration

| Setting | Source | Default |
|---|---|---|
| DB connection | `ConnectionStrings:Adle` or env `ADLE_DB_CONNECTION` | `Host=localhost;Port=5433;Database=adle_sim;Username=adle_user;Password=Password1` |
| ml_service URL | `MlService:BaseUrl` or env `ML_SERVICE_URL` | `http://localhost:8000/` |

## API surface

| Method | Route | Backed by |
|---|---|---|
| GET | `/api/areas`, `/api/devices`, `/api/operations`, `/api/graphs` | EF Core |
| POST/PUT/DELETE | `/api/areas`, `/api/devices`, `/api/operations`, `/api/graphs` | EF Core |
| POST | `/api/graphs/{id}/run` | `AdleGraph` (random walks) |
| POST | `/api/analysis/predict` | `Adle.Analysis` (`PredictionService`) |
| POST | `/api/anomaly/tsne`, `/api/anomaly/random-forest` | proxy → `ml_service` |

Swagger UI (Development): http://localhost:5080/swagger

## Regenerate TypeScript types

With the API running (`:5080`):

```bash
cd adle-web && npm run gen:api      # → src/api/schema.d.ts
```

## Frontend layout (`adle-web/`)

```
src/
├─ api/       schema.d.ts (generated) + client.ts (typed fetch helpers)
├─ hooks/     useApi (fetch-on-mount)
├─ charts/    Plotly wrappers
└─ pages/     Areas · Devices · Operations · Graphs · Prediction · Anomaly
```

Pages are `React.lazy`-loaded so Plotly lands in its own async chunk (initial
bundle ~194 kB).

## Tests

```bash
dotnet test Adle.Api.Test/Adle.Api.Test.csproj    # API HTTP integration tests
cd adle-web && npm run test                        # React component tests (Vitest)
```

- **API** (`Adle.Api.Test`): HTTP tests over `WebApplicationFactory`. The DB is
  real (needs the PostgreSQL infra up); `ml_service` is stubbed in-process. They
  run serially (`[assembly: DoNotParallelize]`) — see the concurrency note below.
- **Frontend** (Vitest + Testing Library): component tests mock the API client
  and stub Plotly, so they need neither a backend nor a browser.

CI (`.github/workflows/ci.yml`, `web` job) runs both: it spins up a Postgres
service, loads `db/schema.sql` + `db/seed-ci.sql`, runs the API suite, then the
frontend tests + build + Docker image build.

## Known limitations

- **Single-user assumption.** `AdleGraph`'s graph rebuild resolves types through
  a static IoC container that is not safe for *concurrent* graph builds, so
  parallel `/api/graphs/{id}/run` calls can fail. Fine for the single-user
  research app; the integration tests run serially for the same reason.
- **No auth / multi-tenancy** (out of scope, as in the proposal).
- **Graph runs are non-deterministic** (`Random.Shared`, no seed).
- The web stack lives only on the `web` branch; `master` remains the desktop +
  core + benchmark track. Core changes flow `master → web` via merge.
