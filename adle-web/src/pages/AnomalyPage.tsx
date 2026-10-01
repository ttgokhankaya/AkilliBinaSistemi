import { useRef, useState } from 'react'
import Plot from 'react-plotly.js'
import { api, type RandomForestResponseDto } from '../api/client'
import { useTheme } from '../ThemeContext'
import './AnomalyPage.css'

// Design: "ADLE Anomaly.dc.html". The mockup fabricated data with Math.random;
// here the same UI drives the real /api/anomaly/* endpoints. A deterministic
// 6-D sample with three well-separated clusters gives t-SNE something to pull
// apart and the forest a normal-vs-anomalous split to route on. Every status
// label (obs count, sample sizes, matrix size) is derived from this sample.
const CENTERS = [
  [0, 0, 0, 0, 0, 0],
  [8, 8, 8, 8, 8, 8],
  [0, 8, 0, 8, 0, 8],
]
const PER_CLUSTER = 15
const FEATURES = CENTERS[0].length
const N_OBS = CENTERS.length * PER_CLUSTER
const CLUSTER_NAMES = ['Cluster A', 'Cluster B', 'Cluster C']

function buildSample(): { observations: number[][]; clusters: number[] } {
  // Small deterministic PRNG so runs are reproducible (no Math.random).
  let seed = 1337
  const rand = () => {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff
    return seed / 0x7fffffff
  }
  const observations: number[][] = []
  const clusters: number[] = []
  CENTERS.forEach((center, c) => {
    for (let k = 0; k < PER_CLUSTER; k++) {
      observations.push(center.map((v) => v + (rand() - 0.5) * 3))
      clusters.push(c)
    }
  })
  return { observations, clusters }
}

// Plotly palettes per theme (SVG can't read CSS vars), matching the design tokens.
const PLOT = {
  light: {
    text2: '#525b6b',
    grid: '#e2e6ec',
    seq: ['#3b5bdb', '#0d9488', '#a6690f'],
    heat: [
      [0, '#eef2fb'],
      [0.5, '#8fa4ec'],
      [1, '#26398f'],
    ],
  },
  dark: {
    text2: '#9aa5b6',
    grid: '#2a3342',
    seq: ['#5b7cfa', '#2dd4bf', '#e0a53a'],
    heat: [
      [0, '#141b28'],
      [0.5, '#3a4f9e'],
      [1, '#7f9bff'],
    ],
  },
}

// Rough depth from sklearn export_text indentation (each level adds a "|").
function treeDepth(text: string): number {
  let max = 0
  for (const line of text.split('\n')) {
    const bars = line.split('|').length - 1
    if (bars > max) max = bars
  }
  return max
}

const HelpTip = ({ text, width }: { text: string; width?: number }) => {
  const [open, setOpen] = useState(false)
  return (
    <span
      className="a-help"
      tabIndex={0}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
      onFocus={() => setOpen(true)}
      onBlur={() => setOpen(false)}
    >
      ?
      {open && (
        <span className="a-tip" style={width ? { width } : undefined}>
          {text}
        </span>
      )}
    </span>
  )
}

function TsneSection({ fire }: { fire: (msg: string) => void }) {
  const { theme } = useTheme()
  const [hasData, setHasData] = useState(false)
  const [running, setRunning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [perplexity, setPerplexity] = useState(8)
  const [nComp, setNComp] = useState(2)
  const [output, setOutput] = useState<number[][] | null>(null)
  const [clusters, setClusters] = useState<number[]>([])

  async function run(comp = nComp) {
    if (running) return
    // sklearn requires perplexity < n_samples — surface it as the design's error
    // banner instead of a round-trip that 500s.
    if (perplexity >= N_OBS) {
      setError(
        `Perplexity (${perplexity}) must be less than the number of observations (${N_OBS}). Lower it and run again.`,
      )
      return
    }
    setRunning(true)
    setError(null)
    try {
      const sample = buildSample()
      const res = await api.tsne({ observations: sample.observations, perplexity, nComponents: comp })
      setOutput(res.output ?? [])
      setClusters(sample.clusters)
      fire(`Projected ${N_OBS} observations`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setRunning(false)
    }
  }

  function load() {
    setHasData(true)
    run()
  }

  function setComp(c: number) {
    setNComp(c)
    if (hasData) run(c) // 2-D vs 3-D is a different embedding — re-project
  }

  const p = PLOT[theme]
  const font = { family: 'ui-monospace, Consolas, monospace', size: 11, color: p.text2 }
  const axis = { gridcolor: p.grid, zerolinecolor: p.grid, tickfont: font, showline: false, title: '' }
  const traces =
    output && !error
      ? [0, 1, 2].map((c) => {
          const rows = output.filter((_, i) => clusters[i] === c)
          const common = {
            name: CLUSTER_NAMES[c],
            marker: { color: p.seq[c], size: nComp === 3 ? 4 : 8, line: { width: 1, color: 'rgba(255,255,255,.25)' } },
            hovertemplate: `${CLUSTER_NAMES[c]}<extra></extra>`,
          }
          if (nComp === 3) {
            return {
              ...common,
              type: 'scatter3d',
              mode: 'markers',
              x: rows.map((r) => r[0]),
              y: rows.map((r) => r[1]),
              z: rows.map((r) => r[2] ?? 0),
            }
          }
          return { ...common, type: 'scatter', mode: 'markers', x: rows.map((r) => r[0]), y: rows.map((r) => r[1]) }
        })
      : []
  const layout =
    nComp === 3
      ? {
          paper_bgcolor: 'transparent',
          font,
          margin: { l: 0, r: 0, t: 0, b: 0 },
          legend: { font, orientation: 'h', y: 1.05 },
          scene: {
            xaxis: { ...axis, backgroundcolor: 'transparent' },
            yaxis: { ...axis, backgroundcolor: 'transparent' },
            zaxis: { ...axis, backgroundcolor: 'transparent' },
          },
        }
      : {
          paper_bgcolor: 'transparent',
          plot_bgcolor: 'transparent',
          font,
          margin: { l: 38, r: 14, t: 28, b: 34 },
          legend: { font, orientation: 'h', y: 1.08, x: 0 },
          xaxis: axis,
          yaxis: axis,
        }

  return (
    <section className="a-panel">
      <div className="a-panel-head">
        <div className="a-panel-head-l">
          <div className="a-icon">
            <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="6" cy="7" r="2" />
              <circle cx="17" cy="6" r="2" />
              <circle cx="9" cy="17" r="2" />
              <circle cx="18" cy="16" r="2" />
              <circle cx="12" cy="11" r="1.5" />
            </svg>
          </div>
          <div>
            <div className="a-panel-title">t-SNE projection</div>
            <div className="a-panel-sub">Dimensionality reduction of observation vectors</div>
          </div>
        </div>
        <button className={`a-btn${running ? ' running' : ''}`} onClick={() => run()} disabled={!hasData}>
          {running && <span className="a-spin-sm" />}
          {running ? 'Running…' : 'Re-run'}
        </button>
      </div>

      <div className="adle-stack">
        <div className="adle-ctl">
          <div>
            <div className="a-ctl-label">Observations</div>
            <div className="a-status-box">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--text-3)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="3" width="18" height="18" rx="2" />
                <path d="M3 9h18M9 21V9" />
              </svg>
              <span className="a-status-val" style={{ color: hasData ? 'var(--text)' : 'var(--text-3)' }}>
                {hasData ? `${N_OBS} obs · ${FEATURES} features` : 'not loaded'}
              </span>
            </div>
          </div>

          <div>
            <div className="a-ctl-row">
              <span className="lbl">Perplexity</span>
              <HelpTip text="Balances local vs global structure. Must be < number of observations. Lower ≈ tighter local clusters." />
              <span className="a-slider-val">{perplexity}</span>
            </div>
            <input
              type="range"
              min={0.5}
              max={60}
              step={0.5}
              value={perplexity}
              onChange={(e) => setPerplexity(parseFloat(e.target.value))}
              style={{ width: '100%' }}
            />
            <div className="a-range-legend">
              <span>0.5</span>
              <span>60</span>
            </div>
          </div>

          <div>
            <div className="a-ctl-label">Components</div>
            <div className="a-seg">
              {[2, 3].map((c) => (
                <button key={c} className={`a-seg-btn${nComp === c ? ' active' : ''}`} onClick={() => setComp(c)}>
                  {c}D
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="a-result">
          {error && (
            <div className="a-error">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
                <circle cx="12" cy="12" r="9" />
                <path d="M12 8v4M12 16h.01" />
              </svg>
              <div>
                <div className="a-error-title">Invalid parameters</div>
                <div className="a-error-msg">{error}</div>
              </div>
            </div>
          )}
          <div className="a-chart-frame">
            {output && !error && (
              <Plot
                data={traces}
                layout={layout}
                style={{ width: '100%', height: '100%' }}
                useResizeHandler
                config={{ displayModeBar: false, responsive: true }}
              />
            )}
            {!hasData && (
              <div className="a-empty-overlay">
                <div>
                  <div className="a-empty-icon">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <circle cx="7" cy="8" r="2.5" />
                      <circle cx="17" cy="9" r="2.5" />
                      <circle cx="11" cy="16" r="2.5" />
                    </svg>
                  </div>
                  <div className="a-empty-title">No projection yet</div>
                  <div className="a-empty-desc">Load synthetic sample observations to explore the reduced space.</div>
                  <button className="a-load-btn" onClick={load}>
                    Load sample data
                  </button>
                </div>
              </div>
            )}
            {running && (
              <div className="a-overlay">
                <div className="a-overlay-col">
                  <span className="a-spin" />
                  <span className="a-overlay-txt">Computing embedding…</span>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  )
}

function RandomForestSection({ fire }: { fire: (msg: string) => void }) {
  const { theme } = useTheme()
  const [hasData, setHasData] = useState(false)
  const [running, setRunning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [nTrees, setNTrees] = useState(100)
  const [result, setResult] = useState<RandomForestResponseDto | null>(null)
  const [counts, setCounts] = useState({ s0: 0, s1: 0, all: 0 })
  const [open, setOpen] = useState<Record<number, boolean>>({ 0: true })

  async function run() {
    if (running) return
    setRunning(true)
    setError(null)
    try {
      const { observations, clusters } = buildSample()
      // Train "normal" (cluster 0) vs "anomalous" (cluster 1); score all points.
      const s0 = observations.filter((_, i) => clusters[i] === 0)
      const s1 = observations.filter((_, i) => clusters[i] === 1)
      setCounts({ s0: s0.length, s1: s1.length, all: observations.length })
      const res = await api.randomForest({ s0, s1, allObservations: observations, nTrees })
      setResult(res)
      fire(`Forest built · ${nTrees} trees`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setRunning(false)
    }
  }

  function load() {
    setHasData(true)
    run()
  }

  const p = PLOT[theme]
  const matrix = result?.proximityMatrix ?? null
  const dim = matrix ? matrix.length : 0
  const treeTexts = result?.treeTexts ?? []
  const shown = Math.min(4, treeTexts.length)
  const font = { family: 'ui-monospace, Consolas, monospace', size: 10, color: p.text2 }
  const heatData = matrix
    ? [
        {
          type: 'heatmap',
          z: matrix,
          colorscale: p.heat,
          zmin: 0,
          zmax: 1,
          xgap: 1.5,
          ygap: 1.5,
          colorbar: { thickness: 9, len: 0.85, outlinewidth: 0, tickfont: font },
          hovertemplate: '%{y} ↔ %{x}: %{z:.2f}<extra></extra>',
        },
      ]
    : []
  const heatLayout = {
    paper_bgcolor: 'transparent',
    plot_bgcolor: 'transparent',
    font,
    margin: { l: 30, r: 6, t: 6, b: 26 },
    xaxis: { showgrid: false },
    yaxis: { showgrid: false, autorange: 'reversed' },
  }

  return (
    <section className="a-panel">
      <div className="a-panel-head">
        <div className="a-panel-head-l">
          <div className="a-icon">
            <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 22V11M12 11 7 7M12 11l5-4M12 6a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM7 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM17 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" />
            </svg>
          </div>
          <div>
            <div className="a-panel-title">Random Forest proximity</div>
            <div className="a-panel-sub">Sample-to-sample similarity &amp; decision trees</div>
          </div>
        </div>
        <button className={`a-btn${running ? ' running' : ''}`} onClick={() => run()} disabled={!hasData}>
          {running && <span className="a-spin-sm" />}
          {running ? 'Building…' : 'Rebuild'}
        </button>
      </div>

      <div className="adle-stack">
        <div className="adle-ctl">
          <div>
            <div className="a-ctl-label">Training samples</div>
            <div className="a-samples">
              <div className="a-sample-row">
                <span className="a-sample-lbl">S0 · normal</span>
                <span className="a-sample-val" style={{ color: hasData ? 'var(--text)' : 'var(--text-3)' }}>
                  {hasData ? counts.s0 : '—'}
                </span>
              </div>
              <div className="a-sample-row">
                <span className="a-sample-lbl">S1 · anomalous</span>
                <span className="a-sample-val" style={{ color: hasData ? 'var(--text)' : 'var(--text-3)' }}>
                  {hasData ? counts.s1 : '—'}
                </span>
              </div>
              <div className="a-sample-row">
                <span className="a-sample-lbl">All obs.</span>
                <span className="a-sample-val" style={{ color: hasData ? 'var(--text)' : 'var(--text-3)' }}>
                  {hasData ? counts.all : '—'}
                </span>
              </div>
            </div>
          </div>

          <div>
            <div className="a-ctl-row">
              <span className="lbl">Trees</span>
              <HelpTip text="More trees = smoother, more stable proximity estimates, at higher compute cost." width={200} />
            </div>
            <input
              className="a-num"
              type="number"
              min={1}
              max={500}
              value={nTrees}
              onChange={(e) => {
                let v = parseInt(e.target.value, 10)
                if (isNaN(v) || v < 1) v = 1
                if (v > 500) v = 500
                setNTrees(v)
              }}
            />
          </div>
        </div>

        <div className="a-result rf">
          {error && (
            <div className="a-error">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
                <circle cx="12" cy="12" r="9" />
                <path d="M12 8v4M12 16h.01" />
              </svg>
              <div>
                <div className="a-error-title">Request failed</div>
                <div className="a-error-msg">{error}</div>
              </div>
            </div>
          )}

          {!hasData && !error && (
            <div className="a-empty-box">
              <div>
                <div className="a-empty-icon">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                    <rect x="3" y="3" width="7" height="7" rx="1" />
                    <rect x="14" y="3" width="7" height="7" rx="1" />
                    <rect x="3" y="14" width="7" height="7" rx="1" />
                    <rect x="14" y="14" width="7" height="7" rx="1" />
                  </svg>
                </div>
                <div className="a-empty-title">No proximity matrix yet</div>
                <div className="a-empty-desc">Load sample training data to build the forest and compute proximities.</div>
                <button className="a-load-btn" onClick={load}>
                  Load sample data
                </button>
              </div>
            </div>
          )}

          {hasData && !error && (
            <div className="a-rf-stack">
              <div className="a-heat-card">
                <div className="a-heat-head">
                  Proximity matrix
                  <span className="a-heat-dim">
                    {dim}×{dim}
                  </span>
                </div>
                <div className="a-heat-body">
                  {matrix && (
                    <Plot
                      data={heatData}
                      layout={heatLayout}
                      style={{ width: '100%', height: '100%' }}
                      useResizeHandler
                      config={{ displayModeBar: false, responsive: true }}
                    />
                  )}
                  {running && (
                    <div className="a-overlay">
                      <span className="a-spin" />
                    </div>
                  )}
                </div>
              </div>

              <div>
                <div className="a-trees-head">
                  Decision trees
                  <span className="a-trees-note">{treeTexts.length ? `showing ${shown} of ${treeTexts.length}` : ''}</span>
                </div>
                <div className="a-trees-list">
                  {treeTexts.slice(0, 4).map((text, i) => (
                    <div className="a-tree" key={i}>
                      <button className="a-tree-head" onClick={() => setOpen((o) => ({ ...o, [i]: !o[i] }))}>
                        <span className={`a-chev${open[i] ? ' open' : ''}`}>▸</span>
                        <span className="a-tree-name">Tree {i + 1}</span>
                        <span className="a-tree-depth">depth {treeDepth(text)}</span>
                      </button>
                      {open[i] && <pre className="a-tree-pre">{text}</pre>}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </section>
  )
}

export function AnomalyPage() {
  const [toast, setToast] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (timer.current) window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => setToast(null), 2600)
  }

  return (
    <>
      <div className="a-head">
        <div className="a-eyebrow">Simulate &amp; analyze</div>
        <h1 className="a-title">Anomaly detection</h1>
        <p className="a-desc">
          Two complementary tools for finding behavior that deviates from the norm — a t-SNE projection of activity
          feature vectors, and a Random Forest proximity analysis.
        </p>
      </div>

      <TsneSection fire={fire} />
      <RandomForestSection fire={fire} />

      {toast && (
        <div className="a-toast">
          <span className="a-toast-check">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
              <path d="M20 6 9 17l-5-5" />
            </svg>
          </span>
          <span className="a-toast-msg">{toast}</span>
        </div>
      )}
    </>
  )
}
