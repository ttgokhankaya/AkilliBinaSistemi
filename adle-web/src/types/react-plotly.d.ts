// Minimal ambient types for react-plotly.js.
// The DefinitelyTyped @types/react-plotly.js peer-depends on @types/react 18,
// which conflicts with this project's React 19, so we declare just what we use.
declare module 'react-plotly.js' {
  import type { CSSProperties, Component } from 'react'

  export interface PlotParams {
    data: Array<Record<string, unknown>>
    layout?: Record<string, unknown>
    config?: Record<string, unknown>
    style?: CSSProperties
    className?: string
    useResizeHandler?: boolean
    onInitialized?: (...args: unknown[]) => void
    onUpdate?: (...args: unknown[]) => void
  }

  export default class Plot extends Component<PlotParams> {}
}
