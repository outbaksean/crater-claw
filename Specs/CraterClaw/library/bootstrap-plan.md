# CraterClaw Library Bootstrap Plan

## Decisions

- Semantic Kernel is excluded from the bootstrap. The connectivity check is a direct HTTP call and does not benefit from it. Semantic Kernel may be introduced in a later child spec if it materially supports a specific capability.
- Dependency injection uses `Microsoft.Extensions.DependencyInjection`. The console app constructs a `ServiceCollection`, registers `IProviderStatusService` and `HttpClient`, and resolves the service from the built provider. This approach is consistent with how the future web stack will wire dependencies.

## Overview
- This plan implements the bootstrap spec across three phases.
- Each phase is sized to be fully implemented, tested, and verified in one AI session.
- Phases must be executed in order. Phase 2 depends on Phase 1 contracts. Phase 3 depends on Phase 2 services.

---

## Phase 1: Solution Scaffold and Provider Contracts

### Status
- Done

### Goal
- Produce a building solution with three projects and the minimum contracts needed for an Ollama status check.

### Contract
- `ProviderEndpoint`: record or immutable class with `Name` (string) and `BaseUrl` (string).
- `ProviderStatus`: record or immutable class with `IsReachable` (bool) and `ErrorMessage` (string?).
- `IProviderStatusService`: single method `Task<ProviderStatus> CheckStatusAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)`.
- These three types are the only public surface added in this phase. No implementations yet.

### Tasks
- Create a .NET 10 solution file named `CraterClaw.slnx`.
- Add the core library project (`CraterClaw.Core`) targeting .NET 10.
- Add the console harness project (`CraterClaw.Console`) targeting .NET 10.
- Add the xUnit test project (`CraterClaw.Core.Tests`) targeting .NET 10.
- Wire test project reference to the library project.
- Wire console project reference to the library project.
- Define `ProviderEndpoint`, `ProviderStatus`, and `IProviderStatusService` in the library project.
- Confirm the solution restores and builds with zero code in the implementation layer.

### Tests
- Verify `ProviderEndpoint` can be constructed with a name and base URL and exposes both values.
- Verify `ProviderStatus` can represent a success state (IsReachable true, no error) and a failure state (IsReachable false, error message present).
- These tests must pass without any implementation beyond the types themselves.

### Manual Verification
- Run `dotnet build` from the solution root and confirm it succeeds with no errors.
- Run the xUnit tests and confirm both contract tests pass.
- Confirm `CraterClaw.Core`, `CraterClaw.Console`, and `CraterClaw.Core.Tests` all target .NET 10.

---

## Phase 2: Ollama Provider Implementation and Unit Tests

### Status
- Done

### Goal
- Implement `IProviderStatusService` against the Ollama `/api/tags` health endpoint using mocked HTTP behavior in tests, with no dependency on a live instance.

### Contract
- No new public surface. All work is behind `IProviderStatusService`.
- Internal implementation class `OllamaProviderStatusService` takes an `HttpClient` through constructor injection.
- HTTP GET to `{BaseUrl}/api/tags` — a 200 response is treated as reachable; any exception or non-2xx response is treated as unreachable with a descriptive error message.

### Tasks
- Add `OllamaProviderStatusService` as an internal class in `CraterClaw.Core`.
- Implement `CheckStatusAsync` according to the contract above.
- Register `IProviderStatusService` to `OllamaProviderStatusService` using `Microsoft.Extensions.DependencyInjection` in a static extension method on `IServiceCollection` within the library (e.g. `AddCraterClawCore()`), so the console and future web host can register it with one call.
- Semantic Kernel is not used in this phase per the Decisions section above.

### Tests
- Successful connectivity: mock `HttpMessageHandler` returns 200; assert `IsReachable` is true and `ErrorMessage` is null.
- Unreachable host: mock handler throws `HttpRequestException`; assert `IsReachable` is false and `ErrorMessage` is non-empty.
- Non-2xx response: mock handler returns 503; assert `IsReachable` is false and `ErrorMessage` is non-empty.
- Cancellation: mock handler respects a cancelled `CancellationToken`; assert the method propagates or wraps the cancellation appropriately without hanging.
- All four tests must pass without a real Ollama instance.

### Manual Verification
- No console harness wiring yet. Verify only via automated tests passing.

---

## Phase 3: Console Harness Wiring and End-to-End Manual Verification

### Status
- Done

### Goal
- Wire the console harness to accept an Ollama endpoint and run the library connectivity check, producing readable output for both reachable and unreachable targets.

### Contract
- No changes to library contracts or implementations.
- The console app reads a base URL from a command-line argument or prompts the user for one if none is provided.
- The output must clearly distinguish reachable from unreachable on the console.

### Tasks
- In `CraterClaw.Console`, create a `ServiceCollection`, call `AddCraterClawCore()`, register `HttpClient`, and build the service provider.
- Resolve `IProviderStatusService` from the provider — no direct reference to `OllamaProviderStatusService` in the console project.
- Accept a base URL from a command-line argument; prompt the user if none is provided.
- Call `CheckStatusAsync` and print the result clearly.
- Handle and display unexpected exceptions without crashing silently.

### Tests
- No new automated tests in this phase. Console wiring is thin by design and covered by Phase 2 unit tests.

### Manual Verification Plan
- Start the harness with a known reachable Ollama endpoint and confirm the output indicates success.
- Start the harness with an unreachable or invalid URL and confirm the output clearly indicates failure with a useful message.
- Confirm the library project has no reference to console-specific types, keeping the dependency direction correct.

### Session Verification Notes
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes (6/6).
- `dotnet run --project CraterClaw.Console -- http://localhost:1` returns an explicit unreachable result and error message.
- User confirmed manual verification is complete and signed off.

### Spec Sync
- If the endpoint acceptance mechanism (args vs. prompt vs. config file) is changed during implementation, update this plan and the bootstrap spec to reflect the final approach.

---

## Completion Criteria
- All Phase 1, 2, and 3 status fields are marked Done.
- The solution builds cleanly.
- All automated tests pass without a real Ollama instance.
- Manual verification in Phase 3 has been completed and signed off by the user.
- The bootstrap-spec.md Status field is updated to Done.
