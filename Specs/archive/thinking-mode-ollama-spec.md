# Spec: thinking-mode-ollama

## Goal

Surface thinking tokens emitted by thinking-capable Ollama models (e.g., qwen3) in the console harness and Vue frontend. Thinking content is displayed distinctly from the main response and can be shown or hidden by the user.

## Background

Thinking-capable models (e.g., qwen3) generate thinking tokens automatically. The raw Ollama API returns these as stream chunks where `message.thinking` is populated and `message.content` is empty. The current streaming loop in `SemanticKernelAgenticExecutionService` only reads `chunk.Content`, so thinking tokens are silently dropped.

The SK Ollama connector (1.73.0-alpha) does not expose a `Think` control property on `OllamaPromptExecutionSettings`. The `ExtensionData` dictionary is not forwarded to the underlying OllamaSharp `ChatRequest`, so setting `AdditionalProperties["think"]` has no effect. The checkpoint description assumed this API existed; it does not. This spec surfaces thinking that is already present in the stream. API-level thinking control (enabling/disabling at the Ollama request level) is deferred to a future checkpoint.

Thinking content is accessible from each streaming chunk via:

```csharp
if (chunk.InnerContent is OllamaSharp.Models.Chat.ChatResponseStream stream)
    var thinking = stream.Message?.Thinking; // null when not a thinking token
```

`OllamaSharp` is already a transitive dependency of `Microsoft.SemanticKernel.Connectors.Ollama`. A direct package reference must be added to `CraterClaw.Core.csproj` to use its types without relying on transitive resolution.

## Scope

`CraterClaw.Core`, `CraterClaw.Console`, `CraterClaw.Core.Tests`, `CraterClaw.Api`, `CraterClaw.Api.Tests`, `CraterClaw.Web`.

---

## Phase 1: Core - surface thinking tokens

**Status: Done**

### Context

Add `StreamThinkingChunk` to `AgenticRequest` (parallel to `StreamChunk`). In the streaming loop, detect thinking tokens from `chunk.InnerContent` and invoke the callback. Switch from `PromptExecutionSettings` to `OllamaPromptExecutionSettings` for Ollama-specific type access.

### External API Verification

Before implementing, verify in `CraterClaw.Core`:

- `OllamaSharp.Models.Chat.ChatResponseStream` is accessible after adding the direct package reference.
- `chunk.InnerContent is OllamaSharp.Models.Chat.ChatResponseStream` succeeds at runtime during a streaming call to a thinking model.
- `stream.Message?.Thinking` is non-null/non-empty during thinking chunks and null/empty otherwise.
- `chunk.Content` is `null` or empty string `""` during thinking chunks (not `null`; confirm exact value to avoid calling `StreamChunk` with empty strings).

### Contract

**`CraterClaw.Core/CraterClaw.Core.csproj`**

- Add `<PackageReference Include="OllamaSharp" Version="5.2.2" />`.

**`CraterClaw.Core/AgenticRequest.cs`**

- Add `Func<string, Task>? StreamThinkingChunk = null` parameter after `StreamChunk`.

**`CraterClaw.Core/SemanticKernelAgenticExecutionService.cs`**

- Replace `PromptExecutionSettings` with `OllamaPromptExecutionSettings`.
- In the streaming branch, after the `foreach` chunk loop, add thinking detection:
    - Cast `chunk.InnerContent` to `OllamaSharp.Models.Chat.ChatResponseStream`.
    - If `stream?.Message?.Thinking` is non-null and non-empty, call `await request.StreamThinkingChunk(thinking)`.
    - Only call `StreamChunk` when `chunk.Content` is non-null and non-empty (guarding against empty string during thinking phase).

The `contentBuilder` accumulates only actual content (not thinking tokens). The final `content` extraction from `chatHistory` is unchanged.

### Tests

All existing tests must continue to pass. No new thinking-specific tests in this phase (thinking token detection requires casting `InnerContent` to an OllamaSharp type; testing this would require a real Ollama instance per the testing rules).

### Implement

1. Add `OllamaSharp` direct package reference.
2. Update `AgenticRequest.cs`.
3. Update `SemanticKernelAgenticExecutionService.cs` — thinking detection and `OllamaPromptExecutionSettings`.
4. Run: `dotnet build CraterClaw.slnx && dotnet test CraterClaw.slnx`.

### README Sync

No user-visible changes.

### Current Architecture Sync

Update `current-architecture.md`:

- `AgenticRequest` has `StreamThinkingChunk: Func<string, Task>?`.
- `SemanticKernelAgenticExecutionService` uses `OllamaPromptExecutionSettings`.
- Note OllamaSharp direct package reference and the `InnerContent` cast pattern.

### Manual Verification Plan

Dependencies: Ollama running with qwen3:8b (or another thinking-capable model).

1. Run the console harness.
2. Select endpoint and qwen3 model.
3. Enter a prompt, observe the response — confirm no regression in basic execution.
4. Proceed to agentic phase, send a task prompt.
5. Add a temporary `Console.Write` in the `StreamThinkingChunk` path (or enable via Phase 2 steps early) and confirm thinking tokens appear.

---

## Phase 2: Console - show thinking with toggle

**Status: Done**

### Context

Before the agentic task prompt, ask the user whether to show thinking tokens. If yes, pass a `StreamThinkingChunk` callback that writes thinking in a visually distinct style. If no, pass `null`.

### Contract

**`CraterClaw.Console/Program.cs`**

After the task prompt input and before `AgenticRequest` construction, insert:

```
Console.Write("Show thinking? [y/N]: ");
var showThinkingInput = Console.ReadLine();
var showThinking = string.Equals(showThinkingInput?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
```

Pass to `AgenticRequest`:

```csharp
StreamThinkingChunk: showThinking
    ? chunk =>
      {
          Console.ForegroundColor = ConsoleColor.DarkGray;
          Console.Write(chunk);
          Console.ResetColor();
          return Task.CompletedTask;
      }
    : null,
```

### Tests

No automated tests — console I/O is not unit tested.

### Implement

1. Update `Console/Program.cs`.
2. Run: `dotnet build CraterClaw.slnx`.

### README Sync

Update the Console Flow section in `README.md` to note the "Show thinking?" prompt before the task prompt.

### Current Architecture Sync

Update `current-architecture.md` Console Harness Flow — add step: "Prompt 'Show thinking? [y/N]'; if yes, thinking tokens are written to console in dark gray."

### Manual Verification Plan

Dependencies: Phase 1 complete. Ollama running with qwen3:8b.

1. Run the console harness.
2. Select endpoint, qwen3 model, and a profile.
3. At "Show thinking? [y/N]:", type `y`.
4. Enter a task prompt. Confirm thinking tokens appear in dark gray before the main response.
5. Repeat with `N` (or blank). Confirm no thinking tokens appear, main response is unchanged.

---

## Phase 3: API and web - thinking in SSE stream

**Status: Done**

### Context

Extend the SSE streaming endpoint to emit `thinking` events alongside `chunk` events. The Vue frontend collects thinking tokens and displays them in a collapsible section above the response.

### SSE Protocol (extended)

```
data: {"type":"thinking","content":"Okay, the user sent..."}

data: {"type":"chunk","content":"Hello! "}

data: {"type":"done","finishReason":"Completed","toolsInvoked":[]}

```

The `thinking` events arrive before (or interleaved with) `chunk` events, depending on the model. Consumers must handle either order.

### Contract

**`CraterClaw.Api/Models/ApiModels.cs`**

- Add `record AgenticSseThinking(string Type, string Content)`.
- Add `bool ShowThinking = false` to `AgenticStreamApiRequest` (a new record type identical to `AgenticApiRequest` plus `ShowThinking`). Alternatively, add `bool? ShowThinking` to the existing `AgenticApiRequest` record used by the stream endpoint.

For clarity, add `bool? ShowThinking` to `AgenticApiRequest` (nullable, defaults to `false` when absent):

```csharp
public sealed record AgenticApiRequest(
    string ModelName,
    string Prompt,
    string ProfileId,
    int? MaxIterations,
    bool? ShowThinking);
```

**`CraterClaw.Api/Endpoints/ProvidersEndpoints.cs`** — update `POST /api/providers/{name}/agentic/stream`:

- Pass `StreamThinkingChunk` when `request.ShowThinking == true`:
    ```csharp
    StreamThinkingChunk: request.ShowThinking == true
        ? async chunk =>
          {
              await httpContext.Response.WriteAsync(
                  $"data: {JsonSerializer.Serialize(new AgenticSseThinking("thinking", chunk), SseJsonOptions)}\n\n",
                  cancellationToken);
              await httpContext.Response.Body.FlushAsync(cancellationToken);
          }
        : null,
    ```

**`CraterClaw.Web/src/api/types.ts`** — add:

```typescript
export interface AgenticSseThinking {
    type: "thinking";
    content: string;
}
export type AgenticSseEvent =
    | AgenticSseChunk
    | AgenticSseDone
    | AgenticSseThinking;
```

**`CraterClaw.Web/src/api/client.ts`** — update `AgenticRequest` type to include `showThinking?: boolean`.

**`CraterClaw.Web/src/composables/useAgentic.ts`** — add:

- `thinking: Ref<string>` — accumulates thinking tokens.
- `showThinking: Ref<boolean>` — user toggle, default `false`.
- Reset `thinking` to `''` on each `run()` call.
- Append `AgenticSseThinking` content to `thinking`.

**`CraterClaw.Web/src/components/AgenticPanel.vue`** — add:

- A "Show thinking" checkbox that binds to `agentic.showThinking`.
- When `showThinking` is true, pass `showThinking: true` in the agentic request.
- Display `agentic.thinking` in a visually distinct block (e.g., smaller text, muted color) above the response content when non-empty. Use a `<details>` element so it is collapsed by default.

### Tests

**`CraterClaw.Api.Tests/AgenticStreamEndpointTests.cs`** — add:

- `PostAgenticStream_WithShowThinking_EmitsThinkingEvents` — fake service calls `StreamThinkingChunk` with a thinking string; assert a `thinking` type SSE event appears in the response.
- `PostAgenticStream_WithoutShowThinking_NoThinkingEvents` — fake service has thinking content but `showThinking` is false; assert no `thinking` events appear.

Update `FakeAgenticExecutionService` to support calling `StreamThinkingChunk`:

- Add optional `IReadOnlyList<string>? thinkingChunks` constructor parameter.
- If non-null and `request.StreamThinkingChunk` is not null, call it for each before regular chunks.

**`CraterClaw.Web/src/composables/useAgentic.spec.ts`** — add:

- `accumulates thinking chunks into thinking ref`
- `thinking is reset on each run`

**`CraterClaw.Web/src/components/AgenticPanel.spec.ts`** — add:

- `passes showThinking true when toggle is checked`
- `displays thinking content when present`

### Implement

1. Update `ApiModels.cs` — add `AgenticSseThinking`, update `AgenticApiRequest`.
2. Update `ProvidersEndpoints.cs` — emit thinking SSE events.
3. Update `FakeAgenticExecutionService.cs`.
4. Add tests to `AgenticStreamEndpointTests.cs`.
5. Run: `dotnet test CraterClaw.slnx`.
6. Update `types.ts`.
7. Update `client.ts`.
8. Update `useAgentic.ts` and `useAgentic.spec.ts`.
9. Update `AgenticPanel.vue` and `AgenticPanel.spec.ts`.
10. Run: `npm run lint && npm run test:unit` in `CraterClaw.Web`.

### README Sync

No user-visible workflow changes — thinking display is additive.

### Current Architecture Sync

Update `current-architecture.md`:

- Document `AgenticSseThinking` SSE event type and extended SSE protocol.
- Document `ShowThinking` field on `AgenticApiRequest`.
- Document `thinking` ref and `showThinking` toggle in `useAgentic`.
- Document thinking display in `AgenticPanel`.

### Manual Verification Plan

Dependencies: Phase 2 complete. API and web dev server running. qwen3:8b available.

1. Open the Vue frontend.
2. Select provider, qwen3 model, and any profile.
3. Check the "Show thinking" checkbox.
4. Submit a task prompt.
5. Confirm thinking tokens appear in the collapsible section above the response, in a visually distinct style.
6. Confirm the main response content renders normally after thinking completes.
7. Uncheck "Show thinking" and submit another prompt. Confirm no thinking section appears.
