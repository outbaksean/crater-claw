# Child Agents — Research Notes

**Produced by:** investigate-child-agents checkpoint
**SK version:** 1.73.0 / Connectors.Ollama 1.73.0-alpha

---

## Findings

### 1. SK agent orchestration support

`Microsoft.SemanticKernel.Agents.Core` is a **separate stable package** (not included in the `Microsoft.SemanticKernel` meta-package). It must be added explicitly. It contains `ChatCompletionAgent`, which is built entirely on `IChatCompletionService` with no OpenAI coupling — it works with the Ollama connector.

`AgentGroupChat` is **deprecated** as of mid-2025. The replacement is `GroupChatOrchestration` from `Microsoft.SemanticKernel.Agents.Orchestration`, which is experimental/prerelease. Five orchestration patterns exist: Concurrent, Sequential, Handoff, GroupChat, Magentic. All are service-agnostic at the orchestration layer.

For a parent-calls-child pattern (not group chat), the recommended path is a **manual `KernelFunction` wrapper**: a `KernelFunction` whose method body invokes the child agent and returns the result as a string. This requires no prerelease packages beyond `Agents.Core`.

### 2. Feasible child agent unit

The most natural unit is: a `KernelFunction` registered on the parent's kernel that internally calls `IAgenticExecutionService.ExecuteAsync` with a different `AgenticRequest` (different model, system prompt, or plugin set). The parent model sees it as a tool call; the child runs a full agentic loop and returns a string result.

Key characteristics:

- The child is **isolated** — separate `ChatHistory`, no shared conversation context with the parent
- The child can use a **different model** on the same or a different provider
- The child can have a **different plugin set** — enabling role specialization (e.g. a search-only agent, a file-only agent)
- The parent decides when to call the child based on the task prompt and available tools

A child agent does not need to be backed by `ChatCompletionAgent` from `Agents.Core` — it can simply be a wrapper around the existing `IAgenticExecutionService`. This means no new dependencies are required.

### 3. Use cases in CraterClaw

The current and near-term planned behaviors are:

- `qbittorrent-home` / `qbittorrent-seedbox`: manage torrents, search, add, monitor
- `media-manual` (planned): qBitTorrent + FTP + media library — sequential transfer workflow
- `media-supervised` (planned): Radarr + Sonarr + Jellyfin — arr stack management

For these workflows a single agentic loop is sufficient. The tools are well-scoped, the tasks are sequential, and context doesn't grow large enough to strain a single model run.

The most plausible future case for child agents is a **planner/executor split**:

- A larger orchestrator model breaks down a complex task ("download and organize the latest season of X") into steps
- Smaller/faster child agents handle individual operations (search, evaluate results, execute transfers)

This pattern becomes compelling once the media plugins exist and real multi-step workflows are being run. It is not yet justified by current behaviors.

A secondary use case: **model specialization**. Some tasks (e.g. evaluating search results, summarizing torrent listings) don't need a large reasoning model. A child agent using a smaller model for these sub-tasks would reduce latency. This requires the multi-model workflow to be working and measured before it's worth designing.

### 4. Constraints and risks

**Latency:** Child agent calls are full Ollama inference round-trips. On local hardware, chained calls are strictly sequential within SK's function call handling. A two-level agent tree could easily take 2–3x the wall time of a single loop. This is acceptable for background workflows but noticeable for interactive use.

**Context isolation:** Child agents get a fresh `ChatHistory`. They don't inherit the parent's conversation. This is a feature (bounded context, no bloat) but the parent must pass all needed context in the tool call arguments. Poorly specified calls produce poor results.

**Loop prevention:** A child agent that itself has a sub-agent tool could recurse. The existing `MaxIterations` limit applies per-loop, not globally. A depth limit or tool design convention (child agents don't receive child agent tools) is needed.

**Error propagation:** If a child throws, the parent's tool invocation returns an exception result. The parent model sees a tool error and decides how to proceed — it may retry, skip, or give up. This is the same as any other tool failure and is already handled by the existing loop.

**Concurrent Ollama calls:** Running two Ollama inference requests simultaneously on local hardware with one GPU would likely serialize on the GPU anyway. Parallel child agents would not actually run in parallel in practice.

**Streaming:** The current implementation uses streaming for the parent loop. A child agent invoked as a tool call runs non-streaming internally and returns a string result. The parent continues streaming after the child returns. This is straightforward.

### 5. UX surface

The simplest approach: treat a child agent call like any other tool invocation. The parent's tool invocation summary already shows which tools were called. No special UI is needed to start.

A richer approach (deferred): if the child agent has its own streaming output, it could be shown in a collapsible nested block in the web UI, similar to the existing thinking block. This would require the streaming API to support nested event types.

---

## Recommendation

**Implement a thin `SubAgentFunction` helper, but defer wiring it into behaviors.**

A `SubAgentFunction` is a static factory that takes a name, description, `ProviderEndpoint`, and `AgenticRequest` template, and returns a `KernelFunction` whose body calls `IAgenticExecutionService.ExecuteAsync`. It can be registered on a parent kernel as a plugin. No new packages are required — only the existing `IAgenticExecutionService` interface.

This is a small, low-risk checkpoint. The right time to use it is when the media workflows exist and a concrete planner/executor split is motivated by real usage.

**Do not use the prerelease orchestration packages** (`Agents.Orchestration`, `Agents.Runtime.InProcess`) yet. They are experimental, the API is unstable, and the simpler `KernelFunction` wrapper approach covers the immediate need.

---

## Proposed Checkpoints

### child-agent-function

**Type: Code**

Add `SubAgentFunction`: a static factory in `CraterClaw.Core` that creates a `KernelFunction` wrapping a child `IAgenticExecutionService` call. Takes a function name, description, endpoint, model name, system prompt, and plugin list. The function accepts a single `string prompt` parameter and returns the child's response content as a string. `MaxIterations` defaults to the same value as the parent loop. No depth tracking in this checkpoint — child agents should not be given sub-agent tools by convention.

No behavior wiring in this checkpoint — it is a building block only.

### child-agent-behavior (deferred — pending media workflows)

**Type: Code**

Wire `SubAgentFunction` into a behavior that benefits from a planner/executor split. Scope to be determined once the media plugins exist and a real use case is identified.

---

## Microsoft Agent Framework

The Microsoft Agent Framework (public preview, April 2026) is the announced successor to both Semantic Kernel and AutoGen, built by the same teams. In C# it is built on `Microsoft.Extensions.AI.IChatClient` rather than SK's `IChatCompletionService`. Ollama is a supported first-class provider (function tools and structured output; no MCP tools, which CraterClaw has already removed).

For multi-agent patterns it is a better fit than SK: tools are plain methods with optional `[Description]` attributes (no `[KernelFunction]` required), the unified `AIAgent` type replaces the separate agent classes, and the Workflows API provides explicit graph-based multi-agent orchestration.

**Why not now:** Public preview — API is unstable. Adopting it for `child-agent-function` would mean building on a moving target and migrating all of `CraterClaw.Core`, the console harness, and the API to new namespaces and patterns before the framework is stable.

**Future direction:** When the Agent Framework goes stable, migrating CraterClaw from SK would simplify the codebase and make multi-agent patterns much cleaner. This is tracked as the `agent-framework-migration` planned checkpoint.

## Decisions

- **No parallel execution.** Child agents run sequentially. No concurrent Ollama calls within a single agentic tree.
- **Model selection: resolve a behavior profile.** The child agent is identified by a behavior profile name. The profile supplies the model, system prompt, and plugin list — consistent with how all other behaviors are configured in `craterclaw.json`. A fixed model override via the UI is a possible future addition but not in scope now.
- **Depth: hardcoded limit.** `IAgenticExecutionService` accepts a depth parameter (default 0). `SubAgentFunction` increments depth when invoking the child. Calls at or beyond a hardcoded max depth (e.g. 2) return an error string instead of executing. No convention-based approach — enforcement is in the service.
- **Streaming child output: deferred.** Child agent output is returned as a string result to the parent. No nested streaming events in this checkpoint.
