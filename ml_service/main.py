from fastapi import FastAPI
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
