---
name: start-infra
description: >-
  Start and health-check the project infrastructure (PostgreSQL + ML service via
  docker-compose). Use when the user says "altyapıyı başlat", "docker'ı kaldır",
  "postgres/ml servisi ayakta mı", or when a task needs the DB or ML service running.
---

# Start Infrastructure

Bring up PostgreSQL and the ML service with Docker Compose and verify both are healthy. Run from the repo root.

## Steps

1. **Check Docker is running:** `docker info` — if it fails, tell the user to start Docker Desktop; nothing else will work.

2. **Start services:**

   ```powershell
   docker-compose up -d
   ```

   Add `--build ml_service` if `ml_service/` code, `requirements.txt`, or the Dockerfile changed since the last build.

3. **Verify PostgreSQL** (db `adle_sim`, user `adle_user`, port 5432):

   ```powershell
   docker-compose exec postgres pg_isready -U adle_user -d adle_sim
   ```

4. **Verify ML service:**

   ```powershell
   curl http://localhost:8000/health
   ```

   If it isn't responding, check `docker-compose logs ml_service --tail 50` for Python import/startup errors.

5. **Report** the status of both services in one line each.

## Troubleshooting

- **Port 5432 in use:** a local PostgreSQL installation is conflicting — report it; don't kill processes without asking.
- **Clean slate needed** (schema corrupted, migration history broken): `docker-compose down -v && docker-compose up -d` — **this deletes all DB data**; confirm with the user first. Migrations and seed data re-run automatically on next GUI_Simulation startup.
