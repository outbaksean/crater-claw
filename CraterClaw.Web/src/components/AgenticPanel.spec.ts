import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import AgenticPanel from './AgenticPanel.vue'
import * as client from '../api/client'
import type { AgenticSseEvent } from '../api/types'

vi.mock('../api/client')

const mockStreamAgentic = vi.mocked(client.streamAgentic)

async function* makeStream(...events: AgenticSseEvent[]) {
  for (const event of events) yield event
}

beforeEach(() => {
  vi.clearAllMocks()
})

function mountPanel() {
  return mount(AgenticPanel, {
    props: {
      providerName: 'local',
      modelName: 'qwen3:8b',
      profileId: 'qbittorrent-manager',
    },
  })
}

describe('AgenticPanel', () => {
  it('submits correct request and displays streamed content', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'chunk', content: 'Here are your torrents.' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: ['ListTorrents'] },
      ),
    )

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('List my torrents')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mockStreamAgentic).toHaveBeenCalledWith(
      'local',
      { modelName: 'qwen3:8b', prompt: 'List my torrents', profileId: 'qbittorrent-manager' },
      expect.any(AbortSignal),
    )
    expect(wrapper.text()).toContain('Here are your torrents.')
    expect(wrapper.text()).toContain('ListTorrents')
    expect(wrapper.text()).toContain('Completed')
  })

  it('omits tools section when toolsInvoked is empty', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'chunk', content: '2 + 2 = 4' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('What is 2 + 2?')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('.tools-line').exists()).toBe(false)
    expect(wrapper.text()).toContain('2 + 2 = 4')
  })

  it('disables textarea and button while loading', async () => {
    let resolveStream!: () => void
    const streamPromise = new Promise<void>((r) => (resolveStream = r))

    mockStreamAgentic.mockReturnValue(
      (async function* () {
        await streamPromise
        yield { type: 'done' as const, finishReason: 'Completed', toolsInvoked: [] }
      })(),
    )

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('test')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('textarea').attributes('disabled')).toBeDefined()
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()

    resolveStream()
  })

  it('displays error on failure', async () => {
    mockStreamAgentic.mockReturnValue(
      (async function* () {
        throw new Error('model error')
        yield { type: 'done' as const, finishReason: 'Completed', toolsInvoked: [] }
      })(),
    )

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('test')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('.error').exists()).toBe(true)
    expect(wrapper.find('.error').text()).toContain('model error')
  })
})
