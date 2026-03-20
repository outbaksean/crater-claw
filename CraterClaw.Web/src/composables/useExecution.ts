import { ref } from 'vue'
import { postExecute } from '../api/client'
import type { MessageItem } from '../api/types'

export function useExecution() {
    const messages = ref<MessageItem[]>([])
    const loading = ref(false)
    const error = ref<string | null>(null)

    async function send(providerName: string, modelName: string, content: string) {
        const userMessage: MessageItem = { role: 'User', content }
        messages.value = [...messages.value, userMessage]
        loading.value = true
        error.value = null
        try {
            const response = await postExecute(providerName, {
                modelName,
                messages: messages.value,
            })
            messages.value = [...messages.value, { role: 'Assistant', content: response.content }]
        } catch (e) {
            error.value = e instanceof Error ? e.message : 'Execution failed'
        } finally {
            loading.value = false
        }
    }

    function reset() {
        messages.value = []
        error.value = null
    }

    return { messages, loading, error, send, reset }
}
