import { useEffect, useRef, useState } from 'react'
import { api, type OperationDto } from '../api/client'
import './OperationsPage.css'

// Design: "ADLE Operations.dc.html". The mockup drove a local fake list; here
// the same UI is wired to the real /api/operations CRUD endpoints. Every
// derived value (timeline bars, row start/end, counts) comes from live data.

// Per-routine colour, stable by list position (matches the mockup palette).
const PALETTE = ['var(--warn)', 'var(--success)', 'var(--accent)', 'var(--info)']

const GRID_HOURS = [0, 6, 12, 18, 24]

// durationSeconds -> "1h 30m" / "45m" / "20s"
function fmtDuration(seconds: number): string {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  if (h && m) return `${h}h ${m}m`
  if (h) return `${h}h`
  if (m) return `${m}m`
  return `${Math.round(seconds)}s`
}

// Minutes-from-midnight of an ISO start time (local clock).
function startMinutes(iso: string): number {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return 0
  return d.getHours() * 60 + d.getMinutes()
}

// Minutes-from-midnight -> "HH:mm", wrapping past 24h.
function hhmm(totalMin: number): string {
  const t = ((Math.round(totalMin) % 1440) + 1440) % 1440
  const h = Math.floor(t / 60)
  const m = t % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

// ISO -> value a datetime-local input accepts ("YYYY-MM-DDTHH:mm").
function toLocalInput(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

type FormState = { id: number | null; name: string; start: string; hours: string; minutes: string }

const EMPTY_FORM: FormState = { id: null, name: '', start: '', hours: '1', minutes: '0' }

function durationSecondsOf(f: FormState): number {
  return (parseInt(f.hours, 10) || 0) * 3600 + (parseInt(f.minutes, 10) || 0) * 60
}

export function OperationsPage() {
  const [data, setData] = useState<OperationDto[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [hoverId, setHoverId] = useState<number | null>(null)

  const [form, setForm] = useState<FormState | null>(null) // create/edit modal state
  const [saving, setSaving] = useState(false)

  const [confirm, setConfirm] = useState<OperationDto | null>(null) // delete-confirm target
  const [deleting, setDeleting] = useState(false)

  const [toast, setToast] = useState<string | null>(null)
  const toastTimer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (toastTimer.current) window.clearTimeout(toastTimer.current)
    toastTimer.current = window.setTimeout(() => setToast(null), 2400)
  }

  async function reload() {
    setData(await api.operations())
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const ops = await api.operations()
        if (!cancelled) setData(ops)
      } catch (e) {
        if (!cancelled) setLoadError(e instanceof Error ? e.message : String(e))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  function openCreate() {
    setSaveError(null)
    setForm({ ...EMPTY_FORM })
  }

  function openEdit(o: OperationDto) {
    setSaveError(null)
    const secs = o.durationSeconds ?? 0
    setForm({
      id: o.id ?? null,
      name: o.name ?? '',
      start: toLocalInput(o.startTime ?? ''),
      hours: String(Math.floor(secs / 3600)),
      minutes: String(Math.floor((secs % 3600) / 60)),
    })
  }

  function closeForm() {
    if (saving) return
    setForm(null)
  }

  async function save() {
    if (!form) return
    setSaveError(null)
    setSaving(true)
    try {
      const dto = {
        name: form.name.trim() || 'Untitled routine',
        startTime: form.start ? new Date(form.start).toISOString() : new Date().toISOString(),
        durationSeconds: durationSecondsOf(form),
      }
      const editing = form.id != null
      if (editing) await api.updateOperation(form.id as number, dto)
      else await api.createOperation(dto)
      setForm(null)
      await reload()
      fire(editing ? 'Routine updated' : 'Routine created')
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : String(e))
    } finally {
      setSaving(false)
    }
  }

  async function confirmDelete() {
    if (!confirm || confirm.id == null) return
    setSaveError(null)
    setDeleting(true)
    try {
      await api.deleteOperation(confirm.id)
      setConfirm(null)
      await reload()
      fire('Routine deleted')
    } catch (e) {
      setSaveError(e instanceof Error ? e.message : String(e))
      setConfirm(null)
    } finally {
      setDeleting(false)
    }
  }

  const durSecPreview = form ? durationSecondsOf(form) : 0

  return (
    <>
      <div className="op-head-row">
        <div>
          <div className="ui-eyebrow accent">Model the home</div>
          <h1 className="ui-h1">Operations</h1>
          <p className="ui-lead">Define the daily routines that structure a resident&apos;s day.</p>
        </div>
        <button className="ui-btn" onClick={openCreate} disabled={!data}>
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          New routine
        </button>
      </div>

      {loadError && (
        <div className="ui-error" style={{ marginBottom: 18 }}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Couldn&apos;t load operations</div>
            <div className="ui-error-msg">{loadError}</div>
          </div>
        </div>
      )}

      {saveError && (
        <div className="ui-error" style={{ marginBottom: 18 }}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Request failed</div>
            <div className="ui-error-msg">{saveError}</div>
          </div>
        </div>
      )}

      {/* 24h timeline */}
      <section className="op-section">
        <div className="op-section-head">Daily schedule</div>
        <div className="op-timeline-body">
          {data && data.length > 0 ? (
            <>
              <div className="op-track">
                {GRID_HOURS.map((h) => (
                  <div key={h} className="op-gridline" style={{ left: `${(h / 24) * 100}%` }} />
                ))}
                <div className="op-track-base" />
                {data.map((o, i) => {
                  const sm = startMinutes(o.startTime ?? '')
                  const durMin = (o.durationSeconds ?? 0) / 60
                  const left = (sm / 1440) * 100
                  const width = Math.max(2, Math.min(durMin / 1440 * 100, 100 - left))
                  const color = PALETTE[i % PALETTE.length]
                  const shortName = (o.name ?? '').replace(/ routine$/i, '')
                  return (
                    <div
                      key={o.id ?? i}
                      className={`op-bar${hoverId === o.id ? ' hovered' : ''}`}
                      style={{ left: `${left}%`, width: `${width}%`, background: color }}
                      title={`${o.name ?? ''} · ${hhmm(sm)} · ${fmtDuration(o.durationSeconds ?? 0)}`}
                      onMouseEnter={() => setHoverId(o.id ?? null)}
                      onMouseLeave={() => setHoverId(null)}
                    >
                      <span className="op-bar-label">{shortName}</span>
                    </div>
                  )
                })}
              </div>
              <div className="op-axis">
                <span>00:00</span>
                <span>06:00</span>
                <span>12:00</span>
                <span>18:00</span>
                <span>24:00</span>
              </div>
            </>
          ) : (
            <div className="op-timeline-empty">{data ? 'No routines yet' : <span className="ui-skel-line" style={{ width: '100%' }} />}</div>
          )}
        </div>
      </section>

      {/* table */}
      <section className="op-section">
        <div className="op-section-head">All routines · {data ? data.length : '—'}</div>
        <div className="ui-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
          <table className="ui-table">
            <thead>
              <tr>
                <th>Routine</th>
                <th>Start</th>
                <th>Duration</th>
                <th>Ends</th>
                <th className="right"></th>
              </tr>
            </thead>
            <tbody>
              {!data &&
                [0, 1, 2, 3].map((i) => (
                  <tr key={`skel-${i}`}>
                    <td><span className="ui-skel-line" style={{ width: '55%' }} /></td>
                    <td><span className="ui-skel-line" style={{ width: 60 }} /></td>
                    <td><span className="ui-skel-line" style={{ width: 50 }} /></td>
                    <td><span className="ui-skel-line" style={{ width: 60 }} /></td>
                    <td className="actions"><span className="ui-skel-line" style={{ width: 60, marginLeft: 'auto' }} /></td>
                  </tr>
                ))}

              {data && data.length === 0 && (
                <tr>
                  <td colSpan={5}>
                    <div className="ui-empty">
                      <div className="ui-empty-icon">
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                          <circle cx="12" cy="12" r="9" />
                          <path d="M12 7v5l3 2" />
                        </svg>
                      </div>
                      <div className="ui-empty-title">No routines yet</div>
                      <div className="ui-empty-desc">Create your first daily routine to structure the resident&apos;s day.</div>
                      <button className="ui-btn sm" onClick={openCreate}>New routine</button>
                    </div>
                  </td>
                </tr>
              )}

              {data?.map((o, i) => {
                const sm = startMinutes(o.startTime ?? '')
                const endMin = sm + (o.durationSeconds ?? 0) / 60
                return (
                  <tr
                    key={o.id ?? i}
                    className={`op-row${hoverId === o.id ? ' hovered' : ''}`}
                    onMouseEnter={() => setHoverId(o.id ?? null)}
                    onMouseLeave={() => setHoverId(null)}
                  >
                    <td className="name">
                      <span className="op-name-cell">
                        <span className="op-dot" style={{ background: PALETTE[i % PALETTE.length] }} />
                        {o.name}
                      </span>
                    </td>
                    <td className="mono">{hhmm(sm)}</td>
                    <td>{fmtDuration(o.durationSeconds ?? 0)}</td>
                    <td className="mono">{hhmm(endMin)}</td>
                    <td className="actions">
                      <button className="ui-icon-btn" aria-label="Edit" onClick={() => openEdit(o)}>
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
                        </svg>
                      </button>
                      <button className="ui-icon-btn danger" aria-label="Delete" onClick={() => setConfirm(o)}>
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

      {/* create / edit form modal */}
      {form && (
        <div className="ui-modal-backdrop" onClick={closeForm}>
          <div className="op-form-modal" onClick={(e) => e.stopPropagation()}>
            <div className="op-form-head">{form.id == null ? 'New routine' : 'Edit routine'}</div>
            <div className="op-form-body">
              <div className="ui-field">
                <label className="ui-label">Name</label>
                <input
                  className="ui-input"
                  value={form.name}
                  placeholder="e.g. Morning routine"
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </div>
              <div className="ui-field">
                <label className="ui-label">Start time</label>
                <input
                  className="ui-input"
                  type="datetime-local"
                  value={form.start}
                  onChange={(e) => setForm({ ...form, start: e.target.value })}
                />
              </div>
              <div className="ui-field">
                <label className="ui-label">Duration</label>
                <div className="op-dur-row">
                  <div className="op-dur-unit">
                    <input
                      className="ui-input mono op-dur-num"
                      type="number"
                      min={0}
                      max={23}
                      value={form.hours}
                      onChange={(e) => setForm({ ...form, hours: e.target.value })}
                    />
                    <span className="op-dur-suffix">h</span>
                  </div>
                  <div className="op-dur-unit">
                    <input
                      className="ui-input mono op-dur-num"
                      type="number"
                      min={0}
                      max={59}
                      value={form.minutes}
                      onChange={(e) => setForm({ ...form, minutes: e.target.value })}
                    />
                    <span className="op-dur-suffix">m</span>
                  </div>
                  <span className="op-dur-secs">{durSecPreview} s</span>
                </div>
              </div>
            </div>
            <div className="op-form-foot">
              <button className="ui-btn secondary" onClick={closeForm} disabled={saving}>
                Cancel
              </button>
              <button className={`ui-btn${saving ? ' running' : ''}`} onClick={save} disabled={saving}>
                {saving && <span className="ui-spin-sm" />}
                {form.id == null ? 'Create routine' : 'Save changes'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* delete confirm modal */}
      {confirm && (
        <div className="ui-modal-backdrop" onClick={() => !deleting && setConfirm(null)}>
          <div className="ui-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ui-modal-row">
              <div className="ui-modal-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                </svg>
              </div>
              <div>
                <h3 className="ui-modal-title">Delete routine?</h3>
                <p className="ui-modal-text">
                  &ldquo;{confirm.name}&rdquo; will be permanently removed. This cannot be undone.
                </p>
              </div>
            </div>
            <div className="ui-modal-actions">
              <button className="ui-btn secondary" onClick={() => setConfirm(null)} disabled={deleting}>
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
