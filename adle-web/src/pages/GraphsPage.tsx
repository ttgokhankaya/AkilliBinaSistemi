import { useEffect, useState } from 'react'
import { api, type GraphDto, type RunGraphResponse } from '../api/client'

// A ready-to-run linear graph A->B->C->D as a tab-separated adjacency matrix
// (the format Graph.ToMatrisString produces / CreateNewGraphFromMatrixString parses).
const SAMPLE_MATRIX = [
  '\tA\tB\tC\tD',
  'A\t0\t1\t0\t0',
  'B\t0\t0\t1\t0',
  'C\t0\t0\t0\t1',
  'D\t0\t0\t0\t0',
].join('\n')

export function GraphsPage() {
  const [graphs, setGraphs] = useState<GraphDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('Sample linear')
  const [matrix, setMatrix] = useState(SAMPLE_MATRIX)

  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [startNode, setStartNode] = useState('A')
  const [endNode, setEndNode] = useState('D')
  const [iterations, setIterations] = useState(5)
  const [run, setRun] = useState<RunGraphResponse | null>(null)
  const [busy, setBusy] = useState(false)

  async function reload() {
    try {
      const list = await api.graphs()
      setGraphs(list)
      if (selectedId == null && list.length) setSelectedId(list[0].id)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function create() {
    setError(null)
    try {
      const g = await api.createGraph({ name, matrixValue: matrix })
      setSelectedId(g.id)
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  async function runSelected() {
    if (selectedId == null) return
    setBusy(true)
    setError(null)
    setRun(null)
    try {
      setRun(await api.runGraph(selectedId, { startNode, endNode, iterations }))
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section>
      <h2>Graphs & simulation</h2>
      <p className="hint">
        Persist a graph (adjacency matrix) and generate random walks via
        /api/graphs/&#123;id&#125;/run — each walk feeds straight into prediction.
      </p>

      {error && <p className="status error">Error: {error}</p>}

      <div className="form-grid">
        <label>
          New graph name
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label>
          Adjacency matrix (tab-separated; first row = node names)
          <textarea
            rows={6}
            value={matrix}
            onChange={(e) => setMatrix(e.target.value)}
            style={{ whiteSpace: 'pre', overflowX: 'auto' }}
          />
        </label>
        <div className="form-row">
          <button onClick={create}>Create graph</button>
        </div>
      </div>

      <h3>Saved graphs ({graphs.length})</h3>
      {graphs.length === 0 ? (
        <p className="hint">None yet — create one above.</p>
      ) : (
        <div className="form-row">
          <label>
            Graph
            <select
              value={selectedId ?? ''}
              onChange={(e) => setSelectedId(Number(e.target.value))}
            >
              {graphs.map((g) => (
                <option key={g.id} value={g.id}>
                  #{g.id} {g.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Start
            <input value={startNode} onChange={(e) => setStartNode(e.target.value)} />
          </label>
          <label>
            End
            <input value={endNode} onChange={(e) => setEndNode(e.target.value)} />
          </label>
          <label>
            Iterations
            <input
              type="number"
              value={iterations}
              onChange={(e) => setIterations(Number(e.target.value) || 1)}
            />
          </label>
          <button onClick={runSelected} disabled={busy || selectedId == null}>
            {busy ? 'Running…' : 'Run'}
          </button>
        </div>
      )}

      {run && (
        <div className="result">
          <h3>Walks ({run.walkCount})</h3>
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Path</th>
              </tr>
            </thead>
            <tbody>
              {(run.walks ?? []).map((walk, i) => (
                <tr key={i}>
                  <td>{i + 1}</td>
                  <td>{walk.join(' → ')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
