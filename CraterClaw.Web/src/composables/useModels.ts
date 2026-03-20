import { ref } from 'vue'
import { getModels } from '../api/client'
import type { ModelItem } from '../api/types'

export function useModels() {
    const models = ref<ModelItem[]>([])
    const selectedModel = ref<ModelItem | null>(null)
    const loading = ref(false)
    const error = ref<string | null>(null)

    async function fetchModels(providerName: string) {
        loading.value = true
        error.value = null
        models.value = []
        selectedModel.value = null
        try {
            models.value = await getModels(providerName)
        } catch (e) {
            error.value = e instanceof Error ? e.message : 'Failed to load models'
        } finally {
            loading.value = false
        }
    }

    function selectModel(model: ModelItem) {
        selectedModel.value = model
    }

    return {
        models,
        selectedModel,
        loading,
        error,
        fetchModels,
        selectModel,
    }
}
