# CraterClaw Library Execution Loop Plan

## Decisions

- `IChatCompletionService` (SK core) is used directly rather than `ChatCompletionAgent` (SK Agents) to avoid a separate package dependency and keep the implementation straightforward.
- A new `Kernel` is built per `ExecuteAsync` call. This is intentional: each call may specify a different model or endpoint, and kernel lifetime is scoped to the task.
- Plugins are registered via `kernel.Plugins.AddFromObject(plugin)`. The plugin object must have `[KernelFunction]` decorated methods; no further type constraint is enforced.
- `FunctionChoiceBehavior.Auto()` is set on `PromptExecutionSettings`. `PromptExecutionSettings` and `FunctionChoiceBehaviorOptions` in SK 1.73.0 do not expose a max auto-invoke attempts property. The `MaxIterations` field in `AgenticRequest` is retained for future use when SK exposes this setting.
- Tool invocations are collected after the call from `ChatHistory` entries where the role is `Tool`, by extracting `FunctionResultContent.FunctionName` items.
- `IterationLimitReached` is detected by checking if the last returned `ChatMessageContent` contains any `FunctionCallContent` items (meaning SK stopped before completing the loop).
- Tests use a fake `IChatCompletionService` injected into the kernel via `KernelBuilder` to avoid hitting a live Ollama instance. One test verifies tool tracking using a real in-process kernel function.
- The console uses a fixed `MaxIterations` of 10 unless the model or profile defines a lower limit. No user-facing configuration for this value yet.

---

## Phase 1: Contracts, Service Implementation, and Tests

### Status
- Done

### Goal
- Define the agentic contract types, implement `SemanticKernelAgenticExecutionService`, register in DI, and cover with unit tests.

### Contract

**`AgenticFinishReason`** (public enum, `CraterClaw.Core`):
```csharp
public enum AgenticFinishReason { Completed, IterationLimitReached }
```

**`AgenticRequest`** (public record, `CraterClaw.Core`):
```csharp
public sealed record AgenticRequest(
    string ModelName,
    string Prompt,
    IReadOnlyList<object> Plugins,
    int MaxIterations);
```

**`AgenticResponse`** (public record, `CraterClaw.Core`):
```csharp
public sealed record AgenticResponse(
    string Content,
    AgenticFinishReason FinishReason,
    IReadOnlyList<string> ToolsInvoked);
```

**`IAgenticExecutionService`** (public interface, `CraterClaw.Core`):
```csharp
public interface IAgenticExecutionService
{
    Task<AgenticResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        AgenticRequest request,
        CancellationToken cancellationToken);
}
```

### Tasks

**`CraterClaw.Core`**
- Add `AgenticFinishReason.cs`.
- Add `AgenticRequest.cs`.
- Add `AgenticResponse.cs`.
- Add `IAgenticExecutionService.cs`.
- Add `SemanticKernelAgenticExecutionService.cs` (internal, sealed):
  - Constructor: `ILogger<SemanticKernelAgenticExecutionService> logger`
  - `ExecuteAsync` implementation:
    1. Build kernel: `Kernel.CreateBuilder().AddOllamaChatCompletion(request.ModelName, new Uri(endpoint.BaseUrl)).Build()`
    2. Register plugins: `foreach (var p in request.Plugins) kernel.Plugins.AddFromObject(p)`
    3. Construct `ChatHistory`, add user message
    4. Build `PromptExecutionSettings` with `FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()` and `MaxAutoInvokeAttempts = request.MaxIterations` (confirm property name at implementation time)
    5. Call `kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentsAsync(chatHistory, settings, kernel, cancellationToken)`
    6. Collect tool names: `chatHistory.Where(m => m.Role == AuthorRole.Tool).SelectMany(m => m.Items.OfType<FunctionResultContent>()).Select(f => f.FunctionName)`
    7. Detect iteration limit: `messages.LastOrDefault()?.Items.Any(i => i is FunctionCallContent) ?? false`
    8. Extract content: last assistant message's `.Content`
    9. Log each tool invoked at Information; log finish reason at Information
    10. Return `AgenticResponse`
- Update `ServiceCollectionExtensions.AddCraterClawCore()`:
  - `services.AddTransient<IAgenticExecutionService, SemanticKernelAgenticExecutionService>()`

**`CraterClaw.Core.Tests`**
- Add `SemanticKernelAgenticExecutionServiceTests.cs`.

### Fake IChatCompletionService

Tests inject a fake via `KernelBuilder`:
```csharp
private static Kernel BuildKernelWithFake(params ChatMessageContent[] responses)
{
    var fake = new FakeChatCompletionService(responses);
    return Kernel.CreateBuilder()
        .AddSingleton<IChatCompletionService>(fake)
        .Build();
}
```

`FakeChatCompletionService` implements `IChatCompletionService`, returns queued `ChatMessageContent` instances, and throws `NotImplementedException` for streaming.

> Note: confirm `AddSingleton<IChatCompletionService>` is the correct registration method on `IKernelBuilder` at implementation time. An alternative is `builder.Services.AddSingleton<IChatCompletionService>(fake)`.

### Tests

`SemanticKernelAgenticExecutionServiceTests`:
- `ExecuteAsync_ReturnsContent_WhenAgentRespondsDirectly`: fake returns one plain-text assistant message; assert `response.Content` matches and `FinishReason == Completed`.
- `ExecuteAsync_ReturnsEmptyToolsInvoked_WhenNoToolsUsed`: fake returns plain text; assert `response.ToolsInvoked` is empty.
- `ExecuteAsync_ReturnsIterationLimitReached_WhenLastMessageHasFunctionCall`: fake returns a message containing a `FunctionCallContent` item; assert `FinishReason == IterationLimitReached`.
- `ExecuteAsync_TracksToolsInvoked_WhenFunctionIsCalledAndReturned`: register a real in-process kernel function (`KernelFunctionFactory.CreateFromMethod`) that returns a fixed string; fake returns a function-call message then a plain-text message; assert `response.ToolsInvoked` contains the function name.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with new tests included.

---

## Phase 2: Console Wiring

### Status
- Done

### Goal
- After profile selection and function listing, prompt the user for a task and run the agentic loop.

### Tasks

**`CraterClaw.Console/Program.cs`**
- Resolve `IAgenticExecutionService` from DI.
- After the plugin function listing block, add:
  - If a model was selected (i.e. `selectedModelName` is not null):
    - `Console.Write("Enter task prompt (leave blank to skip): ")`
    - Read input; skip if blank
    - Build `plugins`: if `permitted.Count > 0` add `qBitTorrentPlugin` to the list, else empty list
    - Build `AgenticRequest(selectedModelName, prompt, plugins, MaxIterations: 10)`
    - Call `agenticExecutionService.ExecuteAsync(endpoint, request, CancellationToken.None)`
    - For each tool in `response.ToolsInvoked`: `Console.WriteLine($"Tool: {tool}")`
    - `Console.WriteLine("Response:")` then `Console.WriteLine(response.Content)`
    - `Console.WriteLine($"Tools invoked: {response.ToolsInvoked.Count}")`
    - If `response.FinishReason == AgenticFinishReason.IterationLimitReached`: `Console.WriteLine("(iteration limit reached)")`
    - Wrap in try/catch; print error and continue on failure

### Tests
- No new tests required.

### Manual Verification Plan
- Prerequisites: Ollama running with a tool-use capable model (e.g. `qwen2.5:7b`); qBitTorrent running with WebUI enabled; `qbittorrent:*` credentials in user secrets.
- Run the console, select the Ollama endpoint, select a compatible model, select the `qbittorrent-manager` profile.
- Enter a task prompt such as `List my active torrents`.
- Confirm tool calls appear as `Tool: ListTorrents` (or similar) as the agent runs.
- Confirm the final response is displayed under `Response:`.
- Confirm `Tools invoked: {n}` is displayed.
- Run again with the `no-tools` profile and confirm no tool calls appear in the output.

---

## Completion Criteria
- Both phase statuses are marked Done.
- All automated tests pass.
- Manual verification confirms end-to-end tool use against qBitTorrent.
- `execution-loop-spec.md` Status is updated to Done.
