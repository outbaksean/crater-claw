# Spec: api-controller-separation

## Goal

Refactor `CraterClaw.Api/Program.cs` so that endpoint handlers live in separate files grouped by domain, request/response model types live in a dedicated models file, and a `ProviderResolver` helper service centralises the repeated provider-lookup pattern. `Program.cs` retains only startup and DI wiring. No behavioral changes — existing API tests must remain green throughout.

## Scope

Only `CraterClaw.Api` and `CraterClaw.Api.Tests` are affected. `CraterClaw.Core` is unchanged.

---

## Phase 1: Extract models and endpoint groups

**Status: Done**

### Context

`Program.cs` currently contains:

- All six endpoint handlers inline
- Eight request/response record types at the bottom of the file

### Contract

Two static endpoint-registration classes are introduced as extension methods on `WebApplication`. The existing record types move to a single models file. `Program.cs` becomes startup-only.

**New files:**

`CraterClaw.Api/Models/ApiModels.cs`

- Contains all eight record types moved verbatim from `Program.cs`:
  `ProviderEndpointResponse`, `ProviderStatusResponse`, `ModelApiItem`,
  `ExecutionApiRequest`, `MessageApiItem`, `ExecutionApiResponse`,
  `AgenticApiRequest`, `AgenticApiResponse`, `PluginBindingApiItem`,
  `BehaviorProfileApiItem`

`CraterClaw.Api/Endpoints/ProvidersEndpoints.cs`

- `internal static class ProvidersEndpoints` with a single public method:
  `public static void MapProviderEndpoints(this WebApplication app)`
- Registers: `GET /api/providers`, `GET /api/providers/{name}/status`,
  `GET /api/providers/{name}/models`, `POST /api/providers/{name}/execute`,
  `POST /api/providers/{name}/agentic`

`CraterClaw.Api/Endpoints/ProfilesEndpoints.cs`

- `internal static class ProfilesEndpoints` with a single public method:
  `public static void MapProfileEndpoints(this WebApplication app)`
- Registers: `GET /api/profiles`

**Updated `Program.cs`:**

- Retains all startup and DI wiring unchanged
- Replaces the inline endpoint blocks with two calls:
    ```csharp
    app.MapProviderEndpoints();
    app.MapProfileEndpoints();
    ```
- Record type definitions removed (now in `ApiModels.cs`)

### Tests

No new tests required for this phase. All existing `CraterClaw.Api.Tests` tests must pass without modification — the external API contract is unchanged.

### Implement

1. Create `CraterClaw.Api/Models/ApiModels.cs` with all record types from the bottom of `Program.cs`. Preserve `internal sealed record` access and sealing.
2. Create `CraterClaw.Api/Endpoints/ProvidersEndpoints.cs`. Copy the five provider endpoint lambdas verbatim into `MapProviderEndpoints`. Add the required `using` directives.
3. Create `CraterClaw.Api/Endpoints/ProfilesEndpoints.cs`. Copy the profiles endpoint lambda into `MapProfileEndpoints`. Add the required `using` directives.
4. Edit `Program.cs`: remove record type definitions, remove inline endpoint blocks, add `app.MapProviderEndpoints();` and `app.MapProfileEndpoints();` after `app.UseCors();`.
5. Build and verify: `dotnet build CraterClaw.slnx`
6. Run tests: `dotnet test CraterClaw.slnx`

### README Sync

No user-visible changes. README requires no update.

### Current Architecture Sync

Update `current-architecture.md` to reflect the new file layout under `CraterClaw.Api`.

### Manual Verification Plan

Dependencies: API must be running (`craterclaw run`).

1. Start the API.
2. `GET /api/providers` — returns provider list.
3. `GET /api/providers/{name}/status` — returns reachability result.
4. `GET /api/providers/{name}/models` — returns model list.
5. `POST /api/providers/{name}/execute` with a valid request — returns a response.
6. `GET /api/profiles` — returns profile list.
7. `POST /api/providers/{name}/agentic` with a valid request — returns agentic response.

---

## Phase 2: ProviderResolver helper service

**Status: Done**

### Context

Four of the six endpoints contain an identical pattern:

```csharp
if (!opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
    return Results.NotFound();
var endpoint = new ProviderEndpoint(name, endpointOpts.BaseUrl);
```

This pattern should be encapsulated in a helper service registered in DI, so endpoint handlers do not reference `IOptions<ProviderOptions>` directly and the lookup logic has a single, testable home.

### Contract

**New interface and implementation:**

`CraterClaw.Api/Services/IProviderResolver.cs`

```csharp
internal interface IProviderResolver
{
    ProviderEndpoint? Resolve(string name);
}
```

`CraterClaw.Api/Services/ProviderResolver.cs`

```csharp
internal sealed class ProviderResolver : IProviderResolver
{
    private readonly IOptions<ProviderOptions> _opts;

    public ProviderResolver(IOptions<ProviderOptions> opts) => _opts = opts;

    public ProviderEndpoint? Resolve(string name)
    {
        if (!_opts.Value.Endpoints.TryGetValue(name, out var endpointOpts))
            return null;
        return new ProviderEndpoint(name, endpointOpts.BaseUrl);
    }
}
```

**DI registration in `Program.cs`:**

```csharp
builder.Services.AddSingleton<IProviderResolver, ProviderResolver>();
```

**Updated `ProvidersEndpoints.cs`:**

- All four affected endpoint handlers replace `IOptions<ProviderOptions>` injection with `IProviderResolver resolver`
- The TryGetValue pattern is replaced with:
    ```csharp
    var endpoint = resolver.Resolve(name);
    if (endpoint is null)
        return Results.NotFound();
    ```

### Tests

New test file: `CraterClaw.Api.Tests/ProviderResolverTests.cs`

Test cases (xUnit):

- `Resolve_KnownProvider_ReturnsEndpoint` — configured provider returns a `ProviderEndpoint` with correct name and URL
- `Resolve_UnknownProvider_ReturnsNull` — unknown name returns `null`
- `Resolve_EmptyEndpoints_ReturnsNull` — empty endpoint map returns `null`

All existing `CraterClaw.Api.Tests` tests must continue to pass.

### Implement

1. Create `CraterClaw.Api/Services/IProviderResolver.cs` and `CraterClaw.Api/Services/ProviderResolver.cs` per the contract above.
2. Register `IProviderResolver` as singleton in `Program.cs`.
3. Update the four affected handlers in `ProvidersEndpoints.cs` to inject and use `IProviderResolver`.
4. Create `CraterClaw.Api.Tests/ProviderResolverTests.cs` with the three test cases.
5. Build and run all tests: `dotnet test CraterClaw.slnx`

### README Sync

No user-visible changes. README requires no update.

### Current Architecture Sync

Update `current-architecture.md` to document `IProviderResolver` and its role in the API layer.

### Manual Verification Plan

Dependencies: API must be running.

Same verification steps as Phase 1. The external behavior is unchanged; verify no regressions.
