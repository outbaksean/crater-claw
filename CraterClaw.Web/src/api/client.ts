import type {
  ProviderEndpoint,
  ProviderStatus,
  ModelItem,
  ExecutionRequest,
  ExecutionResponse,
  BehaviorProfile,
  McpServer,
  McpAvailability,
  AgenticRequest,
  AgenticResponse,
} from './types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`)
  if (!res.ok) throw new Error(`GET ${path} failed: ${res.status}`)
  return res.json() as Promise<T>
}

async function post<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${baseUrl}${path}`, {
    method: 'POST',
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) throw new Error(`POST ${path} failed: ${res.status}`)
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

export function postExecute(
  providerName: string,
  request: ExecutionRequest,
): Promise<ExecutionResponse> {
  return post(`/api/providers/${encodeURIComponent(providerName)}/execute`, request)
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
