import { ref, watch } from 'vue'
import type { Ref } from 'vue'
import type { BehaviorProfile, ProviderEndpoint, ModelItem } from '../api/types'

export function useBehaviorDefaults(
  providers: Ref<ProviderEndpoint[]>,
  models: Ref<ModelItem[]>,
  selectProvider: (provider: ProviderEndpoint) => void,
  selectModel: (model: ModelItem) => void,
) {
  const behaviorWarnings = ref<string[]>([])
  const pendingModelName = ref<string | null>(null)

  watch(models, (newModels) => {
    if (!pendingModelName.value) return
    const name = pendingModelName.value
    const match = newModels.find((m) => m.name.toLowerCase() === name.toLowerCase())
    if (match) {
      selectModel(match)
      pendingModelName.value = null
      behaviorWarnings.value = behaviorWarnings.value.filter((w) => !w.includes(name))
    }
  })

  function applyProfileDefaults(profile: BehaviorProfile) {
    behaviorWarnings.value = []
    pendingModelName.value = null

    if (profile.preferredProviderName) {
      const match = providers.value.find(
        (p) => p.name.toLowerCase() === profile.preferredProviderName!.toLowerCase(),
      )
      if (match) {
        selectProvider(match)
      } else {
        behaviorWarnings.value.push(
          `Profile prefers provider '${profile.preferredProviderName}' which is not configured`,
        )
      }
    }

    if (profile.preferredModelName) {
      const match = models.value.find(
        (m) => m.name.toLowerCase() === profile.preferredModelName!.toLowerCase(),
      )
      if (match) {
        selectModel(match)
      } else {
        pendingModelName.value = profile.preferredModelName
        behaviorWarnings.value.push(
          `Profile prefers model '${profile.preferredModelName}' which is not available`,
        )
      }
    }
  }

  return { behaviorWarnings, applyProfileDefaults }
}
