# CraterClaw Library Model Listing Spec

## Name
- CraterClaw Library Model Listing

## Purpose
- Define contracts and services for retrieving the list of downloaded models available at the active provider endpoint.

## Scope
- Add model listing contracts:
  - `ModelDescriptor`: immutable record with model name, size in bytes, and last-modified timestamp
  - `IModelListingService`: retrieve available models for a given provider endpoint
- Add an Ollama-backed implementation that parses the model list from the `/api/tags` response body.
  - The tag endpoint is already used by `OllamaProviderStatusService` for connectivity checks; the listing implementation parses the same endpoint's JSON payload rather than introducing a new HTTP call path.
- Wire the console harness to display available models after endpoint selection and status check.
- Add automated tests for contract behavior and service parsing without a live Ollama instance.

## Contract Notes
- `ModelDescriptor` is an immutable record and a read-only data transfer type; it carries no behavior.
- `IModelListingService` accepts a `ProviderEndpoint` and returns the models available at that endpoint. Callers resolve the active endpoint via `IProviderConfigurationService` before calling this service.
- An empty model list (provider reachable but no models downloaded) and a parse failure are distinct outcomes. The service should return an empty list for the former and surface an error for the latter.
- Model names returned by Ollama include the tag suffix (e.g., `llama3.2:latest`). The descriptor preserves the name as-is; callers are responsible for display formatting.
- Keep the descriptor minimal. Do not add Ollama-specific fields (digest, parameter count, families) to the shared contract; those belong in a provider-specific extension if needed later.

## Console Integration
- After the status check succeeds, display the list of downloaded model names with their sizes.
- If no models are downloaded, display a clear message rather than an empty list.
- If listing fails, display the error and allow the user to continue with the rest of the flow.

## Manual Verification
- Run the console harness against a configured endpoint that has at least one downloaded model.
- Confirm model names and sizes appear in the output after the status check.
- Confirm that an endpoint with no downloaded models produces a clear empty-list message rather than an error.

## Status
- Planning
