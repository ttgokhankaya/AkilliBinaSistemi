import { lazy, Suspense, useState } from 'react'
import './App.css'
import './theme.css'
import './ui.css'
import { ThemeProvider, useTheme } from './ThemeContext'

// Lazy-load pages so the Plotly-heavy ones land in their own async chunks
// instead of bloating the initial bundle.
const DashboardPage = lazy(() => import('./pages/DashboardPage').then((m) => ({ default: m.DashboardPage })))
const AreasPage = lazy(() => import('./pages/AreasPage').then((m) => ({ default: m.AreasPage })))
const DevicesPage = lazy(() => import('./pages/DevicesPage').then((m) => ({ default: m.DevicesPage })))
const OperationsPage = lazy(() => import('./pages/OperationsPage').then((m) => ({ default: m.OperationsPage })))
const PredictionPage = lazy(() => import('./pages/PredictionPage').then((m) => ({ default: m.PredictionPage })))
const AnomalyPage = lazy(() => import('./pages/AnomalyPage').then((m) => ({ default: m.AnomalyPage })))
const GraphsPage = lazy(() => import('./pages/GraphsPage').then((m) => ({ default: m.GraphsPage })))

type TabKey = 'dashboard' | 'areas' | 'devices' | 'operations' | 'graphs' | 'prediction' | 'anomaly'

// Nav is split into two groups (design: "model the home" then "simulate &
// analyze"). Dashboard is reached via the logo, so it is not a nav item.
const NAV: { key: TabKey; label: string }[][] = [
  [
    { key: 'areas', label: 'Areas' },
    { key: 'devices', label: 'Devices' },
    { key: 'operations', label: 'Operations' },
  ],
  [
    { key: 'graphs', label: 'Graphs' },
    { key: 'prediction', label: 'Prediction' },
    { key: 'anomaly', label: 'Anomaly' },
  ],
]

function renderPage(tab: TabKey, navigate: (key: TabKey) => void) {
  switch (tab) {
    case 'dashboard':
      return <DashboardPage onNavigate={navigate} />
    case 'areas':
      return <AreasPage />
    case 'devices':
      return <DevicesPage />
    case 'operations':
      return <OperationsPage />
    case 'graphs':
      return <GraphsPage />
    case 'prediction':
      return <PredictionPage />
    case 'anomaly':
      return <AnomalyPage />
  }
}

function Shell() {
  const { theme, toggle } = useTheme()
  const [tab, setTab] = useState<TabKey>('dashboard')

  return (
    <div className="adle-root" data-theme={theme}>
      <header className="adle-header">
        <button className="adle-brand" onClick={() => setTab('dashboard')} aria-label="ADLE home">
          <span className="adle-logo">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M3 3v18h18" />
              <path d="M7 15l3.5-4 3 2.5L21 7" />
            </svg>
          </span>
          <span className="adle-wordmark">ADLE</span>
        </button>
        <nav className="adle-nav">
          {NAV.map((group, gi) => (
            <span key={gi} style={{ display: 'contents' }}>
              {gi > 0 && <span className="adle-nav-div" />}
              {group.map(({ key, label }) => (
                <button
                  key={key}
                  className={`adle-navlink${key === tab ? ' active' : ''}`}
                  onClick={() => setTab(key)}
                >
                  {label}
                </button>
              ))}
            </span>
          ))}
        </nav>
        <button className="adle-toggle" onClick={toggle} aria-label="Toggle color theme">
          {theme === 'dark' ? (
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="4" />
              <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
            </svg>
          ) : (
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z" />
            </svg>
          )}
          <span>{theme === 'dark' ? 'Light' : 'Dark'}</span>
        </button>
      </header>
      <main className="adle-main">
        <Suspense fallback={<p className="status">Loading…</p>}>{renderPage(tab, setTab)}</Suspense>
      </main>
    </div>
  )
}

function App() {
  return (
    <ThemeProvider>
      <Shell />
    </ThemeProvider>
  )
}

export default App
