# Vue Frontend Spec

## Name
- CraterClaw Web (Vue Frontend)

## Purpose
- Provide a browser UI that mirrors the console harness flows by consuming the CraterClaw API. No business logic lives in the frontend; it is a thin client over the API.

## Scope
- Add `CraterClaw.Web` as a Vue 3 + TypeScript project (Vite scaffold) in the solution root.
- Consume all existing `/api` endpoints.
- Mirror the console harness flow as a set of views/components.
- Unit test components and composables with Vitest.
- No authentication, no user accounts. Development CORS is already permissive on the API side.

## Views and User Flows

### Provider Selection
- List all configured providers from `GET /api/providers`.
- Allow the user to select one (numbered list or clickable items).
- Check and display status for the selected provider via `GET /api/providers/{name}/status`.

### Model Selection
- After a provider is selected and reachable, list its models from `GET /api/providers/{name}/models`.
- Allow the user to select a model.

### Interactive Execution
- After provider and model are selected, provide a chat-like input area.
- Send single or multi-turn messages via `POST /api/providers/{name}/execute`.
- Display the response.

### Behavior Profiles
- List profiles from `GET /api/profiles`.
- Allow the user to select a profile.

### Agentic Execution
- After provider, model, and profile are selected, provide a prompt input.
- Submit via `POST /api/providers/{name}/agentic`.
- Display the response content, finish reason, and tools invoked.

### MCP Servers
- List configured servers from `GET /api/mcp`.
- Allow the user to check availability of one via `POST /api/mcp/{name}/availability`.
- Display the result.

## Tech Stack
- Vue 3 with Composition API and `<script setup>`
- TypeScript (strict)
- Vite
- Vitest for unit tests
- No UI component library (plain CSS or scoped component styles)

## Project Layout
```
CraterClaw.Web/
  src/
    api/         -- typed fetch wrappers for each API resource
    components/  -- reusable UI components
    views/       -- page-level components
    App.vue
    main.ts
  index.html
  vite.config.ts
  vitest.config.ts
  tsconfig.json
  package.json
```

## Contract Notes

All types mirror the API response shapes from the web-api spec. Define them in `src/api/types.ts`:

```ts
export interface ProviderEndpoint { name: string; baseUrl: string }
export interface ProviderStatus { isReachable: boolean; errorMessage: string | null }
export interface ModelItem { name: string; sizeBytes: number; modifiedAt: string }
export interface ExecutionRequest { modelName: string; messages: MessageItem[]; temperature?: number; maxTokens?: number }
export interface MessageItem { role: 'User' | 'Assistant'; content: string }
export interface ExecutionResponse { content: string; modelName: string; finishReason: string }
export interface BehaviorProfile { id: string; name: string; description: string; recommendedModelTags: string[]; allowedMcpServerNames: string[] }
export interface McpServer { name: string; label: string; enabled: boolean }
export interface McpAvailability { name: string; isAvailable: boolean; errorMessage: string | null }
export interface AgenticRequest { modelName: string; prompt: string; profileId: string; maxIterations?: number }
export interface AgenticResponse { content: string; finishReason: string; toolsInvoked: string[] }
```

API base URL is read from `import.meta.env.VITE_API_BASE_URL` (defaults to `http://localhost:5000`).

---

## Phase 1: Scaffold + provider and model selection
**Status: Done**

### Scope
- Scaffold `CraterClaw.Web` using `npm create vue@latest` (Vue 3, TypeScript, Vitest, no router, no Pinia).
- Add the project to `CraterClaw.slnx` as a non-build folder reference (or document it alongside the solution).
- Create `src/api/types.ts` with all shared types.
- Create `src/api/client.ts` with typed fetch functions:
  - `getProviders(): Promise<ProviderEndpoint[]>`
  - `getProviderStatus(name: string): Promise<ProviderStatus>`
  - `getModels(providerName: string): Promise<ModelItem[]>`
- Create a `useProviders` composable that fetches provider list and manages selected provider and its status.
- Create a `useModels` composable that fetches models for the selected provider.
- Implement `App.vue` with:
  - Provider list (numbered, selectable).
  - Status indicator for selected provider (reachable / unreachable / loading).
  - Model list (appears when provider is reachable).
  - Selected model display.
- Tests (Vitest):
  - `useProviders` composable: fetches providers, sets selected, fetches status.
  - `useModels` composable: fetches models when provider is set.
  - API client functions: called with correct URL and method (mock `fetch`).

### Manual Verification
- Prerequisites: `CraterClaw.Api` running at `http://localhost:5000` with at least one provider configured.
- Run `npm run dev` and confirm the provider list loads.
- Select a provider and confirm the status is displayed.
- Confirm the model list loads when the provider is reachable.

---

## Phase 2: Interactive execution
**Status: Done**

### Scope
- Add `postExecute(providerName: string, request: ExecutionRequest): Promise<ExecutionResponse>` to `client.ts`.
- Create a `useExecution` composable managing the conversation message list and submission state.
- Implement an `InteractiveChat` component:
  - Displays conversation history (user and assistant turns).
  - Input field and submit button.
  - Disables input while a request is in flight.
  - Appends the assistant response to the conversation on success.
  - Displays an error message on failure.
- Wire into `App.vue`: appears after a model is selected.
- Tests:
  - `useExecution`: appends user message, calls API, appends assistant response.
  - `InteractiveChat`: renders message history, submits on button click, disables during loading.

### Manual Verification
- Prerequisites: Ollama running with at least one model downloaded; `CraterClaw.Api` running.
- Send a message and confirm the assistant response is displayed.
- Send a follow-up and confirm multi-turn history renders correctly.

---

## Phase 3: Behavior profiles and agentic execution
**Status: Planned**

### Scope
- Add `getProfiles(): Promise<BehaviorProfile[]>` and `postAgentic(providerName: string, request: AgenticRequest): Promise<AgenticResponse>` to `client.ts`.
- Create a `useProfiles` composable that fetches and manages selected profile.
- Implement a `ProfileSelector` component: numbered list of profiles with name and description.
- Implement an `AgenticPanel` component:
  - Prompt input and submit button.
  - Displays response content, finish reason, and tools invoked list.
  - Disables input while request is in flight.
- Wire into `App.vue`: profile selector and agentic panel appear after a model is selected.
- Tests:
  - `useProfiles`: fetches profiles, sets selected.
  - `AgenticPanel`: submits correct request, displays tools invoked.

### Manual Verification
- Prerequisites: Ollama running, `CraterClaw.Api` running, at least one profile configured.
- Select a profile and submit a prompt.
- Confirm response content and tools invoked list are displayed.

---

## Phase 4: MCP server list and availability check
**Status: Planned**

### Scope
- Add `getMcpServers(): Promise<McpServer[]>` and `postMcpAvailability(name: string): Promise<McpAvailability>` to `client.ts`.
- Create a `useMcp` composable managing server list and availability check state.
- Implement an `McpPanel` component:
  - Lists configured MCP servers (name, label, enabled flag).
  - Button per server to check availability.
  - Displays result inline (available / unavailable / error message).
- Wire into `App.vue` (can be a collapsible or separate section).
- Tests:
  - `useMcp`: fetches servers, triggers availability check, stores result.
  - `McpPanel`: renders server list, calls availability on button click, displays result.

### Manual Verification
- Prerequisites: `CraterClaw.Api` running, at least one MCP server configured in `craterclaw.json`.
- Confirm server list loads.
- Click availability check and confirm result is displayed.
