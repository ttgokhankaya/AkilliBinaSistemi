import { lazy, Suspense, useState } from 'react'
import './App.css'

// Lazy-load pages so the Plotly-heavy ones land in their own async chunks
// instead of bloating the initial bundle.
const AreasPage = lazy(() => import('./pages/AreasPage').then((m) => ({ default: m.AreasPage })))
const DevicesPage = lazy(() => import('./pages/DevicesPage').then((m) => ({ default: m.DevicesPage })))
const OperationsPage = lazy(() => import('./pages/OperationsPage').then((m) => ({ default: m.OperationsPage })))
const PredictionPage = lazy(() => import('./pages/PredictionPage').then((m) => ({ default: m.PredictionPage })))
const AnomalyPage = lazy(() => import('./pages/AnomalyPage').then((m) => ({ default: m.AnomalyPage })))
const GraphsPage = lazy(() => import('./pages/GraphsPage').then((m) => ({ default: m.GraphsPage })))

const TABS = {
  areas: { label: 'Areas', render: () => <AreasPage /> },
  devices: { label: 'Devices', render: () => <DevicesPage /> },
  operations: { label: 'Operations', render: () => <OperationsPage /> },
  graphs: { label: 'Graphs', render: () => <GraphsPage /> },
  prediction: { label: 'Prediction', render: () => <PredictionPage /> },
  anomaly: { label: 'Anomaly', render: () => <AnomalyPage /> },
} as const

type TabKey = keyof typeof TABS

function App() {
  const [tab, setTab] = useState<TabKey>('areas')

  return (
    <div className="app">
      <header>
        <h1>ADLE Web</h1>
        <nav>
          {(Object.keys(TABS) as TabKey[]).map((key) => (
            <button
              key={key}
              className={key === tab ? 'active' : ''}
              onClick={() => setTab(key)}
            >
              {TABS[key].label}
            </button>
          ))}
        </nav>
      </header>
      <main>
        <Suspense fallback={<p className="status">Loading…</p>}>{TABS[tab].render()}</Suspense>
      </main>
    </div>
  )
}

export default App
