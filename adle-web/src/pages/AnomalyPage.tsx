import { useState } from 'react'
import Plot from 'react-plotly.js'
import { api, type RandomForestResponseDto } from '../api/client'

// A deterministic 4-D sample with three well-separated clusters, so t-SNE has
// something real to pull apart. Each row carries its known cluster for colouring
// the reduced scatter — a quick eyeball check that the pipeline works.
const CENTERS = [
  [0, 0, 0, 0],
  [8, 8, 8, 8],
  [0, 8, 0, 8],
]
const OFFSETS = [-0.4, -0.2, 0, 0.2, 0.4]

function buildSample(): { observations: number[][]; clusters: number[] } {
  const observations: number[][] = []
  const clusters: number[] = []
  CENTERS.forEach((center, c) => {
    for (const off of OFFSETS) {
      observations.push(center.map((v, dim) => v + off * (dim + 1)))
      clusters.push(c)
    }
  })
  return { observations, clusters }
}

function TsneSection() {
  const [perplexity, setPerplexity] = useState(5)
  const [output, setOutput] = useState<number[][] | null>(null)
  const [clusters, setClusters] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    setError(null)
    try {
      const sample = buildSample()
      const res = await api.tsne({ observations: sample.observations, perplexity, nComponents: 2 })
      setOutput(res.output ?? [])
      setClusters(sample.clusters)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  const traces = output
    ? [...new Set(clusters)].map((c) => {
        const pts = output.filter((_, i) => clusters[i] === c)
        return {
          type: 'scatter',
          mode: 'markers',
          name: `Cluster ${c}`,
          x: pts.map((p) => p[0]),
          y: pts.map((p) => p[1]),
          marker: { size: 12 },
        }
      })
    : []

  return (
    <div>
      <h3>t-SNE projection</h3>
      <p className="hint">Reduces a 4-D sample (3 clusters) to 2-D via /api/anomaly/tsne.</p>
      <div className="form-row">
        <label>
          Perplexity
          <input type="number" step="0.5" value={perplexity} onChange={(e) => setPerplexity(Number(e.target.value) || 1)} />
        </label>
        <button onClick={run} disabled={busy}>
          {busy ? 'Running…' : 'Run t-SNE'}
        </button>
      </div>
      {error && <p className="status error">Error: {error}</p>}
      {output && (
        <Plot
          data={traces}
          layout={{
            title: { text: 't-SNE (2-D)' },
            xaxis: { title: { text: 'dim 1' } },
            yaxis: { title: { text: 'dim 2' } },
            legend: { title: { text: 'Source cluster' } },
            margin: { t: 40, r: 16, b: 40, l: 48 },
            autosize: true,
          }}
          style={{ width: '100%', height: '400px' }}
          useResizeHandler
          config={{ displayModeBar: false, responsive: true }}
        />
      )}
    </div>
  )
}

function RandomForestSection() {
  const [result, setResult] = useState<RandomForestResponseDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    setError(null)
    try {
      const { observations, clusters } = buildSample()
      // Train class 0 vs class 1; score the whole sample for proximity.
      const s0 = observations.filter((_, i) => clusters[i] === 0)
      const s1 = observations.filter((_, i) => clusters[i] === 1)
      setResult(await api.randomForest({ s0, s1, allObservations: observations, nTrees: 100 }))
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  const matrix = result?.proximityMatrix ?? null

  return (
    <div>
      <h3>Random-forest proximity</h3>
      <p className="hint">
        Trains cluster 0 vs 1, then scores all points via /api/anomaly/random-forest. Bright blocks =
        points the ensemble routes together.
      </p>
      <div className="form-row">
        <button onClick={run} disabled={busy}>
          {busy ? 'Running…' : 'Run random forest'}
        </button>
      </div>
      {error && <p className="status error">Error: {error}</p>}
      {matrix && (
        <Plot
          data={[
            {
              type: 'heatmap',
              z: matrix,
              colorscale: 'Viridis',
              zmin: 0,
              zmax: 1,
            },
          ]}
          layout={{
            title: { text: 'Proximity matrix' },
            yaxis: { autorange: 'reversed', title: { text: 'sample #' } },
            xaxis: { title: { text: 'sample #' } },
            margin: { t: 40, r: 16, b: 40, l: 48 },
            autosize: true,
          }}
          style={{ width: '100%', height: '420px' }}
          useResizeHandler
          config={{ displayModeBar: false, responsive: true }}
        />
      )}
      {result?.treeTexts?.[0] && (
        <>
          <h4>Sample tree</h4>
          <pre className="tree">{result.treeTexts[0]}</pre>
        </>
      )}
    </div>
  )
}

export function AnomalyPage() {
  return (
    <section>
      <h2>Anomaly exploration</h2>
      <TsneSection />
      <RandomForestSection />
    </section>
  )
}
