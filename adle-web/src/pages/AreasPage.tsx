import { useEffect, useMemo, useRef, useState } from 'react'
import Plot from 'react-plotly.js'
import { api, type AreaDto, type AreaWriteDto } from '../api/client'
import { useTheme } from '../ThemeContext'
import './AreasPage.css'

// Design: "ADLE Areas.dc.html". The mockup fabricated areas + a shelf-packed
// floor plan and let you pick a type by *name*. Here the same UI drives the
// real /api/areas CRUD. Two real gaps vs. the mockup:
//   1. The DTO has no floor-plan coordinates — only width/height — so the
//      "Spatial layout" panel is a themed width×height scatter (the faithful
//      real-data equivalent), coloured/legended by area type.
//   2. There is no api.areaTypes(); the write DTO takes areaTypeId, not a name.
//      The type dropdown is therefore built from the distinct (id, name) pairs
//      already present in the live areas — you can reuse an existing type but
//      cannot invent a new one from this page.

// Plotly palettes per theme (SVG can't read CSS vars), matching the design
// tokens / brief sequence colours.
const PLOT = {
  light: {
    text2: '#525b6b',
    grid: '#e2e6ec',
    seq: ['#3b5bdb', '#0d9488', '#7c3aed', '#a6690f', '#158f52', '#db2777'],
  },
  dark: {
    text2: '#9aa5b6',
    grid: '#2a3342',
    seq: ['#5b7cfa', '#2dd4bf', '#a78bfa', '#e0a53a', '#37c47f', '#f472b6'],
  },
}

type FormState = {
  name: string
  width: string
  height: string
  areaTypeId: string // '' = none
  parentAreaId: string // '' = none
}

const EMPTY_FORM: FormState = { name: '', width: '320', height: '260', areaTypeId: '', parentAreaId: '' }

const numOrNull = (v: string): number | null => (v.trim() === '' ? null : Number(v))
const idKey = (a: AreaDto) => a.id ?? -1

export function AreasPage() {
  const { theme } = useTheme()
  const [data, setData] = useState<AreaDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [saving, setSaving] = useState(false)

  const [confirmId, setConfirmId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [hoverId, setHoverId] = useState<number | null>(null)

  const [toast, setToast] = useState<string | null>(null)
  const toastTimer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (toastTimer.current) window.clearTimeout(toastTimer.current)
    toastTimer.current = window.setTimeout(() => setToast(null), 2400)
  }

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

  const areas = data ?? []
  const byId = useMemo(() => new Map(areas.map((a) => [idKey(a), a])), [areas])

  // Distinct area types present in the real data → dropdown options + a stable
  // colour index per type for both the scatter and the row pills.
  const types = useMemo(() => {
    const seen = new Map<number, string>()
    for (const a of areas) {
      if (a.areaTypeId != null && !seen.has(a.areaTypeId)) {
        seen.set(a.areaTypeId, a.areaTypeName ?? `Type ${a.areaTypeId}`)
      }
    }
    return [...seen.entries()].map(([id, name]) => ({ id, name }))
  }, [areas])

  const colorOf = useMemo(() => {
    const seq = PLOT[theme].seq
    const map = new Map<number, string>()
    types.forEach((t, i) => map.set(t.id, seq[i % seq.length]))
    return (typeId: number | null | undefined) =>
      typeId != null && map.has(typeId) ? map.get(typeId)! : PLOT[theme].text2
  }, [types, theme])

  function openCreate() {
    setEditingId(null)
    setForm({ ...EMPTY_FORM, areaTypeId: types[0] ? String(types[0].id) : '' })
    setModalOpen(true)
  }

  function openEdit(a: AreaDto) {
    setEditingId(a.id ?? null)
    setForm({
      name: a.name ?? '',
      width: String(a.width ?? 0),
      height: String(a.height ?? 0),
      areaTypeId: a.areaTypeId == null ? '' : String(a.areaTypeId),
      parentAreaId: a.parentAreaId == null ? '' : String(a.parentAreaId),
    })
    setModalOpen(true)
  }

  function closeModal() {
    if (saving) return
    setModalOpen(false)
  }

  async function save() {
    setError(null)
    const dto: AreaWriteDto = {
      name: form.name.trim() || 'Untitled area',
      width: Number(form.width) || 0,
      height: Number(form.height) || 0,
      areaTypeId: numOrNull(form.areaTypeId),
      parentAreaId: numOrNull(form.parentAreaId),
    }
    setSaving(true)
    try {
      if (editingId == null) await api.createArea(dto)
      else await api.updateArea(editingId, dto)
      setModalOpen(false)
      await reload()
      fire(editingId == null ? 'Area created' : 'Area updated')
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setSaving(false)
    }
  }

  async function confirmDelete() {
    if (confirmId == null) return
    setError(null)
    setDeleting(true)
    try {
      await api.deleteArea(confirmId)
      const gone = confirmId
      setConfirmId(null)
      if (editingId === gone) {
        setEditingId(null)
        setModalOpen(false)
      }
      await reload()
      fire('Area deleted')
    } catch (e) {
      setConfirmId(null)
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setDeleting(false)
    }
  }

  // --- Scatter (width × height, one trace per area type) -------------------
  const p = PLOT[theme]
  const font = { family: 'ui-monospace, Consolas, monospace', size: 11, color: p.text2 }
  const axis = (title: string) => ({
    gridcolor: p.grid,
    zerolinecolor: p.grid,
    tickfont: font,
    title: { text: title, font },
    showline: false,
  })
  const scatterTraces = types.map((t) => {
    const items = areas.filter((a) => a.areaTypeId === t.id)
    return {
      type: 'scatter',
      mode: 'markers+text',
      name: t.name,
      x: items.map((a) => a.width ?? 0),
      y: items.map((a) => a.height ?? 0),
      text: items.map((a) => a.name ?? ''),
      textposition: 'top center',
      textfont: { ...font, size: 10 },
      marker: { size: 14, color: colorOf(t.id), line: { width: 1, color: 'rgba(255,255,255,.25)' } },
      hovertemplate: '%{text}<br>%{x} × %{y} cm<extra></extra>',
    }
  })
  // Areas with no type still deserve a dot on the plot.
  const untyped = areas.filter((a) => a.areaTypeId == null)
  if (untyped.length) {
    scatterTraces.push({
      type: 'scatter',
      mode: 'markers+text',
      name: '(no type)',
      x: untyped.map((a) => a.width ?? 0),
      y: untyped.map((a) => a.height ?? 0),
      text: untyped.map((a) => a.name ?? ''),
      textposition: 'top center',
      textfont: { ...font, size: 10 },
      marker: { size: 14, color: p.text2, line: { width: 1, color: 'rgba(255,255,255,.25)' } },
      hovertemplate: '%{text}<br>%{x} × %{y} cm<extra></extra>',
    })
  }
  const scatterLayout = {
    paper_bgcolor: 'transparent',
    plot_bgcolor: 'transparent',
    font,
    margin: { l: 52, r: 16, t: 12, b: 46 },
    legend: { font, orientation: 'h', y: 1.08, x: 0 },
    xaxis: axis('Width (cm)'),
    yaxis: axis('Height (cm)'),
  }

  const confirmArea = confirmId != null ? byId.get(confirmId) : null

  return (
    <>
      <div className="ui-page-head ar-head-row">
        <div>
          <div className="ui-eyebrow accent">Model the home</div>
          <h1 className="ui-h1">Areas</h1>
          <p className="ui-lead">Define the rooms and zones of the home and see their spatial layout.</p>
        </div>
        <button className="ui-btn" onClick={openCreate} disabled={!data}>
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          New area
        </button>
      </div>

      {error && (
        <div className="ui-error ar-error-gap">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Something went wrong</div>
            <div className="ui-error-msg">{error}</div>
          </div>
        </div>
      )}

      {/* Spatial layout — themed width×height scatter, coloured by area type. */}
      <section className="ar-card">
        <div className="ar-card-head">
          <div className="ar-card-title">Spatial layout</div>
        </div>
        <div className="ar-chart-body">
          {data && areas.length > 0 && (
            <Plot
              data={scatterTraces}
              layout={scatterLayout}
              style={{ width: '100%', height: '100%' }}
              useResizeHandler
              config={{ displayModeBar: false, responsive: true }}
            />
          )}
          {!data && (
            <div className="ar-chart-state">
              <span className="ui-spin" />
            </div>
          )}
          {data && areas.length === 0 && (
            <div className="ar-chart-state">
              <div className="ui-empty">
                <div className="ui-empty-icon">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                    <rect x="3" y="3" width="7" height="7" rx="1" />
                    <rect x="14" y="3" width="7" height="7" rx="1" />
                    <rect x="3" y="14" width="7" height="7" rx="1" />
                    <rect x="14" y="14" width="7" height="7" rx="1" />
                  </svg>
                </div>
                <div className="ui-empty-title">No areas yet</div>
                <div className="ui-empty-desc">Create the first room or zone to see it plotted here.</div>
                <button className="ui-btn" onClick={openCreate}>
                  New area
                </button>
              </div>
            </div>
          )}
        </div>
      </section>

      {/* All areas table */}
      <section className="ar-card">
        <div className="ar-card-head">
          <div className="ar-card-title">All areas · {data ? areas.length : '—'}</div>
        </div>
        <div className="ar-table-scroll">
          <table className="ar-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>Size (cm)</th>
                <th>Parent</th>
                <th className="right"></th>
              </tr>
            </thead>
            <tbody>
              {!data &&
                [0, 1, 2, 3].map((i) => (
                  <tr key={`sk${i}`}>
                    <td colSpan={5}>
                      <div className="ui-skel-line" style={{ width: `${70 - i * 8}%` }} />
                    </td>
                  </tr>
                ))}

              {data && areas.length === 0 && (
                <tr>
                  <td colSpan={5} className="ar-empty-cell">
                    <div className="ui-empty">
                      <div className="ui-empty-title">No areas</div>
                      <div className="ui-empty-desc">Use “New area” to add your first room.</div>
                    </div>
                  </td>
                </tr>
              )}

              {data &&
                areas.map((a) => {
                  const parent = a.parentAreaId != null ? byId.get(a.parentAreaId) : null
                  const typeName = a.areaTypeName ?? (a.areaTypeId != null ? `Type ${a.areaTypeId}` : '—')
                  const c = colorOf(a.areaTypeId)
                  return (
                    <tr
                      key={idKey(a)}
                      className={hoverId === idKey(a) ? 'hover' : ''}
                      onMouseEnter={() => setHoverId(idKey(a))}
                      onMouseLeave={() => setHoverId(null)}
                    >
                      <td>
                        <span className={`ar-name${a.parentAreaId != null ? ' child' : ''}`}>{a.name}</span>
                      </td>
                      <td>
                        {a.areaTypeId != null ? (
                          <span
                            className="ar-pill"
                            style={{ color: c, background: `color-mix(in srgb, ${c} 14%, transparent)` }}
                          >
                            {typeName}
                          </span>
                        ) : (
                          <span className="ar-parent">—</span>
                        )}
                      </td>
                      <td className="ar-size">
                        {a.width ?? 0} × {a.height ?? 0}
                      </td>
                      <td className="ar-parent">{parent ? parent.name : '—'}</td>
                      <td className="ar-actions">
                        <button className="ui-icon-btn" aria-label="Edit" onClick={() => openEdit(a)}>
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M12 20h9" />
                            <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
                          </svg>
                        </button>
                        <button
                          className="ui-icon-btn danger"
                          aria-label="Delete"
                          onClick={() => a.id != null && setConfirmId(a.id)}
                        >
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                          </svg>
                        </button>
                      </td>
                    </tr>
                  )
                })}
            </tbody>
          </table>
        </div>
      </section>

      {/* Create / edit form modal */}
      {modalOpen && (
        <div className="ui-modal-backdrop" onClick={closeModal}>
          <div className="ar-form-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ar-modal-header">{editingId == null ? 'New area' : 'Edit area'}</div>
            <div className="ar-modal-body">
              <div className="ui-field">
                <label className="ui-label">Name</label>
                <input
                  className="ui-input"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  placeholder="e.g. Living Room"
                  autoFocus
                />
              </div>

              <div className="ar-modal-grid">
                <div className="ui-field">
                  <label className="ui-label">Width (cm)</label>
                  <input
                    className="ui-input mono"
                    type="number"
                    value={form.width}
                    onChange={(e) => setForm({ ...form, width: e.target.value })}
                  />
                </div>
                <div className="ui-field">
                  <label className="ui-label">Height (cm)</label>
                  <input
                    className="ui-input mono"
                    type="number"
                    value={form.height}
                    onChange={(e) => setForm({ ...form, height: e.target.value })}
                  />
                </div>
              </div>

              <div className="ar-modal-grid">
                <div className="ui-field">
                  <label className="ui-label">Area type</label>
                  <div className="ui-select-wrap">
                    <select
                      className="ui-select"
                      value={form.areaTypeId}
                      onChange={(e) => setForm({ ...form, areaTypeId: e.target.value })}
                    >
                      <option value="">— none —</option>
                      {types.map((t) => (
                        <option key={t.id} value={t.id}>
                          {t.name}
                        </option>
                      ))}
                    </select>
                    <svg className="ui-select-chevron" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M6 9l6 6 6-6" />
                    </svg>
                  </div>
                </div>
                <div className="ui-field">
                  <label className="ui-label">Parent area</label>
                  <div className="ui-select-wrap">
                    <select
                      className="ui-select"
                      value={form.parentAreaId}
                      onChange={(e) => setForm({ ...form, parentAreaId: e.target.value })}
                    >
                      <option value="">— none —</option>
                      {areas
                        .filter((a) => a.id != null && a.id !== editingId)
                        .map((a) => (
                          <option key={idKey(a)} value={a.id ?? ''}>
                            {a.name}
                          </option>
                        ))}
                    </select>
                    <svg className="ui-select-chevron" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M6 9l6 6 6-6" />
                    </svg>
                  </div>
                </div>
              </div>
            </div>
            <div className="ar-modal-footer">
              <button className="ui-btn secondary" onClick={closeModal} disabled={saving}>
                Cancel
              </button>
              <button className={`ui-btn${saving ? ' running' : ''}`} onClick={save} disabled={saving}>
                {saving && <span className="ui-spin-sm" />}
                {editingId == null ? 'Create area' : 'Save changes'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirm */}
      {confirmId != null && (
        <div className="ui-modal-backdrop" onClick={() => !deleting && setConfirmId(null)}>
          <div className="ui-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ui-modal-row">
              <div className="ui-modal-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                </svg>
              </div>
              <div>
                <h3 className="ui-modal-title">Delete area?</h3>
                <p className="ui-modal-text">
                  “{confirmArea?.name ?? 'This area'}” will be permanently removed. Child areas will be detached. This
                  cannot be undone.
                </p>
              </div>
            </div>
            <div className="ui-modal-actions">
              <button className="ui-btn secondary" onClick={() => setConfirmId(null)} disabled={deleting}>
                Cancel
              </button>
              <button className={`ui-btn danger${deleting ? ' running' : ''}`} onClick={confirmDelete} disabled={deleting}>
                {deleting && <span className="ui-spin-sm" />}
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

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
