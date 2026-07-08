import type { components } from './schema'

// DTO aliases sourced from the generated OpenAPI schema — the single contract
// with the backend. Regenerate with `npm run gen:api` after the API changes.
export type AreaDto = components['schemas']['AreaDto']
export type DeviceDto = components['schemas']['DeviceDto']
export type OperationDto = components['schemas']['OperationDto']
export type PredictRequest = components['schemas']['PredictRequest']
export type PredictResponse = components['schemas']['PredictResponse']
export type TsneRequestDto = components['schemas']['TsneRequestDto']
export type TsneResponseDto = components['schemas']['TsneResponseDto']

const BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(`${path} → HTTP ${res.status}`)
  return (await res.json()) as T
}

async function postJson<TReq, TRes>(path: string, body: TReq): Promise<TRes> {
  const res = await fetch(`${BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`${path} → HTTP ${res.status}`)
  return (await res.json()) as TRes
}

export const api = {
  areas: () => getJson<AreaDto[]>('/areas'),
  devices: () => getJson<DeviceDto[]>('/devices'),
  operations: () => getJson<OperationDto[]>('/operations'),
  predict: (req: PredictRequest) => postJson<PredictRequest, PredictResponse>('/analysis/predict', req),
  tsne: (req: TsneRequestDto) => postJson<TsneRequestDto, TsneResponseDto>('/anomaly/tsne', req),
}
