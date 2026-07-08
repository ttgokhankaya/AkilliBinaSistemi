# ADLE Web — Migration Proposal

Proposal for moving the ADLE simulation platform from a Windows WPF desktop app
to a web application, reusing the platform-independent core that already exists.

## 1. Guiding principle

**The web app is a thin delivery layer over the shared core.** The desktop
migration work already extracted the domain logic into platform-independent
`net8.0` libraries, so the web project does not re-implement anything — it
references the same assemblies and exposes them over HTTP.

```
                 ┌────────────────────────────────────────────┐
                 │  Adle.Analysis (net8.0)  — SequenceAnalyzer, │
                 │      LCS, Similarity, Softmax/MinMax          │
   reused today  │  AdleGraph (net8.0)      — graph engine, LCS  │
                 │  ml_service (FastAPI)    — t-SNE, RandomForest│
                 └────────────────────────────────────────────┘
                        ▲                         ▲
        references      │                         │  HTTP (already web-native)
                        │                         │
   ┌────────────────────┴─────────┐     ┌─────────┴───────────┐
   │  Adle.Api (ASP.NET Core)     │────▶│  PostgreSQL (adle_sim)│
   │  REST/JSON, DI, EF Core      │     └──────────────────────┘
   └────────────────────┬─────────┘
                        │  HTTP/JSON
              ┌─────────┴──────────┐
              │  Adle.Web (Blazor) │   ← browser UI
              └────────────────────┘
```

## 2. Recommended stack

| Layer | Choice | Rationale |
|---|---|---|
| Backend API | **ASP.NET Core 8 Web API** | References `Adle.Analysis` + `AdleGraph` directly; first-class DI, config, HTTP client for the ML service |
| Data access | **EF Core 8 + Npgsql** (scaffolded from `db/schema.sql`) | Permanently retires the broken EF6 migration story; native to ASP.NET Core DI; proper migrations going forward |
| Frontend | **Blazor** (WASM or Server) | All-C# stack, matches your skill set; shares DTOs with the backend |
| Charts / viz | Plotly.js via JS interop (or ECharts) | t-SNE scatter + decision-tree/graph views need a real charting lib; **this is the one place Blazor costs more than React** |
| ML | existing **`ml_service`** unchanged | already an HTTP service; the API proxies `/tsne` and `/random-forest` |

**Alternative to flag:** if the visualization ends up dominating the effort
(interactive graph editing, large scatter plots), a **React + TypeScript**
frontend has a richer charting/graph ecosystem. Keep the backend identical;
only the FE choice changes. Decide after the first viz spike.

### EF Core vs. EF6

Do **not** carry EF6 into the web backend. The entities are simple POCOs and
`db/schema.sql` already defines the schema, so:

```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5433;Database=adle_sim;Username=adle_user;Password=Password1" \
  Npgsql.EntityFrameworkCore.PostgreSQL -o Data --context AdleDbContext
```

gives a clean EF Core context in minutes and ends the `dbo`/`public`/snapshot
saga for good. The desktop app keeps using EF6 + `schema.sql` until (if) it is
retired.

## 3. Proposed solution layout (new `web` branch / worktree)

```
Adle.Web.sln
├─ Adle.Api/            ASP.NET Core Web API
│  ├─ Data/             EF Core AdleDbContext (scaffolded)
│  ├─ Endpoints/        Areas, Devices, Operations, Graph, Analysis, Anomaly
│  ├─ MlClient/         typed HttpClient for ml_service (reuse existing DTOs)
│  └─ Program.cs
├─ Adle.Web/            Blazor frontend
├─ Adle.Contracts/      shared request/response DTOs (referenced by API + Web)
└─ (references ../AdleGraph, ../Adle.Analysis via ProjectReference or NuGet)
```

The web solution lives on a separate `web` branch so it evolves in parallel
without destabilising `master`. The shared core libraries are referenced by
relative `ProjectReference` (or packed as local NuGet once stable).

## 4. API surface (maps 1:1 to current WPF features)

| WPF feature | Endpoint(s) | Backed by |
|---|---|---|
| Area / Item management | `GET/POST /api/areas`, `/api/items` | EF Core (DatabaseMigration tables) |
| Device / Actor / Operation | `GET/POST /api/devices`, `/api/operations` | EF Core (SimulationDB tables) |
| Graph build + run (data gen) | `POST /api/graphs`, `POST /api/graphs/{id}/run` | `AdleGraph` |
| Sequence analysis / next-step + person prediction | `POST /api/analysis/predict` | `Adle.Analysis` (SequenceAnalyzer, LCS, Softmax) |
| Anomaly: t-SNE | `POST /api/anomaly/tsne` | proxy → `ml_service /tsne` |
| Anomaly: Random Forest proximity | `POST /api/anomaly/random-forest` | proxy → `ml_service /random-forest` |

## 5. Prerequisite on `master` (I do this next, on the benchmark track)

The WPF code-behind (`GUI_Simulation/**/*.xaml.cs`, e.g.
`MainWindowSequentialPattern.xaml.cs`) still holds orchestration logic —
building the analyzer, running predictions, scoring. Before the API can reuse
it cleanly, that logic must move into `Adle.Analysis` as plain services (e.g.
`PredictionService`, `SimulationRunner`). **This same extraction is exactly what
the benchmark/console project needs**, so the two tracks converge: the console
benchmark and the web API both call the same new services.

## 6. Phased plan

- **P0 – Spike (½ day):** scaffold `Adle.Api`, wire EF Core from `schema.sql`,
  one real endpoint (`GET /api/areas`) returning seeded data. Proves the core
  reuse + DB path end to end.
- **P1 – Read-only API + Blazor shell:** list areas/devices/operations; Blazor
  pages rendering them. One viz spike (t-SNE scatter via Plotly interop) to
  validate the FE charting decision.
- **P2 – Analysis endpoints:** `/api/analysis/predict` + `/api/anomaly/*` over
  the extracted services. Feature parity with the WPF analysis screens.
- **P3 – Write paths + graph editing:** create/run graphs, manage entities.

## 7. Parallel-work workflow (worktree)

```bash
# from the main repo (c:\git\gk\AkilliBinaSistemi)
git worktree add ../AkilliBinaSistemi-web -b web      # new branch + checkout
cd ../AkilliBinaSistemi-web
# scaffold Adle.Web.sln here; commit on `web`

# keep core changes flowing from master:
git merge master           # or rebase, when master gains the extracted services
```

`master` stays the desktop + core + benchmark track; `web` stays the web track;
the shared `net8.0` libraries are the contract between them.

## 8. Out of scope for now

- Auth/multi-tenant (single-user research app today).
- Rewriting the ML service (already web-native).
- Real-time push (SignalR) — revisit only if live simulation streaming is wanted.
