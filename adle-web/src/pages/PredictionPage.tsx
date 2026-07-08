import { useState } from 'react'
import Plot from 'react-plotly.js'
import { api, type PredictRequest, type PredictResponse } from '../api/client'

// Prefilled with the same shape as PredictionServiceTests: a deterministic
// "1P 1I 2P …" habit so the demo returns a meaningful prediction out of the box.
const SAMPLE_TRAINING = `Baba: 1P 1I 2P 2I
Baba: 1P 1I 2P 3P
Anne: 1P 1I 2P 2I`

/** Parses "person: tok1 tok2" lines (person optional) into the request shape. */
function parseTraining(text: string): PredictRequest['training'] {
  return text
    .split('\n')
    .map((l) => l.trim())
    .filter(Boolean)
    .map((line) => {
      const [head, rest] = line.includes(':') ? line.split(/:(.*)/s) : [null, line]
      const tokens = (rest ?? '').trim().split(/\s+/).filter(Boolean)
      return { person: head ? head.trim() : null, tokens }
    })
}

export function PredictionPage() {
  const [training, setTraining] = useState(SAMPLE_TRAINING)
  const [prefix, setPrefix] = useState('1P 1I')
  const [threshold, setThreshold] = useState(0)
  const [result, setResult] = useState<PredictResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    setError(null)
    try {
      const req: PredictRequest = {
        training: parseTraining(training),
        prefix: prefix.trim().split(/\s+/).filter(Boolean),
        lcsThreshold: threshold,
      }
      setResult(await api.predict(req))
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  const dist = (result?.distribution ?? []).filter((d) => d.node)

  return (
    <section>
      <h2>Next-step prediction</h2>
      <p className="hint">LCS + softmax pipeline (thesis method), served by /api/analysis/predict.</p>

      <div className="form-grid">
        <label>
          Training sequences (one per line, <code>person: tokens</code>)
          <textarea rows={5} value={training} onChange={(e) => setTraining(e.target.value)} />
        </label>
        <div className="form-row">
          <label>
            Prefix (observed tokens)
            <input value={prefix} onChange={(e) => setPrefix(e.target.value)} />
          </label>
          <label>
            LCS threshold
            <input
              type="number"
              value={threshold}
              onChange={(e) => setThreshold(Number(e.target.value) || 0)}
            />
          </label>
          <button onClick={run} disabled={busy}>
            {busy ? 'Predicting…' : 'Predict'}
          </button>
        </div>
      </div>

      {error && <p className="status error">Error: {error}</p>}

      {result && (
        <div className="result">
          <div className="cards">
            <div className="card">
              <span className="card-label">Next node</span>
              <span className="card-value">{result.predictedNextNode ?? '—'}</span>
            </div>
            <div className="card">
              <span className="card-label">Person</span>
              <span className="card-value">{result.predictedPerson ?? '—'}</span>
            </div>
            <div className="card">
              <span className="card-label">Matches</span>
              <span className="card-value">{result.matchCount}</span>
            </div>
            <div className="card">
              <span className="card-label">Rule</span>
              <span className="card-value">{result.ruleUsed ?? '—'}</span>
            </div>
          </div>

          {dist.length > 0 && (
            <Plot
              data={[
                {
                  type: 'bar',
                  x: dist.map((d) => d.node as string),
                  y: dist.map((d) => d.probability),
                  marker: { color: '#4f80ff' },
                },
              ]}
              layout={{
                title: { text: 'Next-node probability distribution' },
                yaxis: { title: { text: 'Probability' }, range: [0, 1] },
                margin: { t: 48, r: 16, b: 40, l: 48 },
                autosize: true,
              }}
              style={{ width: '100%', height: '360px' }}
              useResizeHandler
              config={{ displayModeBar: false, responsive: true }}
            />
          )}
        </div>
      )}
    </section>
  )
}
