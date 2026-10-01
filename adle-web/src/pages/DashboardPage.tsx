import { useEffect, useState } from 'react'
import { api, type AreaDto, type DeviceDto, type OperationDto, type GraphDto } from '../api/client'
import './DashboardPage.css'

type Props = { onNavigate: (key: string) => void }

// Distinct area types get a stable colour from this palette (chips on the Areas card).
const TYPE_COLORS = [
  ['var(--info-weak)', 'var(--info)'],
  ['var(--accent-weak)', 'var(--accent)'],
  ['var(--success-weak)', 'var(--success)'],
  ['var(--warn-weak)', 'var(--warn)'],
  ['var(--danger-weak)', 'var(--danger)'],
  ['var(--surface-3)', 'var(--text-2)'],
]
const SEG_COLORS = ['var(--warn)', 'var(--success)', 'var(--accent)', 'var(--info)', 'var(--danger)']

const ICON = {
  map: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <path d="M3 9h18M9 21V9" />
    </svg>
  ),
  cpu: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="6" y="6" width="12" height="12" rx="2" />
      <path d="M9 2v2M15 2v2M9 20v2M15 20v2M2 9h2M2 15h2M20 9h2M20 15h2" />
    </svg>
  ),
  clock: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </svg>
  ),
  graph: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="5" cy="6" r="2.5" />
      <circle cx="19" cy="6" r="2.5" />
      <circle cx="12" cy="18" r="2.5" />
      <path d="M7 7.5 10.5 16M16.5 7.5 13.5 16" />
    </svg>
  ),
  trend: (
    <svg width="21" height="21" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 17l6-6 4 4 6-8" />
      <path d="M14 7h6v6" />
    </svg>
  ),
  scatter: (
    <svg width="21" height="21" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="6" cy="7" r="2" />
      <circle cx="17" cy="6" r="2" />
      <circle cx="9" cy="17" r="2" />
      <circle cx="18" cy="16" r="2" />
    </svg>
  ),
}

const Arrow = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M5 12h14M13 6l6 6-6 6" />
  </svg>
)

export function DashboardPage({ onNavigate }: Props) {
  const [areas, setAreas] = useState<AreaDto[] | null>(null)
  const [devices, setDevices] = useState<DeviceDto[] | null>(null)
  const [operations, setOperations] = useState<OperationDto[] | null>(null)
  const [graphs, setGraphs] = useState<GraphDto[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const [a, d, o, g] = await Promise.all([api.areas(), api.devices(), api.operations(), api.graphs()])
        if (cancelled) return
        setAreas(a)
        setDevices(d)
        setOperations(o)
        setGraphs(g)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : String(e))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const ready = areas && devices && operations && graphs

  // Derived summaries (only when data is present).
  const areaTypes = areas ? [...new Set(areas.map((a) => a.areaTypeName ?? '(none)'))] : []
  const devOn = devices ? devices.filter((d) => d.state === true).length : 0
  const devOff = devices ? devices.length - devOn : 0
  const kindCounts = new Map<string, number>()
  if (devices) for (const d of devices) kindCounts.set(d.kind ?? '(unknown)', (kindCounts.get(d.kind ?? '(unknown)') ?? 0) + 1)
  const kinds = [...kindCounts.entries()].sort((a, b) => b[1] - a[1])
  const maxKind = kinds.reduce((m, [, c]) => Math.max(m, c), 1)

  return (
    <>
      <div className="ui-page-head">
        <div className="ui-eyebrow">Overview</div>
        <h1 className="ui-h1">Home model &amp; analyses</h1>
        <p className="ui-lead">A snapshot of the modeled smart home and quick access to every analysis.</p>
      </div>

      {error && (
        <div className="ui-error" style={{ marginBottom: 20 }}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--danger)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flex: 'none', marginTop: 1 }}>
            <circle cx="12" cy="12" r="9" />
            <path d="M12 8v4M12 16h.01" />
          </svg>
          <div>
            <div className="ui-error-title">Couldn’t load the dashboard</div>
            <div className="ui-error-msg">{error}</div>
          </div>
        </div>
      )}

      {/* KPI row */}
      <div className="dash-kpis">
        {[
          { key: 'areas', icon: ICON.map, value: areas?.length, label: 'Areas', sub: `${areaTypes.length} area types` },
          { key: 'devices', icon: ICON.cpu, value: devices?.length, label: 'Devices', sub: `${devOn} on · ${devOff} off` },
          { key: 'operations', icon: ICON.clock, value: operations?.length, label: 'Operations', sub: 'daily routines' },
          { key: 'graphs', icon: ICON.graph, value: graphs?.length, label: 'Graphs', sub: 'saved topologies' },
        ].map((k) => (
          <button key={k.key} className="ui-card dash-kpi" onClick={() => onNavigate(k.key)}>
            <div className="dash-kpi-top">
              <span className="dash-kpi-icon">{k.icon}</span>
              <Arrow />
            </div>
            <div className="dash-kpi-val">{ready ? k.value : '—'}</div>
            <div className="dash-kpi-label">{k.label}</div>
            <div className="dash-kpi-sub">{ready ? k.sub : 'loading…'}</div>
          </button>
        ))}
      </div>

      {/* Model the home */}
      <div className="ui-section-label">Model the home</div>
      <div className="dash-grid">
        {/* Areas */}
        <div className="ui-card dash-mcard">
          <div className="dash-mcard-head">
            <div className="dash-mcard-titlerow">
              <span className="dash-mcard-ico" style={{ background: 'var(--info-weak)', color: 'var(--info)' }}>{ICON.map}</span>
              <span className="dash-mcard-title">Areas</span>
            </div>
            <div className="dash-mcard-sub">
              {ready ? `${areas!.length} rooms across ${areaTypes.length} area types` : 'loading…'}
            </div>
          </div>
          <div className="dash-mcard-body">
            <div className="dash-rooms">
              {areas?.map((a) => {
                const ti = Math.max(0, areaTypes.indexOf(a.areaTypeName ?? '(none)'))
                const [bg, fg] = TYPE_COLORS[ti % TYPE_COLORS.length]
                return (
                  <span key={a.id} className="dash-room" style={{ background: bg, color: fg }}>
                    {a.name}
                  </span>
                )
              })}
            </div>
          </div>
          <button className="dash-mcard-foot" onClick={() => onNavigate('areas')}>
            Open Areas
            <Arrow />
          </button>
        </div>

        {/* Devices */}
        <div className="ui-card dash-mcard">
          <div className="dash-mcard-head">
            <div className="dash-mcard-titlerow">
              <span className="dash-mcard-ico" style={{ background: 'var(--success-weak)', color: 'var(--success)' }}>{ICON.cpu}</span>
              <span className="dash-mcard-title">Devices</span>
            </div>
            <div className="dash-mcard-sub">
              <span className="dot"><span style={{ background: 'var(--success)' }} />{devOn} on</span>
              <span className="dot"><span style={{ background: 'var(--text-3)' }} />{devOff} off</span>
            </div>
          </div>
          <div className="dash-mcard-body">
            <div className="dash-kinds">
              {kinds.map(([name, count]) => (
                <div key={name} className="dash-kind-row">
                  <span className="dash-kind-name">{name}</span>
                  <div className="dash-kind-track">
                    <div className="dash-kind-bar" style={{ width: `${(count / maxKind) * 100}%` }} />
                  </div>
                  <span className="dash-kind-count">{count}</span>
                </div>
              ))}
            </div>
          </div>
          <button className="dash-mcard-foot" onClick={() => onNavigate('devices')}>
            Open Devices
            <Arrow />
          </button>
        </div>

        {/* Operations */}
        <div className="ui-card dash-mcard">
          <div className="dash-mcard-head">
            <div className="dash-mcard-titlerow">
              <span className="dash-mcard-ico" style={{ background: 'var(--warn-weak)', color: 'var(--warn)' }}>{ICON.clock}</span>
              <span className="dash-mcard-title">Operations</span>
            </div>
            <div className="dash-mcard-sub">{ready ? `${operations!.length} daily routines` : 'loading…'}</div>
          </div>
          <div className="dash-mcard-body dash-timeline-wrap">
            <div className="dash-timeline">
              <div className="dash-timeline-track" />
              {operations?.map((o, i) => {
                const dt = new Date(o.startTime ?? 0)
                const start = dt.getHours() + dt.getMinutes() / 60
                const durH = (o.durationSeconds ?? 0) / 3600
                const left = Math.min(100, (start / 24) * 100)
                const width = Math.max(2, Math.min(100 - left, (durH / 24) * 100))
                return (
                  <div
                    key={o.id ?? i}
                    className="dash-timeline-seg"
                    title={o.name ?? ''}
                    style={{ left: `${left}%`, width: `${width}%`, background: SEG_COLORS[i % SEG_COLORS.length] }}
                  />
                )
              })}
            </div>
            <div className="dash-timeline-legend">
              <span>00:00</span>
              <span>12:00</span>
              <span>24:00</span>
            </div>
          </div>
          <button className="dash-mcard-foot" onClick={() => onNavigate('operations')}>
            Open Operations
            <Arrow />
          </button>
        </div>
      </div>

      {/* Simulate & analyze */}
      <div className="ui-section-label">Simulate &amp; analyze</div>
      <div className="dash-grid" style={{ marginBottom: 0 }}>
        {[
          {
            key: 'graphs',
            icon: ICON.graph,
            title: 'Graphs',
            desc: 'Model the state-transition topology and simulate resident movement as random walks.',
            meta: ready ? `${graphs!.length} topologies` : 'topologies',
          },
          {
            key: 'prediction',
            icon: ICON.trend,
            title: 'Prediction',
            desc: 'Forecast the next node and likely resident from observed activity, using LCS + softmax.',
            meta: 'LCS + softmax',
          },
          {
            key: 'anomaly',
            icon: ICON.scatter,
            title: 'Anomaly',
            desc: 't-SNE projection and Random Forest proximity to surface behavior that deviates from the norm.',
            meta: 't-SNE + Random Forest',
          },
        ].map((a) => (
          <button key={a.key} className="ui-card dash-acard" onClick={() => onNavigate(a.key)}>
            <span className="dash-acard-ico">{a.icon}</span>
            <div className="dash-acard-title">{a.title}</div>
            <div className="dash-acard-desc">{a.desc}</div>
            <div className="dash-acard-foot">
              <span className="dash-acard-meta">{a.meta}</span>
              <span className="dash-acard-open">
                Open
                <Arrow />
              </span>
            </div>
          </button>
        ))}
      </div>
    </>
  )
}
