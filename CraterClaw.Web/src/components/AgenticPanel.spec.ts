import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import AgenticPanel from './AgenticPanel.vue'
import * as client from '../api/client'
import type { AgenticResponse } from '../api/types'

vi.mock('../api/client')

const mockPostAgentic = vi.mocked(client.postAgentic)

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
  it('submits correct request and displays tools invoked', async () => {
    const response: AgenticResponse = {
      content: 'Here are your torrents.',
      finishReason: 'Completed',
      toolsInvoked: ['ListTorrents'],
    }
    mockPostAgentic.mockResolvedValue(response)

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('List my torrents')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mockPostAgentic).toHaveBeenCalledWith('local', {
      modelName: 'qwen3:8b',
      prompt: 'List my torrents',
      profileId: 'qbittorrent-manager',
    })
    expect(wrapper.text()).toContain('ListTorrents')
    expect(wrapper.text()).toContain('Here are your torrents.')
    expect(wrapper.text()).toContain('Completed')
  })

  it('omits tools section when toolsInvoked is empty', async () => {
    const response: AgenticResponse = {
      content: '2 + 2 = 4',
      finishReason: 'Completed',
      toolsInvoked: [],
    }
    mockPostAgentic.mockResolvedValue(response)

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('What is 2 + 2?')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('.tools-line').exists()).toBe(false)
    expect(wrapper.text()).toContain('2 + 2 = 4')
  })

  it('disables textarea and button while loading', async () => {
    let resolve: (v: AgenticResponse) => void
    mockPostAgentic.mockReturnValue(new Promise((r) => (resolve = r)))

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('test')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('textarea').attributes('disabled')).toBeDefined()
    expect(wrapper.find('button').attributes('disabled')).toBeDefined()

    resolve!({ content: 'done', finishReason: 'Completed', toolsInvoked: [] })
  })

  it('displays error on failure', async () => {
    mockPostAgentic.mockRejectedValue(new Error('model error'))

    const wrapper = mountPanel()
    await wrapper.find('textarea').setValue('test')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('.error').exists()).toBe(true)
    expect(wrapper.find('.error').text()).toContain('model error')
  })
})
