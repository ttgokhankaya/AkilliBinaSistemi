import { api } from '../api/client'
import { useApi } from '../hooks/useApi'

export function DevicesPage() {
  const { data, error, loading } = useApi(api.devices)

  if (loading) return <p className="status">Loading devices…</p>
  if (error) return <p className="status error">Error: {error}</p>
  if (!data) return null

  return (
    <section>
      <h2>Devices ({data.length})</h2>
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>IP</th>
            <th>Area</th>
            <th>Kind</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          {data.map((d) => (
            <tr key={d.id}>
              <td>{d.id}</td>
              <td>{d.name}</td>
              <td>{d.ip ?? '—'}</td>
              <td>{d.areaName ?? d.areaId}</td>
              <td>{d.kind}</td>
              <td>{d.state == null ? '—' : d.state ? 'on' : 'off'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
