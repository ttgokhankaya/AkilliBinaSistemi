import { useRef, useState } from 'react'
import Plot from 'react-plotly.js'
import { api, type PredictRequest, type PredictResponse } from '../api/client'
import { useTheme } from '../ThemeContext'
import './PredictionPage.css'

// Design: "ADLE Prediction.dc.html". The mockup fabricated its distribution with
// a client-side LCS; here the same editor drives the real /api/analysis/predict
// (LCS + softmax, the thesis method). A prefilled habit — Resident A repeats a
// Kitchen→Hallway→Living→Bedroom routine — returns a meaningful prediction out of
// the box for the seeded "Kitchen › Hallway" prefix.
const VOCAB = ['Kitchen', 'Living', 'Hallway', 'Bedroom', 'Bath']
const NO_PERSON = '—'
const PERSONS = [NO_PERSON, 'Resident A', 'Resident B']

type SeqRow = { person: string; tokens: string[] }

const INITIAL_TRAINING: SeqRow[] = [
  { person: 'Resident A', tokens: ['Kitchen', 'Hallway', 'Living', 'Bedroom'] },
  { person: 'Resident A', tokens: ['Kitchen', 'Living', 'Bedroom'] },
  { person: 'Resident B', tokens: ['Bath', 'Hallway', 'Bedroom'] },
  { person: 'Resident B', tokens: ['Kitchen', 'Hallway', 'Bath'] },
  { person: 'Resident A', tokens: ['Living', 'Hallway', 'Bedroom'] },
]

// Plotly palette per theme (SVG can't read CSS vars), matching the design tokens.
const PLOT = {
  light: { text2: '#525b6b', grid: '#e2e6ec', accent: '#3b5bdb', weak: '#cdd6f5' },
  dark: { text2: '#9aa5b6', grid: '#2a3342', accent: '#5b7cfa', weak: '#2b3a63' },
}

const ChevronDown = ({ className }: { className: string }) => (
  <svg
    className={className}
    width="11"
    height="11"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2.6"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M6 9l6 6 6-6" />
  </svg>
)

const CloseIcon = () => (
  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.6" strokeLinecap="round">
    <path d="M6 6l12 12M18 6 6 18" />
  </svg>
)

export function PredictionPage() {
  const { theme } = useTheme()
  const [training, setTraining] = useState<SeqRow[]>(INITIAL_TRAINING)
  const [prefix, setPrefix] = useState<string[]>(['Kitchen', 'Hallway'])
  const [lcs, setLcs] = useState(0)
  const [tip, setTip] = useState(false)
  const [running, setRunning] = useState(false)
  const [result, setResult] = useState<PredictResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [toast, setToast] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (timer.current) window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => setToast(null), 2600)
  }

  // --- training editor mutations ---
  const addSeq = () => setTraining((s) => [...s, { person: NO_PERSON, tokens: [] }])
  const removeSeq = (i: number) => setTraining((s) => s.filter((_, k) => k !== i))
  const setPerson = (i: number, v: string) =>
    setTraining((s) => s.map((r, k) => (k === i ? { ...r, person: v } : r)))
  const addToken = (i: number, v: string) => {
    if (!v) return
    setTraining((s) => s.map((r, k) => (k === i ? { ...r, tokens: [...r.tokens, v] } : r)))
  }
  const removeToken = (i: number, j: number) =>
    setTraining((s) => s.map((r, k) => (k === i ? { ...r, tokens: r.tokens.filter((_, x) => x !== j) } : r)))

  // --- prefix mutations ---
  const addPrefix = (v: string) => {
    if (!v) return
    setPrefix((p) => [...p, v])
  }
  const removePrefix = (j: number) => setPrefix((p) => p.filter((_, x) => x !== j))

  async function run() {
    if (running || !prefix.length) {
      if (!prefix.length) setError('Add at least one node to the prefix before running.')
      return
    }
    setRunning(true)
    setError(null)
    try {
      const req: PredictRequest = {
        training: training
          .filter((r) => r.tokens.length > 0)
          .map((r) => ({ tokens: r.tokens, person: r.person === NO_PERSON ? null : r.person })),
        prefix,
        lcsThreshold: lcs,
      }
      const res = await api.predict(req)
      setResult(res)
      fire(res.predictedNextNode ? `Predicted: ${res.predictedNextNode}` : 'No next node found')
    } catch (e) {
      setResult(null)
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setRunning(false)
    }
  }

  // --- distribution chart ---
  const dist = (result?.distribution ?? []).filter((d) => d.node)
  const topProb = dist.length ? Math.max(...dist.map((d) => d.probability ?? 0)) : 0
  const p = PLOT[theme]
  // Plotly renders bottom-up; reverse so the highest bar sits on top.
  const bars = dist.slice().sort((a, b) => (a.probability ?? 0) - (b.probability ?? 0))
  const font = { family: 'ui-monospace, Consolas, monospace', size: 11, color: p.text2 }
  const confColor = topProb >= 0.5 ? 'var(--success)' : topProb >= 0.3 ? 'var(--warn)' : 'var(--text)'

  return (
    <>
      <div className="ui-page-head">
        <div className="ui-eyebrow accent">Simulate &amp; analyze</div>
        <h1 className="ui-h1">Next-step prediction</h1>
        <p className="ui-lead">
          Given labelled training sequences and an observed prefix, predict the most likely next node and which
          resident produced it.
        </p>
      </div>

      {/* inputs */}
      <div className="p-input-grid">
        {/* TRAINING */}
        <section className="ui-card">
          <div className="p-card-head">
            <div>
              <div className="p-card-head-title">Training sequences</div>
              <div className="p-card-head-sub">Ordered node tokens, with an optional resident label</div>
            </div>
            <button className="p-add-seq" onClick={addSeq}>
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
                <path d="M12 5v14M5 12h14" />
              </svg>
              Add sequence
            </button>
          </div>
          <div className="p-card-body">
            {training.map((row, i) => (
              <div className="p-seq-row" key={i}>
                <div className="p-person-wrap">
                  <select
                    className={`p-person-select${row.person === NO_PERSON ? ' muted' : ''}`}
                    value={row.person}
                    onChange={(e) => setPerson(i, e.target.value)}
                    aria-label="Resident label"
                  >
                    {PERSONS.map((person) => (
                      <option key={person} value={person}>
                        {person}
                      </option>
                    ))}
                  </select>
                  <ChevronDown className="p-select-chevron" />
                </div>
                <div className="p-chip-lane">
                  {row.tokens.map((tk, j) => (
                    <span className="p-chip" key={j}>
                      {tk}
                      <button className="p-chip-x" onClick={() => removeToken(i, j)} aria-label="Remove token">
                        <CloseIcon />
                      </button>
                      {j < row.tokens.length - 1 && <span className="p-arrow">›</span>}
                    </span>
                  ))}
                  <div className="p-add-wrap">
                    <select
                      className="p-add-select"
                      value=""
                      onChange={(e) => addToken(i, e.target.value)}
                      aria-label="Add node to sequence"
                    >
                      <option value="">+ node</option>
                      {VOCAB.map((v) => (
                        <option key={v} value={v}>
                          {v}
                        </option>
                      ))}
                    </select>
                    <ChevronDown className="p-add-chevron" />
                  </div>
                </div>
                <button className="p-seq-del" onClick={() => removeSeq(i)} aria-label="Delete sequence">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                  </svg>
                </button>
              </div>
            ))}
          </div>
        </section>

        {/* QUERY */}
        <section className="ui-card p-query">
          <div className="p-card-head">
            <div className="p-card-head-title">Query</div>
          </div>
          <div className="p-query-body">
            <div>
              <div className="p-field-label">Observed prefix</div>
              <div className="p-prefix-box">
                {prefix.map((tk, j) => (
                  <span className="p-chip accent" key={j}>
                    {tk}
                    <button className="p-chip-x" onClick={() => removePrefix(j)} aria-label="Remove">
                      <CloseIcon />
                    </button>
                    {j < prefix.length - 1 && <span className="p-arrow">›</span>}
                  </span>
                ))}
                <div className="p-add-wrap">
                  <select
                    className="p-add-select"
                    value=""
                    onChange={(e) => addPrefix(e.target.value)}
                    aria-label="Add node to prefix"
                  >
                    <option value="">+ node</option>
                    {VOCAB.map((v) => (
                      <option key={v} value={v}>
                        {v}
                      </option>
                    ))}
                  </select>
                  <ChevronDown className="p-add-chevron" />
                </div>
              </div>
            </div>

            <div>
              <div className="p-field-row">
                <span className="p-field-label" style={{ margin: 0 }}>
                  LCS threshold
                </span>
                <span
                  className="ui-help"
                  tabIndex={0}
                  onMouseEnter={() => setTip(true)}
                  onMouseLeave={() => setTip(false)}
                  onFocus={() => setTip(true)}
                  onBlur={() => setTip(false)}
                >
                  ?
                  {tip && (
                    <span className="ui-tip">
                      Minimum longest-common-subsequence length between the prefix and a training sequence for it to
                      count as a match. 0 uses all sequences.
                    </span>
                  )}
                </span>
              </div>
              <input
                className="p-lcs-input"
                type="number"
                min={0}
                max={10}
                value={lcs}
                onChange={(e) => {
                  let v = parseInt(e.target.value, 10)
                  if (isNaN(v) || v < 0) v = 0
                  if (v > 10) v = 10
                  setLcs(v)
                }}
              />
            </div>

            <button className={`p-run${running ? ' running' : ''}`} onClick={run} disabled={running || !prefix.length}>
              {running && <span className="ui-spin-sm" />}
              {running ? 'Predicting…' : 'Run prediction'}
            </button>
          </div>
        </section>
      </div>

      {/* RESULT */}
      <div className="p-result">
        {error && (
          <div className="ui-error">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
              <circle cx="12" cy="12" r="9" />
              <path d="M12 8v4M12 16h.01" />
            </svg>
            <div>
              <div className="ui-error-title">Prediction failed</div>
              <div className="ui-error-msg">{error}</div>
            </div>
          </div>
        )}

        {!result && !error && (
          <div className="ui-card p-empty">
            <div className="ui-empty-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <path d="M4 17l6-6 4 4 6-8" />
                <path d="M14 7h6v6" />
              </svg>
            </div>
            <div className="ui-empty-title">No prediction yet</div>
            <div className="ui-empty-desc">Set a prefix and run to see the next-node distribution.</div>
          </div>
        )}

        {result && !error && (
          <div className="p-result-grid">
            <div className="ui-card" style={{ overflow: 'hidden' }}>
              <div className="p-result-headline">
                <div className="ui-result-label">Predicted next node</div>
                <div className="p-result-headline-value">{result.predictedNextNode ?? '—'}</div>
              </div>
              <div className="p-result-meta-grid">
                <div>
                  <div className="p-meta-k">PERSON</div>
                  <div className="p-meta-v">{result.predictedPerson ?? 'Unknown'}</div>
                </div>
                <div>
                  <div className="p-meta-k">CONFIDENCE</div>
                  <div className="p-meta-v" style={{ color: confColor }}>
                    {Math.round(topProb * 100)}%
                  </div>
                </div>
                <div>
                  <div className="p-meta-k">RULE USED</div>
                  <div className="p-meta-v">{result.ruleUsed ?? '—'}</div>
                </div>
                <div>
                  <div className="p-meta-k">MATCH COUNT</div>
                  <div className="p-meta-v">{result.matchCount ?? 0}</div>
                </div>
              </div>
            </div>

            <div className="ui-card" style={{ overflow: 'hidden' }}>
              <div className="p-chart-head">
                Next-node distribution
                <span className="p-chart-tag">SOFTMAX</span>
              </div>
              <div className="p-chart-body">
                {dist.length > 0 ? (
                  <Plot
                    data={[
                      {
                        type: 'bar',
                        orientation: 'h',
                        x: bars.map((d) => d.probability ?? 0),
                        y: bars.map((d) => d.node as string),
                        marker: {
                          color: bars.map((_d, i) => (i === bars.length - 1 ? p.accent : p.weak)),
                          line: { width: 0 },
                        },
                        cliponaxis: false,
                        text: bars.map((d) => `${Math.round((d.probability ?? 0) * 100)}%`),
                        textposition: 'outside',
                        textfont: font,
                        hovertemplate: '%{y}: %{x:.0%}<extra></extra>',
                      },
                    ]}
                    layout={{
                      paper_bgcolor: 'transparent',
                      plot_bgcolor: 'transparent',
                      font,
                      showlegend: false,
                      margin: { l: 74, r: 40, t: 12, b: 30 },
                      xaxis: {
                        range: [0, Math.min(1, topProb * 1.2 + 0.02)],
                        tickformat: '.0%',
                        gridcolor: p.grid,
                        zeroline: false,
                      },
                      yaxis: { gridcolor: 'transparent' },
                    }}
                    style={{ width: '100%', height: '100%' }}
                    useResizeHandler
                    config={{ displayModeBar: false, responsive: true }}
                  />
                ) : (
                  <div className="ui-empty" style={{ paddingTop: 60 }}>
                    <div className="ui-empty-title">No distribution</div>
                    <div className="ui-empty-desc">The API returned no candidate next nodes for this prefix.</div>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </div>

      {toast && (
        <div className="ui-toast">
          <span className="ui-toast-icon">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
              <path d="M20 6 9 17l-5-5" />
            </svg>
          </span>
          <span className="ui-toast-msg">{toast}</span>
        </div>
      )}
    </>
  )
}
