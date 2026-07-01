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
- **`GUI_Simulation`** — Primary app. Anomaly analysis, t-SNE visualization, Random Forest analysis. Runs migrations for both DBs on startup.
- **`GUI`** — Secondary smart building control UI.

### Two Separate Database Contexts

There are two EF6 migration stacks targeting the same PostgreSQL instance (`adle_sim`):
- **`DatabaseMigration`** → `DB.cs` — Manages `Areas`, `AreaTypes`, `Items`, `Memories`
- **`SimulationDB_Migrations`** → `DB.cs` — Manages `Devices`, `Actors`, `Operations`

Both are auto-migrated in `App.xaml.cs` at startup. When adding new entities or migrations, be mindful of which context owns them.

### IoC / Dependency Injection

A custom IoC container lives in `IoC/Container.cs` (singleton). It uses a service-locator pattern — types are registered by interface and resolved at runtime. The graph system bootstraps through this container in `GUI_Simulation/App.xaml.cs`.

### Data Access Layer

`DataAccess` project implements a generic repository pattern supporting two providers:
- **EF6 + Npgsql** (primary) for PostgreSQL
- **MongoDB.Driver** (optional secondary)

Key interfaces: `IRepository<T>`, `IUnitOfWork`, `IDataContext`, `ITransaction`.

### AdleGraph

A self-contained directed/undirected graph engine with:
- Weighted nodes/edges, matrix operations, path-finding
- A WPF `GraphCanvas` component for visualization
- `Utility.cs` for graph serialization/deserialization

### ML Service

Python FastAPI at `http://localhost:8000` with two endpoints:
- `POST /tsne` — t-SNE dimensionality reduction
- `POST /random-forest` — Decision Tree ensemble + proximity matrix

The C# client `GUI_Simulation/MlServiceClient.cs` calls these endpoints. Health check: `GET /health`.

### Domain Model

Core entities flow: `Area` (rooms/zones) → contains `Item`s (devices/sensors) → produce `Memory` records (event log). `SimulationObjects` project (`FakeDevices`) provides fake device implementations for testing without real hardware.

## Infrastructure

**PostgreSQL connection:**
- Host: `localhost`, Port: `5432`, Database: `adle_sim`, User: `adle_user`, Password: `Password1`

**Test framework:** MSTest v3 (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`, `MSTest.TestAdapter`)
