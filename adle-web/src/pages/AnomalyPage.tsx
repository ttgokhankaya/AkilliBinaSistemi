import { useState } from 'react'
import Plot from 'react-plotly.js'
import { api } from '../api/client'

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

export function AnomalyPage() {
  const [perplexity, setPerplexity] = useState(5)
  const [output, setOutput] = useState<number[][] | null>(null)
  const [clusters, setClusters] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    setError(null)
    try {
      const { observations, clusters } = buildSample()
      const res = await api.tsne({ observations, perplexity, nComponents: 2 })
      setOutput(res.output ?? [])
      setClusters(clusters)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  // One Plotly trace per cluster so points keep their known colour/legend.
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
    <section>
      <h2>Anomaly — t-SNE</h2>
      <p className="hint">
        Reduces a 4-D sample (3 clusters) to 2-D via /api/anomaly/tsne → ml_service.
      </p>

      <div className="form-row">
        <label>
          Perplexity
          <input
            type="number"
            step="0.5"
            value={perplexity}
            onChange={(e) => setPerplexity(Number(e.target.value) || 1)}
          />
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
            title: { text: 't-SNE projection (2-D)' },
            xaxis: { title: { text: 'dim 1' } },
            yaxis: { title: { text: 'dim 2' } },
            legend: { title: { text: 'Source cluster' } },
            margin: { t: 48, r: 16, b: 48, l: 48 },
            autosize: true,
          }}
          style={{ width: '100%', height: '440px' }}
          useResizeHandler
          config={{ displayModeBar: false, responsive: true }}
        />
      )}
    </section>
  )
}
