# CraterClaw Library Model Listing Plan

## Decisions

- `ModelDescriptor` carries name, size in bytes, and last-modified timestamp. No Ollama-specific fields (digest, families, parameter count) are included in the shared contract.
- The Ollama implementation calls `GET /api/tags` and parses the response body. This is the same endpoint used by `OllamaProviderStatusService`; no new HTTP call path is introduced.
- Size is stored as `long` bytes in the contract. The console harness is responsible for human-readable formatting (e.g., GB/MB).
- An empty model list is a valid successful result, not an error. A malformed or unreadable response is an error.
- `IModelListingService` is registered as Transient in `AddCraterClawCore()`, following the same pattern as `IProviderStatusService`.

## Overview
- This plan implements the model listing spec in two phases.
- Phase 1 defines the contract and tests. Phase 2 adds the Ollama implementation, DI registration, and console wiring.
- Phases execute in order.

---

## Phase 1: Contract and Tests

### Status
- Done

### Goal
- Define the `ModelDescriptor` record and `IModelListingService` interface, and establish automated tests that will drive the Phase 2 implementation.

### Contract
- `ModelDescriptor` record in `CraterClaw.Core`:
  - `Name` (string): model name including tag suffix, e.g. `llama3.2:latest`
  - `SizeBytes` (long): model size in bytes
  - `ModifiedAt` (DateTimeOffset): timestamp of last modification
- `IModelListingService` interface in `CraterClaw.Core`:
  - `Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)`

### Tasks
- Add `ModelDescriptor.cs` as a sealed record in `CraterClaw.Core`.
- Add `IModelListingService.cs` in `CraterClaw.Core`.
- Add `OllamaModelListingServiceTests.cs` in `CraterClaw.Core.Tests` using a fake `HttpMessageHandler` (follow the `DelegatingTestHandler` pattern from `OllamaProviderStatusServiceTests`).

### Tests
- Returns a correctly populated `ModelDescriptor` list for a valid `/api/tags` JSON response with multiple models.
- Returns an empty list for a valid response with an empty `models` array.
- Surfaces a meaningful error (exception or failed result) for a response with malformed JSON.
- Propagates cancellation when the token is cancelled before the HTTP call completes.

### Ollama `/api/tags` Response Shape (for test fixtures)
```json
{
  "models": [
    {
      "name": "llama3.2:latest",
      "size": 2019393189,
      "modified_at": "2024-10-21T14:30:00Z"
    }
  ]
}
```

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with the new tests included.

---

## Phase 2: Ollama Implementation, DI Registration, and Console Wiring

### Status
- Done

### Goal
- Implement `OllamaModelListingService`, register it in DI, and update the console harness to display available models after a successful status check.

### Contract
- No new public surface beyond Phase 1.
- Internal implementation parses the `/api/tags` response body into `ModelDescriptor` records.
- Console output shows model name and human-readable size for each model; shows a distinct message when the list is empty; shows the error message and continues the flow if listing fails.

### Tasks
- Add `OllamaModelListingService.cs` (internal, sealed) in `CraterClaw.Core`:
  - Inject `HttpClient` via constructor (same pattern as `OllamaProviderStatusService`).
  - Call `GET {endpoint.BaseUrl}/api/tags`.
  - Deserialize the response using a private DTO matching the Ollama response shape.
  - Map to `ModelDescriptor` records.
  - Return empty list for an empty `models` array.
  - Throw `InvalidOperationException` with a descriptive message for malformed JSON.
- Register `IModelListingService` as Transient → `OllamaModelListingService` in `ServiceCollectionExtensions.AddCraterClawCore()`.
- Update `CraterClaw.Console/Program.cs`:
  - After a successful status check, call `ListModelsAsync` on the active endpoint.
  - If models are returned, display each as `{name}  ({size})` where size is formatted as the largest clean unit (GB if >= 1 GB, otherwise MB).
  - If the list is empty, display `No models downloaded on this endpoint.`
  - If listing fails, display the error message and continue.

### Size Formatting Reference
- >= 1,073,741,824 bytes: display as `X.X GB`
- < 1,073,741,824 bytes: display as `X.X MB`

### Tests
- No new tests required in this phase; Phase 1 tests cover the service behavior.

### Manual Verification Plan
- Dependencies: at least one model must be downloaded on the active Ollama endpoint.
- Run the console harness with a configured provider endpoint that has at least one downloaded model.
- Confirm model names and human-readable sizes appear after the status check.
- Run against an endpoint with no downloaded models and confirm the empty-list message appears.

---

## Completion Criteria
- Both phase statuses are marked Done.
- `CraterClaw.Core` and `CraterClaw.Console` build successfully.
- Automated tests for the listing contract and service behavior pass.
- Manual verification confirms model names and sizes display correctly.
- `model-listing-spec.md` Status is updated to Done.
