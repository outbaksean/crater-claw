import { ref } from 'vue'
import { streamAgentic } from '../api/client'
import type { AgenticRequest } from '../api/types'

export function useAgentic() {
  const content = ref('')
  const thinking = ref('')
  const showThinking = ref(false)
  const finishReason = ref<string | null>(null)
  const toolsInvoked = ref<string[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  let abortController: AbortController | null = null

  async function run(providerName: string, request: AgenticRequest) {
    if (loading.value) return
    loading.value = true
    error.value = null
    content.value = ''
    thinking.value = ''
    finishReason.value = null
    toolsInvoked.value = []
    abortController = new AbortController()

    const fullRequest: AgenticRequest = showThinking.value
      ? { ...request, showThinking: true }
      : request

    try {
      for await (const event of streamAgentic(providerName, fullRequest, abortController.signal)) {
        if (event.type === 'chunk') {
          content.value += event.content
        } else if (event.type === 'thinking') {
          thinking.value += event.content
        } else if (event.type === 'done') {
          finishReason.value = event.finishReason
          toolsInvoked.value = event.toolsInvoked
        }
      }
    } catch (e) {
      if ((e as Error).name !== 'AbortError') {
        error.value = (e as Error).message
      }
    } finally {
      loading.value = false
      abortController = null
    }
  }

  function cancel() {
    abortController?.abort()
  }

  return {
    content,
    thinking,
    showThinking,
    finishReason,
    toolsInvoked,
    loading,
    error,
    run,
    cancel,
  }
}
