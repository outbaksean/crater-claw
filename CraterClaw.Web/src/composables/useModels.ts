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
    try {
      const loaded = await getModels(providerName)
      models.value = loaded
      if (selectedModel.value && !loaded.some((m) => m.name === selectedModel.value!.name)) {
        selectedModel.value = null
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load models'
      selectedModel.value = null
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
