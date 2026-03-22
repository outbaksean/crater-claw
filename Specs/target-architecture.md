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
Tools are exposed as Semantic Kernel kernel plugins registered against the agentic execution loop. Initial integration: qBitTorrent. Planned integrations: Radarr, Sonarr, Jellyfin, FTP client, local media library filesystem.

### Media Management Strategy
Two complementary behaviors handle media management at different levels:

- **media-supervised**: Uses Radarr, Sonarr, Jellyfin, and qBitTorrent plugins. The arr stack (Radarr/Sonarr) handles automated acquisition and library organization for monitored titles. The AI provides a natural language interface: checking queue status, triggering searches, identifying missing or stalled items. No file operations — the arr stack owns that.

- **media-manual**: Uses qBitTorrent, FTP, Jellyfin, and media library filesystem plugins. Automates the manual workflow for content outside the arr stack: check completed torrents on the remote seedbox, transfer via FTP to the local media library, place in the correct directory, verify it appeared in Jellyfin. For obscure releases, manual grabs, and one-off transfers.

The media library is hosted on a minipc (Proxmox LXC) with an external hard drive. Jellyfin serves as the media server (DLNA, web UI, REST API). The library is accessible to CraterClaw via SMB UNC path for file operations and via the Jellyfin API for library queries and scan triggers.

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
