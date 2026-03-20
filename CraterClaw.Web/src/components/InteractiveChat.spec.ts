import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import InteractiveChat from './InteractiveChat.vue'
import * as client from '../api/client'

vi.mock('../api/client')

const mockPostExecute = vi.mocked(client.postExecute)

beforeEach(() => {
    vi.clearAllMocks()
})

function mountChat() {
    return mount(InteractiveChat, {
        props: { providerName: 'local', modelName: 'qwen3:8b' },
    })
}

describe('InteractiveChat', () => {
    it('renders empty state initially', () => {
        const wrapper = mountChat()
        expect(wrapper.text()).toContain('No messages yet.')
    })

    it('submits message on button click and displays response', async () => {
        mockPostExecute.mockResolvedValue({
            content: 'Hello!',
            modelName: 'qwen3:8b',
            finishReason: 'Completed',
        })

        const wrapper = mountChat()
        await wrapper.find('input').setValue('Hi')
        await wrapper.find('form').trigger('submit')
        await flushPromises()

        const messages = wrapper.findAll('.message')
        expect(messages[0].text()).toContain('Hi')
        expect(messages[1].text()).toContain('Hello!')
    })

    it('disables input and button while loading', async () => {
        let resolve: (v: unknown) => void
        mockPostExecute.mockReturnValue(new Promise((r) => (resolve = r)))

        const wrapper = mountChat()
        await wrapper.find('input').setValue('Hi')
        await wrapper.find('form').trigger('submit')
        await nextTick()

        expect(wrapper.find('input').attributes('disabled')).toBeDefined()
        expect(wrapper.find('button').attributes('disabled')).toBeDefined()

        resolve!({
            content: 'done',
            modelName: 'qwen3:8b',
            finishReason: 'Completed',
        })
    })

    it('clears the input after submitting', async () => {
        mockPostExecute.mockResolvedValue({
            content: 'Hi back',
            modelName: 'qwen3:8b',
            finishReason: 'Completed',
        })

        const wrapper = mountChat()
        await wrapper.find('input').setValue('Hello')
        await wrapper.find('form').trigger('submit')
        await flushPromises()
        expect(wrapper.find('input').element.value).toBe('')
    })

    it('displays error message on failure', async () => {
        mockPostExecute.mockRejectedValue(new Error('timeout'))

        const wrapper = mountChat()
        await wrapper.find('input').setValue('Hi')
        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(wrapper.find('.error').exists()).toBe(true)
        expect(wrapper.find('.error').text()).toContain('timeout')
    })
})
