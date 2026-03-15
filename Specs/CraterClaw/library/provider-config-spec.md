# CraterClaw Library Provider Configuration Spec

## Name
- CraterClaw Library Provider Configuration

## Purpose
- Define the implementation slice that adds provider endpoint configuration management and active endpoint selection on top of the completed bootstrap connectivity contracts.

## Scope
- Add provider configuration contracts for multiple named endpoints.
- Add library services to:
  - Load and save provider configuration
  - List configured provider endpoints
  - Resolve the active provider endpoint
  - Set the active provider endpoint by name
- Validate configuration rules:
  - Endpoint names are unique (case-insensitive)
  - Endpoint base URLs are required absolute URLs
  - Active provider name must match a configured endpoint when present
- Add a minimal file-backed JSON configuration implementation in `CraterClaw.Core` for local development and test fixtures.
- Wire the console harness to:
  - Accept a configuration file path
  - Load configured endpoints
  - Show available endpoint names
  - Let the user select the active endpoint
  - Persist active endpoint selection
  - Run the existing `IProviderStatusService` connectivity check against the active endpoint
- Add automated tests for configuration contracts and service behavior without depending on a live Ollama instance.

## Acceptance Focus
- Provider endpoint configuration can be loaded from JSON without hardcoded endpoints in the console app.
- Active provider endpoint can be changed and persisted.
- Connectivity checks run against the selected active endpoint through existing status contracts.
- Automated tests validate parsing, validation, selection updates, persistence, and error handling.

## Out Of Scope
- Model listing and model download operations
- Interactive prompt execution
- Scheduled and recurring task execution
- Behavior profile selection
- MCP server loading or availability checks
- Multi-provider request/response adapters beyond endpoint metadata and selection

## Contract Notes
- Reuse bootstrap contracts (`ProviderEndpoint`, `ProviderStatus`, `IProviderStatusService`) and avoid duplicating provider status logic.
- Keep business logic in `CraterClaw.Core`; the console app remains a thin interaction layer.
- Keep the JSON schema intentionally small so later child specs can extend it safely.

## Manual Verification
- Provide a config file with at least two endpoints.
- Run the console harness with that config file.
- Select an endpoint by name and confirm connectivity checks use that endpoint.
- Change selection to another endpoint and confirm behavior updates.
- Restart the console harness and confirm active selection persistence.

## Status
- Done
