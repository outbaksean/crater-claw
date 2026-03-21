import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useProviders } from './useProviders'
import * as client from '../api/client'
import type { ProviderEndpoint, ProviderStatus } from '../api/types'

vi.mock('../api/client')

const mockGetProviders = vi.mocked(client.getProviders)
const mockGetProviderStatus = vi.mocked(client.getProviderStatus)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('useProviders', () => {
  it('fetchProviders populates providers', async () => {
    const data: ProviderEndpoint[] = [{ name: 'local', baseUrl: 'http://localhost:11434' }]
    mockGetProviders.mockResolvedValue(data)

    const { providers, fetchProviders } = useProviders()
    await fetchProviders()

    expect(providers.value).toEqual(data)
  })

  it('fetchProviders sets error on failure', async () => {
    mockGetProviders.mockRejectedValue(new Error('network error'))

    const { error, fetchProviders } = useProviders()
    await fetchProviders()

    expect(error.value).toContain('network error')
  })

  it('selectProvider sets selectedProvider and fetches status', async () => {
    const provider: ProviderEndpoint = { name: 'local', baseUrl: 'http://localhost:11434' }
    const statusData: ProviderStatus = { isReachable: true, errorMessage: null }
    mockGetProviderStatus.mockResolvedValue(statusData)

    const { selectedProvider, status, selectProvider } = useProviders()
    await selectProvider(provider)

    expect(selectedProvider.value).toEqual(provider)
    expect(status.value).toEqual(statusData)
    expect(mockGetProviderStatus).toHaveBeenCalledWith('local')
  })

  it('selectProvider sets error when status check fails', async () => {
    const provider: ProviderEndpoint = { name: 'local', baseUrl: 'http://localhost:11434' }
    mockGetProviderStatus.mockRejectedValue(new Error('timeout'))

    const { error, selectProvider } = useProviders()
    await selectProvider(provider)

    expect(error.value).toContain('timeout')
  })
})
