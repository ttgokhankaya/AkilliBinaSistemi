# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**Akilli Bina Sistemi (ADLE)** — Activity Driven Life Environments. A C# .NET 8 WPF smart building simulation platform with a Python ML microservice. The app simulates resident behavior, device automation, and anomaly detection in smart homes.

## Prerequisites & Quick Start

- Docker Desktop, Visual Studio 2022+, .NET 8 SDK

```bash
# Start infrastructure (PostgreSQL + ML service)
docker-compose up -d

# Run app: launch GUI_Simulation project in Visual Studio
# Migrations and seed data run automatically on first startup
```

## Build & Test Commands

```powershell
# Build entire solution
dotnet build AkilliBinaSistemi.sln

# Run all tests
dotnet test

# Run a single test project
dotnet test GUI.Test/GUI.Test.csproj
dotnet test FakeDataProvider.Test/FakeDataProvider.Test.csproj
dotnet test FakeDevices.Test/SimulationObjects.Test.csproj  # note: project name differs from folder name

# Run a single test class
dotnet test --filter "FullyQualifiedName~IsikTest"

# ML service (local, without Docker)
cd ml_service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

## Manual Database Migrations (if auto-migration fails)

In Visual Studio Package Manager Console:

```powershell
# ADLE database (Default Project: DatabaseMigration)
Update-Database

# Simulation database (Default Project: SimulationDB_Migrations)
Update-Database
```

## Architecture

### Solution Structure

The solution has two main entry-point applications:

- **`GUI_Simulation`** — Primary app. Anomaly analysis, t-SNE visualization, Random Forest analysis. Entry window: `SimulationPortal.PortalWindow`. Runs migrations for both DBs on startup.
- **`GUI`** — Secondary smart building control UI.

Naming quirks to be aware of:

- Folder `FakeDevices/` contains project **`SimulationObjects`**; folder `FakeDevices.Test/` contains project **`SimulationObjects.Test`**.
- Domain code mixes Turkish and English names (`Isik` = light, `Oturma Odası` = living room, `FeildModel` is an intentional/legacy spelling). Match the convention of the file you edit; don't rename existing members.

### Two Separate Database Contexts

Two EF6 stacks target the **same** PostgreSQL database (`adle_sim`), but they are migrated **differently** at startup (`GUI_Simulation/App.xaml.cs` → `RunMigrations`):

- **`DatabaseMigration`** → `DB.cs` — owns `Areas`, `AreaTypes`, `Items`, `Memories`. Applied via `DbMigrator` (real EF6 migrations; upgrade path works on existing DBs).
- **`SimulationDB_Migrations`** → `DB.cs` — owns `Devices`, `Actors`, `Operations`. Applied via `Database.CreateIfNotExists()` + `Configuration.SeedData(db)` — **schema changes do NOT auto-apply to an existing database**; they require manual `Update-Database` or a recreated DB (`docker-compose down -v`, destroys data).

Rules:

- Never add an entity or FK that crosses the two stacks; cross-boundary references stay as plain ID columns.
- Migration failures at startup show a MessageBox warning and the app **continues running** — a half-migrated DB can therefore go unnoticed.
- Connection strings are hardcoded as `const ConnStr` in each stack's `DB.cs` (not in App.config). `DataAccess/UnitOfWorkFactory.cs` also contains a legacy SQL Server connection string — the PostgreSQL path is the live one.
- Npgsql is wired via `DbConfiguration.SetConfiguration(new DatabaseMigration.NpgsqlDbConfiguration())` in `App`'s static constructor.

First run seeds sample data: area types (LivingRoom, Bedroom, Kitchen…), areas (Ev, Oturma Odası…), devices (Lamba, Termostat, Hareket Sensörü, Akıllı Priz at 192.168.1.x), two residents, and morning/noon/evening/night operation routines.

### IoC / Dependency Injection

A custom IoC container lives in `IoC/Container.cs` (singleton, service-locator pattern). Startup sequence in `GUI_Simulation/App.xaml.cs`: `Container.InitContainer()` → `Container.Register<IGraph, Graph>()`. Types must be registered before anything resolves them — registration order is startup-sequence dependent. Do not introduce Microsoft.Extensions.DependencyInjection.

### Data Access Layer

`DataAccess` project implements a generic repository pattern supporting two providers:

- **EF6 + Npgsql** (primary) for PostgreSQL
- **MongoDB.Driver** (optional secondary)

Key interfaces: `IRepository<T>`, `IUnitOfWork`, `IDataContext`, `ITransaction`. When changing these interfaces, keep both provider implementations compiling.

### AdleGraph

A self-contained directed/undirected graph engine with:

- Weighted nodes/edges, matrix operations, path-finding
- A WPF `GraphCanvas` component for visualization
- `Utility.cs` for graph serialization/deserialization

### ML Service

Python FastAPI at `http://localhost:8000` with two endpoints:

- `POST /tsne` — t-SNE dimensionality reduction
- `POST /random-forest` — Decision Tree ensemble + proximity matrix

The C# client `GUI_Simulation/MlServiceClient.cs` calls these endpoints. Health check: `GET /health`. Any endpoint or model change must be applied on **both** sides (Python Pydantic models ↔ C# client DTOs, JSON casing included).

### Domain Model

Core entities flow: `Area` (rooms/zones) → contains `Item`s (devices/sensors) → produce `Memory` records (event log). `SimulationObjects` project (`FakeDevices` folder) provides fake device implementations for testing without real hardware; fake devices register in the **static** `Device.Devices` registry and are looked up by IP (`Device.Devices.find(ip)`).

## Testing Notes

- Framework: MSTest v3 (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter`), classic `Assert` style.
- Unit tests must not require PostgreSQL or the ML service to be running.
- Tests that touch the device registry must call `Device.Devices.Reset()` first — the registry is static and leaks state between tests (see `FakeDevices.Test/IsikTest.cs` for the canonical pattern).

## Infrastructure

**PostgreSQL connection:**

- Host: `localhost`, Port: `5432`, Database: `adle_sim`, User: `adle_user`, Password: `Password1`

**Reset local DB (destroys data):** `docker-compose down -v && docker-compose up -d`

## Claude Code Configuration

Project-specific agents (`.claude/agents/`: dotnet-developer, test-engineer, code-reviewer, db-migration-expert, ml-engineer) and skills (`.claude/skills/`: build-and-test, start-infra, add-migration, new-device) are checked in — prefer them for matching tasks.
