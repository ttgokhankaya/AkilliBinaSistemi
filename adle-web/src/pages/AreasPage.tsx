import { useEffect, useState } from 'react'
import { api, type AreaDto, type AreaWriteDto } from '../api/client'
import { AreaScatter } from '../charts/AreaScatter'

const EMPTY: AreaWriteDto = { name: '', width: 0, height: 0, parentAreaId: null, areaTypeId: null }
const numOrNull = (v: string): number | null => (v.trim() === '' ? null : Number(v))

export function AreasPage() {
  const [data, setData] = useState<AreaDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<AreaWriteDto>(EMPTY)
  const [editingId, setEditingId] = useState<number | null>(null)

  async function reload() {
    try {
      setData(await api.areas())
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
      if (editingId == null) await api.createArea(form)
      else await api.updateArea(editingId, form)
      resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  async function remove(id: number) {
    setError(null)
    try {
      await api.deleteArea(id)
      if (editingId === id) resetForm()
      await reload()
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    }
  }

  function startEdit(a: AreaDto) {
    setEditingId(a.id)
    setForm({
      name: a.name ?? '',
      width: a.width,
      height: a.height,
      parentAreaId: a.parentAreaId ?? null,
      areaTypeId: a.areaTypeId ?? null,
    })
  }

  if (!data) return <p className="status">{error ? `Error: ${error}` : 'Loading areas…'}</p>

  return (
    <section>
      <h2>Areas ({data.length})</h2>
      <AreaScatter areas={data} />

      <h3>{editingId == null ? 'New area' : `Edit area #${editingId}`}</h3>
      <div className="form-row">
        <label>
          Name
          <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        </label>
        <label>
          Width
          <input type="number" value={form.width} onChange={(e) => setForm({ ...form, width: Number(e.target.value) || 0 })} />
        </label>
        <label>
          Height
          <input type="number" value={form.height} onChange={(e) => setForm({ ...form, height: Number(e.target.value) || 0 })} />
        </label>
        <label>
          Parent ID
          <input type="number" value={form.parentAreaId ?? ''} onChange={(e) => setForm({ ...form, parentAreaId: numOrNull(e.target.value) })} />
        </label>
        <label>
          Type ID
          <input type="number" value={form.areaTypeId ?? ''} onChange={(e) => setForm({ ...form, areaTypeId: numOrNull(e.target.value) })} />
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
            <th>Type</th>
            <th>Width</th>
            <th>Height</th>
            <th>Parent</th>
            <th></th>
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
              <td className="actions">
                <button className="link" onClick={() => startEdit(a)}>Edit</button>
                <button className="link danger" onClick={() => remove(a.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
