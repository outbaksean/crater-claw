# Spec: web-agentic-streaming

## Goal

Add streaming support to the agentic execution path. The API exposes a new SSE endpoint. The Vue frontend consumes it via a `useAgentic` composable, displaying content as it arrives rather than waiting for the full response.

The existing `POST /api/providers/{name}/agentic` endpoint is unchanged.

## Scope

`CraterClaw.Core`, `CraterClaw.Console`, `CraterClaw.Api`, `CraterClaw.Api.Tests`, `CraterClaw.Web`.

---

## Phase 1: Core async StreamChunk and SSE endpoint

**Status: Done**

### Context

`AgenticRequest.StreamChunk` is currently `Action<string>?`. Writing to an HTTP response requires async I/O, so it must become `Func<string, Task>?`. The console harness and the agentic service both need updating.

The new endpoint uses Server-Sent Events (SSE): the response is `text/event-stream` and each event is a JSON line prefixed with `data: ` and terminated with `\n\n`.

### SSE Protocol

```
data: {"type":"chunk","content":"Hello "}

data: {"type":"chunk","content":"world"}

data: {"type":"done","finishReason":"Completed","toolsInvoked":["ListTorrents"]}

```

### Contract

**`CraterClaw.Core/AgenticRequest.cs`**

- `StreamChunk` changes from `Action<string>?` to `Func<string, Task>?`

**`CraterClaw.Core/SemanticKernelAgenticExecutionService.cs`**

- `request.StreamChunk(chunk.Content)` → `await request.StreamChunk(chunk.Content)`

**`CraterClaw.Console/Program.cs`**

- `StreamChunk: Console.Write` → `StreamChunk: chunk => { Console.Write(chunk); return Task.CompletedTask; }`

**`CraterClaw.Api/Models/ApiModels.cs`** — two new SSE event records:

- `AgenticSseChunk(string Type, string Content)`
- `AgenticSseDone(string Type, AgenticFinishReason FinishReason, IReadOnlyList<string> ToolsInvoked)`

**`CraterClaw.Api/Endpoints/ProvidersEndpoints.cs`** — new endpoint:

- `POST /api/providers/{name}/agentic/stream`
- Returns `text/event-stream` with `Cache-Control: no-cache`
- On unknown provider: 404. On unknown profile: 400.
- Streams `chunk` events from `StreamChunk` callback; writes `done` event after `ExecuteAsync` returns.
- Uses `JsonSerializerOptions` with `PropertyNamingPolicy = CamelCase` and `JsonStringEnumConverter`.

**`CraterClaw.Api.Tests/FakeAgenticExecutionService.cs`**

- Add optional `IReadOnlyList<string>? chunks` constructor parameter.
- If `chunks` is not null and `request.StreamChunk` is not null, call `await request.StreamChunk(chunk)` for each before returning.
- Make `ExecuteAsync` `async Task<AgenticResponse>`.

### Tests

New file `CraterClaw.Api.Tests/AgenticStreamEndpointTests.cs`:

- `PostAgenticStream_StreamsChunksAndDoneEvent` — fake service streams two chunks; assert response is `text/event-stream`, parse SSE lines, verify chunk events and done event.
- `PostAgenticStream_UnknownProvider_Returns404`
- `PostAgenticStream_UnknownProfile_Returns400`

All existing `CraterClaw.Api.Tests` tests must continue to pass.

### Implement

1. Update `AgenticRequest.cs`.
2. Update `SemanticKernelAgenticExecutionService.cs`.
3. Update `Console/Program.cs`.
4. Add SSE event records to `ApiModels.cs`.
5. Add streaming endpoint to `ProvidersEndpoints.cs`.
6. Update `FakeAgenticExecutionService.cs`.
7. Add `AgenticStreamEndpointTests.cs`.
8. Run tests: `dotnet test CraterClaw.slnx`

### README Sync

No user-visible changes to document yet.

### Current Architecture Sync

Update `current-architecture.md` — add the streaming endpoint and note `StreamChunk` type change.

---

## Phase 2: Frontend streaming

**Status: Done**

### Context

`AgenticPanel.vue` currently calls `postAgentic` and waits for the full response. This phase replaces that with a `useAgentic` composable backed by `streamAgentic`, displaying content as chunks arrive.

### Contract

**`CraterClaw.Web/src/api/types.ts`** — add:

```typescript
export interface AgenticSseChunk {
    type: "chunk";
    content: string;
}
export interface AgenticSseDone {
    type: "done";
    finishReason: string;
    toolsInvoked: string[];
}
export type AgenticSseEvent = AgenticSseChunk | AgenticSseDone;
```

**`CraterClaw.Web/src/api/client.ts`** — add:

```typescript
export async function* streamAgentic(
  providerName: string,
  request: AgenticRequest,
  signal?: AbortSignal,
): AsyncGenerator<AgenticSseEvent>
```

Reads SSE response body as a stream; splits on `\n\n`; yields parsed `data:` lines.

**`CraterClaw.Web/src/composables/useAgentic.ts`** — new composable:

- Refs: `content`, `finishReason`, `toolsInvoked`, `loading`, `error`
- `run(providerName, request)` — calls `streamAgentic`, accumulates chunks into `content`, populates `finishReason`/`toolsInvoked` from done event.
- `cancel()` — aborts the in-flight request.

**`CraterClaw.Web/src/components/AgenticPanel.vue`** — updated:

- Replace inline state with `useAgentic()`.
- Display `agentic.content` progressively as chunks stream in.
- Show finish reason and tools after the done event.

### Tests

**`CraterClaw.Web/src/composables/useAgentic.spec.ts`** (new):

- `accumulates chunks into content`
- `sets finishReason and toolsInvoked from done event`
- `sets error on stream failure`
- `loading is true during run and false after`

**`CraterClaw.Web/src/components/AgenticPanel.spec.ts`** (updated):

- Replace `postAgentic` mock with `streamAgentic` async generator mock.
- `submits correct request and displays streamed content`
- `disables inputs while loading`
- `displays error on failure`
- `omits tools section when toolsInvoked is empty`

### Implement

1. Add SSE types to `types.ts`.
2. Add `streamAgentic` to `client.ts`.
3. Create `useAgentic.ts` and `useAgentic.spec.ts`.
4. Update `AgenticPanel.vue` and `AgenticPanel.spec.ts`.
5. Run: `npm run lint && npm run test:unit` in `CraterClaw.Web`.

### README Sync

No changes — streaming is an internal improvement, the user-facing workflow is the same.

### Current Architecture Sync

Update `current-architecture.md` — document `streamAgentic`, `useAgentic`, and SSE protocol.
