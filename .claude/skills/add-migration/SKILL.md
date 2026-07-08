---
name: add-migration
description: >-
  Change the database schema. NOTE: this project no longer uses EF6 migrations
  at runtime — the schema lives in db/schema.sql. Use when the user wants to add
  a column/table/relationship or change an entity.
---

# Change the Schema (db/schema.sql)

> **Important:** Runtime schema creation does NOT use EF6 `DbMigrator`. The
> SQL Server → PostgreSQL port left the migration snapshots inconsistent, so
> the schema is now defined in **`db/schema.sql`** (idempotent DDL, `public`
> schema) and executed at startup by `GUI_Simulation/App.xaml.cs`. To change
> the schema:
>
> 1. Edit the entity class (and its `DbSet` in the owning `DB.cs`).
> 2. Add the corresponding `CREATE TABLE`/`ALTER TABLE ... IF NOT EXISTS` (or a
>    new idempotent statement) to `db/schema.sql`, in dependency order.
> 3. If you added a table, decide the owning context (see below) and add the
>    `DbSet`. Never create an FK across the two contexts.
> 4. Recreate the local DB to test: drop the `public` schema (or
>    `docker-compose down -v`, destroys data) and relaunch `GUI_Simulation`.
>
> The `*/Migrations/` folders are kept for historical reference only — do not
> rely on `Update-Database` / `DbMigrator`.

## Which context owns the entity

The solution has **two independent EF6 migration stacks** targeting the same PostgreSQL DB (`adle_sim`). Putting a change in the wrong one breaks startup auto-migration.

## Step 1 — Pick the owning stack

| Entity | Stack / project |
|---|---|
| `Area`, `AreaType`, `Item`, `Memory` | `DatabaseMigration` (context: `DatabaseMigration/DB.cs`) |
| `Device`, `Actor`, `Operation` | `SimulationDB_Migrations` (context: `SimulationDB_Migrations/DB.cs`) |

A new entity goes in the stack of whichever domain it belongs to. **Never** create an FK/navigation property across stacks — cross-boundary references stay as plain ID columns.

## Step 2 — Change the model

Edit the entity class and the owning `DB.cs` (DbSet, fluent config) following the patterns already in that project. Keep `HasDefaultSchema("public")`.

## Step 3 — Update db/schema.sql

Add the matching idempotent DDL to `db/schema.sql`: `CREATE TABLE IF NOT EXISTS public."X" (...)` for a new table (place it after the tables it references), or a guarded `ALTER TABLE` for a new column. Match EF6 type mapping: `int`→`integer` (identity PK → `serial`), `string`→`text`, `double`→`double precision`, `bool`→`boolean`, `DateTime`→`timestamp`, `TimeSpan`→`interval`.

## Step 4 — Verify from a clean database

1. `dotnet build AkilliBinaSistemi.sln`
2. Reset the schema: `docker-compose exec postgres psql -U adle_user -d adle_sim -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"` (local dev data only — **destroys data**).
3. Launch `GUI_Simulation` (or apply `db/schema.sql` with `psql`) and confirm no "Veritabanı Hatası" dialog and that seed data lands.
4. Inspect: `docker-compose exec postgres psql -U adle_user -d adle_sim -c "\d public.\"<Table>\""`

## Report

State which context owns the entity, the schema delta added to `db/schema.sql`, and how you verified from a clean database.
