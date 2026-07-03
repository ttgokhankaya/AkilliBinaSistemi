# Akilli Bina Sistemi (ADLE)

**ADLE — Activity Driven Life Environments**: an adaptive, context-aware, learning smart-home simulation platform built with C# .NET 8 (WPF) and a Python FastAPI ML microservice.

The platform models resident behavior as sequential patterns, predicts the next action and the acting person using **Longest Common Subsequence (LCS)** similarity with **min-max + softmax** probability distribution, and generates reproducible synthetic behavior data through weighted **graph-based simulation**. A separate anomaly-exploration module adds **t-SNE** and **Random Forest proximity** analysis via the ML service.

This codebase implements and extends the M.Sc. thesis:

> Gökhan Kaya, *"Adaptif, Bağlam Bilinçli ve Öğrenebilen Akıllı Ev Sistemi"* (Adaptive, Context-Aware and Learning Smart Home System), Gazi University, Graduate School of Natural and Applied Sciences, June 2019. YÖK Thesis No. 588139. Supported by TÜRKTRUST A.Ş. as an R&D project (AdLe / Adaptive Learning Building System).

## Architecture

| Component | Description |
| --- | --- |
| `Adle.Analysis` | **Platform-independent analysis core** (net8.0): `SequenceAnalyzer`, rule pipeline (`SimilarityRule`, `LCSRule`), min-max / softmax / z-score normalizers |
| `AdleGraph` | **Platform-independent graph engine** (net8.0): weighted nodes/edges, sequence generation, LCS computation |
| `AdleGraph.Wpf` | WPF `GraphCanvas` visualization component |
| `SharedObject`, `ActionModel`, `ItemModel`, `FeildModel`, `MemoryModel` | ADLE layered device/area/memory abstraction (see thesis §4.2.1) |
| `DataAccess` | Generic repository pattern (EF6 + Npgsql primary, MongoDB optional) |
| `DatabaseMigration` / `SimulationDB_Migrations` | Two independent EF6 migration stacks over PostgreSQL `adle_sim` |
| `GUI_Simulation` | Main WPF app: sequential-pattern simulation, anomaly exploration, t-SNE / Random Forest views |
| `GUI` | Secondary smart-building control UI |
| `FakeDevices` (project `SimulationObjects`) | Simulated IoT devices (lights, PIR sensors) |
| `ml_service` | Python FastAPI + scikit-learn microservice (`/tsne`, `/random-forest`) |

## Quick Start

Prerequisites: Docker Desktop, .NET 8 SDK, Visual Studio 2022+ (for the WPF apps).

```bash
# 1. Start infrastructure (PostgreSQL + ML service)
docker-compose up -d

# 2. Run the app: launch the GUI_Simulation project in Visual Studio
#    Migrations and seed data are applied automatically on first startup.
```

| Service | Port | Notes |
| --- | --- | --- |
| PostgreSQL | 5432 | db `adle_sim`, user `adle_user` (override with `ADLE_DB_CONNECTION` env var) |
| ML Service | 8000 | health check: `curl http://localhost:8000/health` |

## Build & Test

```powershell
dotnet build AkilliBinaSistemi.sln
dotnet test

# analysis core tests only
dotnet test Adle.Analysis.Test/Adle.Analysis.Test.csproj

# ML service locally without Docker
cd ml_service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

Manual migrations (if auto-migration fails) via Visual Studio Package Manager Console: `Update-Database` with Default Project set to `DatabaseMigration` or `SimulationDB_Migrations`.

## Roadmap

- [x] Open-source hygiene: license, CI, platform-independent core libraries
- [ ] Benchmarks against real datasets (CASAS) and baseline methods (Markov chain, HMM, LSTM)
- [ ] Reproducible experiment runner + paper submission (JOSS / smart-environment venue)

## License

Apache License 2.0 — see [LICENSE](LICENSE).

---

<details>
<summary><b>Türkçe özet</b></summary>

ADLE, ev sakinlerinin hareketlerini sıralı örüntüler olarak modelleyen, LCS benzerliği ve softmax olasılık dağılımı ile bir sonraki adımı ve işlemi yapan kişiyi tahmin eden, çizge tabanlı sentetik veri üretimiyle test edilebilen akıllı ev simülasyon platformudur. 2019 Gazi Üniversitesi yüksek lisans tezinin (YÖK No. 588139) uygulaması ve devamıdır.

Başlangıç: `docker-compose up -d` ile PostgreSQL + ML servisini başlatın, Visual Studio'da `GUI_Simulation` projesini çalıştırın. Migration ve örnek veriler ilk açılışta otomatik yüklenir.

</details>
