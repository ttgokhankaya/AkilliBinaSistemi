from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sklearn.manifold import TSNE
from sklearn.tree import DecisionTreeClassifier, export_text
import numpy as np

app = FastAPI(title="ADLE ML Service")


class TSNERequest(BaseModel):
    observations: list[list[float]]
    perplexity: float = 0.9
    n_components: int = 2


class RandomForestRequest(BaseModel):
    s0: list[list[float]]
    s1: list[list[float]]
    all_observations: list[list[float]]
    n_trees: int = 100


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/tsne")
def compute_tsne(req: TSNERequest):
    data = np.array(req.observations)
    n_samples = len(data)
    if n_samples < 2:
        return {"output": data.tolist()}
    perplexity = min(req.perplexity, max(1.0, n_samples - 1))
    result = TSNE(
        n_components=req.n_components,
        perplexity=perplexity,
        random_state=0,
    ).fit_transform(data)
    return {"output": result.tolist()}


@app.post("/random-forest")
def compute_random_forest(req: RandomForestRequest):
    s0 = np.array(req.s0)
    s1 = np.array(req.s1)
    all_obs = np.array(req.all_observations)

    X = np.vstack([s0, s1])
    y = np.array([1] * len(s0) + [0] * len(s1))

    rng = np.random.default_rng(0)
    trees = []
    for _ in range(req.n_trees):
        idx = rng.choice(len(X), size=len(X), replace=True)
        tree = DecisionTreeClassifier(criterion="entropy", max_features="sqrt")
        tree.fit(X[idx], y[idx])
        trees.append(tree)

    n = len(all_obs)
    proximity = np.zeros((n, n))
    for tree in trees:
        leaves = tree.apply(all_obs)
        for i in range(n):
            proximity[i] += (leaves == leaves[i]).astype(float)
    proximity /= req.n_trees

    tree_texts = [export_text(t) for t in trees[:5]]
    return {
        "proximity_matrix": proximity.tolist(),
        "tree_texts": tree_texts,
    }


# ---------------------------------------------------------------------------
# Sequence-prediction baselines for the ADLE benchmark.
# Given labelled train/test token sequences, returns for each test sequence and
# each prefix the ranked next-token candidates and a predicted label, so the C#
# benchmark can score every model with the identical PredictionScorer.
# ---------------------------------------------------------------------------


class LabelledSequence(BaseModel):
    label: str = ""
    tokens: list[str]


class SequenceBenchmarkRequest(BaseModel):
    model: str = "hmm"          # "hmm" (and "lstm" once added)
    train: list[LabelledSequence]
    test: list[LabelledSequence]
    n_states: int = 8
    top_k: int = 20


def _build_vocab(train, test):
    vocab = sorted({t for s in train for t in s.tokens} |
                   {t for s in test for t in s.tokens})
    return vocab, {tok: i for i, tok in enumerate(vocab)}


def _fit_categorical_hmm(sequences_ids, n_features, n_states, seed=0):
    """Fit a CategoricalHMM on integer-encoded sequences; floor emissions so
    unseen symbols never yield zero probability (avoids NaNs at predict time)."""
    from hmmlearn.hmm import CategoricalHMM

    seqs = [s for s in sequences_ids if len(s) > 0]
    if not seqs:
        return None
    lengths = [len(s) for s in seqs]
    X = np.concatenate(seqs).reshape(-1, 1)
    states = max(1, min(n_states, sum(lengths)))
    model = CategoricalHMM(n_components=states, n_iter=20, random_state=seed,
                           init_params="ste", params="ste")
    model.n_features = n_features
    try:
        model.fit(X, lengths)
    except Exception:
        return None
    # pad/floor emission matrix to full vocab width
    emis = np.array(model.emissionprob_)
    if emis.shape[1] < n_features:
        pad = np.full((emis.shape[0], n_features - emis.shape[1]), 1e-6)
        emis = np.hstack([emis, pad])
    emis = emis + 1e-6
    emis = emis / emis.sum(axis=1, keepdims=True)
    model.emissionprob_ = emis
    model.n_features = n_features
    return model


def _run_hmm(req: SequenceBenchmarkRequest):
    vocab, idx = _build_vocab(req.train, req.test)
    V = len(vocab)
    if V == 0:
        return {"test": []}

    enc = lambda toks: np.array([idx[t] for t in toks], dtype=int)
    train_ids = [enc(s.tokens) for s in req.train]

    # next-token model: one HMM over all training sequences
    global_hmm = _fit_categorical_hmm(train_ids, V, req.n_states)

    # label model: one HMM per label with enough data
    per_label = {}
    labels = sorted({s.label for s in req.train if s.label})
    for lab in labels:
        seqs = [enc(s.tokens) for s in req.train if s.label == lab]
        if len(seqs) >= 2:
            per_label[lab] = _fit_categorical_hmm(seqs, V, req.n_states)
    label_prior = {lab: sum(1 for s in req.train if s.label == lab) for lab in labels}
    total = max(1, sum(label_prior.values()))

    def next_ranked(prefix_ids):
        if global_hmm is None or len(prefix_ids) == 0:
            return []
        X = prefix_ids.reshape(-1, 1)
        try:
            gamma = global_hmm.predict_proba(X)          # (T, S) smoothed
        except Exception:
            return []
        last = gamma[-1]                                 # filtered P(state|prefix)
        nxt = last @ global_hmm.transmat_ @ global_hmm.emissionprob_   # (V,)
        order = np.argsort(nxt)[::-1][: req.top_k]
        return [vocab[i] for i in order]

    def predict_label(prefix_ids):
        if not per_label or len(prefix_ids) == 0:
            return None
        X = prefix_ids.reshape(-1, 1)
        best, best_score = None, -np.inf
        for lab, m in per_label.items():
            if m is None:
                continue
            try:
                ll = m.score(X)
            except Exception:
                continue
            score = ll + np.log(label_prior[lab] / total)
            if score > best_score:
                best_score, best = score, lab
        return best

    out = []
    for s in req.test:
        ids = enc(s.tokens)
        steps = []
        for k in range(1, len(ids)):
            prefix = ids[:k]
            steps.append({
                "ranked": next_ranked(prefix),
                "label": predict_label(prefix) or "",
            })
        out.append({"steps": steps})
    return {"test": out}


@app.post("/sequence-benchmark")
def sequence_benchmark(req: SequenceBenchmarkRequest):
    if req.model.lower() == "hmm":
        return _run_hmm(req)
    raise HTTPException(status_code=400, detail=f"unknown model '{req.model}'")
