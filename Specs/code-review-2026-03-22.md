# Code Review — 2026-03-22

## Fixed

### DefaultKernelFactory — HttpClient resource leak

**File:** `CraterClaw.Core/DefaultKernelFactory.cs`

`new HttpClient()` was called on every `Create()` invocation, allocating a new connection pool each time without disposal. Over many kernel creations this exhausts sockets. Fixed by injecting `IHttpClientFactory` (already registered via `AddHttpClient`) and calling `CreateClient()`, which delegates to the shared pooled `SocketsHttpHandler`.

### Vue API client — error messages swallowed response body

**File:** `CraterClaw.Web/src/api/client.ts`

`get` and `post` threw errors containing only the HTTP status code. The response body (which contains the actual error detail from the API) was discarded. Fixed to read the response body and append it to the error message. Updated `client.spec.ts` mock to include `text()`.

---

## Not Fixed — Noted for Reference

### QBitTorrentPlugin — broad exception catching in kernel functions

**File:** `CraterClaw.Core/QBitTorrentPlugin.cs`

All kernel functions catch `Exception` and return `"Error: {message}"` as a string. This is an intentional design choice — SK kernel functions surface errors to the LLM as text so the model can observe and potentially recover. The tradeoff is that all failures look the same to the caller. Left as-is; revisit under `agentic-error-recovery` when real failure modes are better understood.

### QBitTorrentPlugin — `_sid` not thread-safe

**File:** `CraterClaw.Core/QBitTorrentPlugin.cs`, line 16

`_sid` has no lock. Concurrent calls could trigger parallel authentication. In practice this is not a real issue today — plugin instances are created fresh per behavior resolution and the agentic loop invokes tools sequentially. Worth addressing if concurrent tool dispatch is ever added.

### SemanticKernelAgenticExecutionService — function invocation exceptions not caught

**File:** `CraterClaw.Core/SemanticKernelAgenticExecutionService.cs`, line 115

`functionCall.InvokeAsync(kernel, cancellationToken)` is not wrapped in a try/catch. Since all kernel functions currently catch their own exceptions and return error strings, this is unlikely to throw in practice. If it does, the exception propagates to the outer catch in `Program.cs`. Revisit under `agentic-error-recovery`.

### craterclaw.json — Windows-specific path committed

**File:** `craterclaw.json`, line 16

`"path": "C:\\ollama-logs"` is a Windows absolute path in the committed config. This is the developer's personal config and works fine for local use, but won't work on Linux. If the project ever runs on Linux or is used by others, this should be cleared to `""` in the committed file and set via user secrets or environment variable.

### craterclaw.json — `aiLogging.enabled: true` committed

**File:** `craterclaw.json`, line 15

AI logging is enabled with a machine-specific path in the committed config. This is a local preference but could confuse a new developer. Consider defaulting to `false` in the committed file.

### QBitTorrentPlugin — filename truncation has no indicator

**File:** `CraterClaw.Core/QBitTorrentPlugin.cs`, line 291

Filenames over 120 characters are silently truncated with no ellipsis. The LLM receives an incomplete filename with no indication it was cut. Low priority — the truncation is intentional to reduce token count — but worth adding `"..."` so the model knows the name is incomplete.

### Test coverage gaps

- `SemanticKernelAgenticExecutionServiceTests` — no test for what happens when `functionCall.InvokeAsync` throws. All tests use fake implementations that succeed.
- `OllamaProviderStatusService` — no dedicated test file. The service catches all exceptions and returns a failure result; edge cases (timeout, malformed response) are not covered.
- No concurrent access tests for `QBitTorrentPlugin._sid`.

### MCP code — dead

The entire MCP infrastructure (`IMcpClientProvider`, `McpClientProvider`, availability service, config types, API endpoints, console flow step) is unused for actual tool integration. `remove-mcp` is a planned checkpoint to clean this up.
