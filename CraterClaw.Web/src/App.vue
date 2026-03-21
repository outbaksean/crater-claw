<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useProviders } from './composables/useProviders'
import { useModels } from './composables/useModels'
import { useProfiles } from './composables/useProfiles'
import InteractiveChat from './components/InteractiveChat.vue'
import ProfileSelector from './components/ProfileSelector.vue'
import AgenticPanel from './components/AgenticPanel.vue'
import type { ProviderEndpoint, ModelItem, BehaviorProfile } from './api/types'

const {
  providers,
  selectedProvider,
  status,
  loadingProviders,
  loadingStatus,
  error: providerError,
  fetchProviders,
  selectProvider,
} = useProviders()

const {
  models,
  selectedModel,
  loading: loadingModels,
  error: modelError,
  fetchModels,
  selectModel,
} = useModels()

onMounted(fetchProviders)

watch(
  () => status.value,
  (s) => {
    if (s?.isReachable && selectedProvider.value) {
      fetchModels(selectedProvider.value.name)
    }
  },
)

function onSelectProvider(provider: ProviderEndpoint) {
  selectProvider(provider)
}

function onSelectModel(model: ModelItem) {
  selectModel(model)
}

const {
  profiles,
  selectedProfile,
  loading: loadingProfiles,
  error: profileError,
  fetchProfiles,
  selectProfile,
} = useProfiles()

onMounted(fetchProfiles)

function onSelectProfile(profile: BehaviorProfile) {
  selectProfile(profile)
}

function formatSize(bytes: number): string {
  if (bytes >= 1_073_741_824) return `${(bytes / 1_073_741_824).toFixed(1)}gb`
  return `${(bytes / 1_048_576).toFixed(0)}mb`
}
</script>

<template>
  <div class="root">
    <header class="app-header">
      <h1 class="wordmark">CRATERCLAW</h1>
    </header>
    <main class="workspace">
      <section class="panel">
        <p class="panel-label">provider</p>
        <p v-if="loadingProviders" class="dim">loading...</p>
        <p v-else-if="providerError" class="error">{{ providerError }}</p>
        <ul v-else class="item-list">
          <li
            v-for="(provider, i) in providers"
            :key="provider.name"
            :class="['item', { 'item--selected': selectedProvider?.name === provider.name }]"
            @click="onSelectProvider(provider)"
          >
            <span class="item-index">{{ i + 1 }}</span>
            <span class="item-name">{{ provider.name }}</span>
            <span class="item-meta">{{ provider.baseUrl }}</span>
            <span v-if="selectedProvider?.name === provider.name" class="item-status">
              <span v-if="loadingStatus" class="pill pill--loading">checking</span>
              <span
                v-else-if="status"
                :class="['pill', status.isReachable ? 'pill--ok' : 'pill--err']"
                >{{ status.isReachable ? 'reachable' : 'unreachable' }}</span
              >
            </span>
          </li>
        </ul>
      </section>

      <Transition name="panel">
        <section v-if="status?.isReachable" class="panel">
          <p class="panel-label">model</p>
          <p v-if="loadingModels" class="dim">loading...</p>
          <p v-else-if="modelError" class="error">{{ modelError }}</p>
          <ul v-else class="item-list">
            <li
              v-for="(model, i) in models"
              :key="model.name"
              :class="['item', { 'item--selected': selectedModel?.name === model.name }]"
              @click="onSelectModel(model)"
            >
              <span class="item-index">{{ i + 1 }}</span>
              <span class="item-name">{{ model.name }}</span>
              <span class="item-meta">{{ formatSize(model.sizeBytes) }}</span>
            </li>
          </ul>
        </section>
      </Transition>

      <Transition name="panel">
        <section v-if="selectedModel && selectedProvider" class="panel">
          <p class="panel-label">chat</p>
          <InteractiveChat
            :provider-name="selectedProvider.name"
            :model-name="selectedModel.name"
          />
        </section>
      </Transition>

      <Transition name="panel">
        <section v-if="status?.isReachable" class="panel">
          <p class="panel-label">profile</p>
          <p v-if="loadingProfiles" class="dim">loading...</p>
          <p v-else-if="profileError" class="error">{{ profileError }}</p>
          <ProfileSelector
            v-else
            :profiles="profiles"
            :selected-profile="selectedProfile"
            @select="onSelectProfile"
          />
        </section>
      </Transition>

      <Transition name="panel">
        <section v-if="selectedProfile && selectedModel && selectedProvider" class="panel">
          <p class="panel-label">task &mdash; {{ selectedProfile.name }}</p>
          <AgenticPanel
            :provider-name="selectedProvider.name"
            :model-name="selectedModel.name"
            :profile-id="selectedProfile.id"
          />
        </section>
      </Transition>
    </main>
  </div>
</template>

<style scoped>
.root {
  max-width: 680px;
  margin: 0 auto;
  padding: 0 24px;
}

.app-header {
  padding: 32px 0 24px;
}

.wordmark {
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 800;
  letter-spacing: 0.05em;
  color: var(--text);
}

.workspace {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding-bottom: 48px;
}

.panel {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 20px 24px;
}

.panel-label {
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--text-dim);
  margin-bottom: 12px;
}

.item-list {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: var(--radius);
  cursor: pointer;
  border-left: 3px solid transparent;
  transition:
    background var(--transition),
    border-color var(--transition);
}

.item:hover {
  background: var(--surface-raised);
}

.item--selected {
  border-left-color: var(--accent);
  background: var(--surface-raised);
}

.item-index {
  color: var(--text-dim);
  min-width: 16px;
  flex-shrink: 0;
}

.item-name {
  flex: 1;
}

.item-meta {
  color: var(--text-dim);
  font-size: 11px;
}

.item-status {
  margin-left: auto;
}

.pill {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 999px;
}

.pill--loading {
  color: var(--text-dim);
}

.pill--ok {
  background: rgba(62, 207, 120, 0.12);
  color: var(--ok);
}

.pill--err {
  background: rgba(224, 85, 85, 0.12);
  color: var(--err);
}

.dim {
  color: var(--text-dim);
}

.error {
  color: var(--err);
}
</style>
