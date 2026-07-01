---
name: build-and-test
description: >-
  Build the AkilliBinaSistemi solution and run the MSTest suite, reporting a
  concise pass/fail summary. Use when the user says "build et", "testleri çalıştır",
  "derle", "her şey geçiyor mu", or after a set of code changes needs verification.
---

# Build & Test

Build the full solution and run all tests for the ADLE project. Run from the repo root.

## Steps

1. **Build:**

   ```powershell
   dotnet build AkilliBinaSistemi.sln
   ```

   If the build fails, stop, report the first real errors (not the cascade), and fix or hand back — do not run tests against a broken build.

2. **Test:**

   ```powershell
   dotnet test AkilliBinaSistemi.sln --nologo
   ```

   Test projects (MSTest v3):
   - `GUI.Test/GUI.Test.csproj`
   - `FakeDataProvider.Test/FakeDataProvider.Test.csproj`
   - `FakeDevices.Test/SimulationObjects.Test.csproj` ← folder and project names differ

   To scope: `dotnet test <csproj>` or `dotnet test --filter "FullyQualifiedName~<ClassName>"`.

3. **Report** — one short summary:
   - Build: OK / N errors (list them)
   - Tests: passed/failed/skipped counts per project
   - For each failure: test name, assertion message, and the likely root cause after reading the test and the code under test.

## Notes

- Unit tests do not need PostgreSQL or the ML service running. If a test fails with a connection error, that's a test-isolation bug worth flagging, not a reason to start Docker.
- Failures that only appear in the full run but not in isolation usually mean missing `Device.Devices.Reset()` in a device test (static registry leak).
