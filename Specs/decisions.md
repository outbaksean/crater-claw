# Architecture Decisions

Significant decisions made during development, with rationale. Pending decisions are noted at the bottom.

---

## Made

### Use Jellyfin as the media server

Jellyfin over Plex. Jellyfin is free, fully self-hosted, requires no external account, and has a REST API suitable for a future CraterClaw plugin. DLNA is built in, replacing the existing DLNA setup. Plex was ruled out due to its subscription model and dependency on Plex servers.

### Native SK plugins over MCP

MCP tool integration was scaffolded but never completed. Decision: build native Semantic Kernel plugins for all integrations (qBitTorrent, Radarr, Sonarr, Jellyfin, FTP, filesystem) rather than using MCP servers. Reasons: full control over the function surface exposed to the model, no additional processes to manage, no mature MCP servers exist for the arr stack or Jellyfin, and local Ollama models work better with a curated well-described function set. MCP infrastructure is being removed (`remove-mcp` checkpoint). Can be reconsidered if a compelling official MCP server emerges.

### Two media behaviors instead of one

Rather than a single media management behavior, the design uses two complementary behaviors:

- `media-supervised` — natural language interface to the arr stack (Radarr, Sonarr, Jellyfin, qBitTorrent). No file operations; the arr stack owns those.
- `media-manual` — automates the existing manual workflow (list completed torrents, FTP to library, verify). For content outside the arr stack.

Radarr/Sonarr handle the automated 80% case; CraterClaw handles the manual 20% and provides a conversational layer across both.

### Radarr and Sonarr alongside CraterClaw, not replaced by it

CraterClaw is not a replacement for the arr stack. Radarr/Sonarr handle automated acquisition and library organization for monitored titles. CraterClaw provides a natural language interface on top and handles the manual workflow for content outside the arr stack.

### Movies-first, TV deferred

Media library starts with movies only. TV requires season/episode directory nesting and filename conventions that are meaningfully more complex. Defer TV until the movies workflow is working well. Expand later without major refactoring.

### Flat file structure in movies/

Files placed directly inside `movies/` with no subdirectories. Jellyfin handles movie metadata with a flat layout. Avoids premature structure that would need to change if quality tiers or collections are added later.

### Per-plugin config inside behavior binding

Plugin connection settings (e.g. qBitTorrent credentials, base URL) live inside each behavior's plugin binding in `craterclaw.json`, not in a top-level plugin section. This allows multiple behaviors to reference the same plugin with different connections (e.g. home vs seedbox qBitTorrent). Credentials are excluded from `GET /api/profiles` API responses.

### LXC provisioned manually first, Terraform later

The Jellyfin LXC on Proxmox is set up manually for the initial `media-server` checkpoint. Terraform + cloud-init automation is a separate later checkpoint (`lxc-terraform`). Avoids blocking on infrastructure-as-code tooling before the setup is validated.

### IHttpClientFactory over new HttpClient()

`DefaultKernelFactory` previously created a `new HttpClient()` per kernel, leaking socket connections. Changed to inject `IHttpClientFactory` so the underlying `SocketsHttpHandler` is pooled and reused.

### web-ux-refactor-2 and investigate-child-agents before media/arr stack

The media and arr stack checkpoints are deferred until `web-ux-refactor-2` and `investigate-child-agents` are complete. The child agents research will inform how behaviors are structured going forward, which may affect the design of media behaviors. The web UX refactor will establish the interaction patterns that future behaviors surface through. Proceeding with media work before these are resolved risks rework.

---

## Pending

### API authentication strategy

The API currently has permissive CORS (all origins, headers, methods) and no authentication. Fine for local development. Needs a decision before any LAN or remote deployment. Candidates: Tailscale as the network boundary (no app-level auth), API key header, or bearer token. Related to the deployment strategy (Tailscale + LXC mentioned in target-architecture.md).

### Console long-term role

The console harness was built for manual testing and verification during early development. The web UI now covers the same workflows. Options: keep it as a developer/debug tool with no further investment, deprecate and remove it, or evolve it into a scriptable CLI. No decision needed immediately but worth resolving before significant new console-specific work is added.
