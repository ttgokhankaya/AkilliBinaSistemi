import { api } from '../api/client'
import { useApi } from '../hooks/useApi'

function fmtDuration(seconds: number): string {
  const m = Math.round(seconds / 60)
  if (m < 60) return `${m} min`
  const h = Math.floor(m / 60)
  const rem = m % 60
  return rem ? `${h}h ${rem}m` : `${h}h`
}

function fmtTime(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString()
}

export function OperationsPage() {
  const { data, error, loading } = useApi(api.operations)

  if (loading) return <p className="status">Loading operations…</p>
  if (error) return <p className="status error">Error: {error}</p>
  if (!data) return null

  return (
    <section>
      <h2>Operations ({data.length})</h2>
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Start</th>
            <th>Duration</th>
          </tr>
        </thead>
        <tbody>
          {data.map((o) => (
            <tr key={o.id}>
              <td>{o.id}</td>
              <td>{o.name}</td>
              <td>{fmtTime(o.startTime)}</td>
              <td>{fmtDuration(o.durationSeconds)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
