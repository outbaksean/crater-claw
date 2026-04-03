import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AppTaskbar from './AppTaskbar.vue'
import type { ProviderEndpoint, ProviderStatus, ModelItem, BehaviorProfile } from '../api/types'

const provider: ProviderEndpoint = { name: 'local', baseUrl: 'http://localhost:11434' }
const model: ModelItem = {
  name: 'qwen3:8b',
  sizeBytes: 1_000_000,
  modifiedAt: '2024-01-01T00:00:00Z',
}
const profile: BehaviorProfile = {
  id: 'no-tools',
  name: 'No Tools',
  description: 'Basic chat',
  systemPrompt: '',
  preferredProviderName: null,
  preferredModelName: null,
  plugins: [],
}
const reachableStatus: ProviderStatus = { isReachable: true, errorMessage: null }
const unreachableStatus: ProviderStatus = { isReachable: false, errorMessage: 'refused' }

function mountTaskbar(overrides: Record<string, unknown> = {}) {
  return mount(AppTaskbar, {
    props: {
      providers: [provider],
      selectedProvider: null,
      providerStatus: null,
      loadingProviders: false,
      loadingStatus: false,
      models: [model],
      selectedModel: null,
      loadingModels: false,
      profiles: [profile],
      selectedProfile: null,
      loadingProfiles: false,
      warnings: [],
      ...overrides,
    },
  })
}

async function openSection(wrapper: ReturnType<typeof mount>, section: string) {
  await wrapper.find(`[data-section="${section}"] .selector-trigger`).trigger('click')
}

describe('Taskbar', () => {
  it('renders provider list with selected state', async () => {
    const wrapper = mountTaskbar({ selectedProvider: provider })
    await openSection(wrapper, 'provider')
    const options = wrapper.findAll('[data-section="provider"] .selector-option')
    expect(options).toHaveLength(1)
    expect(options[0].text()).toContain('local')
    expect(options[0].classes()).toContain('selector-option--selected')
  })

  it('renders model list with selected state', async () => {
    const wrapper = mountTaskbar({ selectedProvider: provider, selectedModel: model })
    await openSection(wrapper, 'model')
    const options = wrapper.findAll('[data-section="model"] .selector-option')
    expect(options).toHaveLength(1)
    expect(options[0].text()).toContain('qwen3:8b')
    expect(options[0].classes()).toContain('selector-option--selected')
  })

  it('model section is disabled when no provider is selected', () => {
    const wrapper = mountTaskbar({ selectedProvider: null })
    const trigger = wrapper.find('[data-section="model"] .selector-trigger')
    expect(trigger.attributes('disabled')).toBeDefined()
  })

  it('renders profile list with selected state', async () => {
    const wrapper = mountTaskbar({ selectedProfile: profile })
    await openSection(wrapper, 'profile')
    const options = wrapper.findAll('[data-section="profile"] .selector-option')
    expect(options).toHaveLength(1)
    expect(options[0].text()).toContain('No Tools')
    expect(options[0].classes()).toContain('selector-option--selected')
  })

  it('emits selectProvider when a provider is clicked', async () => {
    const wrapper = mountTaskbar()
    await openSection(wrapper, 'provider')
    await wrapper.find('[data-section="provider"] .selector-option').trigger('click')
    expect(wrapper.emitted('selectProvider')).toBeTruthy()
    expect(wrapper.emitted('selectProvider')![0]).toEqual([provider])
  })

  it('emits selectModel when a model is clicked', async () => {
    const wrapper = mountTaskbar({ selectedProvider: provider })
    await openSection(wrapper, 'model')
    await wrapper.find('[data-section="model"] .selector-option').trigger('click')
    expect(wrapper.emitted('selectModel')).toBeTruthy()
    expect(wrapper.emitted('selectModel')![0]).toEqual([model])
  })

  it('emits selectProfile when a profile is clicked', async () => {
    const wrapper = mountTaskbar()
    await openSection(wrapper, 'profile')
    await wrapper.find('[data-section="profile"] .selector-option').trigger('click')
    expect(wrapper.emitted('selectProfile')).toBeTruthy()
    expect(wrapper.emitted('selectProfile')![0]).toEqual([profile])
  })

  it('displays warnings when the warnings prop is non-empty', () => {
    const wrapper = mountTaskbar({ warnings: ['Provider not found', 'Model not available'] })
    const items = wrapper.findAll('.taskbar-warning')
    expect(items).toHaveLength(2)
    expect(items[0].text()).toContain('Provider not found')
  })

  it('shows reachable pill when selected provider status is reachable', async () => {
    const wrapper = mountTaskbar({ selectedProvider: provider, providerStatus: reachableStatus })
    const pill = wrapper.find('.pill--ok')
    expect(pill.exists()).toBe(true)
  })

  it('shows unreachable pill when selected provider status is not reachable', async () => {
    const wrapper = mountTaskbar({ selectedProvider: provider, providerStatus: unreachableStatus })
    const pill = wrapper.find('.pill--err')
    expect(pill.exists()).toBe(true)
  })
})
