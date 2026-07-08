import { api } from '../api/client'
import { useApi } from '../hooks/useApi'
import { AreaScatter } from '../charts/AreaScatter'

export function AreasPage() {
  const { data, error, loading } = useApi(api.areas)

  if (loading) return <p className="status">Loading areas…</p>
  if (error) return <p className="status error">Error: {error}</p>
  if (!data) return null

  return (
    <section>
      <h2>Areas ({data.length})</h2>
      <AreaScatter areas={data} />
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Type</th>
            <th>Width</th>
            <th>Height</th>
            <th>Parent</th>
          </tr>
        </thead>
        <tbody>
          {data.map((a) => (
            <tr key={a.id}>
              <td>{a.id}</td>
              <td>{a.name}</td>
              <td>{a.areaTypeName ?? '—'}</td>
              <td>{a.width}</td>
              <td>{a.height}</td>
              <td>{a.parentAreaId ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
