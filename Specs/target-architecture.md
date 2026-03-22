# Target Architecture

## Purpose

CraterClaw is a local-first, supervised natural-language orchestration layer over explicitly approved tools and services. It provides provider-backed model execution, curated behavior profiles, and optional tool integrations behind strict capability boundaries. It is accessible through a console harness and a web application.

The initial domain is media management, but media is not the long-term boundary of the system. CraterClaw is intended to grow into a unified interface over multiple personal and homelab domains such as media, calendar, notes, and other local services where natural-language interaction is useful.

The primary product value is not generic chat. It is a constrained, inspectable control plane where the model can only act through explicitly allowed tools. Part of the project's value is also architectural: learning how to build a useful local AI system with clear safety boundaries and predictable behavior.

## Components

### CraterClaw.Core (Library)

The shared C# library that owns all orchestration, configuration, provider abstraction, and model workflow logic. All entry points use the same contracts and services; no provider or orchestration logic is duplicated in the console or web layers.

### CraterClaw.Console (Console Harness)

A menu-driven console application for manual use and verification. A thin shell over library services.

### CraterClaw.Api (Web API)

A C# API exposing library workflows over HTTP.

### CraterClaw.Web (Vue Frontend)

A Vue TypeScript application consuming the C# API, mirroring the console harness flows in a browser UI.

## Provider Model

- Initial provider: Ollama (localhost or LAN).
- Provider implementations are behind stable provider-agnostic abstractions so paid AI providers (e.g. OpenAI, Anthropic) can be added later without broad refactoring.

## Behavior Profiles

Behavior profiles are a fixed catalog of curated combinations of model guidance and permitted tool sets. Users select from predefined profiles. User-defined profile composition is permanently out of scope.

Each behavior profile defines a hard capability boundary. The model is deny-by-default and may only invoke the tools, tool subsets, and configuration explicitly assigned to the selected behavior. If a capability is not declared in behavior configuration, it is out of scope and inaccessible to the model.

Behavior configuration is therefore a security control, not just a UX convenience. The architecture should continue to evolve toward explicit validation, narrow tool surfaces, auditable execution, and fail-closed behavior when configuration is invalid or incomplete.

## Tool Integrations

Tools are exposed as Semantic Kernel kernel plugins registered against the agentic execution loop. Initial integration: qBitTorrent. Planned integrations include Radarr, Sonarr, Jellyfin, FTP client, local media library filesystem, and future non-media tools such as calendar and notes integrations.

Tool integrations should remain explicit, narrow, and domain-scoped. CraterClaw is not intended to expose arbitrary host access, arbitrary shell execution, or unrestricted filesystem/network reachability to the model.

### Media Management Strategy

Two complementary behaviors handle media management at different levels:

- **media-supervised**: Uses Radarr, Sonarr, Jellyfin, and qBitTorrent plugins. The arr stack (Radarr/Sonarr) handles automated acquisition and library organization for monitored titles. The AI provides a natural language interface: checking queue status, triggering searches, identifying missing or stalled items. No file operations — the arr stack owns that.

- **media-manual**: Uses qBitTorrent, FTP, Jellyfin, and media library filesystem plugins. Automates the manual workflow for content outside the arr stack: check completed torrents on the remote seedbox, transfer via FTP to the local media library, place in the correct directory, verify it appeared in Jellyfin. For obscure releases, manual grabs, and one-off transfers.

The media library is hosted on a minipc (Proxmox LXC) with an external hard drive. Jellyfin serves as the media server (DLNA, web UI, REST API). The library is accessible to CraterClaw via SMB UNC path for file operations and via the Jellyfin API for library queries and scan triggers.

## MCP Integration

Not currently implemented. MCP infrastructure was removed to keep the codebase lean — native Semantic Kernel plugins provide full control over the tool surface with no additional process overhead. MCP may be reconsidered if a compelling official MCP server emerges for an integration that would otherwise require significant custom plugin work.

Any MCP integration must preserve the same explicit capability-boundary guarantees as native plugins.

## Key Capabilities

- Configure and select provider endpoints
- Check provider status and reachability
- List downloaded models
- Run interactive model sessions
- Run agentic tool-use loops within selected behavior profiles
- Resolve secrets from OS-level environment variables
- Manage torrents via the qBitTorrent plugin
- Present a unified natural-language layer across multiple approved personal or homelab systems
- Enforce explicit per-behavior tool boundaries so the model cannot access undeclared capabilities

## Deployment [Future]

- Candidate exposure path: Tailscale
- Candidate host model: LXC
- Infrastructure management: Terraform
- Environment management: Nix
- Revisit after library, console, and web scopes are complete
