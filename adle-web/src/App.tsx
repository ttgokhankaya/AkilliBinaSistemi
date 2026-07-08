import { useState } from 'react'
import { AreasPage } from './pages/AreasPage'
import { DevicesPage } from './pages/DevicesPage'
import { OperationsPage } from './pages/OperationsPage'
import { PredictionPage } from './pages/PredictionPage'
import { AnomalyPage } from './pages/AnomalyPage'
import './App.css'

const TABS = {
  areas: { label: 'Areas', render: () => <AreasPage /> },
  devices: { label: 'Devices', render: () => <DevicesPage /> },
  operations: { label: 'Operations', render: () => <OperationsPage /> },
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
      <main>{TABS[tab].render()}</main>
    </div>
  )
}

export default App
