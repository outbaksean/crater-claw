import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useExecution } from './useExecution'
import * as client from '../api/client'
import type { ExecutionResponse } from '../api/types'

vi.mock('../api/client')

const mockPostExecute = vi.mocked(client.postExecute)

beforeEach(() => {
  vi.clearAllMocks()
})

describe('useExecution', () => {
  it('appends user message then assistant response on success', async () => {
    const response: ExecutionResponse = {
      content: 'Hello!',
      modelName: 'qwen3:8b',
      finishReason: 'Completed',
    }
    mockPostExecute.mockResolvedValue(response)

    const { messages, send } = useExecution()
    await send('local', 'qwen3:8b', 'Hi')

    expect(messages.value).toHaveLength(2)
    expect(messages.value[0]).toEqual({ role: 'User', content: 'Hi' })
    expect(messages.value[1]).toEqual({ role: 'Assistant', content: 'Hello!' })
  })

  it('passes full conversation history to postExecute', async () => {
    mockPostExecute.mockResolvedValue({
      content: 'Paris',
      modelName: 'qwen3:8b',
      finishReason: 'Completed',
    })

    const { send } = useExecution()
    await send('local', 'qwen3:8b', 'What is the capital of France?')

    mockPostExecute.mockResolvedValue({
      content: '2 million',
      modelName: 'qwen3:8b',
      finishReason: 'Completed',
    })
    await send('local', 'qwen3:8b', 'What is its population?')

    const callArg = mockPostExecute.mock.calls[1][1]
    expect(callArg.messages).toHaveLength(3)
    expect(callArg.messages[0]).toEqual({
      role: 'User',
      content: 'What is the capital of France?',
    })
    expect(callArg.messages[1]).toEqual({ role: 'Assistant', content: 'Paris' })
    expect(callArg.messages[2]).toEqual({ role: 'User', content: 'What is its population?' })
  })

  it('sets error and does not append assistant message on failure', async () => {
    mockPostExecute.mockRejectedValue(new Error('network error'))

    const { messages, error, send } = useExecution()
    await send('local', 'qwen3:8b', 'Hi')

    expect(messages.value).toHaveLength(1)
    expect(messages.value[0].role).toBe('User')
    expect(error.value).toContain('network error')
  })

  it('reset clears messages and error', async () => {
    mockPostExecute.mockResolvedValue({
      content: 'Hi',
      modelName: 'qwen3:8b',
      finishReason: 'Completed',
    })

    const { messages, error, send, reset } = useExecution()
    await send('local', 'qwen3:8b', 'Hello')
    reset()

    expect(messages.value).toHaveLength(0)
    expect(error.value).toBeNull()
  })
})
