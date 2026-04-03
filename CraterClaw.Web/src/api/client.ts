import type {
  ProviderEndpoint,
  ProviderStatus,
  ModelItem,
  BehaviorProfile,
  McpServer,
  McpAvailability,
  AgenticRequest,
  AgenticResponse,
  AgenticSseEvent,
} from './types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`)
  if (!res.ok) {
    const detail = await res.text().catch(() => '')
    throw new Error(`GET ${path} failed: ${res.status}${detail ? ` — ${detail}` : ''}`)
  }
  return res.json() as Promise<T>
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    method: 'POST',
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) {
    const detail = await res.text().catch(() => '')
    throw new Error(`POST ${path} failed: ${res.status}${detail ? ` — ${detail}` : ''}`)
  }
  return res.json() as Promise<T>
}

export function getProviders(): Promise<ProviderEndpoint[]> {
  return get('/api/providers')
}

export function getProviderStatus(name: string): Promise<ProviderStatus> {
  return get(`/api/providers/${encodeURIComponent(name)}/status`)
}

export function getModels(providerName: string): Promise<ModelItem[]> {
  return get(`/api/providers/${encodeURIComponent(providerName)}/models`)
}

export function getProfiles(): Promise<BehaviorProfile[]> {
  return get('/api/profiles')
}

export function getMcpServers(): Promise<McpServer[]> {
  return get('/api/mcp')
}

export function postMcpAvailability(name: string): Promise<McpAvailability> {
  return post(`/api/mcp/${encodeURIComponent(name)}/availability`)
}

export function postAgentic(
  providerName: string,
  request: AgenticRequest,
): Promise<AgenticResponse> {
  return post(`/api/providers/${encodeURIComponent(providerName)}/agentic`, request)
}

export async function* streamAgentic(
  providerName: string,
  request: AgenticRequest,
  signal?: AbortSignal,
): AsyncGenerator<AgenticSseEvent> {
  const res = await fetch(
    `${baseUrl}/api/providers/${encodeURIComponent(providerName)}/agentic/stream`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
      signal,
    },
  )
  if (!res.ok) {
    const detail = await res.text().catch(() => '')
    throw new Error(`POST agentic/stream failed: ${res.status}${detail ? ` — ${detail}` : ''}`)
  }

  const reader = res.body!.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  try {
    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })
      const parts = buffer.split('\n\n')
      buffer = parts.pop()!
      for (const part of parts) {
        const line = part.trim()
        if (line.startsWith('data: ')) {
          yield JSON.parse(line.slice(6)) as AgenticSseEvent
        }
      }
    }
  } finally {
    reader.releaseLock()
  }
}
