import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'

// Plotly is heavy and doesn't render under jsdom — stub it.
vi.mock('react-plotly.js', () => ({ default: () => <div data-testid="plot" /> }))

const predict = vi.fn()
vi.mock('../api/client', () => ({ api: { predict: (...args: unknown[]) => predict(...args) } }))

import { PredictionPage } from './PredictionPage'

describe('PredictionPage', () => {
  beforeEach(() => predict.mockReset())

  it('renders the predicted node and person after Predict', async () => {
    predict.mockResolvedValue({
      predictedNextNode: '2P',
      predictedPerson: 'Baba',
      matchCount: 3,
      ruleUsed: 'Similarity Rule',
      distribution: [{ node: '2P', probability: 1 }],
    })

    render(<PredictionPage />)
    fireEvent.click(screen.getByRole('button', { name: /predict/i }))

    await waitFor(() => expect(screen.getByText('2P')).toBeInTheDocument())
    expect(screen.getByText('Baba')).toBeInTheDocument()
    expect(predict).toHaveBeenCalledTimes(1)
  })

  it('parses "person: tokens" training lines into the request', async () => {
    predict.mockResolvedValue({
      predictedNextNode: '2P', predictedPerson: 'Baba', matchCount: 1, ruleUsed: 'r', distribution: [],
    })

    render(<PredictionPage />)
    fireEvent.click(screen.getByRole('button', { name: /predict/i }))

    await waitFor(() => expect(predict).toHaveBeenCalledTimes(1))
    const req = predict.mock.calls[0][0]
    // default sample: 3 rows, each "Person: tok tok …"
    expect(req.training).toHaveLength(3)
    expect(req.training[0]).toEqual({ person: 'Baba', tokens: ['1P', '1I', '2P', '2I'] })
    expect(req.prefix).toEqual(['1P', '1I'])
  })
})
