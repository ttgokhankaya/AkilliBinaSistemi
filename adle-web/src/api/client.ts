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
export type RandomForestRequestDto = components['schemas']['RandomForestRequestDto']
export type RandomForestResponseDto = components['schemas']['RandomForestResponseDto']
export type GraphDto = components['schemas']['GraphDto']
export type GraphWriteDto = components['schemas']['GraphWriteDto']
export type RunGraphRequest = components['schemas']['RunGraphRequest']
export type RunGraphResponse = components['schemas']['RunGraphResponse']
export type AreaWriteDto = components['schemas']['AreaWriteDto']
export type DeviceWriteDto = components['schemas']['DeviceWriteDto']
export type OperationWriteDto = components['schemas']['OperationWriteDto']

const BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) throw new Error(`${path} → HTTP ${res.status}`)
  return (await res.json()) as T
}

async function send<TReq, TRes>(method: string, path: string, body?: TReq): Promise<TRes> {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) {
    let msg = `HTTP ${res.status}`
    try {
      const e = await res.json()
      if (e?.error) msg = e.error
    } catch {
      /* non-JSON body */
    }
    throw new Error(`${path} → ${msg}`)
  }
  return (res.status === 204 ? (undefined as TRes) : ((await res.json()) as TRes))
}

const postJson = <TReq, TRes>(path: string, body: TReq) => send<TReq, TRes>('POST', path, body)
const putJson = <TReq, TRes>(path: string, body: TReq) => send<TReq, TRes>('PUT', path, body)
const del = (path: string) => send<never, void>('DELETE', path)

export const api = {
  areas: () => getJson<AreaDto[]>('/areas'),
  createArea: (req: AreaWriteDto) => postJson<AreaWriteDto, AreaDto>('/areas', req),
  updateArea: (id: number, req: AreaWriteDto) => putJson<AreaWriteDto, AreaDto>(`/areas/${id}`, req),
  deleteArea: (id: number) => del(`/areas/${id}`),

  devices: () => getJson<DeviceDto[]>('/devices'),
  createDevice: (req: DeviceWriteDto) => postJson<DeviceWriteDto, DeviceDto>('/devices', req),
  updateDevice: (id: number, req: DeviceWriteDto) => putJson<DeviceWriteDto, DeviceDto>(`/devices/${id}`, req),
  deleteDevice: (id: number) => del(`/devices/${id}`),

  operations: () => getJson<OperationDto[]>('/operations'),
  createOperation: (req: OperationWriteDto) => postJson<OperationWriteDto, OperationDto>('/operations', req),
  updateOperation: (id: number, req: OperationWriteDto) =>
    putJson<OperationWriteDto, OperationDto>(`/operations/${id}`, req),
  deleteOperation: (id: number) => del(`/operations/${id}`),

  predict: (req: PredictRequest) => postJson<PredictRequest, PredictResponse>('/analysis/predict', req),
  tsne: (req: TsneRequestDto) => postJson<TsneRequestDto, TsneResponseDto>('/anomaly/tsne', req),
  randomForest: (req: RandomForestRequestDto) =>
    postJson<RandomForestRequestDto, RandomForestResponseDto>('/anomaly/random-forest', req),
  graphs: () => getJson<GraphDto[]>('/graphs'),
  createGraph: (req: GraphWriteDto) => postJson<GraphWriteDto, GraphDto>('/graphs', req),
  runGraph: (id: number, req: RunGraphRequest) =>
    postJson<RunGraphRequest, RunGraphResponse>(`/graphs/${id}/run`, req),
}
