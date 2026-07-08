import Plot from 'react-plotly.js'
import type { AreaDto } from '../api/client'

/**
 * P1 viz spike: proves react-plotly.js renders a real scatter from live API
 * data. Each area is a point (width × height), grouped/coloured by area type.
 */
export function AreaScatter({ areas }: { areas: AreaDto[] }) {
  // One trace per area type so Plotly colours + legends them automatically.
  const byType = new Map<string, AreaDto[]>()
  for (const a of areas) {
    const key = a.areaTypeName ?? '(none)'
    const bucket = byType.get(key)
    if (bucket) bucket.push(a)
    else byType.set(key, [a])
  }

  const data = [...byType.entries()].map(([type, items]) => ({
    type: 'scatter',
    mode: 'markers+text',
    name: type,
    x: items.map((a) => a.width),
    y: items.map((a) => a.height),
    text: items.map((a) => a.name ?? ''),
    textposition: 'top center',
    marker: { size: 14 },
  }))

  return (
    <Plot
      data={data}
      layout={{
        title: { text: 'Areas — width × height (spike)' },
        xaxis: { title: { text: 'Width' } },
        yaxis: { title: { text: 'Height' } },
        legend: { title: { text: 'Area type' } },
        margin: { t: 48, r: 16, b: 48, l: 48 },
        autosize: true,
      }}
      style={{ width: '100%', height: '440px' }}
      useResizeHandler
      config={{ displayModeBar: false, responsive: true }}
    />
  )
}
