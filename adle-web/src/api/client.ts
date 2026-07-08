import type { components } from './schema'

// DTO aliases sourced from the generated OpenAPI schema — the single contract
// with the backend. Regenerate with `npm run gen:api` after the API changes.
export type AreaDto = components['schemas']['AreaDto']
export type DeviceDto = components['schemas']['DeviceDto']
export type OperationDto = components['schemas']['OperationDto']

const BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(`${path} → HTTP ${res.status}`)
  return (await res.json()) as T
}

export const api = {
  areas: () => getJson<AreaDto[]>('/areas'),
  devices: () => getJson<DeviceDto[]>('/devices'),
  operations: () => getJson<OperationDto[]>('/operations'),
}
