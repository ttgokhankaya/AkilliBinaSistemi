import { useEffect, useState } from 'react'

type State<T> = { data: T | null; error: string | null; loading: boolean }

/** Fetches once on mount via the given loader; tracks loading/error state. */
export function useApi<T>(load: () => Promise<T>): State<T> {
  const [state, setState] = useState<State<T>>({ data: null, error: null, loading: true })

  useEffect(() => {
    let alive = true
    setState({ data: null, error: null, loading: true })
    load()
      .then((data) => alive && setState({ data, error: null, loading: false }))
      .catch((e: unknown) =>
        alive && setState({ data: null, error: e instanceof Error ? e.message : String(e), loading: false }),
      )
    return () => {
      alive = false
    }
    // load identity is stable per page (module-level api.* fns)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return state
}
