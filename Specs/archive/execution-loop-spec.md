# CraterClaw Library Execution Loop Spec

## Name
- CraterClaw Library Execution Loop

## Purpose
- Implement `IAgenticExecutionService` as a CraterClaw abstraction over Semantic Kernel's `ChatCompletionAgent`, and wire the console harness to run end-to-end agentic tasks against permitted MCP servers.

## Scope
- Add contract types in `CraterClaw.Core`:
  - `AgenticFinishReason` enum: `Completed`, `IterationLimitReached`
  - `AgenticRequest` immutable record:
    - `ModelName` (string)
    - `Prompt` (string) — the user's task prompt
    - `Plugins` (IReadOnlyList\<object\>) — plugin objects with `[KernelFunction]` methods whose tools are available for this task
    - `MaxIterations` (int) — maximum number of auto-invoke iterations before the loop is cut off
  - `AgenticResponse` immutable record:
    - `Content` (string) — the final assistant text response
    - `FinishReason` (AgenticFinishReason)
    - `ToolsInvoked` (IReadOnlyList\<string\>) — names of tools called during the run, in order
- Add `IAgenticExecutionService` interface in `CraterClaw.Core`:
  - `Task<AgenticResponse> ExecuteAsync(ProviderEndpoint endpoint, AgenticRequest request, CancellationToken cancellationToken)`
- Implement `SemanticKernelAgenticExecutionService` (internal, sealed) in `CraterClaw.Core`:
  - Build a `Kernel` per call using `Kernel.CreateBuilder().AddOllamaChatCompletion(request.ModelName, new Uri(endpoint.BaseUrl)).Build()`
  - Register each plugin in `request.Plugins` via `kernel.Plugins.AddFromObject(plugin)`
  - Create a `ChatHistory` and add the user prompt
  - Resolve `IChatCompletionService` from the kernel and call `GetChatMessageContentsAsync` with `FunctionChoiceBehavior.Auto()` and `MaxAutoInvokeAttempts` from `request.MaxIterations`
  - After the call, collect tool invocation names from `FunctionResultContent` items in the updated `ChatHistory` for `AgenticResponse.ToolsInvoked`
  - Detect `IterationLimitReached`: if the last returned message still contains `FunctionCallContent` items, the iteration limit was hit
  - Inject `ILogger<SemanticKernelAgenticExecutionService>` and log at Information: each tool invoked, final finish reason
- Register `IAgenticExecutionService` as Transient in `AddCraterClawCore()`.
- Update `CraterClaw.Console/Program.cs`:
  - After the tool listing step (from mcp-sessions spec), prompt: `Enter task prompt (leave blank to skip):`
  - If a prompt is entered: resolve permitted servers as in the mcp-sessions step and call `IAgenticExecutionService.ExecuteAsync`
  - Display each tool invocation as it is reported: `Tool: {toolName}`
  - Display the final response under `Response:`
  - Display `Iterations: {n}` after the response
  - If finish reason is `IterationLimitReached`, display `(iteration limit reached)`
- Add automated tests for `SemanticKernelAgenticExecutionService` using a fake `IChatCompletionService` and fake MCP tool responses; do not test against live Ollama or live MCP endpoints.

## Contract Notes
- `SemanticKernelAgenticExecutionService` owns the full lifecycle of `Kernel`, `ChatCompletionAgent`, and `IMcpClient` instances for each task run. The caller does not manage SK objects.
- `MaxIterations` in `AgenticRequest` maps to `MaxAutoInvokeAttempts` on SK's `PromptExecutionSettings` or equivalent. The exact mapping is confirmed at plan time based on SK's API.
- `IAgenticExecutionService` does not expose SK types in its contract. The console and future callers are isolated from SK.
- `AgenticRequest.Plugins` is a list of plain objects with `[KernelFunction]` methods. SK types are not exposed in the contract.

## Manual Verification
- Dependencies: a tool-use capable model downloaded (e.g. `qwen2.5:7b`); `uv` installed; qBitTorrent MCP server reachable.
- Run the console, select an endpoint and model, select the `qbittorrent-manager` profile, and enter a task prompt such as `List my active torrents`.
- Confirm tool calls appear as `Tool: {toolName}` as the agent runs.
- Confirm the final response is displayed under `Response:`.
- Confirm the iteration count is displayed.

## Status
- Done
