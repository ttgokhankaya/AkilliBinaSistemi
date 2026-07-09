import { useEffect, useState } from 'react'
import { api, type OperationDto } from '../api/client'

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

// ISO -> value a datetime-local input accepts ("YYYY-MM-DDTHH:mm").
function toLocalInput(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

const EMPTY = { name: '', startTime: '', durationMinutes: 30 }

export function OperationsPage() {
  const [data, setData] = useState<OperationDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(EMPTY)
  const [editingId, setEditingId] = useState<number | null>(null)

  async function reload() {
    try {
      setData(await api.operations())
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function resetForm() {
    setForm(EMPTY)
    setEditingId(null)
  }

  async function submit() {
    setError(null)
    try {
      const dto = {
        name: form.name,
        startTime: form.startTime || new Date().toISOString(),
        durationSeconds: Math.max(0, form.durationMinutes) * 60,
      }
      if (editingId == null) await api.createOperation(dto)
      else await api.updateOperation(editingId, dto)
      resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  async function remove(id: number) {
    setError(null)
    try {
      await api.deleteOperation(id)
      if (editingId === id) resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  function startEdit(o: OperationDto) {
    setEditingId(o.id)
    setForm({
      name: o.name ?? '',
      startTime: toLocalInput(o.startTime),
      durationMinutes: Math.round(o.durationSeconds / 60),
    })
  }

  if (!data) return <p className="status">{error ? `Error: ${error}` : 'Loading operations…'}</p>

  return (
    <section>
      <h2>Operations ({data.length})</h2>

      <h3>{editingId == null ? 'New operation' : `Edit operation #${editingId}`}</h3>
      <div className="form-row">
        <label>
          Name
          <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </label>
        <label>
          Start
          <input type="datetime-local" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} />
        </label>
        <label>
          Duration (min)
          <input type="number" value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: Number(e.target.value) || 0 })} />
        </label>
        <button onClick={submit} disabled={!form.name.trim()}>
          {editingId == null ? 'Create' : 'Save'}
        </button>
        {editingId != null && (
          <button className="secondary" onClick={resetForm}>
            Cancel
          </button>
        )}
      </div>

      {error && <p className="status error">Error: {error}</p>}

      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Start</th>
            <th>Duration</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data.map((o) => (
            <tr key={o.id}>
              <td>{o.id}</td>
              <td>{o.name}</td>
              <td>{fmtTime(o.startTime)}</td>
              <td>{fmtDuration(o.durationSeconds)}</td>
              <td className="actions">
                <button className="link" onClick={() => startEdit(o)}>Edit</button>
                <button className="link danger" onClick={() => remove(o.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
