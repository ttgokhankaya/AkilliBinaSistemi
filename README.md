# Akilli Bina Sistemi (ADLE)

C# .NET 8 WPF tabanlı Akıllı Bina / ADLE (Activity Driven Life Environments) simülasyon uygulaması.

## Gereksinimler

- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Visual Studio 2022+
- .NET 8 SDK

## Hızlı Başlangıç

### 1. Servisleri başlat

```bash
docker-compose up -d
```

Bu komut iki servisi ayağa kaldırır:

| Servis | Port | Açıklama |
| --- | --- | --- |
| PostgreSQL | 5432 | Uygulama veritabanı |
| ML Service | 8000 | Python FastAPI — t-SNE ve Random Forest |

### 2. Uygulamayı çalıştır

Visual Studio'da `GUI_Simulation` projesini başlatın.

**Migration ve örnek veri otomatik yüklenir** — ilk çalıştırmada veritabanı tabloları oluşturulur ve aşağıdaki örnek veriler eklenir:

| Kategori | Örnek Veriler |
| --- | --- |
| Alan Türleri | LivingRoom, Bedroom, Kitchen, Bathroom, Hallway |
| Alanlar | Ev, Oturma Odası, Yatak Odası 1 ve 2, Mutfak, Banyo |
| Cihazlar | Lamba, Termostat, Hareket Sensörü, Akıllı Priz (192.168.1.x) |
| Sakinler | Sakin 1, Sakin 2 |
| Operasyonlar | Sabah, Öğle, Akşam, Gece Rutini |

> Migration hatasında uygulama uyarı verir ve devam eder. Docker çalıştığından emin olun.

---

## Servisler

### PostgreSQL Bağlantı Bilgileri

| Parametre | Değer |
| --- | --- |
| Host | `localhost` |
| Port | `5432` |
| Database | `adle_sim` |
| Username | `adle_user` |
| Password | `Password1` |

### ML Servisi (Python FastAPI)

| Endpoint | Metod | Açıklama |
| --- | --- | --- |
| `/health` | GET | Servis sağlık kontrolü |
| `/tsne` | POST | t-SNE boyut indirgeme (scikit-learn) |
| `/random-forest` | POST | Random Forest eğitim + yakınlık matrisi |

**Sağlık kontrolü:**

```bash
curl http://localhost:8000/health
```

**Lokal geliştirme (Docker olmadan):**

```bash
cd ml_service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

---

## Proje Yapısı

| Proje | Açıklama |
| --- | --- |
| `DomainObjects` | Domain entity modelleri (Area, Item, Memory) |
| `DataAccess` | EF6 generic repository pattern |
| `DatabaseMigration` | ADLE DB migration + seed (Areas, Items, Memories) |
| `SimulationDB_Migrations` | Simülasyon DB migration + seed (Devices, Actors, Operations) |
| `GUI` | Ana WPF arayüzü |
| `GUI_Simulation` | Simülasyon WPF arayüzü (anomali analizi, t-SNE, Random Forest) |
| `AdleGraph` | Graf veri yapısı ve WPF canvas bileşeni |
| `FakeDevices` | Sahte cihaz simülasyon modelleri |
| `FakeDataProvider` | Test verisi sağlayıcı |
| `ml_service` | Python FastAPI + scikit-learn ML microservice |

---

## Manuel Migration (Gerekirse)

Otomatik migration çalışmazsa Visual Studio Package Manager Console ile manuel çalıştırın:

**ADLE veritabanı:**

```powershell
# Default Project: DatabaseMigration
Update-Database
```

**Simülasyon veritabanı:**

```powershell
# Default Project: SimulationDB_Migrations
Update-Database
```

---

## Servisleri Durdur

```bash
docker-compose down

# Verileri de sil
docker-compose down -v
```
