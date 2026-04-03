<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useProviders } from './composables/useProviders'
import { useModels } from './composables/useModels'
import { useProfiles } from './composables/useProfiles'
import { useBehaviorDefaults } from './composables/useBehaviorDefaults'
import AppTaskbar from './components/AppTaskbar.vue'
import AgenticPanel from './components/AgenticPanel.vue'
import type { ProviderEndpoint, ModelItem, BehaviorProfile } from './api/types'

const {
  providers,
  selectedProvider,
  status,
  loadingProviders,
  loadingStatus,
  fetchProviders,
  selectProvider,
} = useProviders()

const { models, selectedModel, loading: loadingModels, fetchModels, selectModel } = useModels()

onMounted(fetchProviders)

watch(
  () => status.value,
  (s) => {
    if (s?.isReachable && selectedProvider.value) {
      fetchModels(selectedProvider.value.name)
    }
  },
)

const {
  profiles,
  selectedProfile,
  loading: loadingProfiles,
  fetchProfiles,
  selectProfile,
} = useProfiles()

const { behaviorWarnings, applyProfileDefaults } = useBehaviorDefaults(
  providers,
  models,
  selectProvider,
  selectModel,
)

onMounted(fetchProfiles)

function onSelectProvider(provider: ProviderEndpoint) {
  selectProvider(provider)
}

function onSelectModel(model: ModelItem) {
  selectModel(model)
}

function onSelectProfile(profile: BehaviorProfile) {
  selectProfile(profile)
  applyProfileDefaults(profile)
}
</script>

<template>
  <div class="app">
    <AppTaskbar
      :providers="providers"
      :selected-provider="selectedProvider"
      :provider-status="status"
      :loading-providers="loadingProviders"
      :loading-status="loadingStatus"
      :models="models"
      :selected-model="selectedModel"
      :loading-models="loadingModels"
      :profiles="profiles"
      :selected-profile="selectedProfile"
      :loading-profiles="loadingProfiles"
      :warnings="behaviorWarnings"
      @select-provider="onSelectProvider"
      @select-model="onSelectModel"
      @select-profile="onSelectProfile"
    />
    <main class="content">
      <div v-if="selectedProvider && selectedModel" class="chat-area">
        <AgenticPanel
          :provider-name="selectedProvider.name"
          :model-name="selectedModel.name"
          :profile-id="selectedProfile?.id ?? 'no-tools'"
        />
      </div>
      <p v-else class="placeholder">select a provider and model to begin</p>
    </main>
  </div>
</template>

<style scoped>
.app {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.content {
  flex: 1;
  max-width: 760px;
  width: 100%;
  margin: 0 auto;
  padding: 24px 24px 48px;
}

.chat-area {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.placeholder {
  margin-top: 48px;
  text-align: center;
  color: var(--text-dim);
  font-size: 13px;
}
</style>
