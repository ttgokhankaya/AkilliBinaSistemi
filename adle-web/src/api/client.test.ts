import { afterEach, describe, expect, it, vi } from 'vitest'
import { api } from './client'

afterEach(() => vi.restoreAllMocks())

describe('api client', () => {
  it('surfaces the API error message on a non-2xx response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: false, status: 400, json: async () => ({ error: 'bad FK' }) }),
    )
    await expect(
      api.createArea({ name: 'x', width: 1, height: 1, parentAreaId: null, areaTypeId: null }),
    ).rejects.toThrow(/bad FK/)
  })

  it('resolves to undefined for a 204 (delete)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }))
    await expect(api.deleteArea(5)).resolves.toBeUndefined()
  })

  it('posts JSON and returns the parsed body', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ predictedNextNode: '2P', predictedPerson: 'Baba', matchCount: 1, ruleUsed: 'r', distribution: [] }),
    })
    vi.stubGlobal('fetch', fetchMock)

    const res = await api.predict({ training: [], prefix: ['1P'], lcsThreshold: 0 })

    expect(res.predictedNextNode).toBe('2P')
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/analysis/predict')
    expect(init.method).toBe('POST')
  })
})
