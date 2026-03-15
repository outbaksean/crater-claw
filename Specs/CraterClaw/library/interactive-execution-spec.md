# CraterClaw Library Interactive Execution Spec

## Name
- CraterClaw Library Interactive Execution

## Purpose
- Define contracts and services for sending conversational messages to a model at a provider endpoint and receiving a text response, without tool use.

## Scope
- Add interactive execution contracts:
  - `MessageRole`: discriminated value (User, Assistant, System)
  - `ConversationMessage`: immutable record with role and text content
  - `ExecutionRequest`: immutable record with model name, ordered message history, and optional generation parameters (temperature, max tokens)
  - `ExecutionResponse`: immutable record with assistant content, the model name that produced it, and a finish reason (Stop, Length)
  - `IModelExecutionService`: send an execution request to a provider endpoint and return a response
- Add an Ollama-backed implementation using the `/api/chat` endpoint with `stream: false`.
- Wire the console harness to accept a model name and prompt, send a single-turn request against the active endpoint, and display the response.
- Add automated tests for the execution contract and service behavior without a live Ollama instance.

## Contract Notes
- `IModelExecutionService` accepts a `ProviderEndpoint` and an `ExecutionRequest`; it does not resolve the active endpoint internally. The caller provides both.
- The message list in `ExecutionRequest` represents a full conversation turn sequence. Single-turn use is the common case: one User message. Multi-turn use passes the prior exchanges as additional messages.
- Do not include tool definitions or tool call handling in this spec. Those are introduced in the agentic execution spec. The `ExecutionResponse` carries only text content.
- Generation parameters (temperature, max tokens) are optional. Omitting them uses provider defaults.
- The Ollama implementation must not reuse the `HttpClient` instance registered for status checks without going through DI. Register a named or typed client for model execution to avoid sharing state.

## Console Integration
- After the model list is displayed, prompt the user to enter a model name and then a prompt message.
- Display the assistant response after execution completes.
- Handle and display errors (model not found, provider unreachable) without crashing.

## Manual Verification
- Dependencies: at least one model must be downloaded on the active Ollama endpoint.
- Run the console harness, select an endpoint, and enter a simple prompt.
- Confirm the assistant response appears in the output.
- Confirm that specifying a model name that does not exist produces a clear error.

## Status
- Done
