# CraterClaw Library Model Catalog Spec

## Name
- CraterClaw Library Model Catalog

## Purpose
- Define the next library slice after provider configuration: listing available models from the active provider and requesting model downloads through provider-agnostic contracts.

## Scope
- Add model catalog contracts to represent:
  - Installed or available model metadata from a provider
  - Model pull/download requests and outcomes
- Add library services to:
  - List models from the active configured provider endpoint
  - Request model pull/download by model identifier
- Reuse existing provider configuration and active endpoint selection from the provider-config slice.
- Add Ollama-backed implementation for:
  - Listing models from `/api/tags`
  - Pulling models via Ollama pull endpoint
- Add unit tests using mocked HTTP behavior only (no live Ollama dependency).
- Add minimal console harness integration to:
  - Show the active provider
  - List models
  - Trigger a model pull request
  - Print clear success/failure output

## Acceptance Focus
- Model listing works against the selected active provider endpoint.
- Model pull requests are routed through library contracts, not console-specific logic.
- Automated tests cover successful and failure paths with mocked HTTP.
- Console output is clear enough for manual verification of list/pull workflows.

## Out Of Scope
- Interactive inference prompt execution
- Scheduled execution and recurring tasks
- Behavior profile selection
- MCP server loading or availability checks
- Advanced model management (delete, update strategy, caching policies)

## Contract Notes
- Keep model contracts provider-agnostic so additional providers can be introduced later without changing caller code.
- Preserve contract-first implementation: contracts and tests must exist before service logic.
- Keep console logic as orchestration/UI only; model operation logic remains in `CraterClaw.Core`.

## Manual Verification
- With a valid active provider endpoint, list models and confirm output displays model names.
- Request a pull for a model identifier and confirm success/failure messaging.
- Confirm failures (invalid model or unreachable provider) return readable error output.

## Status
- Planning
