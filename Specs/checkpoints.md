# Checkpoints

Each checkpoint describes a verifiable, runnable state of the application. Dependencies are noted where a checkpoint builds on a prior one.

## Done

### 1. bootstrap

Solution scaffolded: `CraterClaw.Core`, `CraterClaw.Console`, `CraterClaw.Core.Tests` on .NET 10. Basic Ollama connectivity check runnable from the console.

### 2. provider-config

Endpoint configuration loaded from `craterclaw.json`. Active endpoint selection from a numbered list; blank to use default.

### 3. model-listing

List downloaded models at the active endpoint. Numbered model selection in the console.
Depends on: provider-config

### 4. interactive-execution

Send a conversational prompt to the selected model and display the response.
Depends on: model-listing

### 5. secrets

Resolve `${VAR_NAME}` references in config values from OS user-level environment variables at point of use.

### 6. mcp-config

Load MCP server definitions from `craterclaw.json`. Check availability on demand from the console.

### 7. behavior-profiles

Fixed catalog of curated behavior profiles. Numbered selection in the console.

### 8. logging

Structured logging with a rolling daily file sink in the console harness.

### 9. agentic-execution

Semantic Kernel tool-use loop: send a prompt, process function calls, invoke tools, iterate until completion or iteration limit. Registered as `IAgenticExecutionService`.
Depends on: interactive-execution, behavior-profiles

### 10. qbittorrent-plugin

`QBitTorrentPlugin` SK kernel plugin with six torrent management functions. Streaming agentic execution from the console with tool invocation summary.
Depends on: agentic-execution

### 11. web-api

C# Web API (`CraterClaw.Api`) exposing library workflows: provider status, model listing, interactive execution, agentic execution, behavior profiles, MCP availability.
Depends on: qbittorrent-plugin (checkpoint 10)

### 12. vue-frontend

Vue TypeScript frontend (`CraterClaw.Web`) consuming the Web API. Provider selection, status check, model listing, interactive chat, behavior profile selection, and agentic task execution. MCP server UI excluded.
Depends on: web-api (checkpoint 11)

### 13. qbittorrent-search-tool

Add a `SearchTorrents` kernel function to `QBitTorrentPlugin` that queries qBitTorrent's built-in search plugin system and returns matching torrent results.
Depends on: qbittorrent-plugin (checkpoint 10)

### 14. ide-debugging

Configure VS Code launch configurations and pre-launch build tasks to support C# debugging for `CraterClaw.Console` and `CraterClaw.Api`.

### 15. front-end-ux

Redesign the Vue frontend with a unified monospace dark workspace aesthetic: DM Mono + Syne fonts, CSS design token system, panel layout with progressive disclosure, left-border selection pattern, inline status pills, textarea inputs with Enter-to-submit, animated loading states, and panel reveal transitions.
Depends on: vue-frontend (checkpoint 12)

### 16. logging-breakout-ollama-responses

Log ollama requests and responses separately from main logging. AI traffic logged under `CraterClaw.AiTraffic` category, routed to a separate rolling file when `aiLogging.enabled` is true. Main log excludes AI traffic. Both console and API use the same sub-logger Serilog configuration. Full content logged with no truncation.

### 17. vue-lint

Add ESLint to the Vue project with the Vue and TypeScript plugins, flat config format, Vitest globals, and Prettier configured with LF line endings. `npm run lint` and `npm run lint:fix` work end to end with zero errors.
Depends on: vue-frontend

### 19. powershell-aliases

PowerShell module (`tools/CraterClaw.psm1`) providing the `craterclaw` command from any directory. Subcommands: `run` (API + web in separate windows, or console harness), `build`, `test`, `format`. Install script (`tools/Install-CraterClaw.ps1`) sets `CRATERCLAW_ROOT` and patches the PS profile. Supports PS7 and Windows PowerShell 5.1.
Depends on: vue-lint

### 18. ide-formatting

Reconciled `.editorconfig` (LF everywhere, 2-space for web files) with Prettier. Added `.vscode/settings.json` with format on save and per-language formatter assignments. `npm run lint` and `dotnet format` both produce zero changes.

### behavior-refactor

Each behavior defined in `craterclaw.json` with system prompt, preferred provider/model, and a list of plugin bindings with per-tool filtering and per-binding connection config. Config-driven `IBehaviorProfileService`. `IPluginRegistry` resolves bindings to pre-filtered SK `KernelPlugin` instances. `AgenticRequest` carries system prompt and resolved plugins. Two qBitTorrent behaviors (home, seedbox) with separate credentials. Vue frontend applies preferred provider/model defaults on profile selection and shows warnings when preferred values are unavailable.

### craterclaw-config-override

`craterclaw run` accepts `-Config <path>` to pass an alternate `craterclaw.json`. Both console and API accept `--config <path>` CLI arg and `CRATERCLAW_CONFIG` env var (for test injection). Path resolved to absolute in PowerShell before forwarding.

### code-review-1

Codebase review identifying resource leaks, error handling gaps, test coverage gaps, and architectural observations. Fixed: `DefaultKernelFactory` HttpClient leak (replaced `new HttpClient()` with `IHttpClientFactory`); Vue API client error messages now include response body. Notes documented in `Specs/code-review-2026-03-22.md`. Remaining items tracked for `agentic-error-recovery` and `remove-mcp`.

### remove-mcp

Removed all MCP infrastructure from the codebase: 10 Core files, 3 Core.Tests files, 1 Api.Tests file. Removed MCP registrations from `ServiceCollectionExtensions.cs`, MCP endpoints from `CraterClaw.Api/Program.cs`, MCP console steps from `CraterClaw.Console/Program.cs`, MCP section from `craterclaw.json`, and MCP references from `README.md` and `current-architecture.md`. Removed the `ModelContextProtocol` NuGet package.

### qbittorrent-search-result-truncation

Search result filenames longer than 120 characters now include a `"..."` suffix so the model knows the name is incomplete. Changed `fileName[..120]` to `fileName[..117] + "..."` in `QBitTorrentPlugin.SearchTorrentsAsync`.

### test-coverage-gaps

Added `OllamaProviderStatusService` tests: reachable on HTTP 200, unreachable on `HttpRequestException`, unreachable on non-success status, cancellation propagated. Added `SemanticKernelAgenticExecutionService` test: exception propagates when a kernel function throws during invocation.

### qbittorrent-list-fields

`ListTorrentsAsync` extended to return four additional fields per torrent: `amount_left`, `priority`, `size`, and `category`. `[Description]` attribute and `GetFunctionDescriptions()` updated to match. Tests updated to cover all projected fields and graceful handling of absent fields.
Depends on: qbittorrent-plugin

### api-controller-separation

Refactored `CraterClaw.Api/Program.cs` into separate files: request/response record types to `Models/ApiModels.cs`, provider endpoint handlers to `Endpoints/ProvidersEndpoints.cs`, profiles endpoint to `Endpoints/ProfilesEndpoints.cs`. Extracted repeated provider-lookup pattern into `IProviderResolver`/`ProviderResolver` singleton registered in DI. `Program.cs` retains startup and DI wiring only. `InternalsVisibleTo` added to expose API internals to the test project. All 78 tests pass.

## Planned

### behavior-secrets

**Type: Code**

Audit behavior definitions for sensitive data — system prompts may reference personal details, internal instructions, or other content that should not be committed. Determine whether behavior definitions (or parts of them) should be stored in user secrets or environment variables rather than craterclaw.json. Implement whatever secret handling approach is appropriate and document the pattern for future behaviors.

### ollama-lan

**Type: Infrastructure**

Make Ollama accessible on the LAN. Currently Ollama only runs on the host machine and is not reachable from other devices on the network. Needs investigation — the right approach depends on the host OS, network config, and whether Ollama should be exposed directly or proxied.

### media-server

**Type: Infrastructure**

Set up Jellyfin in a Proxmox LXC on the minipc, with the external hard drive as the media library storage. Jellyfin is free, fully self-hosted, and has a REST API for future CraterClaw integration. DLNA is built in (replaces existing DLNA setup). SMB share on the LXC provides direct file access for CraterClaw's media library plugin.

Infrastructure: minipc running Proxmox, external hard drive attached to the minipc and bind-mounted into the LXC. LXC provisioned manually for now — Terraform + cloud-init is a later checkpoint.

See `Specs/media-server-spec.md` for setup details.

### media-library-config

**Type: Code**

Add a `mediaLibrary` configuration section to `craterclaw.json` defining the UNC path to the media library root and a named map of category directories (initially just `movies`). Bind to a new options type with validation. No tools yet — config and options types only. FTP config is a separate checkpoint.
Depends on: media-server

### media-library-tool

**Type: Code**

SK kernel plugin that operates on the configured local media library via the UNC path. Functions: list files in a category directory, check whether a title already exists anywhere in the library, move a downloaded file into the correct category directory. Files are placed flat inside the category directory. Depends on: media-library-config.

### ftp-client-tool

**Type: Code**

SK kernel plugin for transferring files from a remote FTP server to the local media library. Functions: list files in a remote directory, download a file from a remote path to a local category directory. Uses the configured FTP credentials. Depends on: media-library-config.

### radarr-sonarr-setup

**Type: Infrastructure**

Set up Radarr and Sonarr alongside the existing qBitTorrent setup. Both pointed at qBitTorrent as the download client and at the media library directories as their root folders. Radarr handles movies, Sonarr handles TV. This is an infrastructure checkpoint — no CraterClaw code. The arr stack handles the automated 80% case (monitored titles); CraterClaw handles the manual 20% case via the media-manual behavior.

### radarr-plugin

**Type: Code**

SK kernel plugin for the Radarr REST API. Functions: list movies in the library, list the download queue, search for a movie by title, add a movie to the wanted list, get the status of a movie (missing, queued, downloaded). Requires Radarr base URL and API key in config.
Depends on: radarr-sonarr-setup

### sonarr-plugin

**Type: Code**

SK kernel plugin for the Sonarr REST API. Functions: list series in the library, list the download queue, search for a series by title, add a series to the wanted list, get episode status. Requires Sonarr base URL and API key in config.
Depends on: radarr-sonarr-setup

### media-supervised-behavior

**Type: Code**

Behavior profile using Radarr, Sonarr, Jellyfin, and qBitTorrent plugins together. The AI provides a natural language interface to the arr stack: what's missing, what's downloading, what stalled, trigger searches, check queue health. No file operations — the arr stack handles everything mechanical.
Depends on: radarr-plugin, sonarr-plugin, jellyfin-api-plugin, qbittorrent-plugin

### media-manual-behavior

**Type: Code**

Behavior profile using qBitTorrent, FTP, and media library plugins together. The AI automates the manual workflow: check completed torrents, transfer files from the remote seedbox via FTP, place them in the correct library directory, verify they landed. For content outside the arr stack — obscure releases, manual grabs, one-off transfers.
Depends on: media-library-tool, ftp-client-tool, qbittorrent-plugin, jellyfin-api-plugin

### agentic-error-recovery

**Type: Research / Code**

Investigate and address error handling and recovery patterns across the agentic loop and plugins. To be scoped when the media plugins exist and real failure modes are known.

### jellyfin-api-plugin

**Type: Code**

SK kernel plugin for the Jellyfin REST API. Functions: trigger a library scan for a specific library, check whether a title exists in the library by name. Useful for the AI to verify a file was picked up after being moved into the library. Requires Jellyfin base URL and API key in config.
Depends on: media-server, media-library-config

### lxc-terraform

**Type: Infrastructure**

Terraform module and cloud-init config to provision the Jellyfin LXC on Proxmox, replacing the manual setup from `media-server`. Includes: container resource definitions, bind mount for the external drive, network config, and cloud-init for Jellyfin + Samba installation.
Depends on: media-server

### thinking-mode-ollama

**Type: Code**

Enable thinking mode by using OllamaPromptExecutionSettings instead of PromptExecutionSettings in SemanticKernelAgenticExecutionService and include "think" true in AdditionalProperties. Thinking should be toggleable by the user.

### web-agentic-streaming

**Type: Code**

Add streaming support to the agentic execution path in the API and web frontend. Currently streaming only works in the console harness via `StreamChunk`. The API returns the full response only after completion, leaving the web UI blank for the duration of long tasks. Requires a streaming endpoint (SSE or chunked transfer) and a Vue composable update to consume it.
Depends on: vue-frontend

### web-ux-refactor-2

**Type: Code**

Refactor the web ux with better placement of providers, models, behavior, chat boxes

### investigate-child-agents

**Type: Research**

Investigate allowing the model to spawn subagents. The output will either be checkpoints or a notes file

### linux-aliases

**Type: Code**

Bash/zsh equivalent of the powershell-aliases module. Shell function file installed via install.sh to ~/.local/share/craterclaw/, sourced from .bashrc/.zshrc. Same craterclaw subcommand interface as the PowerShell module.
Depends on: powershell-aliases
