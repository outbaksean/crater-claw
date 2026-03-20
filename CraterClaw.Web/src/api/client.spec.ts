import { describe, it, expect, vi, beforeEach } from 'vitest'
import { getProviders, getProviderStatus, getModels } from './client'
import type { ProviderEndpoint, ProviderStatus, ModelItem } from './types'

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

function mockResponse(data: unknown, ok = true, status = 200) {
    return Promise.resolve({
        ok,
        status,
        json: () => Promise.resolve(data),
    } as Response)
}

beforeEach(() => {
    mockFetch.mockReset()
})

describe('getProviders', () => {
    it('calls GET /api/providers and returns the result', async () => {
        const data: ProviderEndpoint[] = [{ name: 'local', baseUrl: 'http://localhost:11434' }]
        mockFetch.mockReturnValue(mockResponse(data))

        const result = await getProviders()

        expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/api/providers'))
        expect(result).toEqual(data)
    })

    it('throws when the response is not ok', async () => {
        mockFetch.mockReturnValue(mockResponse(null, false, 500))
        await expect(getProviders()).rejects.toThrow('500')
    })
})

describe('getProviderStatus', () => {
    it('calls GET /api/providers/{name}/status', async () => {
        const data: ProviderStatus = { isReachable: true, errorMessage: null }
        mockFetch.mockReturnValue(mockResponse(data))

        const result = await getProviderStatus('local')

        expect(mockFetch).toHaveBeenCalledWith(
            expect.stringContaining('/api/providers/local/status'),
        )
        expect(result).toEqual(data)
    })

    it('encodes special characters in provider name', async () => {
        mockFetch.mockReturnValue(mockResponse({ isReachable: false, errorMessage: null }))
        await getProviderStatus('my provider')
        expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('my%20provider'))
    })
})

describe('getModels', () => {
    it('calls GET /api/providers/{name}/models', async () => {
        const data: ModelItem[] = [
            { name: 'qwen3:8b', sizeBytes: 1000, modifiedAt: '2024-01-01T00:00:00Z' },
        ]
        mockFetch.mockReturnValue(mockResponse(data))

        const result = await getModels('local')

        expect(mockFetch).toHaveBeenCalledWith(
            expect.stringContaining('/api/providers/local/models'),
        )
        expect(result).toEqual(data)
    })
})
