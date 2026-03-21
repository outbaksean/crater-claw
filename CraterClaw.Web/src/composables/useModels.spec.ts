import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useModels } from './useModels'
import * as client from '../api/client'
import type { ModelItem } from '../api/types'

vi.mock('../api/client')

const mockGetModels = vi.mocked(client.getModels)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('useModels', () => {
  it('fetchModels populates models', async () => {
    const data: ModelItem[] = [
      { name: 'qwen3:8b', sizeBytes: 5000000, modifiedAt: '2024-01-01T00:00:00Z' },
    ]
    mockGetModels.mockResolvedValue(data)

    const { models, fetchModels } = useModels()
    await fetchModels('local')

    expect(models.value).toEqual(data)
    expect(mockGetModels).toHaveBeenCalledWith('local')
  })

  it('fetchModels resets models and selectedModel before fetching', async () => {
    const initial: ModelItem[] = [{ name: 'old', sizeBytes: 0, modifiedAt: '' }]
    mockGetModels.mockResolvedValueOnce(initial)

    const { models, selectedModel, fetchModels, selectModel } = useModels()
    await fetchModels('local')
    selectModel(initial[0])

    const next: ModelItem[] = [{ name: 'new', sizeBytes: 0, modifiedAt: '' }]
    mockGetModels.mockResolvedValueOnce(next)
    await fetchModels('other')

    expect(models.value).toEqual(next)
    expect(selectedModel.value).toBeNull()
  })

  it('fetchModels sets error on failure', async () => {
    mockGetModels.mockRejectedValue(new Error('not found'))

    const { error, fetchModels } = useModels()
    await fetchModels('local')

    expect(error.value).toContain('not found')
  })

  it('selectModel sets selectedModel', () => {
    const model: ModelItem = { name: 'qwen3:8b', sizeBytes: 0, modifiedAt: '' }
    const { selectedModel, selectModel } = useModels()
    selectModel(model)
    expect(selectedModel.value).toEqual(model)
  })
})
