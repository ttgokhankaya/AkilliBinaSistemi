# ADLE — Sistem Sağlık Değerlendirmesi

**Tarih:** 2026-09-13 · **Kapsam:** `master` (c02e630) · **Ortam:** Linux, .NET 8.0.131 SDK, Python 3.11

Bu rapor sistemin tamamının derlenerek, testleri çalıştırılarak ve ML servisi ile
benchmark uçtan uca koşturularak yapılan incelemenin sonucudur. Her bulgu
gözlemle doğrulanmıştır; tahmin veya statik okuma değildir.

---

## 1. Genel durum

| Alan | Durum | Not |
|---|---|---|
| Derleme (WPF dışı 20 proje) | ✅ | Linux'ta sorunsuz derleniyor |
| Derleme (3 WPF projesi) | ⚠️ | Yalnızca Windows'ta derlenir (beklenen) |
| Testler | ✅ 25/25 geçti | Ancak kapsam çok düşük, biri boş gövdeli |
| ML servisi `/health`, `/tsne`, `/random-forest` | ✅ | Doğru yanıt veriyor |
| ML servisi `/sequence-benchmark` (HMM) | ⚠️ | Çalışıyor ama seyrek veride sessizce boş dönüyor |
| ML servisi `/sequence-benchmark` (LSTM) | ❌ | Belgelenen kurulumda HTTP 500 |
| Benchmark (sentetik) | ✅ | Çalışıyor, fakat koşumlar arası değişken |
| Benchmark (CASAS gerçek veri) | ❌ | Veri yolu kişisel makineye sabit kodlu |

**Özet:** sistem ayakta ve ana akış çalışıyor. Asıl risk *yayına hazırlıkta*:
benchmark sonuçlarının üçü (HMM güvenilirliği, LSTM tekrarlanabilirliği,
sentetik veri determinizmi) bağımsız bir okuyucu tarafından yeniden
üretilemez durumda.

## 2. Ölçülen değerler

### Derleme
```
dotnet build AkilliBinaSistemi.sln -c Release
→ 3 error (GUI, GUI_Simulation, AdleGraph.Wpf — Linux'ta WPF SDK yok), 5 warning
```
Uyarılar: `NU1903` (MongoDB.Driver 2.8.0 yüksek önemli güvenlik açığı),
`CS8073` (Operate.cs — her zaman false olan karşılaştırma),
`CA2200` (Adaptor.cs — `throw ex;`), 2× `CS8981`.

### Testler
| Proje | Test | Sonuç |
|---|---|---|
| Adle.Analysis.Test | 19 | ✅ |
| SimulationObjects.Test | 4 | ✅ |
| GUI.Test | 1 | ✅ (gövdesi **boş**) |
| FakeDataProvider.Test | 1 | ✅ |
| **Toplam** | **25** | ~16.000 satır kod için |

Hiç testi olmayan projeler: `AdleGraph` (898 satır — çizge motoru ve LCS),
`IoC`, `DataAccess`, `SharedObject`, `ItemModel`, `ActionModel`, `ml_service`.

### Benchmark (sentetik, arka arkaya iki koşum)
| Model | Koşum 1 | Koşum 2 |
|---|---|---|
| LCS+softmax | 77.3 % | 77.2 % |
| Markov + NB | 73.6 % | 72.0 % |
| HMM | — | 67.5 % |
| LSTM | atlandı (500) | atlandı (500) |

## 3. Bulgular

### P1 — Yayın engelleyici

**B1. HMM temel modeli seyrek veride sessizce boş tahmin üretiyor.**
`_fit_categorical_hmm` yalnızca `emissionprob_` matrisini tabanlıyor,
`transmat_` matrisini tabanlamıyor. Hiç geçiş gözlenmemiş bir durum kalırsa
satır toplamı 0 olur ve `predict_proba` hata fırlatır. Çağıran taraftaki
çıplak `except Exception: return []` bunu yutar; sonuç boş sıralama listesi
olarak, yani **ıskalama** olarak puanlanır. Yeniden üretildi:
`ValueError: transmat_ rows must sum to 1 (got row sums of [1. 0. 0. 0. 1. 1. 1. 0.])`.

**B2. LSTM temel modeli Docker dışında çalışmıyor.** `torch` yalnızca
`ml_service/Dockerfile` içinde kuruluyor, `requirements.txt` içinde yok.
README ve CLAUDE.md ise yerel kurulum için `pip install -r requirements.txt`
diyor. Bu yolu izleyen biri `model=lstm` istediğinde HTTP 500 alıyor —
benchmark çıktısında `[LSTM (torch)] skipped: ... 500` olarak doğrulandı.
`docs/BENCHMARK-RESULTS.md` içindeki LSTM satırı bu haliyle yeniden üretilemez.

**B3. Sentetik veri üretimi deterministik değil.** `AdleGraph/Utility.cs`
`Random.Shared` kullanıyor; tohumlanamaz. Aynı komut iki farklı sonuç veriyor
(yukarıdaki tablo). `docs/BENCHMARK-RESULTS.md` bunu kendi kısıtları arasında
zaten not ediyor.

**B4. Kişisel makineye sabit kodlu yollar.** `C:\Gokhan\CASAS\labeled\hh101.csv`
ve `C:\Gokhan\CASAS\README.md` — ikincisi herkese açık bir depoda veri edinme
referansı olarak yayımlanmış durumda. Ayrıca `C:\ADLE_REPORTS\` ve
`C:\ADLE_Observations\` rapor/dışa aktarma özelliklerinde sabit.

### P2 — Kalite ve altyapı

**B5. WPF olmayan 14 proje gereksiz yere `net8.0-windows` hedefliyor.**
Gerçekten WPF kullanan yalnızca 3 proje var. Bu, CI'ı Windows'a mahkûm ediyor
ve Linux/macOS katkıcılarını dışarıda bırakıyor.

**B6. CI eksik.** Yalnızca `master` dalında tetikleniyor, Linux işi yok,
kapsam (coverage) toplanmıyor, test sonuçları yayımlanmıyor.

**B7. Sahte yeşil test.** `GUI.Test.ConvertersTest.AreaConverterTest` gövdesi
tamamen boş — hiçbir şey doğrulamadan geçiyor.
`FakeDataProvider.Test.UoWTest.TestMethod1` tek testte 6 doğrulama yapıyor ve
`Update` sonrası referans kimliğine dayanıyor.

**B8. `ml_service` için hiç test yok.** CI yalnızca `import main` yapıyor;
`/tsne`, `/random-forest`, `/sequence-benchmark` hiç çağrılmıyor — B1 ve B2
tam da bu yüzden fark edilmemiş.

**B9. `MongoDB.Driver` 2.8.0** yüksek önemli güvenlik açığı taşıyor (NU1903).

**B10. `FakeDevices/Operate.cs` — yangına körükle async.**
`Task.Factory.StartNew(async () => ...)` `Task<Task>` döndürüyor; iç görev
hiç beklenmediği için istisnalar yutuluyor, simülasyon beklenemiyor/iptal
edilemiyor. Ayrıca `if (begingingDate == null)` — `DateTime` null olamayacağı
için ölü kod (CS8073).

### P3 — Bakım

**B11. `IoC/Container.cs`** kayıtlı olmayan tip çözülünce
`Activator.CreateInstance(null)` çağırıp anlaşılmaz `ArgumentNullException`
veriyor; iş parçacığı güvenli değil; yinelenen kaydı sessizce yok sayıyor.

**B12. Sessiz hata yutma.** Açılışta veritabanı hazırlama hatası yalnızca
uyarı kutusu gösterip devam ediyor — uygulama boş şemayla çalışabiliyor
(CLAUDE.md bunu kendisi de not ediyor). Kod tabanında 14 `catch`, 33
`TODO/HACK/FIXME` var; `Adaptor.cs:89` `throw ex;` yığın izini siliyor.

**B13. `MlServiceClient.cs`** `http://localhost:8000/` adresini sabit kodluyor;
oysa `Adle.Benchmark/RemoteBenchmark.cs` zaten `ADLE_ML_URL` değişkenini
destekliyor. Tutarsız.

**B14. Depo hijyeni.** 20 adet ölü `*.vspscc` dosyası takip ediliyor;
`.gitignore` içinde Python bölümü yok; `docker-compose.yml` healthcheck ve
servis bağımlılığı içermiyor, parola dosyada açık.

## 4. Yol haritası

| Sıra | İş | Neden önce |
|---|---|---|
| 1 | B1, B2, B3, B4 | Benchmark sonuçları bunlar olmadan savunulamaz |
| 2 | B8, B7 | Aynı hataların tekrar sızmasını engeller |
| 3 | B5 → B6 | Linux CI, hızlı ve ucuz geri bildirim |
| 4 | B9, B10 | Güvenlik ve çalışma zamanı doğruluğu |
| 5 | B11–B14 | Bakım kolaylığı |
