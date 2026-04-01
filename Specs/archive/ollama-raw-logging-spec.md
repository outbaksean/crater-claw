# Spec: ollama-raw-logging

## Goal

Capture raw HTTP request and response bodies for every call Semantic Kernel makes to the Ollama API, writing them to a separate rolling `.raw.log` file. The raw log is controlled by a new `aiRawLogging` config section (same shape as `aiLogging`). The existing AI semantic log (`CraterClaw.AiTraffic`) is unchanged.

## Scope

`CraterClaw.Core`, `CraterClaw.Core.Tests`, `CraterClaw.Console`, `CraterClaw.Api`.

---

## Phase 1: Raw HTTP logging

**Status: Done**

### Context

`DefaultKernelFactory` currently calls `httpClientFactory.CreateClient()` (unnamed) and configures the `HttpClient` inline. To intercept HTTP traffic, we register a named `HttpClient` `"ollama"` with a `DelegatingHandler` attached. The handler logs request and response bodies to the `CraterClaw.AiTraffic.Raw` logger category. Serilog in both hosts routes that category to a `.raw.log` file when `aiRawLogging.enabled` is true.

Streaming responses (Ollama returns NDJSON for streaming chat completion) must not be buffered up front — doing so would stall all chunk delivery until the full response is complete, breaking the streaming UX. The handler wraps the response `HttpContent` with a `TeeHttpContent` that passes reads through to the original stream and accumulates a copy in memory; the accumulated bytes are flushed to the logger when the content is disposed.

### Contract

**`CraterClaw.Core/OllamaLoggingHandler.cs`** — new file:

```csharp
internal sealed class OllamaLoggingHandler(ILoggerFactory loggerFactory) : DelegatingHandler
```

- Creates `_logger = loggerFactory.CreateLogger("CraterClaw.AiTraffic.Raw")`.
- In `SendAsync`:
  1. If `request.Content` is not null, read the request body as a string and log it at `Debug` level with message `"[REQUEST] {Body}"`.
  2. Call `base.SendAsync` to get the response.
  3. If `response.Content` is not null, replace `response.Content` with `new TeeHttpContent(response.Content, _logger)`.
  4. Return the response.

**`CraterClaw.Core/TeeHttpContent.cs`** — new file:

```csharp
internal sealed class TeeHttpContent(HttpContent inner, ILogger logger) : HttpContent
```

- Copies headers from `inner` into `this.Headers` in the constructor.
- Overrides `SerializeToStreamAsync`: reads from `inner.ReadAsStreamAsync()` in chunks, writes each chunk to `stream` (the destination) and to a `MemoryStream` accumulator.
- Overrides `TryComputeLength`: returns false (length unknown until fully read).
- On `Dispose(bool)`: if the accumulator has bytes, logs them as UTF-8 string at `Debug` level with message `"[RESPONSE] {Body}"`; then disposes `inner`.

**`CraterClaw.Core/DefaultKernelFactory.cs`**:
- Change `httpClientFactory.CreateClient()` to `httpClientFactory.CreateClient("ollama")`.
- Remove the manual `Timeout` assignment (set on the named client registration instead).

**`CraterClaw.Core/ServiceCollectionExtensions.cs`**:
- Add `services.AddTransient<OllamaLoggingHandler>()`.
- Add `services.AddHttpClient("ollama", c => c.Timeout = TimeSpan.FromMinutes(10)).AddHttpMessageHandler<OllamaLoggingHandler>()`.
- Remove `services.AddHttpClient()` if it was added here (if it was only added in the host `Program.cs`, no change needed in Core).

**`CraterClaw.Console/Program.cs`** and **`CraterClaw.Api/Program.cs`**:
- Read `aiRawLogging:enabled` (bool) and `aiRawLogging:path` (string) from config using the same pattern as `aiLogging`.
- Resolve the raw log path with suffix `.raw.log` instead of `.log`: default `logs/ollama-.raw.log` (console) / `logs/ollama-api-.raw.log` (API); directory → `ollama-.raw.log` inside it; file prefix → `{prefix}-.raw.log`.
- Update the main log sub-logger filter to also exclude `CraterClaw.AiTraffic.Raw`.
- When `aiRawLogging.enabled` is true, add a sub-logger for `CraterClaw.AiTraffic.Raw` writing to the raw log path.
- Print raw log file path to console at startup when enabled (console harness only).

### Path resolution

Follow the same rules as `ResolveAiLogPath` but with a `.raw.log` suffix:

| `aiRawLogging:path` value | Resolved path |
|---|---|
| (empty / absent) | `logs/ollama-.raw.log` (console) or `logs/ollama-api-.raw.log` (API) |
| Directory path | `{dir}/ollama-.raw.log` |
| File prefix (e.g. `logs/raw`) | `logs/raw-.raw.log` |

Note: Serilog inserts the date between the prefix and the suffix (e.g. `ollama-20260327.raw.log`).

### Tests

New file `CraterClaw.Core.Tests/OllamaLoggingHandlerTests.cs`:

- `SendAsync_LogsRequestBody` — set up handler with a fake inner handler; send a request with a JSON body; verify logger received a `[REQUEST]` message containing the body text.
- `SendAsync_LogsResponseBody` — fake inner handler returns a response with a string body; read the full response content via the tee; verify logger received a `[RESPONSE]` message containing the body text.
- `SendAsync_ResponseBodyPassesThrough` — verify that reading the tee content returns the same bytes as the original body (data is not corrupted by the tee).
- `SendAsync_NullRequestContent_DoesNotThrow` — send a request with no body; verify no exception.
- `SendAsync_NullResponseContent_DoesNotThrow` — fake inner handler returns a response with no body; verify no exception.

Use `Microsoft.Extensions.Logging.Testing` or a simple fake logger to capture log calls.

### Implement

1. Add `OllamaLoggingHandler.cs` to `CraterClaw.Core`.
2. Add `TeeHttpContent.cs` to `CraterClaw.Core`.
3. Update `DefaultKernelFactory.cs` to use the named client.
4. Update `ServiceCollectionExtensions.cs` to register the named client with the handler.
5. Update `CraterClaw.Console/Program.cs` — raw log config read and Serilog wiring.
6. Update `CraterClaw.Api/Program.cs` — raw log config read and Serilog wiring.
7. Add `OllamaLoggingHandlerTests.cs` to `CraterClaw.Core.Tests`.
8. Run: `dotnet test CraterClaw.slnx`

### README Sync

Add `aiRawLogging` to the Configuration section. Document `enabled` and `path` with same notes as `aiLogging`.

### Current Architecture Sync

- Add `OllamaLoggingHandler` and `TeeHttpContent` to the Logging section of `current-architecture.md`.
- Note the named `"ollama"` HTTP client.
- Update `AiRawLoggingOptions` description under Configuration Types.

### Manual Verification Plan

Dependencies: Ollama running with at least one model.

1. Set `"aiRawLogging": { "enabled": true }` in `craterclaw.json`.
2. Run `craterclaw run` (console harness). The startup output should include the raw log path.
3. Send a task prompt through the agentic panel (any profile).
4. Open the `.raw.log` file; verify `[REQUEST]` entries contain the full Ollama request JSON (system prompt, messages, tools).
5. Verify `[RESPONSE]` entries contain the raw NDJSON response chunks.
6. Verify the main `.log` file does not contain any `CraterClaw.AiTraffic.Raw` entries.
7. Set `"aiRawLogging": { "enabled": false }` and re-run; verify no `.raw.log` entries are written.
