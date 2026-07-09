import { useEffect, useState } from 'react'
import { api, type DeviceDto, type DeviceWriteDto } from '../api/client'

const EMPTY: DeviceWriteDto = { name: '', ip: null, state: null, kind: 'DeviceBase', areaId: 1 }

export function DevicesPage() {
  const [data, setData] = useState<DeviceDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<DeviceWriteDto>(EMPTY)
  const [editingId, setEditingId] = useState<number | null>(null)

  async function reload() {
    try {
      setData(await api.devices())
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
      if (editingId == null) await api.createDevice(form)
      else await api.updateDevice(editingId, form)
      resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  async function remove(id: number) {
    setError(null)
    try {
      await api.deleteDevice(id)
      if (editingId === id) resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  function startEdit(d: DeviceDto) {
    setEditingId(d.id)
    setForm({ name: d.name ?? '', ip: d.ip ?? null, state: d.state ?? null, kind: d.kind, areaId: d.areaId })
  }

  if (!data) return <p className="status">{error ? `Error: ${error}` : 'Loading devices…'}</p>

  return (
    <section>
      <h2>Devices ({data.length})</h2>

      <h3>{editingId == null ? 'New device' : `Edit device #${editingId}`}</h3>
      <div className="form-row">
        <label>
          Name
          <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </label>
        <label>
          IP
          <input value={form.ip ?? ''} onChange={(e) => setForm({ ...form, ip: e.target.value || null })} />
        </label>
        <label>
          Kind
          <input value={form.kind} onChange={(e) => setForm({ ...form, kind: e.target.value })} />
        </label>
        <label>
          Area ID
          <input type="number" value={form.areaId} onChange={(e) => setForm({ ...form, areaId: Number(e.target.value) || 0 })} />
        </label>
        <label className="check">
          On
          <input type="checkbox" checked={form.state === true} onChange={(e) => setForm({ ...form, state: e.target.checked })} />
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
            <th>IP</th>
            <th>Area</th>
            <th>Kind</th>
            <th>State</th>
            <th></th>
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
              <td className="actions">
                <button className="link" onClick={() => startEdit(d)}>Edit</button>
                <button className="link danger" onClick={() => remove(d.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
