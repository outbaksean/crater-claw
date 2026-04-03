<script setup lang="ts">
import { ref } from 'vue'
import type { ProviderEndpoint, ProviderStatus, ModelItem, BehaviorProfile } from '../api/types'

const props = defineProps<{
  providers: ProviderEndpoint[]
  selectedProvider: ProviderEndpoint | null
  providerStatus: ProviderStatus | null
  loadingProviders: boolean
  loadingStatus: boolean

  models: ModelItem[]
  selectedModel: ModelItem | null
  loadingModels: boolean

  profiles: BehaviorProfile[]
  selectedProfile: BehaviorProfile | null
  loadingProfiles: boolean

  warnings: string[]
}>()

const emit = defineEmits<{
  selectProvider: [provider: ProviderEndpoint]
  selectModel: [model: ModelItem]
  selectProfile: [profile: BehaviorProfile]
}>()

const openSection = ref<'profile' | 'provider' | 'model' | null>(null)

function toggle(section: 'profile' | 'provider' | 'model') {
  openSection.value = openSection.value === section ? null : section
}

function onSelectProvider(provider: ProviderEndpoint) {
  emit('selectProvider', provider)
  openSection.value = null
}

function onSelectModel(model: ModelItem) {
  emit('selectModel', model)
  openSection.value = null
}

function onSelectProfile(profile: BehaviorProfile) {
  emit('selectProfile', profile)
  openSection.value = null
}
</script>

<template>
  <div class="taskbar-root">
    <div class="taskbar">
      <span class="wordmark">CRATERCLAW</span>

      <div class="selectors">
        <!-- Profile selector -->
        <div class="selector" data-section="profile">
          <button
            class="selector-trigger selector-trigger--profile"
            :class="{ 'selector-trigger--active': openSection === 'profile' }"
            @click="toggle('profile')"
          >
            <span class="selector-label">profile</span>
            <span class="selector-value">
              {{ props.selectedProfile?.name ?? 'none' }}
            </span>
            <span class="selector-chevron">{{ openSection === 'profile' ? '▲' : '▼' }}</span>
          </button>
          <div v-if="openSection === 'profile'" class="selector-dropdown">
            <p v-if="props.loadingProfiles" class="selector-loading">loading...</p>
            <ul v-else class="selector-list">
              <li
                v-for="p in props.profiles"
                :key="p.id"
                :class="[
                  'selector-option',
                  { 'selector-option--selected': props.selectedProfile?.id === p.id },
                ]"
                @click="onSelectProfile(p)"
              >
                <span class="option-name">{{ p.name }}</span>
                <span class="option-meta">{{ p.description }}</span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Provider selector -->
        <div class="selector" data-section="provider">
          <button
            class="selector-trigger"
            :class="{ 'selector-trigger--active': openSection === 'provider' }"
            @click="toggle('provider')"
          >
            <span class="selector-label">provider</span>
            <span class="selector-value">
              {{ props.selectedProvider?.name ?? 'none' }}
            </span>
            <span v-if="props.selectedProvider && props.providerStatus" class="pill-inline">
              <span v-if="props.loadingStatus" class="pill pill--loading">checking</span>
              <span
                v-else
                :class="['pill', props.providerStatus.isReachable ? 'pill--ok' : 'pill--err']"
                >{{ props.providerStatus.isReachable ? 'ok' : 'err' }}</span
              >
            </span>
            <span class="selector-chevron">{{ openSection === 'provider' ? '▲' : '▼' }}</span>
          </button>
          <div v-if="openSection === 'provider'" class="selector-dropdown">
            <p v-if="props.loadingProviders" class="selector-loading">loading...</p>
            <ul v-else class="selector-list">
              <li
                v-for="p in props.providers"
                :key="p.name"
                :class="[
                  'selector-option',
                  { 'selector-option--selected': props.selectedProvider?.name === p.name },
                ]"
                @click="onSelectProvider(p)"
              >
                <span class="option-name">{{ p.name }}</span>
                <span class="option-meta">{{ p.baseUrl }}</span>
              </li>
            </ul>
          </div>
        </div>

        <!-- Model selector -->
        <div class="selector" data-section="model">
          <button
            class="selector-trigger"
            :class="{ 'selector-trigger--active': openSection === 'model' }"
            :disabled="!props.selectedProvider"
            @click="toggle('model')"
          >
            <span class="selector-label">model</span>
            <span class="selector-value">
              {{ props.selectedModel?.name ?? 'none' }}
            </span>
            <span class="selector-chevron">{{ openSection === 'model' ? '▲' : '▼' }}</span>
          </button>
          <div v-if="openSection === 'model'" class="selector-dropdown selector-dropdown--right">
            <p v-if="props.loadingModels" class="selector-loading">loading...</p>
            <ul v-else class="selector-list">
              <li
                v-for="m in props.models"
                :key="m.name"
                :class="[
                  'selector-option',
                  { 'selector-option--selected': props.selectedModel?.name === m.name },
                ]"
                @click="onSelectModel(m)"
              >
                <span class="option-name">{{ m.name }}</span>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>

    <div v-if="props.warnings.length" class="taskbar-warnings">
      <span v-for="warning in props.warnings" :key="warning" class="taskbar-warning">{{
        warning
      }}</span>
    </div>
  </div>
</template>

<style scoped>
.taskbar-root {
  border-bottom: 1px solid var(--border);
  background: var(--surface);
}

.taskbar {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 0 24px;
  height: 48px;
}

.wordmark {
  font-family: var(--font-display);
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 0.06em;
  color: var(--text);
  flex-shrink: 0;
}

.selectors {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: 1;
}

.selector {
  position: relative;
}

.selector-trigger {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--radius);
  cursor: pointer;
  font-family: var(--font-ui);
  font-size: 12px;
  color: var(--text);
  transition:
    background var(--transition),
    border-color var(--transition);
  white-space: nowrap;
}

.selector-trigger:hover:not(:disabled) {
  background: var(--surface-raised);
  border-color: var(--border);
}

.selector-trigger--active {
  background: var(--surface-raised);
  border-color: var(--border-active);
}

.selector-trigger--profile {
  border-left: 2px solid var(--accent);
}

.selector-trigger:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.selector-label {
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--text-dim);
}

.selector-value {
  color: var(--text);
  font-weight: 500;
}

.selector-chevron {
  font-size: 8px;
  color: var(--text-dim);
}

.pill-inline {
  display: flex;
  align-items: center;
}

.pill {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 999px;
  font-family: var(--font-ui);
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

.selector-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  z-index: 100;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  min-width: 200px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
}

.selector-dropdown--right {
  left: auto;
  right: 0;
}

.selector-loading {
  padding: 10px 14px;
  font-size: 12px;
  color: var(--text-dim);
}

.selector-list {
  list-style: none;
  padding: 4px;
}

.selector-option {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 7px 10px;
  border-radius: calc(var(--radius) - 2px);
  cursor: pointer;
  border-left: 2px solid transparent;
  transition:
    background var(--transition),
    border-color var(--transition);
}

.selector-option:hover {
  background: var(--surface-raised);
}

.selector-option--selected {
  border-left-color: var(--accent);
  background: var(--surface-raised);
}

.option-name {
  font-size: 12px;
  color: var(--text);
}

.option-meta {
  font-size: 10px;
  color: var(--text-dim);
}

.taskbar-warnings {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  padding: 4px 24px 6px;
  border-top: 1px solid var(--border);
}

.taskbar-warning {
  font-size: 11px;
  color: var(--warn, #c87a20);
}
</style>
