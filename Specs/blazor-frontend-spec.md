# Blazor Frontend Spec

## Checkpoint

blazor-frontend

## Purpose

Provide a second browser UI, built in Blazor WebAssembly, that mirrors the Vue frontend flows by consuming the same CraterClaw API. No business logic lives in the frontend; it is a thin client over the API. The two frontends are independent and interchangeable; they share no runtime code.

## Scope

- Add `CraterClaw.Blazor` as a Blazor WebAssembly project (.NET 10) in the solution root.
- Add the project to `CraterClaw.slnx`.
- Consume provider, model, execution, profile, and agentic endpoints.
- Mirror the console harness flows: provider selection and status, model listing, interactive chat, behavior profile selection, and agentic task execution.
- Unit test components with bUnit (xUnit-based).
- No authentication, no user accounts.
- No Blazor Server hosting model — WASM only.
- MCP server UI is out of scope.

## Views and User Flows

### Provider Selection

- List all configured providers from `GET /api/providers`.
- Allow the user to select one (numbered list).
- Check and display status for the selected provider via `GET /api/providers/{name}/status`.

### Model Selection

- After a provider is selected and reachable, list its models from `GET /api/providers/{name}/models`.
- Allow the user to select a model (numbered list).

### Interactive Execution

- After provider and model are selected, show a chat input area.
- Send messages via `POST /api/providers/{name}/execute`.
- Display the multi-turn conversation history.

### Behavior Profiles

- List profiles from `GET /api/profiles`.
- Allow the user to select a profile (numbered list).
- Apply preferred provider/model defaults from the selected profile; display a warning when a preferred value is not available.

### Agentic Execution

- After provider, model, and profile are selected, show a task prompt input.
- Submit via `POST /api/providers/{name}/agentic`.
- Display response content, finish reason, and tools invoked list.

## Tech Stack

- Blazor WebAssembly (.NET 10)
- C# (nullable enabled)
- `HttpClient` for API calls (registered via `IHttpClientFactory`)
- bUnit + xUnit for component tests
- No third-party component library (plain CSS)

## Project Layout

```
CraterClaw.Blazor/
  Api/
    Types.cs          -- C# record types mirroring API response shapes
    CraterClawClient.cs  -- typed HttpClient wrapper for all API endpoints
  Components/
    ProviderPanel.razor
    ModelPanel.razor
    InteractiveChat.razor
    ProfileSelector.razor
    AgenticPanel.razor
  Pages/
    Home.razor        -- root page; wires all panels together
  wwwroot/
    index.html
    app.css
  App.razor
  Program.cs
  CraterClaw.Blazor.csproj

CraterClaw.Blazor.Tests/
  CraterClawClientTests.cs
  ProviderPanelTests.cs
  ModelPanelTests.cs
  InteractiveChatTests.cs
  ProfileSelectorTests.cs
  AgenticPanelTests.cs
  CraterClaw.Blazor.Tests.csproj
```

## API Types (`Api/Types.cs`)

```csharp
namespace CraterClaw.Blazor.Api;

public record ProviderEndpoint(string Name, string BaseUrl);

public record ProviderStatus(bool IsReachable, string? ErrorMessage);

public record ModelItem(string Name, long SizeBytes, string ModifiedAt);

public record MessageItem(string Role, string Content);

public record ExecutionRequest(string ModelName, MessageItem[] Messages, double? Temperature = null, int? MaxTokens = null);

public record ExecutionResponse(string Content, string ModelName, string FinishReason);

public record PluginBinding(string Name, string[] Tools);

public record BehaviorProfile(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    PluginBinding[] Plugins);

public record AgenticRequest(string ModelName, string Prompt, string ProfileId, int? MaxIterations = null);

public record AgenticResponse(string Content, string FinishReason, string[] ToolsInvoked);
```

API base URL is read from configuration key `ApiBaseUrl` (set in `wwwroot/appsettings.json`; defaults to `http://localhost:5000`).

---

## Phase 1: Scaffold + provider and model selection

**Status: Done**

### Contract

```csharp
// CraterClawClient
Task<List<ProviderEndpoint>> GetProvidersAsync(CancellationToken ct = default);
Task<ProviderStatus> GetProviderStatusAsync(string name, CancellationToken ct = default);
Task<List<ModelItem>> GetModelsAsync(string providerName, CancellationToken ct = default);
```

`ProviderPanel` exposes:
- A numbered list of providers, selectable by click.
- Status indicator for the selected provider (loading / reachable / unreachable with error message).
- An `EventCallback<ProviderEndpoint>` parameter (`OnProviderSelected`).

`ModelPanel` exposes:
- A numbered list of models for the given provider, selectable by click.
- Hidden when provider is null or unreachable.
- An `EventCallback<ModelItem>` parameter (`OnModelSelected`).

### Tests

- `CraterClawClientTests`: `GetProvidersAsync` calls `GET /api/providers`; `GetProviderStatusAsync` calls `GET /api/providers/{name}/status`; `GetModelsAsync` calls `GET /api/providers/{name}/models`. Use a mocked `HttpMessageHandler`.
- `ProviderPanelTests` (bUnit): renders numbered provider list; selecting an item raises `OnProviderSelected`; status displays correctly for reachable and unreachable states.
- `ModelPanelTests` (bUnit): renders numbered model list; selecting an item raises `OnModelSelected`; hidden when no provider passed.

### Implement

- Scaffold `CraterClaw.Blazor` with `dotnet new blazorwasm`.
- Add `CraterClaw.Blazor.Tests` with `dotnet new xunit`; add bUnit and `Microsoft.Extensions.Http` packages.
- Add both projects to `CraterClaw.slnx`.
- Implement `Types.cs`, `CraterClawClient.cs`, `ProviderPanel.razor`, `ModelPanel.razor`.
- Wire into `Home.razor` and `App.razor`.
- Register `HttpClient` with `ApiBaseUrl` base address in `Program.cs`.
- Add `wwwroot/appsettings.json` with `"ApiBaseUrl": "http://localhost:5000"`.

### README Sync

- Add `CraterClaw.Blazor` to the Prerequisites and Running sections.
- Document `ApiBaseUrl` in the Configuration section.

### Current Architecture Sync

- Add `CraterClaw.Blazor` section describing the project layout, tech stack, and implemented flows.

### Manual Verification

- Prerequisites: `CraterClaw.Api` running at `http://localhost:5000` with at least one provider configured.
- Run `dotnet run --project CraterClaw.Blazor` and open the browser URL shown.
- Confirm the provider list loads and is numbered.
- Select a provider and confirm status is displayed.
- Confirm the model list appears when the provider is reachable.

---

## Phase 2: Interactive execution

**Status: Done**

### Contract

```csharp
// CraterClawClient
Task<ExecutionResponse> PostExecuteAsync(string providerName, ExecutionRequest request, CancellationToken ct = default);
```

`InteractiveChat` component:
- Accepts `ProviderName` (string) and `ModelName` (string) parameters.
- Displays conversation history (user and assistant turns).
- Textarea input; submit button disabled while request is in flight.
- Appends assistant response on success; displays error on failure.

### Tests

- `CraterClawClientTests`: `PostExecuteAsync` calls `POST /api/providers/{name}/execute` with the correct body.
- `InteractiveChatTests` (bUnit): renders empty history; submitting appends user message then assistant response; disables input during loading; shows error on failure.

### Implement

- Add `PostExecuteAsync` to `CraterClawClient`.
- Implement `InteractiveChat.razor`.
- Wire into `Home.razor`: appears after a model is selected.

### README Sync

- Update the Current State section to include interactive chat.

### Current Architecture Sync

- Update the `CraterClaw.Blazor` section to include `InteractiveChat`.

### Manual Verification

- Prerequisites: Ollama running with at least one model downloaded; `CraterClaw.Api` running.
- Send a message and confirm the assistant response appears.
- Send a follow-up and confirm multi-turn history renders correctly.

---

## Phase 3: Behavior profiles and agentic execution

**Status: Done**

### Contract

```csharp
// CraterClawClient
Task<List<BehaviorProfile>> GetProfilesAsync(CancellationToken ct = default);
Task<AgenticResponse> PostAgenticAsync(string providerName, AgenticRequest request, CancellationToken ct = default);
```

`ProfileSelector` component:
- Numbered list of profiles with name and description.
- Raises `EventCallback<BehaviorProfile>` (`OnProfileSelected`) when a profile is chosen.
- Applies preferred provider/model defaults via output parameters or callbacks; surfaces a warning string when a preferred value is not available.

`AgenticPanel` component:
- Accepts `ProviderName`, `ModelName`, `ProfileId` parameters.
- Textarea prompt input and submit button.
- Displays response content, finish reason, and tools invoked list.
- Disables input while request is in flight.

### Tests

- `CraterClawClientTests`: `GetProfilesAsync` calls `GET /api/profiles`; `PostAgenticAsync` calls `POST /api/providers/{name}/agentic` with correct body.
- `ProfileSelectorTests` (bUnit): renders numbered profile list; selecting raises `OnProfileSelected`.
- `AgenticPanelTests` (bUnit): submits correct request; renders tools invoked list; shows finish reason.

### Implement

- Add `GetProfilesAsync` and `PostAgenticAsync` to `CraterClawClient`.
- Implement `ProfileSelector.razor` and `AgenticPanel.razor`.
- Wire into `Home.razor`: profile selector and agentic panel appear after a model is selected; preferred provider/model defaults applied on profile selection with warnings.

### README Sync

- Update the Current State section to include profile selection and agentic execution.

### Current Architecture Sync

- Update the `CraterClaw.Blazor` section to include `ProfileSelector` and `AgenticPanel`.

### Manual Verification

- Prerequisites: Ollama running; `CraterClaw.Api` running; at least one behavior profile configured.
- Select a profile and confirm any preferred provider/model defaults are applied (or warnings shown).
- Submit a task prompt and confirm response content, finish reason, and tools invoked are displayed.
