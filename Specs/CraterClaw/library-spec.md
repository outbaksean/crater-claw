# CraterClaw Library Spec

## Name
- CraterClaw Library

## Purpose
- Define the provider-agnostic core services that manage provider selection, model operations, behavior profile selection, interactive execution, scheduled execution, and MCP-aware orchestration, along with the console harness used to exercise those services.

## Scope
- Implement the core C# library used by all CraterClaw entry points.
- Implement the C# console harness in the same workstream so manual verification is available alongside core service development.
- Support provider endpoint selection, starting with Ollama endpoints on localhost or the LAN.
- Expose contracts and services for:
  - Provider configuration and provider status checks
  - Model listing and model download requests
  - Interactive model execution
  - Scheduled and recurring model execution
  - Selection of curated behavior profiles
  - Loading configured MCP definitions and requesting availability checks
- Provide a menu-driven console experience for manually exercising those library workflows, including:
  - Selecting a configured provider endpoint
  - Viewing provider status
  - Listing available or downloaded models
  - Triggering model downloads
  - Running interactive model requests
  - Creating or triggering scheduled and recurring tasks
  - Selecting curated behavior profiles
  - Listing configured MCP servers and requesting availability checks
- Keep provider implementations behind stable abstractions so future paid AI providers can be added without broad changes to calling code.
- Keep business logic in the shared library. The console harness should remain a thin interaction layer over library services.
- Exclude the web UI, deployment automation, and MCP server lifecycle management.

## Contract Notes
- The library is the source of truth for shared application contracts used by the console harness and web stack.
- Behavior profiles are predefined application data with stable identifiers; the library must support selection, not authoring.
- Automated tests must rely on doubles, fakes, or fixtures rather than a real Ollama instance.
- The console harness supports manual verification but does not replace automated tests for library contracts.

## Child Specs
- [CraterClaw/library/bootstrap-spec.md](CraterClaw/library/bootstrap-spec.md): Minimal testable bootstrap slice for library scaffolding, console harness scaffolding, and basic Ollama connectivity
- [CraterClaw/library/provider-config-spec.md](CraterClaw/library/provider-config-spec.md): Provider endpoint configuration, active endpoint selection, and persistence for connectivity checks

## Status
- Planning
