# Web API Spec

## Name
- CraterClaw Web API

## Purpose
- Expose CraterClaw library workflows over HTTP so a frontend client can access provider status, models, execution, behavior profiles, and MCP availability without duplicating any core logic.

## Scope
- Add a `CraterClaw.Api` ASP.NET Core minimal API project (.NET 10) to the solution.
- Add a `CraterClaw.Api.Tests` xUnit test project.
- Load `craterclaw.json` and user secrets using the same pattern as the console harness.
- Register `AddCraterClawCore` so the API uses the same services and configuration types.
- Configure a permissive CORS policy for development (all origins, headers, and methods).
- Expose the following endpoints (all under `/api`):

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/providers` | List configured endpoint names and URLs |
| GET | `/api/providers/{name}/status` | Check reachability of a named endpoint |
| GET | `/api/providers/{name}/models` | List downloaded models at a named endpoint |
| POST | `/api/providers/{name}/execute` | Run an interactive execution against a named endpoint |
| GET | `/api/profiles` | List all behavior profiles |
| GET | `/api/mcp` | List configured MCP server names and labels |
| POST | `/api/mcp/{name}/availability` | Check availability of a named MCP server |
| POST | `/api/providers/{name}/agentic` | Run an agentic tool-use task against a named endpoint |

- Return `404` when a named provider endpoint or MCP server is not found in configuration.
- Return `400` when a referenced behavior profile ID does not exist.
- Agentic execution resolves the plugin list from the selected profile's `AllowedMcpServerNames` using the same mapping logic as the console harness. `StreamChunk` is `null` (no streaming; the full response is returned).

## Contract Notes

### Request / Response shapes

**GET /api/providers**
```json
[{ "name": "string", "baseUrl": "string" }]
```

**GET /api/providers/{name}/status**
```json
{ "isReachable": true, "errorMessage": null }
```

**GET /api/providers/{name}/models**
```json
[{ "name": "string", "sizeBytes": 0, "modifiedAt": "ISO8601" }]
```

**POST /api/providers/{name}/execute** (request)
```json
{ "modelName": "string", "messages": [{ "role": "user|assistant", "content": "string" }], "temperature": null, "maxTokens": null }
```
**POST /api/providers/{name}/execute** (response)
```json
{ "content": "string", "modelName": "string", "finishReason": "Completed|Length" }
```

**GET /api/profiles**
```json
[{ "id": "string", "name": "string", "description": "string", "recommendedModelTags": [], "allowedMcpServerNames": [] }]
```

**GET /api/mcp**
```json
[{ "name": "string", "label": "string", "enabled": true }]
```

**POST /api/mcp/{name}/availability** (response)
```json
{ "name": "string", "isAvailable": true, "errorMessage": null }
```

**POST /api/providers/{name}/agentic** (request)
```json
{ "modelName": "string", "prompt": "string", "profileId": "string", "maxIterations": 10 }
```
**POST /api/providers/{name}/agentic** (response)
```json
{ "content": "string", "finishReason": "Completed|IterationLimitReached", "toolsInvoked": ["string"] }
```

### Test setup
- `CraterClaw.Api.Tests` uses `WebApplicationFactory<Program>` with service overrides (manual fakes, no mocking library).
- In-memory configuration must supply at least one valid provider endpoint and one MCP server entry to pass `ProviderOptionsValidator` and `McpOptionsValidator`. `QBitTorrentOptions` validation passes when `BaseUrl` is empty.
- Each test class that needs a custom service fake overrides it via `WebApplicationFactory.WithWebHostBuilder(b => b.ConfigureServices(...))`.

---

## Phase 1: Scaffold + provider endpoints
**Status: Done**

### Scope
- Add `CraterClaw.Api` to the solution: ASP.NET Core minimal API, .NET 10, `Nullable enable`, `ImplicitUsings enable`.
- Load `craterclaw.json` (required) and user secrets.
- Call `AddCraterClawCore(configuration)`.
- Configure CORS: in development, allow all origins, headers, and methods.
- Implement `GET /api/providers` and `GET /api/providers/{name}/status`.
- Add `CraterClaw.Api.Tests` to the solution: xUnit, .NET 10, references `CraterClaw.Api`.
- Tests:
  - `GET /api/providers` returns the list of configured endpoints.
  - `GET /api/providers/{name}/status` returns the result from `IProviderStatusService`.
  - `GET /api/providers/{unknownName}/status` returns `404`.

### Manual Verification
- Prerequisites: none beyond a valid `craterclaw.json` with at least one endpoint.
- Run `CraterClaw.Api` and confirm `GET /api/providers` returns the configured endpoint list.
- Confirm `GET /api/providers/{validName}/status` returns a reachable or unreachable result.
- Confirm `GET /api/providers/unknown/status` returns `404`.

---

## Phase 2: Models + interactive execution
**Status: Done**

### Scope
- Implement `GET /api/providers/{name}/models` using `IModelListingService`.
- Implement `POST /api/providers/{name}/execute` using `IModelExecutionService`.
  - Deserialize the request body into `ExecutionRequest` (map `role` string to `MessageRole` enum).
  - Serialize `ExecutionResponse` fields into the response shape defined above.
- Tests:
  - `GET /api/providers/{name}/models` returns model descriptors from the fake listing service.
  - `POST /api/providers/{name}/execute` returns execution response from the fake execution service.
  - Both return `404` for an unknown endpoint name.

### Manual Verification
- Prerequisites: Ollama running with at least one model downloaded.
- Confirm `GET /api/providers/{name}/models` returns the model list.
- Confirm `POST /api/providers/{name}/execute` with a simple prompt returns a non-empty response.

---

## Phase 3: Profiles, MCP, and agentic execution
**Status: Done**

### Scope
- Implement `GET /api/profiles` using `IBehaviorProfileService.GetAll()`.
- Implement `GET /api/mcp` from `McpOptions.Servers` (name, label, enabled).
- Implement `POST /api/mcp/{name}/availability` using `IMcpAvailabilityService`.
  - Resolve the `McpServerDefinition` from `McpOptions.Servers`; return `404` if not found.
- Implement `POST /api/providers/{name}/agentic` using `IAgenticExecutionService`.
  - Resolve the profile via `IBehaviorProfileService.GetById(profileId)`; return `400` if not found.
  - Build the plugins list: inject `QBitTorrentPlugin`; include it when `AllowedMcpServerNames` is non-empty (same logic as the console harness).
  - Pass `StreamChunk: null` to `AgenticRequest`.
- Tests:
  - `GET /api/profiles` returns all profiles from the fake profile service.
  - `GET /api/mcp` returns configured server names and labels.
  - `POST /api/mcp/{name}/availability` returns the availability result; `404` for unknown name.
  - `POST /api/providers/{name}/agentic` returns the agentic response; `400` for unknown profile; `404` for unknown endpoint.

### Manual Verification
- Prerequisites: none beyond a valid `craterclaw.json`.
- Confirm `GET /api/profiles` returns the profile catalog.
- Confirm `GET /api/mcp` returns configured servers.
- Confirm `POST /api/mcp/{name}/availability` returns an availability result.
- Prerequisites for agentic: Ollama running, a model selected, qBitTorrent running with credentials in user secrets.
- Confirm `POST /api/providers/{name}/agentic` with the `qbittorrent-manager` profile returns a response and lists tools invoked.
