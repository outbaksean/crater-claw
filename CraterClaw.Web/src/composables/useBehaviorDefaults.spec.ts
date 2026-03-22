import { describe, it, expect, vi } from 'vitest'
import { ref } from 'vue'
import { useBehaviorDefaults } from './useBehaviorDefaults'
import type { ProviderEndpoint, ModelItem, BehaviorProfile } from '../api/types'

const providers: ProviderEndpoint[] = [
  { name: 'local', baseUrl: 'http://localhost:11434' },
  { name: 'remote', baseUrl: 'http://remote:11434' },
]

const models: ModelItem[] = [
  { name: 'llama3.2', sizeBytes: 1000, modifiedAt: '' },
  { name: 'mistral', sizeBytes: 2000, modifiedAt: '' },
]

function makeProfile(overrides: Partial<BehaviorProfile> = {}): BehaviorProfile {
  return {
    id: 'test',
    name: 'Test',
    description: '',
    systemPrompt: '',
    preferredProviderName: null,
    preferredModelName: null,
    plugins: [],
    ...overrides,
  }
}

describe('useBehaviorDefaults', () => {
  it('calls selectProvider when preferred provider matches', () => {
    const selectProvider = vi.fn()
    const selectModel = vi.fn()
    const { applyProfileDefaults } = useBehaviorDefaults(
      ref(providers),
      ref(models),
      selectProvider,
      selectModel,
    )

    applyProfileDefaults(makeProfile({ preferredProviderName: 'local' }))

    expect(selectProvider).toHaveBeenCalledWith(providers[0])
    expect(selectModel).not.toHaveBeenCalled()
  })

  it('stores warning and does not call selectProvider when preferred provider is not configured', () => {
    const selectProvider = vi.fn()
    const selectModel = vi.fn()
    const { applyProfileDefaults, behaviorWarnings } = useBehaviorDefaults(
      ref(providers),
      ref(models),
      selectProvider,
      selectModel,
    )

    applyProfileDefaults(makeProfile({ preferredProviderName: 'unknown-provider' }))

    expect(selectProvider).not.toHaveBeenCalled()
    expect(behaviorWarnings.value).toHaveLength(1)
    expect(behaviorWarnings.value[0]).toContain('unknown-provider')
  })

  it('calls selectModel when preferred model matches', () => {
    const selectProvider = vi.fn()
    const selectModel = vi.fn()
    const { applyProfileDefaults } = useBehaviorDefaults(
      ref(providers),
      ref(models),
      selectProvider,
      selectModel,
    )

    applyProfileDefaults(makeProfile({ preferredModelName: 'llama3.2' }))

    expect(selectModel).toHaveBeenCalledWith(models[0])
    expect(selectProvider).not.toHaveBeenCalled()
  })

  it('stores warning and does not call selectModel when preferred model is not available', () => {
    const selectProvider = vi.fn()
    const selectModel = vi.fn()
    const { applyProfileDefaults, behaviorWarnings } = useBehaviorDefaults(
      ref(providers),
      ref(models),
      selectProvider,
      selectModel,
    )

    applyProfileDefaults(makeProfile({ preferredModelName: 'nonexistent-model' }))

    expect(selectModel).not.toHaveBeenCalled()
    expect(behaviorWarnings.value).toHaveLength(1)
    expect(behaviorWarnings.value[0]).toContain('nonexistent-model')
  })

  it('clears warnings from previous profile when new profile is applied', () => {
    const selectProvider = vi.fn()
    const selectModel = vi.fn()
    const { applyProfileDefaults, behaviorWarnings } = useBehaviorDefaults(
      ref(providers),
      ref(models),
      selectProvider,
      selectModel,
    )

    applyProfileDefaults(makeProfile({ preferredProviderName: 'unknown-provider' }))
    expect(behaviorWarnings.value).toHaveLength(1)

    applyProfileDefaults(makeProfile())
    expect(behaviorWarnings.value).toHaveLength(0)
  })
})
