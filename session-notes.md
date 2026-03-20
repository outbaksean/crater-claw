# Session Notes

## Execution Loop (spec: execution-loop)

Implemented both phases of the execution-loop spec.

### Phase 1 - Contracts and service

- Added `AgenticFinishReason`, `AgenticRequest`, `AgenticResponse`, `IAgenticExecutionService`
- Added `IKernelFactory` / `DefaultKernelFactory` as a testability seam for kernel construction
- Implemented `SemanticKernelAgenticExecutionService` with a manual agentic loop (see below)
- Registered all types in `AddCraterClawCore`
- 4 unit tests using a fake `IChatCompletionService` and fake `IKernelFactory`

### Phase 2 - Console wiring

- After profile and model selection, user is prompted for a task
- `AgenticRequest` built with permitted plugins and sent to `IAgenticExecutionService`
- Response streams to console in real time; tool invocations and finish reason displayed after

---

## Key Technical Findings

### Ollama SK connector does not implement the auto-invoke loop

`IChatCompletionService.GetChatMessageContentsAsync` on the Ollama connector invokes tool
functions but does not call the LLM again with the results. The full loop must be driven
manually. Fix: use `FunctionChoiceBehavior.Auto(autoInvoke: false)` and implement the
loop in the service — call LLM, invoke function calls via `FunctionCallContent.InvokeAsync`,
add results to `ChatHistory`, repeat until no function calls or `MaxIterations` reached.

### SK 1.73.0 has no max auto-invoke attempts setting

`PromptExecutionSettings` and `FunctionChoiceBehaviorOptions` do not expose a max
iterations property. `AgenticRequest.MaxIterations` is used by the manual loop instead.

---

## qBitTorrent Connectivity

### Reverse proxy requires Referer and Origin headers

The qBitTorrent instance sits behind a reverse proxy at `/qbittorrent`. Direct API calls
returned 401 until `Referer` and `Origin` headers were added to the login request. These
are derived from `BaseUrl` at runtime in `QBitTorrentPlugin.EnsureAuthenticatedAsync`.

### ListTorrents response was 123KB

The raw qBitTorrent API response overwhelmed the model's context window. `ListTorrentsAsync`
now trims the response to three fields per torrent: `name`, `state`, `added_on`.

---

## Model Selection Notes (GeForce RTX 3080 Ti, 12GB VRAM)

### Recommended models for tool-use agentic tasks

| Model | VRAM (Q4) | Notes |
|---|---|---|
| `qwen2.5:14b` | ~8GB | Best fit; strong tool use and instruction following |
| `qwen2.5:14b-instruct-q5_k_m` | ~10GB | Higher quality, still fits |
| `llama3.1:8b` | ~5GB | Fast, decent tool use, plenty of headroom |
| `mistral-nemo:12b` | ~7GB | Good all-rounder |

Avoid anything above ~14B at Q4 — it will either not fit or spill to CPU and become very slow.

### Context limits

With `qwen2.5:14b` at Q4 (~8GB), roughly 4GB of VRAM remains for KV cache. At ~2 bytes
per token (Q8 cache) that is approximately 8-12K usable context tokens before slowdowns
or OOM.

Qwen 2.5 14B supports up to 128K context natively but VRAM is the practical ceiling.
Do not raise `num_ctx` in Ollama beyond what fits — excess KV cache pre-allocation reduces
headroom for model weights and can cause the model to fail to load.

For short agentic tasks the default Ollama context (2K-4K) is sufficient. Bumping to 8K
is safe on this hardware with a 14B Q4 model. Only raise further if passing long
conversations or large documents as context.

---

## Other Changes

- `AgenticRequest` gained an optional `StreamChunk` callback; when provided, the service
  uses `GetStreamingChatMessageContentsAsync` and forwards each token to the callback.
  The console passes `Console.Write` to stream tokens in real time.
- `DefaultKernelFactory` creates an `HttpClient` with a 10-minute timeout per kernel
  to support slow large models.
- A system prompt is added to every `ChatHistory` instructing the model to answer the
  user's question directly using tool results.
- Debug logging added to `SemanticKernelAgenticExecutionService` logs the full chat
  history before each LLM call and the response received (at `[DBG]` level).
- `External API Verification` rule added to `CLAUDE.md`: plans may not enter
  implementation with unverified external API assumptions.
- Resolved merge conflicts on `wip/torrent-tool` branch (`.claude/settings.local.json`,
  `library-spec.md`, deleted spec files).
