# CraterClaw Library Interactive Execution Plan

## Decisions

- `MessageRole` is an enum (User, Assistant, System). `FinishReason` is an enum (Stop, Length).
- `ExecutionRequest` carries optional `Temperature` and `MaxTokens` directly as nullable properties rather than in a nested record, keeping the type flat and easy to construct.
- The Ollama implementation targets `/api/chat` with `stream: false`. The response body is parsed in one read; no streaming is introduced in this spec.
- `done_reason` values from Ollama are mapped to `FinishReason`. Unknown values default to `Stop` rather than throwing, to tolerate future Ollama additions.
- Private DTOs inside `OllamaModelExecutionService` use `JsonNamingPolicy.SnakeCaseLower` with `PropertyNameCaseInsensitive = true`. DTO property names are chosen so snake_case matches the Ollama wire format (`Model` → `model`, `DoneReason` → `done_reason`, `NumPredict` → `num_predict`).
- `IModelExecutionService` is registered as Transient in `AddCraterClawCore()` and receives its own `HttpClient` from the DI-managed factory, separate from the status and listing services.
- The console flow is single-turn: one User message per run. Multi-turn conversation history is supported by the contract but not wired in the console harness in this spec.

## Overview
- This plan implements the interactive execution spec in two phases.
- Phase 1 defines contracts and writes service tests against the implementation that Phase 2 will produce.
- Phase 2 adds the Ollama implementation, DI registration, and console wiring.

---

## Phase 1: Contracts and Tests

### Status
- Done

### Goal
- Define all public contract types and the `IModelExecutionService` interface, and write automated tests that verify the Ollama service behavior.

### Contract
- `MessageRole` enum in `CraterClaw.Core`: `User`, `Assistant`, `System`
- `ConversationMessage` sealed record in `CraterClaw.Core`: `Role` (MessageRole), `Content` (string)
- `FinishReason` enum in `CraterClaw.Core`: `Stop`, `Length`
- `ExecutionRequest` sealed record in `CraterClaw.Core`:
  - `ModelName` (string)
  - `Messages` (IReadOnlyList\<ConversationMessage\>)
  - `Temperature` (double?) — optional, omitted from request when null
  - `MaxTokens` (int?) — optional, maps to Ollama `num_predict`, omitted when null
- `ExecutionResponse` sealed record in `CraterClaw.Core`:
  - `Content` (string) — assistant message content
  - `ModelName` (string) — model that produced the response
  - `FinishReason` (FinishReason)
- `IModelExecutionService` interface in `CraterClaw.Core`:
  - `Task<ExecutionResponse> ExecuteAsync(ProviderEndpoint endpoint, ExecutionRequest request, CancellationToken cancellationToken)`

### Tasks
- Add `MessageRole.cs`, `ConversationMessage.cs`, `FinishReason.cs`, `ExecutionRequest.cs`, `ExecutionResponse.cs`, and `IModelExecutionService.cs` in `CraterClaw.Core`.
- Add `OllamaModelExecutionServiceTests.cs` in `CraterClaw.Core.Tests` using the `DelegatingTestHandler` pattern.

### Ollama `/api/chat` Wire Format (for test fixtures)

Request body:
```json
{
  "model": "llama3.2:latest",
  "messages": [
    { "role": "user", "content": "Why is the sky blue?" }
  ],
  "stream": false,
  "options": {
    "temperature": 0.7,
    "num_predict": 512
  }
}
```
`options` is omitted when both `Temperature` and `MaxTokens` are null.

Response body:
```json
{
  "model": "llama3.2:latest",
  "message": {
    "role": "assistant",
    "content": "The sky appears blue because of Rayleigh scattering."
  },
  "done": true,
  "done_reason": "stop"
}
```

### Tests
- Returns `ExecutionResponse` with correct content, model name, and `FinishReason.Stop` for a standard successful response.
- Returns `FinishReason.Length` when `done_reason` is `"length"`.
- Sends `stream: false` and the correct model name and message list in the serialized request body.
- Throws `InvalidOperationException` when the provider returns a non-success HTTP status.
- Throws `InvalidOperationException` for a response body with malformed JSON.
- Propagates cancellation when the token is cancelled before the HTTP call completes.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with the new tests included.

---

## Phase 2: Ollama Implementation, DI Registration, and Console Wiring

### Status
- Done

### Goal
- Implement `OllamaModelExecutionService`, register it in DI, and update the console harness to accept a model name and prompt, execute a request, and display the response.

### Contract
- No new public surface beyond Phase 1.
- `options` object is included in the request body only when at least one of `Temperature` or `MaxTokens` is non-null; individual fields within `options` are omitted when null.

### Tasks
- Add `OllamaModelExecutionService.cs` (internal, sealed) in `CraterClaw.Core`:
  - Inject `HttpClient` via constructor.
  - Build a URI from `endpoint.BaseUrl + "/api/chat"`.
  - Serialize the request using private DTOs with `JsonNamingPolicy.SnakeCaseLower` and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`.
  - Deserialize the response using the same options.
  - Map `done_reason` string to `FinishReason`; default unknown values to `Stop`.
  - Throw `InvalidOperationException` on non-success HTTP status or malformed JSON.
- Register `IModelExecutionService` as Transient → `OllamaModelExecutionService` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update `CraterClaw.Console/Program.cs`:
  - After the model list, prompt: `Enter model name:` and then `Enter prompt:`.
  - If either input is blank, skip execution and exit the flow cleanly.
  - Call `IModelExecutionService.ExecuteAsync` with a single User message.
  - Print `Response:` followed by the assistant content on the next line.
  - If `FinishReason` is `Length`, print `(response truncated by token limit)` after the content.
  - Catch and display errors without crashing.

### Tests
- No new tests required; Phase 1 tests cover service behavior.

### Manual Verification Plan
- Dependencies: at least one model downloaded on the active Ollama endpoint.
- Run the console harness, select an endpoint, and enter a downloaded model name and a simple prompt.
- Confirm the assistant response appears under `Response:`.
- Enter a model name that does not exist and confirm a clear error is displayed without crashing.

---

## Completion Criteria
- Both phase statuses are marked Done.
- `CraterClaw.Core` and `CraterClaw.Console` build successfully.
- Automated tests for execution contract and service behavior pass.
- Manual verification confirms prompt execution and error handling work correctly.
- `interactive-execution-spec.md` Status is updated to Done.
