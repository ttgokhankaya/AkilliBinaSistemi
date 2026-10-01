import { useEffect, useRef, useState } from 'react'
import { api, type AreaDto, type DeviceDto, type DeviceWriteDto } from '../api/client'
import './DevicesPage.css'

// Design: "ADLE Devices.dc.html". The mockup fabricated 14 in-memory rows; here
// the same UI (summary cards, table, create/edit modal, delete confirm, toast)
// drives the real /api/devices CRUD endpoints. Kind is the fixed Turkish set
// the simulation understands; Area is a real dropdown fed by /api/areas.
const KINDS = ['Lamba', 'Termostat', 'Hareket Sensörü', 'Akıllı Priz']

// state is tri-state: true (on) / false (off) / null (n/a). The mockup exposed
// the same three choices as a segmented control inside the modal.
type StateVal = 'on' | 'off' | 'na'
const STATE_OPTS: { v: StateVal; label: string }[] = [
  { v: 'on', label: 'On' },
  { v: 'off', label: 'Off' },
  { v: 'na', label: 'N/A' },
]
const toStateVal = (s: boolean | null | undefined): StateVal => (s == null ? 'na' : s ? 'on' : 'off')
const fromStateVal = (v: StateVal): boolean | null => (v === 'na' ? null : v === 'on')

// Per-kind glyphs, matching the mockup's inline SVGs.
function KindIcon({ kind }: { kind: string }) {
  const common = {
    width: 16,
    height: 16,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 2,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  }
  switch (kind) {
    case 'Lamba':
      return (
        <svg {...common}>
          <path d="M9 18h6M10 21h4M12 3a6 6 0 0 0-4 10.5c.7.7 1 1.3 1 2.5h6c0-1.2.3-1.8 1-2.5A6 6 0 0 0 12 3Z" />
        </svg>
      )
    case 'Termostat':
      return (
        <svg {...common}>
          <path d="M14 14.8V5a2 2 0 0 0-4 0v9.8a4 4 0 1 0 4 0Z" />
        </svg>
      )
    case 'Hareket Sensörü':
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="1.5" />
          <path d="M8.5 8.5a5 5 0 0 0 0 7M15.5 8.5a5 5 0 0 1 0 7M6 6a9 9 0 0 0 0 12M18 6a9 9 0 0 1 0 12" />
        </svg>
      )
    case 'Akıllı Priz':
      return (
        <svg {...common}>
          <path d="M9 2v6M15 2v6M6 8h12v3a6 6 0 0 1-12 0zM12 17v5" />
        </svg>
      )
    default:
      return (
        <svg {...common}>
          <rect x="4" y="4" width="16" height="16" rx="3" />
        </svg>
      )
  }
}

type ModalState = {
  mode: 'create' | 'edit'
  id?: number
  name: string
  kind: string
  areaId: number
  ip: string
  state: StateVal
}

export function DevicesPage() {
  const [data, setData] = useState<DeviceDto[] | null>(null)
  const [areas, setAreas] = useState<AreaDto[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [modal, setModal] = useState<ModalState | null>(null)
  const [saving, setSaving] = useState(false)
  const [confirmId, setConfirmId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)

  function fire(msg: string) {
    setToast(msg)
    if (timer.current) window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => setToast(null), 2600)
  }

  async function reload() {
    try {
      const [devs, ars] = await Promise.all([api.devices(), api.areas()])
      setData(devs)
      setAreas(ars)
    } catch (e) {
      setLoadError(e instanceof Error ? e.message : String(e))
    }
  }

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const defaultAreaId = () => areas[0]?.id ?? 1

  function openCreate() {
    setActionError(null)
    setModal({ mode: 'create', name: '', kind: KINDS[0], areaId: defaultAreaId(), ip: '', state: 'on' })
  }

  function openEdit(d: DeviceDto) {
    setActionError(null)
    setModal({
      mode: 'edit',
      id: d.id,
      name: d.name ?? '',
      kind: d.kind ?? KINDS[0],
      areaId: d.areaId ?? defaultAreaId(),
      ip: d.ip ?? '',
      state: toStateVal(d.state),
    })
  }

  function closeModal() {
    if (!saving) setModal(null)
  }

  async function save() {
    if (!modal || saving) return
    const dto: DeviceWriteDto = {
      name: modal.name.trim(),
      kind: modal.kind,
      areaId: modal.areaId,
      ip: modal.ip.trim() || null,
      state: fromStateVal(modal.state),
    }
    setSaving(true)
    setActionError(null)
    try {
      if (modal.mode === 'edit' && modal.id != null) await api.updateDevice(modal.id, dto)
      else await api.createDevice(dto)
      setModal(null)
      await reload()
      fire(modal.mode === 'edit' ? 'Device updated' : 'Device created')
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setSaving(false)
    }
  }

  async function confirmDelete() {
    if (confirmId == null || deleting) return
    setDeleting(true)
    setActionError(null)
    try {
      await api.deleteDevice(confirmId)
      setConfirmId(null)
      await reload()
      fire('Device deleted')
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setDeleting(false)
    }
  }

  // ---- Loading / hard-error states (first fetch) ----
  if (!data) {
    return (
      <>
        <PageHead onNew={openCreate} disabled />
        {loadError ? (
          <div className="ui-error">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
              <circle cx="12" cy="12" r="9" />
              <path d="M12 8v4M12 16h.01" />
            </svg>
            <div>
              <div className="ui-error-title">Could not load devices</div>
              <div className="ui-error-msg">{loadError}</div>
            </div>
          </div>
        ) : (
          <div className="ui-card dv-skel-card">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="dv-skel-row">
                <span className="ui-skel-line" style={{ width: '30%' }} />
                <span className="ui-skel-line" style={{ width: '18%' }} />
                <span className="ui-skel-line" style={{ width: '20%' }} />
                <span className="ui-skel-line" style={{ width: '14%' }} />
              </div>
            ))}
          </div>
        )}
      </>
    )
  }

  const onCount = data.filter((d) => d.state === true).length
  const offCount = data.filter((d) => d.state === false).length
  const kindSummary = KINDS.map((k) => ({ name: k, count: data.filter((d) => d.kind === k).length }))
  const areaName = (id: number | undefined) =>
    areas.find((a) => a.id === id)?.name ?? data.find((d) => d.areaId === id)?.areaName ?? `#${id ?? '—'}`

  return (
    <>
      <PageHead onNew={openCreate} />

      {/* Summary cards */}
      <div className="dv-summary">
        <div className="dv-stat">
          <div className="dv-stat-label">Total</div>
          <div className="dv-stat-num">{data.length}</div>
        </div>
        <div className="dv-stat">
          <div className="dv-stat-label">On</div>
          <div className="dv-stat-num on">{onCount}</div>
        </div>
        <div className="dv-stat">
          <div className="dv-stat-label">Off</div>
          <div className="dv-stat-num off">{offCount}</div>
        </div>
        <div className="dv-stat dv-kinds">
          {kindSummary.map((k) => (
            <div className="dv-kind-row" key={k.name}>
              <span className="dv-kind-icon">
                <KindIcon kind={k.name} />
              </span>
              <span className="dv-kind-name">{k.name}</span>
              <span className="dv-kind-count">{k.count}</span>
            </div>
          ))}
        </div>
      </div>

      {actionError && (
        <div className="ui-error dv-action-error">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Request failed</div>
            <div className="ui-error-msg">{actionError}</div>
          </div>
        </div>
      )}

      {/* Table */}
      <section className="ui-card">
        <div className="dv-card-head">All devices · {data.length}</div>
        {data.length === 0 ? (
          <div className="ui-empty dv-empty">
            <div className="ui-empty-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                <rect x="3" y="3" width="7" height="7" rx="1" />
                <rect x="14" y="3" width="7" height="7" rx="1" />
                <rect x="3" y="14" width="7" height="7" rx="1" />
                <rect x="14" y="14" width="7" height="7" rx="1" />
              </svg>
            </div>
            <div className="ui-empty-title">No devices yet</div>
            <div className="ui-empty-desc">Add the sensors and actuators installed across the home's areas.</div>
            <button className="ui-btn" onClick={openCreate}>
              New device
            </button>
          </div>
        ) : (
          <div className="ui-table-wrap dv-table-wrap">
            <table className="ui-table">
              <thead>
                <tr>
                  <th>Device</th>
                  <th>Kind</th>
                  <th>Area</th>
                  <th>IP</th>
                  <th>State</th>
                  <th className="right"></th>
                </tr>
              </thead>
              <tbody>
                {data.map((d) => (
                  <tr key={d.id}>
                    <td className="name">{d.name}</td>
                    <td className="muted">
                      <span className="dv-kind-cell">
                        <span className="dv-kind-icon">
                          <KindIcon kind={d.kind ?? ''} />
                        </span>
                        {d.kind}
                      </span>
                    </td>
                    <td className="muted">{d.areaName ?? areaName(d.areaId)}</td>
                    <td className="mono">{d.ip ?? '—'}</td>
                    <td>
                      {d.state == null ? (
                        <span className="ui-pill dv-pill-na">N/A</span>
                      ) : d.state ? (
                        <span className="ui-pill on">
                          <span className="ui-dot" />
                          On
                        </span>
                      ) : (
                        <span className="ui-pill off">
                          <span className="ui-dot" />
                          Off
                        </span>
                      )}
                    </td>
                    <td className="actions">
                      <button className="ui-icon-btn" aria-label="Edit" onClick={() => openEdit(d)}>
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
                        </svg>
                      </button>
                      <button className="ui-icon-btn danger" aria-label="Delete" onClick={() => setConfirmId(d.id ?? null)}>
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                        </svg>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Create / edit modal */}
      {modal && (
        <div className="ui-modal-backdrop" onClick={closeModal}>
          <div className="dv-modal" onClick={(e) => e.stopPropagation()}>
            <div className="dv-modal-head">{modal.mode === 'edit' ? 'Edit device' : 'New device'}</div>
            <div className="dv-modal-body">
              <div className="ui-field">
                <label className="ui-label">Name</label>
                <input
                  className="ui-input"
                  value={modal.name}
                  placeholder="e.g. Tavan Lambası"
                  onChange={(e) => setModal({ ...modal, name: e.target.value })}
                />
              </div>

              <div className="dv-modal-grid">
                <div className="ui-field">
                  <label className="ui-label">Kind</label>
                  <div className="ui-select-wrap">
                    <select className="ui-select" value={modal.kind} onChange={(e) => setModal({ ...modal, kind: e.target.value })}>
                      {KINDS.map((k) => (
                        <option key={k} value={k}>
                          {k}
                        </option>
                      ))}
                    </select>
                    <svg className="ui-select-chevron" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M6 9l6 6 6-6" />
                    </svg>
                  </div>
                </div>

                <div className="ui-field">
                  <label className="ui-label">Area</label>
                  <div className="ui-select-wrap">
                    <select
                      className="ui-select"
                      value={modal.areaId}
                      onChange={(e) => setModal({ ...modal, areaId: Number(e.target.value) })}
                    >
                      {areas.length === 0 && <option value={modal.areaId}>#{modal.areaId}</option>}
                      {areas.map((a) => (
                        <option key={a.id} value={a.id}>
                          {a.name ?? `#${a.id}`}
                        </option>
                      ))}
                    </select>
                    <svg className="ui-select-chevron" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M6 9l6 6 6-6" />
                    </svg>
                  </div>
                </div>
              </div>

              <div className="ui-field">
                <label className="ui-label">IP address</label>
                <input
                  className="ui-input mono"
                  value={modal.ip}
                  placeholder="192.168.1.x"
                  onChange={(e) => setModal({ ...modal, ip: e.target.value })}
                />
              </div>

              <div className="ui-field">
                <label className="ui-label">State</label>
                <div className="dv-seg">
                  {STATE_OPTS.map((o) => (
                    <button
                      key={o.v}
                      type="button"
                      className={`dv-seg-btn${modal.state === o.v ? ' active' : ''}`}
                      onClick={() => setModal({ ...modal, state: o.v })}
                    >
                      {o.label}
                    </button>
                  ))}
                </div>
              </div>

              {actionError && <div className="ui-hint error">{actionError}</div>}
            </div>
            <div className="dv-modal-foot">
              <button className="ui-btn secondary" onClick={closeModal} disabled={saving}>
                Cancel
              </button>
              <button className={`ui-btn${saving ? ' running' : ''}`} onClick={save} disabled={saving || !modal.name.trim()}>
                {saving && <span className="ui-spin-sm" />}
                {saving ? 'Saving…' : modal.mode === 'edit' ? 'Save changes' : 'Create device'}
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
                <h3 className="ui-modal-title">Delete device?</h3>
                <p className="ui-modal-text">
                  &ldquo;{data.find((d) => d.id === confirmId)?.name ?? 'This device'}&rdquo; will be permanently removed. This
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
                {deleting ? 'Deleting…' : 'Delete'}
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

function PageHead({ onNew, disabled }: { onNew: () => void; disabled?: boolean }) {
  return (
    <div className="dv-head">
      <div>
        <div className="ui-eyebrow accent">Model the home</div>
        <h1 className="ui-h1">Devices</h1>
        <p className="ui-lead">Manage the sensors and actuators installed across the home's areas.</p>
      </div>
      <button className="ui-btn" onClick={onNew} disabled={disabled}>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round">
          <path d="M12 5v14M5 12h14" />
        </svg>
        New device
      </button>
    </div>
  )
}
