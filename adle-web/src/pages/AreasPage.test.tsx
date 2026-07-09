import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'

vi.mock('react-plotly.js', () => ({ default: () => <div data-testid="plot" /> }))

const areas = vi.fn()
const createArea = vi.fn()
const updateArea = vi.fn()
const deleteArea = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    areas: (...a: unknown[]) => areas(...a),
    createArea: (...a: unknown[]) => createArea(...a),
    updateArea: (...a: unknown[]) => updateArea(...a),
    deleteArea: (...a: unknown[]) => deleteArea(...a),
  },
}))

import { AreasPage } from './AreasPage'

const SEED = [
  { id: 1, name: 'Ev', width: 120, height: 80, parentAreaId: null, areaTypeId: 6, areaTypeName: 'HouseArea' },
  { id: 2, name: 'Oturma Odası', width: 30, height: 20, parentAreaId: 1, areaTypeId: 1, areaTypeName: 'LivingRoom' },
]

describe('AreasPage', () => {
  beforeEach(() => {
    areas.mockReset()
    createArea.mockReset()
  })

  it('renders areas returned by the API', async () => {
    areas.mockResolvedValue(SEED)

    render(<AreasPage />)

    await waitFor(() => expect(screen.getByText('Ev')).toBeInTheDocument())
    expect(screen.getByText('Oturma Odası')).toBeInTheDocument()
    expect(screen.getByText('LivingRoom')).toBeInTheDocument()
  })

  it('creates an area and reloads the list', async () => {
    areas.mockResolvedValueOnce(SEED) // initial load
    createArea.mockResolvedValue({ ...SEED[1], id: 3, name: 'Yeni Oda' })
    areas.mockResolvedValueOnce([...SEED, { id: 3, name: 'Yeni Oda', width: 5, height: 5, parentAreaId: 1, areaTypeId: 1, areaTypeName: 'LivingRoom' }])

    render(<AreasPage />)
    await waitFor(() => expect(screen.getByText('Ev')).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Yeni Oda' } })
    fireEvent.click(screen.getByRole('button', { name: /^create$/i }))

    await waitFor(() => expect(createArea).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(screen.getByText('Yeni Oda')).toBeInTheDocument())
  })
})
