# CraterClaw Spec

## Name
- CraterClaw

## Purpose
- Define the top-level product scope, system boundaries, and shared contracts for a supervised AI system built around provider-backed model execution, curated behavior profiles, and optional MCP integrations.

## Scope
- This is the parent spec for the CraterClaw system.
- It defines the shared constraints and responsibilities for the library, console harness, website, and MCP child specs.
- In scope for the initial system:
    - Selecting an AI provider endpoint, starting with Ollama endpoints on localhost or the LAN
    - Querying provider availability and status
    - Listing downloaded models and requesting model downloads
    - Running interactive model sessions
    - Running scheduled and recurring model tasks
    - Selecting from a fixed catalog of curated behavior profiles
    - Loading MCP server definitions from configuration and verifying availability on demand
    - Exposing the same core capabilities through a console harness and a web application backed by a C# API
    - Implementing the console harness together with the library so manual verification flows are available as the core services are built
- Out of scope for the initial system:
    - Deployment topology, remote exposure, containerization, and host provisioning
    - User-defined creation or editing of behavior profile combinations
    - Deploying, updating, or otherwise managing MCP servers directly
    - Automated tests that require a real Ollama instance or live MCP endpoints

## System Boundaries
- CraterClaw owns orchestration, configuration, provider abstraction, and user-facing access to model workflows.
- CraterClaw does not own MCP server deployment. MCP servers are external dependencies that may exist on the LAN or the public internet.
- Curated behavior profile combinations are a permanent product constraint. Users may select from predefined combinations but may not compose arbitrary tool or agent behavior sets.
- Provider-specific implementations must remain behind provider-agnostic contracts so the system can add paid AI providers later without broad refactoring.

## Shared Contracts
- The system must define stable application contracts for at least the following concepts:
    - Provider endpoint identity and configuration
    - Provider status and health results
    - Model descriptors and model download requests
    - Behavior profile identifiers and behavior profile metadata
    - Interactive model requests and responses
    - Scheduled task definitions, schedules, and execution results
    - MCP server definitions and MCP availability results
- The console harness and web stack must use the same application services and contracts exposed by the library rather than reimplementing model or MCP logic.
- Child specs may add detail, but they must preserve these shared concepts and system boundaries.

## Child Specs
- [CraterClaw/library-spec.md](CraterClaw/library-spec.md): Core application, provider abstraction layer, and console harness
- [CraterClaw/website-spec.md](CraterClaw/website-spec.md): Web API and Vue frontend for user-facing access
- [CraterClaw/mcp-spec.md](CraterClaw/mcp-spec.md): Configuration and availability model for external MCP servers

## Status
- Planning
