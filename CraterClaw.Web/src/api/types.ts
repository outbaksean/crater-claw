export interface ProviderEndpoint {
  name: string
  baseUrl: string
}

export interface ProviderStatus {
  isReachable: boolean
  errorMessage: string | null
}

export interface ModelItem {
  name: string
  sizeBytes: number
  modifiedAt: string
}

export interface ExecutionRequest {
  modelName: string
  messages: MessageItem[]
  temperature?: number
  maxTokens?: number
}

export interface MessageItem {
  role: 'User' | 'Assistant'
  content: string
}

export interface ExecutionResponse {
  content: string
  modelName: string
  finishReason: string
}

export interface PluginBinding {
  name: string
  tools: string[]
}

export interface BehaviorProfile {
  id: string
  name: string
  description: string
  systemPrompt: string
  preferredProviderName: string | null
  preferredModelName: string | null
  plugins: PluginBinding[]
}

export interface McpServer {
  name: string
  label: string
  enabled: boolean
}

export interface McpAvailability {
  name: string
  isAvailable: boolean
  errorMessage: string | null
}

export interface AgenticRequest {
  modelName: string
  prompt: string
  profileId: string
  maxIterations?: number
}

export interface AgenticResponse {
  content: string
  finishReason: string
  toolsInvoked: string[]
}
