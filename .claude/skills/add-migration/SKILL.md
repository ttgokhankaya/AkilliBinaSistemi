---
name: add-migration
description: >-
  Add or fix an EF6 database migration in the correct migration stack
  (DatabaseMigration or SimulationDB_Migrations). Use when the user wants to add
  a column/table/relationship, change an entity, or fix "migration hatası" at startup.
---

# Add Migration

The solution has **two independent EF6 migration stacks** targeting the same PostgreSQL DB (`adle_sim`). Putting a change in the wrong one breaks startup auto-migration.

## Step 1 — Pick the owning stack

| Entity | Stack / project |
|---|---|
| `Area`, `AreaType`, `Item`, `Memory` | `DatabaseMigration` (context: `DatabaseMigration/DB.cs`) |
| `Device`, `Actor`, `Operation` | `SimulationDB_Migrations` (context: `SimulationDB_Migrations/DB.cs`) |

A new entity goes in the stack of whichever domain it belongs to. **Never** create an FK/navigation property across stacks — cross-boundary references stay as plain ID columns.

## Step 2 — Change the model

Edit the entity class and the owning `DB.cs` (DbSet, fluent config) following the patterns already in that project.

## Step 3 — Create the migration

Look at the existing migration classes in the owning project and add a new migration following the same structure and naming (timestamp prefix, `DbMigration` subclass with `Up()`/`Down()`). EF6 + Npgsql quirks (identifier casing, sequences) — copy what the existing migrations do, not SQL Server defaults.

If Visual Studio is available, the PMC alternative is `Add-Migration <Name>` with Default Project set to the owning migration project.

## Step 4 — Verify both paths

1. `dotnet build AkilliBinaSistemi.sln`
2. **Upgrade path:** with infra up (`docker-compose up -d`), run GUI_Simulation or `Update-Database` — auto-migration in `App.xaml.cs` must apply the new migration cleanly.
3. **Fresh path:** confirm the migration chain works from an empty DB. Local-only data can be reset with `docker-compose down -v && docker-compose up -d` (**destroys data — ask first**).
4. Inspect the result: `docker-compose exec postgres psql -U adle_user -d adle_sim -c "\d <table>"`

## Report

State which stack you touched, the migration name, schema delta, and how you verified fresh + upgrade paths.
