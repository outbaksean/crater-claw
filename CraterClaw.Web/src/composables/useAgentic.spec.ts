import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useAgentic } from './useAgentic'
import * as client from '../api/client'
import type { AgenticSseEvent, AgenticSseThinking } from '../api/types'

vi.mock('../api/client')

const mockStreamAgentic = vi.mocked(client.streamAgentic)

async function* makeStream(...events: (AgenticSseEvent | AgenticSseThinking)[]) {
  for (const event of events) yield event as AgenticSseEvent
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('useAgentic', () => {
  it('accumulates chunks into content', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'chunk', content: 'Hello ' },
        { type: 'chunk', content: 'world' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    const { content, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'hi', profileId: 'p1' })

    expect(content.value).toBe('Hello world')
  })

  it('sets finishReason and toolsInvoked from done event', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream({ type: 'done', finishReason: 'Completed', toolsInvoked: ['ListTorrents'] }),
    )

    const { finishReason, toolsInvoked, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'hi', profileId: 'p1' })

    expect(finishReason.value).toBe('Completed')
    expect(toolsInvoked.value).toEqual(['ListTorrents'])
  })

  it('loading is true during run and false after', async () => {
    let resolveStream!: () => void
    const streamPromise = new Promise<void>((r) => (resolveStream = r))

    mockStreamAgentic.mockReturnValue(
      (async function* () {
        await streamPromise
        yield { type: 'done' as const, finishReason: 'Completed', toolsInvoked: [] }
      })(),
    )

    const { loading, run } = useAgentic()
    const runPromise = run('local', { modelName: 'test', prompt: 'hi', profileId: 'p1' })

    expect(loading.value).toBe(true)
    resolveStream()
    await runPromise
    expect(loading.value).toBe(false)
  })

  it('sets error on stream failure', async () => {
    mockStreamAgentic.mockReturnValue(
      (async function* () {
        throw new Error('network error')
        yield { type: 'done' as const, finishReason: 'Completed', toolsInvoked: [] }
      })(),
    )

    const { error, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'hi', profileId: 'p1' })

    expect(error.value).toBe('network error')
  })

  it('accumulates thinking chunks into thinking ref', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'thinking', content: 'Let me think...' },
        { type: 'thinking', content: ' okay.' },
        { type: 'chunk', content: 'Hello' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    const { thinking, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'hi', profileId: 'p1' })

    expect(thinking.value).toBe('Let me think... okay.')
  })

  it('thinking is reset on each run', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'thinking', content: 'first thought' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    const { thinking, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'first', profileId: 'p1' })
    expect(thinking.value).toBe('first thought')

    mockStreamAgentic.mockReturnValue(
      makeStream({ type: 'done', finishReason: 'Completed', toolsInvoked: [] }),
    )

    await run('local', { modelName: 'test', prompt: 'second', profileId: 'p1' })
    expect(thinking.value).toBe('')
  })

  it('clears previous result on new run', async () => {
    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'chunk', content: 'first' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    const { content, run } = useAgentic()
    await run('local', { modelName: 'test', prompt: 'first', profileId: 'p1' })
    expect(content.value).toBe('first')

    mockStreamAgentic.mockReturnValue(
      makeStream(
        { type: 'chunk', content: 'second' },
        { type: 'done', finishReason: 'Completed', toolsInvoked: [] },
      ),
    )

    await run('local', { modelName: 'test', prompt: 'second', profileId: 'p1' })
    expect(content.value).toBe('second')
  })
})
