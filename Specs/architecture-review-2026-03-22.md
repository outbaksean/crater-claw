# Architecture Review — 2026-03-22

## Scope

This review focuses on architectural quality: boundaries, composition, validation strategy, extensibility, and operational risk.

## Findings (ordered by severity)

### 1. High — Behavior and plugin configuration is not fail-fast validated

Evidence:

- `BehaviorEntry` and `PluginEntry` are unvalidated POCOs with default empty strings/lists (`CraterClaw.Core/BehaviorConfigTypes.cs`).
- Behavior config binding is registered without startup validation (`CraterClaw.Core/ServiceCollectionExtensions.cs:27`).
- `BehaviorProfileService` projects config directly into runtime profiles with no guardrails (`CraterClaw.Core/BehaviorProfileService.cs:11`).
- Plugin creation relies on string dictionary keys and can degrade silently (warnings, then continue) (`CraterClaw.Core/DefaultPluginRegistry.cs:22`, `CraterClaw.Core/DefaultPluginRegistry.cs:39`).

Why this matters:

- Misconfigured behaviors are discovered at runtime, often in the middle of a session.
- Empty/mistyped plugin settings can produce partially functional profiles with weak observability.
- The architecture currently trusts config shape too far into execution.

Recommendation:

- Add `IValidateOptions<Dictionary<string, BehaviorEntry>>` (or bind to a dedicated options type and validate with data annotations/custom validator).
- Validate each behavior has non-empty `Name`, `Description`, `SystemPrompt`.
- Validate each plugin binding has a known plugin name and plugin-specific required keys.
- Upgrade plugin resolution failures from warning-and-continue to deterministic policy (either fail profile load or mark profile invalid and exclude it from selection).

---

### 2. Medium — Provider abstraction exists at interface level, but composition is Ollama-hardwired

Evidence:

- Interfaces are provider-agnostic (`IProviderStatusService`, `IModelListingService`, `IModelExecutionService`).
- DI always wires Ollama implementations (`CraterClaw.Core/ServiceCollectionExtensions.cs:30`, `CraterClaw.Core/ServiceCollectionExtensions.cs:31`, `CraterClaw.Core/ServiceCollectionExtensions.cs:32`).
- Kernel creation is directly coupled to Ollama SK integration (`CraterClaw.Core/DefaultKernelFactory.cs:14`).

Why this matters:

- Adding a second provider requires editing core composition and possibly shared execution services.
- The architecture target says future providers should be addable without broad refactoring; current composition is close, but not there yet.

Recommendation:

- Introduce a provider runtime registry keyed by provider type/name (factory pattern) and bind provider type in endpoint config.
- Move provider-specific kernel construction behind provider adapters.
- Keep `IAgenticExecutionService` and API endpoints provider-agnostic by consuming adapter contracts only.

---

### 3. Medium — Composition root responsibilities are duplicated and drifting across entry points

Evidence:

- Console and API both implement config-path override parsing independently (`CraterClaw.Console/Program.cs:23`, `CraterClaw.Api/Program.cs:16`).
- Console and API both implement AI log path resolution independently (`CraterClaw.Console/Program.cs:35`, `CraterClaw.Api/Program.cs:44`).
- Console startup and interaction are concentrated in one large `Program.cs` (`CraterClaw.Console/Program.cs`).
- API endpoint mapping and DTO definitions are concentrated in one large `Program.cs` (`CraterClaw.Api/Program.cs:90`, `CraterClaw.Api/Program.cs:190`).

Why this matters:

- Behavior drift risk increases as features evolve (different defaults, different edge-case handling).
- Cross-cutting concerns (configuration precedence, logging conventions) are harder to keep consistent.
- Testability of startup policy and request mapping declines over time.

Recommendation:

- Extract shared startup concerns into `CraterClaw.Core` or a dedicated shared infrastructure package:
    - config path policy
    - ai log path policy
    - common host configuration helpers
- Split console flow into orchestrator services and keep `Program.cs` as composition only.
- Split API endpoints into feature modules (extension methods per feature area).

---

### 4. Medium — API network boundary posture is unsafe for accidental exposure

Evidence:

- CORS policy allows any origin/header/method in default pipeline (`CraterClaw.Api/Program.cs:82`, `CraterClaw.Api/Program.cs:84`).
- No authentication or authorization layer is present in API host.

Why this matters:

- Architecture currently assumes local-only trust. If host/network posture changes, risk increases sharply.

Recommendation:

- Keep permissive CORS only under explicit Development environment.
- Add an explicit deployment profile for LAN/remote usage with at least one authentication strategy (API key or bearer token), even if gated behind config.

---

### 5. Low — Validator exists but is not integrated into startup validation pipeline

Evidence:

- `QBitTorrentOptionsValidator` exists (`CraterClaw.Core/QBitTorrentOptionsValidator.cs:5`) but there is no corresponding options registration path for it.

Why this matters:

- Creates a false sense of safety and makes validation approach inconsistent.

Recommendation:

- Either remove the unused validator to reduce dead architecture surface, or wire it into a real options binding path used by plugin settings.

## Strengths

- Strong project separation by delivery surface: core library, console harness, API, and web client.
- Core contracts are clear and support testability at service boundaries.
- Plugin function allowlisting in behavior profiles is a good safety boundary.
- API and core both have meaningful automated test suites in place.

## Suggested execution order

1. Add behavior/plugin validation and fail-fast startup behavior.
2. Extract shared startup policy to eliminate console/API drift.
3. Refactor provider composition to adapter/registry model.
4. Tighten API exposure defaults (environment-scoped CORS + auth strategy).
5. Remove or integrate orphan validators.

## Residual risk if unchanged

- Most likely near-term failures remain runtime configuration surprises and cross-entrypoint drift.
- Most likely long-term cost is provider extensibility refactoring pressure once non-Ollama providers are added.
