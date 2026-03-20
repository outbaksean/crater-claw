import { ref } from 'vue'
import { getProviders, getProviderStatus } from '../api/client'
import type { ProviderEndpoint, ProviderStatus } from '../api/types'

export function useProviders() {
    const providers = ref<ProviderEndpoint[]>([])
    const selectedProvider = ref<ProviderEndpoint | null>(null)
    const status = ref<ProviderStatus | null>(null)
    const loadingProviders = ref(false)
    const loadingStatus = ref(false)
    const error = ref<string | null>(null)

    async function fetchProviders() {
        loadingProviders.value = true
        error.value = null
        try {
            providers.value = await getProviders()
        } catch (e) {
            error.value = e instanceof Error ? e.message : 'Failed to load providers'
        } finally {
            loadingProviders.value = false
        }
    }

    async function selectProvider(provider: ProviderEndpoint) {
        selectedProvider.value = provider
        status.value = null
        loadingStatus.value = true
        error.value = null
        try {
            status.value = await getProviderStatus(provider.name)
        } catch (e) {
            error.value = e instanceof Error ? e.message : 'Failed to check provider status'
        } finally {
            loadingStatus.value = false
        }
    }

    return {
        providers,
        selectedProvider,
        status,
        loadingProviders,
        loadingStatus,
        error,
        fetchProviders,
        selectProvider,
    }
}
