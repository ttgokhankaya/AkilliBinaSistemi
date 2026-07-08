# ADLE Benchmark Results

Comparison of the thesis's **LCS + softmax** next-step predictor against three
baselines — a first-order **Markov** chain, a **hidden Markov model (HMM)**, and
an **LSTM** — on the same train/test split and metrics. This addresses the core
gap in the 2019 thesis (single method, synthetic-only data, no baselines).

Run it: `dotnet run --project Adle.Benchmark` (synthetic) or
`dotnet run --project Adle.Benchmark -- casas [path]` (real data).

## Data

- **Synthetic** — per-person weighted directed graphs (`AdleGraph`) generate
  habit walks (the thesis's own data-generation approach). 2 residents, ~150
  walks each.
- **CASAS hh101** — real WSU CASAS smart-home data (single resident, activity
  annotated). 341 labeled activity sessions, 27 activity classes. Each session
  is the sensor trajectory (ON events, consecutive duplicates collapsed) of one
  labeled activity span. Source and acquisition: `C:\Gokhan\CASAS\README.md`.

## Methodology

- **Tokens** = sensor/node names; each sequence is the ordered trajectory.
- **Split** = stratified 70/30 per label (activity/person), identical for all models.
- **Next-step task**: given a growing prefix, predict the next token. Scored on
  every prefix position of every test sequence.
- **Label task**: predict the activity (CASAS) / person (synthetic) from the prefix.
- **Metrics** (per next-step prediction, actual next token classified as):
  - *top-1 accuracy* (standard): TP / N — actual is the #1 prediction.
  - *thesis accuracy*: (TP + FP) / N — actual is anywhere in the predicted
    distribution. Reported to show how the thesis's custom metric inflates results.
  - precision, recall, F1 on the same TP/FP/FN.

## Results

### CASAS hh101 (real data — deterministic)

| Model | top-1 | thesis-acc | precision | recall | F1 | activity-id |
|---|---|---|---|---|---|---|
| LCS+softmax (thesis) | 69.4% | 98.0% | 70.8% | 97.2% | 81.9% | 37.4% |
| 1st-order Markov + NB | 60.4% | 100.0% | 60.4% | 100.0% | 75.3% | 42.4% |
| HMM (hmmlearn) | 68.3% | 100.0% | 68.3% | 100.0% | 81.1% | 48.6% |
| **LSTM (torch)** | **73.3%** | 100.0% | 73.3% | 100.0% | **84.6%** | 21.9% |

### Synthetic (AdleGraph) — indicative*

| Model | top-1 | F1 | person-id |
|---|---|---|---|
| **LCS+softmax (thesis)** | **78.6%** | **88.0%** | 85.3% |
| 1st-order Markov + NB | 72.1% | 83.8% | 73.6% |
| HMM (hmmlearn) | 68.3% | 81.1% | 85.3% |
| LSTM (torch) | 72.1% | 83.8% | 47.6% |

\* Synthetic numbers vary run-to-run: `AdleGraph.Run` uses a non-seeded RNG.
Seed it before quoting synthetic results in a publication (see Limitations).

## Interpretation

- **Next-step prediction:** on real data the LSTM leads (73.3%), with the thesis
  LCS method a close and competitive second (69.4%), then HMM (68.3%); the naive
  Markov chain trails (60.4%). On the (Markov-generated) synthetic data the LCS
  method actually wins — unsurprising, since exact-prefix matching thrives when
  the generator is simple and fully covered by training.
- **Activity identification (27 classes):** HMM is best (48.6%), Markov's Naive
  Bayes second (42.4%); the LSTM classifier is weakest (21.9%) — it is
  under-trained given few sequences per class. This is the hard, honest task.
- **The thesis's custom accuracy metric** reports ~98–100% for every model,
  versus 60–73% top-1. The gap is the quantified version of the review's
  critique: `(TP+FP)/N` credits any in-distribution hit and inflates results.

**Bottom line for the paper:** the thesis LCS+softmax method is a legitimate,
competitive next-step predictor on real data — not state-of-the-art (the LSTM
edges it on hh101), but ahead of classical baselines and close to the LSTM,
while being far simpler and interpretable. Reported honestly with standard
metrics and baselines, it stands.

## Limitations / next steps

- Seed `AdleGraph.Run` for reproducible synthetic numbers.
- Add more CASAS homes (hh102–hh130, Milan `mn*`) and report mean ± std.
- Cross-validation instead of a single split; hyperparameter tuning for HMM
  states and LSTM depth/epochs.
- The LSTM label classifier needs more capacity/regularization or a multitask
  shared encoder to be competitive at activity id.
