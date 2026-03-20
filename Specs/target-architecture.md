# Target Architecture

## Purpose
CraterClaw is a supervised AI system providing provider-backed model execution, curated behavior profiles, and optional tool integrations. It is accessible through a console harness and a web application.

## Components

### CraterClaw.Core (Library)
The shared C# library that owns all orchestration, configuration, provider abstraction, and model workflow logic. All entry points use the same contracts and services; no provider or orchestration logic is duplicated in the console or web layers.

### CraterClaw.Console (Console Harness)
A menu-driven console application for manual use and verification. A thin shell over library services.

### CraterClaw.Api (Web API) [Planned]
A C# API exposing library workflows over HTTP.

### CraterClaw.Web (Vue Frontend) [Planned]
A Vue TypeScript application consuming the C# API, mirroring the console harness flows in a browser UI.

## Provider Model
- Initial provider: Ollama (localhost or LAN).
- Provider implementations are behind stable provider-agnostic abstractions so paid AI providers (e.g. OpenAI, Anthropic) can be added later without broad refactoring.

## Behavior Profiles
Behavior profiles are a fixed catalog of curated combinations of model guidance and permitted tool sets. Users select from predefined profiles. User-defined profile composition is permanently out of scope.

## Tool Integrations
Tools are exposed as Semantic Kernel kernel plugins registered against the agentic execution loop. Initial integration: qBitTorrent. Future candidates include SearXNG, media management, Obsidian, and writing assistant workflows.

## MCP Integration
MCP servers are external dependencies. CraterClaw loads their definitions from configuration and can check availability on demand. It does not deploy, update, or host MCP servers.

## Key Capabilities
- Configure and select provider endpoints
- Check provider status and reachability
- List downloaded models
- Run interactive model sessions
- Run agentic tool-use loops within selected behavior profiles
- Resolve secrets from OS-level environment variables
- Load MCP server definitions and check availability
- Manage torrents via the qBitTorrent plugin

## Deployment [Future]
- Candidate exposure path: Tailscale
- Candidate host model: LXC
- Infrastructure management: Terraform
- Environment management: Nix
- Revisit after library, console, and web scopes are complete
