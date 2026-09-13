# ADLE — Issue Backlog

14 issues derived from `SYSTEM-HEALTH-REVIEW.md`, ready to open verbatim.
Each is scoped to a single change with its own acceptance criteria.

> **Note:** GitHub Issues is currently disabled on this repository
> (`Settings → General → Features → Issues`). Enable it and these can be
> opened as-is.

| # | Title | Priority | Label |
|---|---|---|---|
| 1 | HMM baseline silently returns empty predictions | P1 | bug |
| 2 | LSTM baseline returns HTTP 500 outside Docker | P1 | bug |
| 3 | Seed the RNG in AdleGraph for reproducible benchmarks | P1 | bug |
| 4 | Remove hardcoded personal Windows paths | P1 | bug |
| 5 | Add a pytest suite for ml_service | P2 | enhancement |
| 6 | Replace the empty/vacuous tests | P2 | bug |
| 7 | Retarget non-WPF projects to net8.0 | P2 | enhancement |
| 8 | Add a Linux CI job, coverage and branch coverage | P2 | enhancement |
| 9 | Upgrade MongoDB.Driver (high-severity advisory) | P2 | bug |
| 10 | Fix fire-and-forget async in FakeDevices/Operate.cs | P2 | bug |
| 11 | Add unit tests for AdleGraph | P2 | enhancement |
| 12 | Harden the IoC container | P3 | enhancement |
| 13 | Stop swallowing failures silently | P3 | bug |
| 14 | Repo hygiene: .vspscc, .gitignore, docker-compose | P3 | enhancement |

---

## 1. HMM baseline silently returns empty predictions (transmat_ not floored)

**P1 · bug**

### Problem

`ml_service/main.py` → `_fit_categorical_hmm` floors and renormalizes the
**emission** matrix so unseen symbols never get zero probability, but leaves
`transmat_` alone. When a fitted state has no observed outgoing transition
(common whenever `n_states` is large relative to the data), that row of
`transmat_` sums to 0 and hmmlearn rejects the model at predict time.

The caller swallows it:

```python
try:
    gamma = global_hmm.predict_proba(X)
except Exception:
    return []          # <-- scored as a miss, not as an error
```

`predict_label` has the same shape (`except Exception: continue` → no label).
So a broken HMM does not fail loudly — it quietly reports 0% and the benchmark
table still prints a number.

### Reproduction

```bash
cd ml_service && uvicorn main:app --port 8000 &
curl -sS -X POST http://127.0.0.1:8000/sequence-benchmark \
  -H 'Content-Type: application/json' \
  -d '{"model":"hmm",
       "train":[{"label":"a","tokens":["x","y","z"]},{"label":"a","tokens":["x","y","q"]},
                {"label":"b","tokens":["p","q","r"]},{"label":"b","tokens":["p","q","s"]}],
       "test":[{"label":"a","tokens":["x","y","z"]}]}'
```

Observed:

```json
{"test":[{"steps":[{"ranked":[],"label":"a"},{"ranked":[],"label":"a"}]}]}
```

Every `ranked` list is empty. Calling `predict_proba` directly shows the cause:

```
ValueError: transmat_ rows must sum to 1 (got row sums of [1. 0. 0. 0. 1. 1. 1. 0.])
```

On the larger synthetic split the model does fit and scores 67.5% top-1, so the
bug is data-dependent — which is exactly what makes it dangerous for a results
table.

### Impact

- HMM numbers in `docs/BENCHMARK-RESULTS.md` are not trustworthy for any split
  where a state goes unused: small CASAS activity classes, short sequences, or
  a raised `n_states` would all silently degrade toward zero.
- The failure is indistinguishable from "the model predicted badly".

### Proposed fix

1. Floor and renormalize `transmat_` (and `startprob_`) the same way
   `emissionprob_` is already handled.
2. Stop swallowing exceptions blindly. Either let the endpoint return 5xx, or
   return an explicit `{"error": ...}` per step so the C# side can report
   "HMM failed" instead of "HMM was wrong".
3. Clamp `n_states` against the observed data — hmmlearn already warns
   `Fitting a model with 111 free scalar parameters with only 12 data points
   will result in a degenerate solution` — and surface that warning.

### Acceptance

- The reproduction above returns non-empty `ranked` lists.
- A regression test covers the sparse-data case (issue #5).
- The HMM row in `docs/BENCHMARK-RESULTS.md` is re-measured after the fix.

---

## 2. LSTM baseline returns HTTP 500 outside Docker — torch missing from requirements.txt

**P1 · bug**

### Problem

`_run_lstm` in `ml_service/main.py` does `import torch`, but `torch` appears
**only** in `ml_service/Dockerfile`:

```dockerfile
RUN pip install --no-cache-dir -r requirements.txt
RUN pip install --no-cache-dir --index-url https://download.pytorch.org/whl/cpu torch==2.3.1
```

`ml_service/requirements.txt` lists fastapi, uvicorn, scikit-learn, numpy,
pydantic, hmmlearn — no torch.

Both README.md and CLAUDE.md document the local path as:

```bash
cd ml_service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

Anyone following that gets a service where `/sequence-benchmark` with
`model=lstm` fails. The import is lazy (inside the function), so the CI smoke
test `python -c "import main"` passes and never catches it.

### Reproduction

```bash
python3 -m venv v && ./v/bin/pip install -r ml_service/requirements.txt
cd ml_service && ../v/bin/uvicorn main:app --port 8000 &
curl -o /dev/null -w "%{http_code}\n" -X POST http://127.0.0.1:8000/sequence-benchmark \
  -H 'Content-Type: application/json' \
  -d '{"model":"lstm","train":[{"label":"a","tokens":["x","y","z"]}],"test":[{"label":"a","tokens":["x","y"]}]}'
```

Observed: `500`. End-to-end through the benchmark:

```
$ dotnet run --project Adle.Benchmark
[LSTM (torch)] skipped: Response status code does not indicate success: 500 (Internal Server Error).
```

### Impact

`docs/BENCHMARK-RESULTS.md` reports LSTM as the best next-step model on CASAS
hh101 (73.3% top-1, 84.6% F1) — the headline result. A reviewer following the
documented setup cannot reproduce it.

### Proposed fix

Pick one and make the docs match:

- **Option A (preferred):** add `torch==2.3.1` to the dependency set with the
  CPU index documented — e.g. a `requirements-ml.txt` extra plus a README note
  on `--index-url https://download.pytorch.org/whl/cpu`. Keeps Docker and local
  setup identical.
- **Option B:** keep torch Docker-only, but make the failure explicit: return
  HTTP 501 with `"LSTM baseline requires torch; use the Docker image"` instead
  of an unhandled 500, and say so in README + BENCHMARK-RESULTS.md.

Either way `/sequence-benchmark` should never return a bare 500 for a missing
optional dependency.

### Acceptance

- The reproduction returns 200 (A) or a documented 501 with a clear message (B).
- README, CLAUDE.md and `docs/BENCHMARK-RESULTS.md` describe the same setup that
  actually produces the published LSTM numbers.
- CI exercises `/sequence-benchmark` for both `hmm` and `lstm`.

---

## 3. Seed the RNG in AdleGraph so synthetic benchmark results are reproducible

**P1 · bug**

### Problem

`AdleGraph/Utility.cs` uses `Random.Shared` throughout:

```csharp
return Random.Shared.Next(minimumValue, maximumValue + 1);   // x2
int randVal = Random.Shared.Next(1, total + 1);              // RandomWeighted
```

`Random.Shared` is process-global and cannot be seeded. `Graph.cs:398` drives
walk generation through `Utility.RandomWeighted`, so every synthetic benchmark
run produces different data.

### Reproduction

Two consecutive runs of `dotnet run --project Adle.Benchmark`:

| Model | Run 1 | Run 2 |
|---|---|---|
| LCS+softmax | 77.3 % | 77.2 % |
| Markov + NB | 73.6 % | 72.0 % |

The Markov figure moved 1.6 points between runs on identical inputs.

### Impact

`docs/BENCHMARK-RESULTS.md` already flags this in its Limitations section
("Seed `AdleGraph.Run` for reproducible synthetic numbers") and marks the whole
synthetic table *indicative*. Until it is fixed those numbers cannot be quoted
in a paper.

### Proposed fix

1. Introduce a seedable RNG source in `AdleGraph` — an injected `Random`
   instance or an `IRandomSource` on the graph, defaulting to `Random.Shared`
   so existing callers are unaffected.
2. Thread it through `Graph`/`Utility.RandomWeighted` instead of touching the
   global.
3. Have `Adle.Benchmark` construct graphs with a fixed seed (overridable via
   `ADLE_SEED`) and print the seed in the run header.

### Acceptance

- `dotnet run --project Adle.Benchmark` twice in a row produces byte-identical
  output.
- The seed is visible in the output so a published number can be re-derived.
- `docs/BENCHMARK-RESULTS.md` drops the *indicative* caveat and states the seed.

---

## 4. Remove hardcoded personal Windows paths

**P1 · bug**

### Problem

Five call sites hardcode absolute Windows paths, two of them pointing at a
personal machine:

| File | Path |
|---|---|
| `Adle.Benchmark/Program.cs:32` | `C:\Gokhan\CASAS\labeled\hh101.csv` |
| `Adle.Benchmark/Program.cs:38` | `C:\Gokhan\CASAS\README.md` |
| `docs/BENCHMARK-RESULTS.md:19` | `C:\Gokhan\CASAS\README.md` |
| `GUI_Simulation/GraphSimulation/MainWindowForGraph.xaml.cs:330,380` | `C:\ADLE_REPORTS\` |
| `GUI_Simulation/SequencePattern/Scoring/ReportControl.xaml.cs:204` | `C:\ADLE_Observations\` |

### Impact

- **Reproducibility.** `docs/BENCHMARK-RESULTS.md` cites
  `C:\Gokhan\CASAS\README.md` as the "Source and acquisition" reference for the
  real-data results. That file is on one laptop; it is not a citation anyone can
  follow. The CASAS dataset has a real public home
  (<https://casas.wsu.edu/datasets/>) that should be cited instead, along with
  the exact file, preprocessing and version used.
- **Portability.** Export/report features write to `C:\` roots that do not exist
  on a clean machine and are not valid paths off Windows.

### Proposed fix

1. Replace the CASAS default with a repo-relative `data/casas/hh101.csv` (or no
   default at all — `ADLE_CASAS_FILE` is already supported) and document real
   acquisition steps in `docs/`.
2. Cite the CASAS dataset properly in `BENCHMARK-RESULTS.md`: URL, home id,
   download date, and the preprocessing this repo applies (`CasasReader`:
   ON events, consecutive duplicates collapsed, `minLength: 3`,
   `maxLength: 40`, `maxSessions: 600`).
3. Route report/observation exports through
   `Environment.GetFolderPath(SpecialFolder.MyDocuments)` (or a configurable
   output directory), and create the directory before writing.

### Acceptance

- `grep -rn 'C:\\' --include=*.cs --include=*.md .` returns nothing.
- A reader can obtain the CASAS data from the cited public source and reproduce
  the reported session count (341 sessions, 27 classes).

---

## 5. Add a pytest suite for ml_service

**P2 · enhancement**

### Problem

`ml_service/` has no tests. CI's only Python check is:

```yaml
run: python -c "import sys; sys.path.insert(0, 'ml_service'); import main; print(...)"
```

That verifies the module imports. It never calls an endpoint. Issues #1 (HMM
returning empty predictions) and #2 (LSTM 500) both survive this check — #2
specifically because `import torch` is inside the function body.

### Proposed fix

Add `ml_service/tests/` using `pytest` + `fastapi.testclient.TestClient`:

- `/health` returns `{"status": "ok"}`.
- `/tsne` — shape of the output matches the input row count; the `n_samples < 2`
  short-circuit is covered; perplexity is clamped for small inputs.
- `/random-forest` — proximity matrix is square, symmetric, diagonal 1.0, values
  in [0, 1]; `tree_texts` capped at 5.
- `/sequence-benchmark` `model=hmm` — **non-empty** `ranked` on the sparse input
  from issue #1 (regression test).
- `/sequence-benchmark` `model=lstm` — 200, or the documented 501 when torch is
  absent (issue #2).
- `/sequence-benchmark` `model=bogus` → 400.
- Step count per test sequence is `len(tokens) - 1`, matching what the C#
  scorer expects.

Wire it into the `ml-service` CI job (`pytest ml_service/tests`) alongside the
existing Docker build.

### Acceptance

- `pytest ml_service/tests` passes locally and in CI.
- Reverting the fix for #1 or #2 makes the suite fail.

---

## 6. Replace the empty and vacuous tests

**P2 · bug**

### Problem

`GUI.Test/ConvertersTest.cs` is the entire GUI.Test project, and its one test
has an empty body:

```csharp
[TestClass]
public class ConvertersTest
{
    [TestMethod]
    public void AreaConverterTest()
    {

    }
}
```

It passes unconditionally and contributes a green tick that means nothing.

`FakeDataProvider.Test/UoWTest.cs` has the opposite problem — one method named
`TestMethod1` carrying six assertions across add/read/update/find/delete. When
it fails, the name and the failure location tell you nothing. It also asserts on
`found1` (fetched *before* `Update`) rather than re-reading, so it passes only
because the repository hands back the same object reference — a behavioural
detail, not the contract under test.

### Proposed fix

- Either implement a real assertion for the area converter or delete the empty
  test; a project with zero tests is more honest than one with a fake pass.
- Split `TestMethod1` into `Add_ReturnsEntityWithId`,
  `Update_PersistsNewName`, `Find_MatchesPredicate`, `Delete_RemovesEntity`.
- Re-read through the repository after `Update` instead of relying on reference
  identity.
- Consider a `dotnet test` analyzer or a review checklist item that rejects
  empty test bodies.

### Acceptance

- No test method in the solution has an empty body.
- Each test name states the behaviour it verifies.

---

## 7. Retarget non-WPF projects to net8.0

**P2 · enhancement**

### Problem

14 of 23 projects target `net8.0-windows` without using any Windows-only API.
Only three actually set `UseWPF`:

| Genuinely WPF | Unnecessarily `net8.0-windows` |
|---|---|
| `GUI`, `GUI_Simulation`, `AdleGraph.Wpf` | `ActionModel`, `DataAccess`, `DatabaseMigration`, `DomainObjects`, `FakeDataProvider`, `FieldModel`, `ItemModel`, `MemoryModel`, `SharedObject`, `SimulationDB_Migrations`, `SimulationObjects`, `Utility`, and the three test projects |

Already `net8.0` and portable: `Adle.Analysis`, `AdleGraph`, `IoC`,
`Adle.Benchmark`, `Adle.Analysis.Test`.

### Evidence

On Linux, `dotnet build AkilliBinaSistemi.sln -c Release` builds 20 of 23
projects successfully; the only three errors are the WPF ones
(`MSB4019: Microsoft.NET.Sdk.WindowsDesktop.targets was not found`). All four
test projects build and all 25 tests run and pass on Linux. So the
`net8.0-windows` TFM on these projects is decoration, not a requirement.

### Impact

- Pins CI to `windows-latest` (slower and more expensive than `ubuntu-latest`).
- Blocks Linux/macOS contributors from building or testing anything.
- Obscures which parts of the codebase are genuinely platform-bound — which
  matters for the web-migration direction in `docs/WEB-PROPOSAL.md`.

### Proposed fix

Change `<TargetFramework>` to `net8.0` project by project, starting from the
leaves (`SharedObject`, `Utility`, `FieldModel`, `MemoryModel`) and working up.
Anything that genuinely needs Windows keeps `net8.0-windows` with a comment
saying why.

### Acceptance

- `dotnet build` and `dotnet test` succeed on Linux for every non-WPF project.
- Each remaining `net8.0-windows` has a comment justifying it.

---

## 8. Add a Linux CI job, code coverage, and non-master branch coverage

**P2 · enhancement**

### Problem

`.github/workflows/ci.yml` today:

- Triggers only on `master` (`push` and `pull_request`), so work on feature
  branches gets no CI until a PR is opened.
- Runs .NET exclusively on `windows-latest`.
- Collects no code coverage and publishes no test results — with 25 tests across
  ~16k lines (issue #11), coverage is the number that matters.
- Python job runs only an import smoke test (issue #5).

### Proposed fix

1. `on: push:` with no branch filter (or `branches-ignore: []`), keeping the
   `pull_request` trigger.
2. Add an `ubuntu-latest` job building the non-WPF projects and running all
   tests — cheap, fast feedback. Depends on issue #7.
3. Keep `windows-latest` for the full solution including WPF.
4. `dotnet test --collect:"XPlat Code Coverage"` and upload the report as an
   artifact; consider a coverage badge.
5. Add `pytest ml_service/tests` (issue #5).
6. Consider `dotnet format --verify-no-changes` for consistent style.

### Acceptance

- CI runs on every push to every branch.
- A coverage number is visible per run.
- The Linux job finishes materially faster than the Windows one.

---

## 9. Upgrade MongoDB.Driver — known high-severity advisory

**P2 · bug**

### Problem

Every restore emits:

```
warning NU1903: Package 'MongoDB.Driver' 2.8.0 has a known high severity
vulnerability, https://github.com/advisories/GHSA-7j9m-j397-g4wx
```

`DataAccess/DataAccess.csproj` pins 2.8.0 (released 2018).

### Context

Per CLAUDE.md the MongoDB path is the optional secondary provider and the
PostgreSQL/EF6 path is the live one — so exposure is low in practice. But the
warning fires on every build of a public repository, and a security-conscious
reader sees it before they see the caveat.

### Proposed fix

Decide between:

- **Upgrade** to a current 2.x/3.x driver and fix the `IRepository<T>` /
  `IUnitOfWork` implementation against the new API. CLAUDE.md requires both
  providers keep compiling.
- **Remove** the MongoDB provider entirely if it is dead code. Check whether
  anything resolves it (`ADLE_MONGO_CONNECTION` in
  `DataAccess/UnitOfWorkFactory.cs` is described as a legacy path).

Removal is likely the better trade if nothing uses it — less surface, no
warning, less to keep compiling.

### Acceptance

- `dotnet restore` is warning-free.
- If kept: both providers still build and the repository contract is unchanged.
- If removed: `UnitOfWorkFactory` and the docs no longer mention MongoDB.

---

## 10. Fix fire-and-forget async and dead null check in FakeDevices/Operate.cs

**P2 · bug**

### Problem

`FakeDevices/Operate.cs:50` — `Begin()` launches the simulation like this:

```csharp
var task = Task.Factory.StartNew(async () =>
{
    if (_threadCheck) return;
    if (begingingDate == null) return;
    ...
});
```

Two defects:

1. **`Task.Factory.StartNew` with an `async` lambda returns `Task<Task>`.** The
   outer task completes as soon as the inner one starts. The inner task is never
   awaited, so any exception inside the simulation loop is swallowed into an
   unobserved task — no log, no crash, the simulation just stops. `task` is
   assigned and never used, so callers cannot await or cancel it either.
2. **`begingingDate == null` is always false.** The parameter is a non-nullable
   `DateTime`; the compiler flags it (`CS8073: The result of the expression is
   always 'false' since a value of type 'DateTime' is never equal to 'null'`).
   The intended guard against an unset date does nothing.

Also at line 96: `rnd = new Random((int)DateTime.Now.Ticks)` re-seeds inside the
loop from the clock — related to the determinism work in issue #3.

### Proposed fix

- Use `Task.Run(async () => ...)` and **return** the task so callers can await
  and observe it; or make `Begin` an `async Task` method.
- Accept a `CancellationToken` so a running simulation can be stopped.
- Replace the dead null check with a real guard (`default(DateTime)`, or make
  the parameter `DateTime?`).
- Create the `Random` once outside the loop, from the injected seed (issue #3).

### Acceptance

- An exception thrown inside the simulation loop surfaces to the caller.
- `CS8073` is gone.
- A running simulation can be cancelled.

---

## 11. Add unit tests for AdleGraph

**P2 · enhancement**

### Problem

`AdleGraph` is 898 lines across 12 files and has **zero tests**, despite being
the graph engine underneath the whole thesis: weighted directed/undirected
graphs, matrix operations, path-finding, `Sequence<T>` with LCS, and the
synthetic data generation every benchmark number rests on.

Current coverage across the solution:

| Project | Tests |
|---|---|
| Adle.Analysis.Test | 19 |
| SimulationObjects.Test | 4 |
| GUI.Test | 1 (empty — issue #6) |
| FakeDataProvider.Test | 1 |
| **AdleGraph** | **0** |
| `IoC`, `DataAccess`, `SharedObject`, `ItemModel`, `ActionModel` | 0 |

`AdleGraph` is the highest-value gap: it is already `net8.0`, pure logic, no
UI and no database, so it is the cheapest project in the solution to test well.

### Proposed fix

Add `AdleGraph.Test` (MSTest v3, matching the existing convention) covering:

- Node/edge add, remove, duplicate handling, and weight updates.
- Adjacency matrix construction — including directed vs undirected symmetry.
- Path-finding: reachable, unreachable, single-node, and cyclic graphs.
- `Sequence<T>` LCS against known-answer pairs: identical, disjoint, prefix,
  suffix, and repeated-element inputs.
- `Utility.RandomWeighted` distribution with a seeded RNG (pairs with issue #3):
  zero weights, single element, and the weights-sum boundary.
- `Utility.cs` serialization round-trips.

### Acceptance

- `AdleGraph.Test` runs in CI on Linux and Windows.
- LCS and `RandomWeighted` have known-answer tests, since the thesis results
  depend on both.

---

## 12. Harden the IoC container

**P3 · enhancement**

### Problem

`IoC/Container.cs` (67 lines, service locator, no tests):

```csharp
public static TInput Resolve<TInput>(params object[] param)
{
    var foundValue = IoCcontainer._dictionary.FirstOrDefault(x => x.Key == typeof(TInput).Name);
    var newObject = param == null ? Activator.CreateInstance(foundValue.Value)
                                  : Activator.CreateInstance(foundValue.Value, param);
    return (TInput)newObject;
}
```

1. **Unregistered type gives an opaque failure.** `FirstOrDefault` on a
   `KeyValuePair` returns `default`, so `foundValue.Value` is null and
   `Activator.CreateInstance(null)` throws `ArgumentNullException: type` — which
   names neither the interface nor the fact that it was never registered.
   CLAUDE.md warns that "registration order is startup-sequence dependent", so
   this is a failure mode the project expects to hit.
2. **Keyed by `Type.Name`, not by type.** Two same-named types in different
   namespaces collide silently. `Adle.Analysis.Scenario` vs
   `SimulationObjects.Scenario` (documented in CLAUDE.md as a real source of
   confusion) is exactly that shape.
3. **`Register` silently ignores duplicates** — a second registration for the
   same interface is dropped with no signal.
4. **Not thread-safe.** Plain `Dictionary`, plus a lazy-init singleton with no
   lock.
5. **`InitContainer(false)` is a no-op** — the `IoCcontainer` getter constructs
   on demand, so the `== null` branch is unreachable.
6. **O(n) lookup** — `FirstOrDefault` over the dictionary instead of
   `TryGetValue`.

### Proposed fix

Keep the service-locator shape (CLAUDE.md rules out
Microsoft.Extensions.DependencyInjection) and tighten it:

- Key on `Type`, not `Type.Name`; use `TryGetValue`.
- Throw `InvalidOperationException($"{typeof(TInput)} is not registered...")`.
- Make `Register` either overwrite explicitly or throw on conflict.
- Use `ConcurrentDictionary` and a `Lazy<Container>` singleton.
- Add an `IoC.Test` project covering all of the above.

### Acceptance

- Resolving an unregistered type names the type and says it was not registered.
- Same-named types in different namespaces resolve independently.
- Concurrent register/resolve is safe.

---

## 13. Stop swallowing failures silently

**P3 · bug**

### Problem

Several places convert a failure into a silent or misleading success. The
codebase has 14 `catch` blocks and 33 `TODO`/`HACK`/`FIXME` markers; these are
the ones that hide real failures:

**Database bootstrap continues after failure** — `GUI_Simulation/App.xaml.cs`:

```csharp
catch (Exception ex)
{
    MessageBox.Show($"Veritabanı hazırlama hatası:\n{ex.Message}\n\n...",
                    "Veritabanı Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
}
```

The app then starts against a missing or half-created schema. CLAUDE.md
acknowledges this: *"DB bootstrap failures at startup show a MessageBox warning
and the app continues running — an incomplete schema can therefore go
unnoticed."* Every later failure then looks like an application bug.

**`throw ex;` resets the stack trace** — `ActionModel/Adaptor.cs:89`
(`CA2200`, emitted on every build). The original throw site is lost. Nearby,
`throw new Exception()` is thrown with no message at all and a `//TODO` where
the message should be.

**Bare `except Exception` in the ML service** — see issue #1; the same pattern
turns a model failure into a wrong answer.

### Proposed fix

- Startup: distinguish fatal from recoverable. A missing schema should block
  startup (or start in an explicit degraded mode the UI shows), not warn once
  and continue.
- Replace `throw ex;` with `throw;`.
- Give every `throw new Exception()` a real message and a specific exception
  type.
- Audit the remaining `catch` blocks: each should either handle the error
  meaningfully or rethrow.
- Add logging so swallowed errors leave a trace.

### Acceptance

- `CA2200` is gone.
- A failed DB bootstrap does not leave the app running as if it succeeded.
- No `catch` block discards an exception without logging or rethrowing.

---

## 14. Repo hygiene: dead .vspscc files, Python .gitignore, docker-compose

**P3 · enhancement**

### Problem

**20 tracked `*.vspscc` files.** One per project — dead Visual Studio
source-control bindings from the pre-git era (`ActionModel.csproj.vspscc`,
`GUI.csproj.vspscc`, …). Pure noise in a public repo.

**`.gitignore` has no Python section.** `ml_service/__pycache__/` appears as
untracked the moment the service is run locally. Missing: `__pycache__/`,
`*.py[cod]`, `.pytest_cache/`, `venv/`, `.venv/`, `*.egg-info/`.

**`docker-compose.yml` is minimal:**

```yaml
services:
  postgres:
    environment:
      POSTGRES_PASSWORD: Password1
  ml_service:
    build: ./ml_service
```

- No `healthcheck` on either service, so `docker-compose up -d` returns before
  Postgres accepts connections — the app's `EnsureDatabaseExists()` can race it
  and hit the silent-failure path from issue #13.
- No dependency ordering between the services.
- The password is committed in plaintext. It matches the hardcoded default in
  both `DatabaseMigration/DB.cs` and `SimulationDB_Migrations/DB.cs`. It is a
  local dev credential, not a secret — but it should come from a `.env` with an
  `.env.example` checked in, so nobody mistakes it for a pattern to copy.

### Proposed fix

1. `git rm --cached **/*.vspscc` and add `*.vspscc` to `.gitignore`.
2. Add the Python section to `.gitignore`.
3. Add healthchecks (`pg_isready` for Postgres, `GET /health` for ml_service)
   and `depends_on: { postgres: { condition: service_healthy } }`.
4. Move credentials to `.env` + committed `.env.example`; keep the current
   values as the documented local defaults.

### Acceptance

- No `.vspscc` file is tracked.
- Running the ML service locally leaves the working tree clean.
- `docker-compose up -d` returns only once both services are actually ready.
