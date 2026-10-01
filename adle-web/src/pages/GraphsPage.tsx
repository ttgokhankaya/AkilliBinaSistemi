import { useEffect, useMemo, useRef, useState } from 'react'
import { api, type GraphDto, type RunGraphResponse } from '../api/client'
import './GraphsPage.css'

// Design: "ADLE Graphs.dc.html". The mockup fabricated presets and simulated
// walks in the browser; here the same three-panel workspace (matrix editor,
// topology preview, run panel) drives the real /api/graphs endpoints. The
// stored contract is a tab-separated adjacency matrix (the format
// Graph.ToMatrisString produces / CreateNewGraphFromMatrixString parses):
// first row = node names, each subsequent row = a "from" node's outgoing weights.

// A ready-to-run linear graph A->B->C->D as a TSV adjacency matrix — used to
// seed the "new graph" draft when nothing is saved yet.
const SAMPLE_MATRIX = [
  '\tA\tB\tC\tD',
  'A\t0\t1\t0\t0',
  'B\t0\t0\t1\t0',
  'C\t0\t0\t0\t1',
  'D\t0\t0\t0\t0',
].join('\n')

type ParsedMatrix = {
  nodes: string[]
  rows: number[][]
}

// Parse the TSV contract into node names + a square numeric matrix. Returns
// null when the shape is inconsistent so the UI can fall back to a raw editor.
function parseMatrix(tsv: string): ParsedMatrix | null {
  const lines = tsv.replace(/\r/g, '').split('\n').filter((l) => l.length > 0)
  if (lines.length < 2) return null
  const nodes = lines[0].split('\t').slice(1).map((n) => n.trim())
  if (nodes.length === 0) return null
  const rows: number[][] = []
  for (let i = 1; i < lines.length; i++) {
    const cells = lines[i].split('\t')
    const values = cells.slice(1).map((c) => {
      const v = parseFloat(c)
      return isNaN(v) ? 0 : v
    })
    if (values.length !== nodes.length) return null
    rows.push(values)
  }
  if (rows.length !== nodes.length) return null
  return { nodes, rows }
}

// Serialize node names + numeric matrix back to the TSV contract.
function toTsv(nodes: string[], rows: number[][]): string {
  const header = '\t' + nodes.join('\t')
  const body = rows.map((row, i) => nodes[i] + '\t' + row.join('\t'))
  return [header, ...body].join('\n')
}

const HelpTip = ({ text }: { text: string }) => {
  const [open, setOpen] = useState(false)
  return (
    <span
      className="ui-help"
      tabIndex={0}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
      onFocus={() => setOpen(true)}
      onBlur={() => setOpen(false)}
    >
      ?
      {open && <span className="ui-tip">{text}</span>}
    </span>
  )
}

export function GraphsPage() {
  const [graphs, setGraphs] = useState<GraphDto[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  // Editable matrix state (node names + numeric grid). Kept as the parsed form
  // so the grid + SVG preview stay in sync; serialized to TSV on create.
  const [nodes, setNodes] = useState<string[]>([])
  const [matrix, setMatrix] = useState<number[][]>([])
  const [rawFallback, setRawFallback] = useState<string | null>(null)

  // New-graph draft
  const [draftName, setDraftName] = useState('New graph')

  // Run parameters (node names, per the RunGraphRequest contract)
  const [startNode, setStartNode] = useState('')
  const [endNode, setEndNode] = useState('')
  const [iterations, setIterations] = useState(25)
  const [useEdgeWeights, setUseEdgeWeights] = useState(true)
  const [allowLoops, setAllowLoops] = useState(false)

  const [running, setRunning] = useState(false)
  const [runError, setRunError] = useState<string | null>(null)
  const [run, setRun] = useState<RunGraphResponse | null>(null)
  const [highlight, setHighlight] = useState<string[] | null>(null)
  const [confirmDel, setConfirmDel] = useState(false)

  const [toast, setToast] = useState<string | null>(null)
  const toastTimer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (toastTimer.current) window.clearTimeout(toastTimer.current)
    toastTimer.current = window.setTimeout(() => setToast(null), 2600)
  }

  // Load the given graph's matrix into the editor; falls back to raw TSV text
  // when the stored value can't be parsed into a clean square matrix.
  function loadMatrix(tsv: string) {
    const parsed = parseMatrix(tsv)
    if (parsed) {
      setNodes(parsed.nodes)
      setMatrix(parsed.rows)
      setRawFallback(null)
      setStartNode((s) => (parsed.nodes.includes(s) ? s : parsed.nodes[0] ?? ''))
      setEndNode((e) =>
        parsed.nodes.includes(e) ? e : parsed.nodes[parsed.nodes.length - 1] ?? '',
      )
    } else {
      setNodes([])
      setMatrix([])
      setRawFallback(tsv)
    }
  }

  async function reload(selectAfter?: number) {
    setLoading(true)
    setListError(null)
    try {
      const list = await api.graphs()
      setGraphs(list)
      const target =
        selectAfter != null
          ? selectAfter
          : selectedId != null && list.some((g) => g.id === selectedId)
            ? selectedId
            : (list[0]?.id ?? null)
      setSelectedId(target)
      const g = list.find((x) => x.id === target)
      if (g?.matrixValue) loadMatrix(g.matrixValue)
      else if (list.length === 0) loadMatrix(SAMPLE_MATRIX)
    } catch (e) {
      setListError(e instanceof Error ? e.message : String(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function selectGraph(id: number) {
    setSelectedId(id)
    setRun(null)
    setRunError(null)
    setHighlight(null)
    const g = graphs.find((x) => x.id === id)
    if (g?.matrixValue) loadMatrix(g.matrixValue)
  }

  function setCell(i: number, j: number, raw: string) {
    let v = parseFloat(raw)
    if (isNaN(v) || v < 0) v = 0
    setMatrix((m) => {
      const next = m.map((r) => r.slice())
      next[i][j] = v
      return next
    })
  }

  // Keep the start/end selections valid after the node set changes.
  function fixEnds(ns: string[]) {
    setStartNode((s) => (ns.includes(s) ? s : (ns[0] ?? '')))
    setEndNode((e) => (ns.includes(e) ? e : (ns[ns.length - 1] ?? '')))
  }

  function uniqueName(existing: string[]): string {
    for (let i = 0; i < 26; i++) {
      const c = String.fromCharCode(65 + i)
      if (!existing.includes(c)) return c
    }
    let k = existing.length + 1
    while (existing.includes('N' + k)) k++
    return 'N' + k
  }

  // Grow the matrix by one node: append a 0 column to every row, then a 0 row.
  function addNode() {
    const name = uniqueName(nodes)
    const nextNodes = [...nodes, name]
    const nextMatrix = matrix.map((r) => [...r, 0])
    nextMatrix.push(new Array(nextNodes.length).fill(0))
    setRawFallback(null)
    setNodes(nextNodes)
    setMatrix(nextMatrix)
    fixEnds(nextNodes)
  }

  // Drop a node and its row + column. Keep at least two so a walk has ends.
  function removeNode(idx: number) {
    if (nodes.length <= 2) return
    const nextNodes = nodes.filter((_, i) => i !== idx)
    const nextMatrix = matrix.filter((_, i) => i !== idx).map((r) => r.filter((_, j) => j !== idx))
    setNodes(nextNodes)
    setMatrix(nextMatrix)
    fixEnds(nextNodes)
  }

  function renameNode(idx: number, name: string) {
    const old = nodes[idx]
    setNodes((prev) => prev.map((n, i) => (i === idx ? name : n)))
    setStartNode((s) => (s === old ? name : s))
    setEndNode((e) => (e === old ? name : e))
  }

  // Begin a fresh 2-node draft to build from scratch (unsaved until "Save").
  function startNewDraft() {
    setSelectedId(null)
    setNodes(['A', 'B'])
    setMatrix([
      [0, 1],
      [0, 0],
    ])
    setRawFallback(null)
    setDraftName('New graph')
    setStartNode('A')
    setEndNode('B')
    setRun(null)
    setRunError(null)
    setHighlight(null)
  }

  // Persist the current editor state as a NEW graph. The client exposes no
  // update endpoint, so edits to an existing graph are saved as a new one.
  async function saveAsNew() {
    setListError(null)
    const matrixValue = rawFallback != null ? rawFallback : toTsv(nodes, matrix)
    try {
      const g = await api.createGraph({ name: draftName.trim() || 'Untitled graph', matrixValue })
      fire(`Saved “${g.name}”`)
      await reload(g.id)
    } catch (e) {
      setListError(e instanceof Error ? e.message : String(e))
    }
  }

  // Persist edits to the currently selected graph in place (PUT).
  async function saveChanges() {
    if (selectedId == null) return
    setListError(null)
    const matrixValue = rawFallback != null ? rawFallback : toTsv(nodes, matrix)
    const name = graphs.find((g) => g.id === selectedId)?.name || draftName.trim() || 'Untitled graph'
    try {
      await api.updateGraph(selectedId, { name, matrixValue })
      fire('Changes saved')
      await reload(selectedId)
    } catch (e) {
      setListError(e instanceof Error ? e.message : String(e))
    }
  }

  async function doDelete() {
    if (selectedId == null) return
    const id = selectedId
    setListError(null)
    try {
      await api.deleteGraph(id)
      setConfirmDel(false)
      setSelectedId(null)
      fire('Graph deleted')
      await reload()
    } catch (e) {
      setConfirmDel(false)
      setListError(e instanceof Error ? e.message : String(e))
    }
  }

  async function runSelected() {
    if (selectedId == null || running) return
    setRunning(true)
    setRunError(null)
    setRun(null)
    setHighlight(null)
    try {
      const res = await api.runGraph(selectedId, {
        startNode,
        endNode,
        iterations,
        useEdgeWeights,
        allowLoops,
      })
      setRun(res)
      fire(`${res.walkCount ?? res.walks?.length ?? 0} walks simulated`)
    } catch (e) {
      setRunError(e instanceof Error ? e.message : String(e))
    } finally {
      setRunning(false)
    }
  }

  // Derived stats from the real walk response (mirrors the mockup's cards).
  const stats = useMemo(() => {
    const walks = run?.walks ?? []
    if (walks.length === 0) return null
    const reached = walks.filter((w) => w.length > 0 && w[w.length - 1] === endNode).length
    const totalLen = walks.reduce((a, w) => a + w.length, 0)
    return {
      count: run?.walkCount ?? walks.length,
      reachedPct: Math.round((reached / walks.length) * 100) + '%',
      avgLen: (totalLen / walks.length).toFixed(1),
    }
  }, [run, endNode])

  // SVG topology layout — nodes on a circle, curved directed edges. The
  // hovered walk highlights the edges it traverses.
  const preview = useMemo(() => {
    const n = nodes.length
    if (n === 0) return null
    const cx0 = 200,
      cy0 = 150,
      R = 108
    const pos = nodes.map((_, i) => {
      const a = ((-90 + (i * 360) / n) * Math.PI) / 180
      return { x: cx0 + R * Math.cos(a), y: cy0 + R * Math.sin(a) }
    })
    const hiSet = new Set<string>()
    if (highlight) {
      for (let k = 0; k < highlight.length - 1; k++) {
        const a = nodes.indexOf(highlight[k])
        const b = nodes.indexOf(highlight[k + 1])
        if (a >= 0 && b >= 0) hiSet.add(a + '-' + b)
      }
    }
    const edges: {
      key: string
      d: string
      stroke: string
      sw: number
      marker: string
      dashed: boolean
    }[] = []
    for (let i = 0; i < n; i++)
      for (let j = 0; j < n; j++) {
        if (i === j || !matrix[i] || matrix[i][j] <= 0) continue
        const a = pos[i],
          b = pos[j]
        const dx = b.x - a.x,
          dy = b.y - a.y,
          len = Math.hypot(dx, dy) || 1
        const ux = dx / len,
          uy = dy / len,
          r = 18
        const sx = a.x + ux * r,
          sy = a.y + uy * r,
          ex = b.x - ux * r,
          ey = b.y - uy * r
        const curve = len * 0.16
        const mx = (a.x + b.x) / 2 - uy * curve,
          my = (a.y + b.y) / 2 + ux * curve
        const on = hiSet.has(i + '-' + j)
        edges.push({
          key: i + '-' + j,
          d: `M${sx.toFixed(1)} ${sy.toFixed(1)} Q${mx.toFixed(1)} ${my.toFixed(1)} ${ex.toFixed(1)} ${ey.toFixed(1)}`,
          stroke: on ? 'var(--accent)' : 'var(--border-strong)',
          sw: on ? 3 : Math.min(3, 1 + matrix[i][j] * 0.4),
          marker: on ? 'url(#g-arrA)' : 'url(#g-arr)',
          dashed: on,
        })
      }
    const gnodes = nodes.map((name, i) => {
      const isStart = name === startNode,
        isEnd = name === endNode
      return {
        cx: pos[i].x.toFixed(1),
        cy: pos[i].y.toFixed(1),
        labelY: (pos[i].y + 3.5).toFixed(1),
        label: name,
        fill: isStart ? 'var(--success-weak)' : isEnd ? 'var(--accent-weak)' : 'var(--surface-2)',
        stroke: isStart ? 'var(--success)' : isEnd ? 'var(--accent)' : 'var(--border-strong)',
        sw: isStart || isEnd ? 2.5 : 1.5,
      }
    })
    return { edges, gnodes }
  }, [nodes, matrix, startNode, endNode, highlight])

  const canRun = selectedId != null && startNode !== '' && endNode !== '' && !running

  return (
    <>
      <div className="g-head">
        <div>
          <div className="ui-eyebrow accent">Simulate &amp; analyze</div>
          <h1 className="ui-h1">Graphs</h1>
          <p className="ui-lead">
            Model the home&apos;s state-transition topology, then simulate resident movement as
            random walks.
          </p>
        </div>
        <div className="g-head-actions">
          <div className="g-picker">
            {graphs.length === 0 ? (
              <span className="g-picker-empty">No saved graphs yet</span>
            ) : (
              graphs.map((g) => (
                <button
                  key={g.id}
                  className={`g-picker-btn${selectedId === g.id ? ' active' : ''}`}
                  onClick={() => selectGraph(g.id)}
                  title={g.name ?? `Graph #${g.id}`}
                >
                  {g.name || `#${g.id}`}
                </button>
              ))
            )}
          </div>
          <div className="ui-toolbar">
            <button className="ui-btn secondary" onClick={startNewDraft}>
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M12 5v14M5 12h14" />
              </svg>
              New
            </button>
            {selectedId != null ? (
              <>
                <button className="ui-btn" onClick={saveChanges}>Save changes</button>
                <button className="ui-btn ghost" onClick={saveAsNew}>Save as copy</button>
                <button className="ui-icon-btn danger" onClick={() => setConfirmDel(true)} aria-label="Delete graph" title="Delete graph">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                  </svg>
                </button>
              </>
            ) : (
              <button className="ui-btn" onClick={saveAsNew}>Save graph</button>
            )}
          </div>
        </div>
      </div>

      {listError && (
        <div className="ui-error" style={{ marginBottom: 18 }}>
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="var(--danger)"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{ flex: 'none', marginTop: 1 }}
          >
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Something went wrong</div>
            <div className="ui-error-msg">{listError}</div>
          </div>
        </div>
      )}

      <div className="g-stack">
        {/* MATRIX EDITOR */}
        <section className="g-panel">
          <div className="g-panel-head">
            <div>
              <div className="g-panel-title">Adjacency matrix</div>
              <div className="g-panel-sub">Transition weight from row &#8594; column</div>
            </div>
            <span className="g-tsv-tag">TSV</span>
          </div>
          {rawFallback != null ? (
            <div className="g-matrix-fallback">
              <div className="ui-field">
                <label className="ui-label">Adjacency matrix (tab-separated)</label>
                <textarea
                  className="ui-textarea g-raw"
                  rows={7}
                  value={rawFallback}
                  onChange={(e) => setRawFallback(e.target.value)}
                />
                <span className="ui-hint">
                  First row = node names, each following row = a node&apos;s outgoing weights.
                </span>
              </div>
            </div>
          ) : nodes.length === 0 ? (
            <div className="g-matrix-fallback">
              <div className="ui-empty">
                <div className="ui-empty-title">No matrix loaded</div>
                <div className="ui-empty-desc">Select or create a graph to edit its matrix.</div>
              </div>
            </div>
          ) : (
            <div className="g-matrix-body">
              <table className="g-matrix">
                <thead>
                  <tr>
                    <th className="g-corner">from &#9656; to</th>
                    {nodes.map((h, j) => (
                      <th key={j} className="g-colhead">
                        <span className="g-node-edit">
                          <input
                            className="g-node-name"
                            value={h}
                            aria-label={`Node ${j + 1} name`}
                            onChange={(e) => renameNode(j, e.target.value)}
                          />
                          {nodes.length > 2 && (
                            <button
                              className="g-node-x"
                              onClick={() => removeNode(j)}
                              aria-label={`Remove node ${h}`}
                              title="Remove node"
                            >
                              &times;
                            </button>
                          )}
                        </span>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {matrix.map((row, i) => (
                    <tr key={i}>
                      <th className="g-rowhead">{nodes[i]}</th>
                      {row.map((v, j) => (
                        <td key={j}>
                          <input
                            className={`g-cell${v > 0 ? ' set' : ''}${i === j ? ' diag' : ''}`}
                            value={v}
                            onChange={(e) => setCell(i, j, e.target.value)}
                            inputMode="decimal"
                          />
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
              <div className="g-matrix-foot">
                <button className="ui-btn ghost" onClick={addNode}>
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M12 5v14M5 12h14" />
                  </svg>
                  Add node
                </button>
                <p className="g-matrix-note">
                  0 = no edge. Higher values are more likely transitions when edge weights are on.
                </p>
              </div>
            </div>
          )}
        </section>

        {/* TOPOLOGY PREVIEW */}
        <section className="g-panel">
          <div className="g-panel-head">
            <div className="g-panel-title">Topology preview</div>
            <div className="g-preview-legend">
              <span className="g-legend-item">
                <span className="g-legend-dot" style={{ background: 'var(--success)' }} />
                Start
              </span>
              <span className="g-legend-item">
                <span className="g-legend-dot" style={{ background: 'var(--accent)' }} />
                End
              </span>
            </div>
          </div>
          <div className="g-preview-body">
            {preview ? (
              <svg viewBox="0 0 400 300" className="g-preview-svg">
                <defs>
                  <marker id="g-arr" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
                    <path d="M0 0 L8 4 L0 8 z" fill="var(--border-strong)" />
                  </marker>
                  <marker
                    id="g-arrA"
                    markerWidth="9"
                    markerHeight="9"
                    refX="7.5"
                    refY="4.5"
                    orient="auto"
                  >
                    <path d="M0 0 L9 4.5 L0 9 z" fill="var(--accent)" />
                  </marker>
                </defs>
                {preview.edges.map((e) => (
                  <path
                    key={e.key}
                    d={e.d}
                    fill="none"
                    stroke={e.stroke}
                    strokeWidth={e.sw}
                    markerEnd={e.marker}
                    style={
                      e.dashed
                        ? { strokeDasharray: '5 6', animation: 'adle-dash .6s linear infinite' }
                        : undefined
                    }
                  />
                ))}
                {preview.gnodes.map((nd) => (
                  <g key={nd.label}>
                    <circle cx={nd.cx} cy={nd.cy} r="17" fill={nd.fill} stroke={nd.stroke} strokeWidth={nd.sw} />
                    <text
                      x={nd.cx}
                      y={nd.labelY}
                      textAnchor="middle"
                      style={{ font: '700 10px system-ui', fill: 'var(--text-2)' }}
                    >
                      {nd.label}
                    </text>
                  </g>
                ))}
              </svg>
            ) : (
              <div className="ui-empty">
                <div className="ui-empty-title">Nothing to preview</div>
                <div className="ui-empty-desc">
                  The matrix must parse into named nodes to draw the topology.
                </div>
              </div>
            )}
          </div>
        </section>

        {/* RUN PANEL */}
        <section className="g-panel g-run">
          <div className="g-panel-head">
            <div className="g-panel-title">Run walk</div>
          </div>
          <div className="g-run-body">
            <div className="g-run-grid">
              <div className="ui-field">
                <label className="ui-label">Start node</label>
                <div className="ui-select-wrap">
                  <select
                    className="ui-select"
                    value={startNode}
                    onChange={(e) => setStartNode(e.target.value)}
                  >
                    {nodes.length === 0 && <option value="">—</option>}
                    {nodes.map((n) => (
                      <option key={n} value={n}>
                        {n}
                      </option>
                    ))}
                  </select>
                  <svg
                    className="ui-select-chevron"
                    width="13"
                    height="13"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.4"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M6 9l6 6 6-6" />
                  </svg>
                </div>
              </div>
              <div className="ui-field">
                <label className="ui-label">End node</label>
                <div className="ui-select-wrap">
                  <select
                    className="ui-select"
                    value={endNode}
                    onChange={(e) => setEndNode(e.target.value)}
                  >
                    {nodes.length === 0 && <option value="">—</option>}
                    {nodes.map((n) => (
                      <option key={n} value={n}>
                        {n}
                      </option>
                    ))}
                  </select>
                  <svg
                    className="ui-select-chevron"
                    width="13"
                    height="13"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.4"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M6 9l6 6 6-6" />
                  </svg>
                </div>
              </div>
            </div>

            <div className="ui-field">
              <label className="ui-label">Iterations</label>
              <input
                className="ui-input mono"
                type="number"
                min={1}
                max={500}
                value={iterations}
                onChange={(e) => {
                  let v = parseInt(e.target.value, 10)
                  if (isNaN(v) || v < 1) v = 1
                  if (v > 500) v = 500
                  setIterations(v)
                }}
              />
            </div>

            <div className="ui-field">
              <label className="ui-label">New graph name</label>
              <input
                className="ui-input"
                value={draftName}
                onChange={(e) => setDraftName(e.target.value)}
                placeholder="Name for “New graph”"
              />
              <span className="ui-hint">Used when you save the current matrix as a new graph.</span>
            </div>

            <div className="g-toggles">
              <button
                className="g-toggle"
                onClick={() => setUseEdgeWeights((v) => !v)}
                aria-pressed={useEdgeWeights}
              >
                <span>
                  <span className="g-toggle-title">
                    Use edge weights
                    <HelpTip text="On: each transition is chosen proportional to its matrix weight. Off: every available edge is equally likely." />
                  </span>
                  <span className="g-toggle-sub">Weighted vs. uniform choice</span>
                </span>
                <span className="ui-switch" role="switch" aria-checked={useEdgeWeights}>
                  <span className="ui-switch-knob" />
                </span>
              </button>
              <button
                className="g-toggle"
                onClick={() => setAllowLoops((v) => !v)}
                aria-pressed={allowLoops}
              >
                <span>
                  <span className="g-toggle-title">
                    Allow loops
                    <HelpTip text="On: a walk may revisit nodes it has already passed through. Off: each node is visited at most once per walk." />
                  </span>
                  <span className="g-toggle-sub">Permit revisiting nodes</span>
                </span>
                <span className="ui-switch" role="switch" aria-checked={allowLoops}>
                  <span className="ui-switch-knob" />
                </span>
              </button>
            </div>

            <button
              className={`ui-btn g-run-btn${running ? ' running' : ''}`}
              onClick={runSelected}
              disabled={!canRun}
            >
              {running ? (
                <span className="ui-spin-sm" />
              ) : (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M8 5v14l11-7z" />
                </svg>
              )}
              {running ? 'Running…' : 'Run simulation'}
            </button>
          </div>
        </section>
      </div>

      {/* RESULTS */}
      <section className="g-results">
        <div className="g-results-head">
          <div className="g-results-title">Walks</div>
          {stats && !runError && (
            <span className="g-results-note">Hover a walk to trace it on the graph</span>
          )}
        </div>

        {runError ? (
          <div style={{ padding: 18 }}>
            <div className="ui-error">
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                stroke="var(--danger)"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ flex: 'none', marginTop: 1 }}
              >
                <circle cx="12" cy="12" r="9" />
                <path d="M12 8v4M12 16h.01" />
              </svg>
              <div>
                <div className="ui-error-title">Run failed</div>
                <div className="ui-error-msg">{runError}</div>
              </div>
            </div>
          </div>
        ) : stats && run ? (
          <div>
            <div className="g-stats">
              <div className="g-stat">
                <div className="g-stat-k">Walk count</div>
                <div className="g-stat-v">{stats.count}</div>
              </div>
              <div className="g-stat">
                <div className="g-stat-k">Reached end</div>
                <div className="g-stat-v ok">{stats.reachedPct}</div>
              </div>
              <div className="g-stat">
                <div className="g-stat-k">Avg. length</div>
                <div className="g-stat-v">{stats.avgLen}</div>
              </div>
            </div>
            <div className="g-walks">
              {(run.walks ?? []).map((walk, i) => {
                const reached = walk.length > 0 && walk[walk.length - 1] === endNode
                return (
                  <div
                    className="g-walk"
                    key={i}
                    onMouseEnter={() => setHighlight(walk)}
                    onMouseLeave={() => setHighlight(null)}
                  >
                    <span className="g-walk-idx">{String(i + 1).padStart(2, '0')}</span>
                    <div className="g-walk-steps">
                      {walk.map((node, k) => {
                        const isStart = node === startNode && k === 0
                        const isEnd = node === endNode && k === walk.length - 1
                        return (
                          <span key={k} style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                            <span
                              className={`g-chip${isEnd ? ' end' : isStart ? ' start' : ''}`}
                            >
                              {node}
                            </span>
                            {k < walk.length - 1 && <span className="g-arrow">&#8250;</span>}
                          </span>
                        )
                      })}
                    </div>
                    {reached && <span className="g-walk-check">&#10003;</span>}
                  </div>
                )
              })}
            </div>
          </div>
        ) : (
          <div className="ui-empty" style={{ padding: '44px 20px' }}>
            <div className="ui-empty-icon">
              <svg
                width="24"
                height="24"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.8"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <circle cx="5" cy="6" r="2.5" />
                <circle cx="19" cy="6" r="2.5" />
                <circle cx="12" cy="18" r="2.5" />
                <path d="M7 7.5 10.5 16M16.5 7.5 13.5 16" />
              </svg>
            </div>
            <div className="ui-empty-title">
              {loading ? 'Loading graphs…' : 'No walks yet'}
            </div>
            <div className="ui-empty-desc">
              {loading
                ? 'Fetching your saved graphs from the API.'
                : 'Set your parameters and run the simulation to generate resident paths.'}
            </div>
          </div>
        )}

        {running && (
          <div className="ui-overlay">
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
              <span className="ui-spin" />
              <span style={{ font: '600 12.5px system-ui', color: 'var(--text-2)' }}>
                Simulating walks…
              </span>
            </div>
          </div>
        )}
      </section>

      {toast && (
        <div className="ui-toast">
          <span className="ui-toast-icon">
            <svg
              width="14"
              height="14"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#fff"
              strokeWidth="3"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M20 6 9 17l-5-5" />
            </svg>
          </span>
          <span className="ui-toast-msg">{toast}</span>
        </div>
      )}

      {confirmDel && selectedId != null && (
        <div className="ui-modal-backdrop" onClick={() => setConfirmDel(false)}>
          <div className="ui-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ui-modal-row">
              <div className="ui-modal-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                </svg>
              </div>
              <div>
                <h3 className="ui-modal-title">Delete this graph?</h3>
                <p className="ui-modal-text">
                  “{graphs.find((g) => g.id === selectedId)?.name || `#${selectedId}`}” will be
                  permanently removed. This cannot be undone.
                </p>
              </div>
            </div>
            <div className="ui-modal-actions">
              <button className="ui-btn secondary" onClick={() => setConfirmDel(false)}>
                Cancel
              </button>
              <button className="ui-btn danger" onClick={doDelete}>
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
