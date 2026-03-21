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

## Planned

### powershell-aliases

PowerShell module providing a `craterclaw` command available from any directory. Subcommands for running, building, testing, and formatting the solution. Install script sets up the module and profile import.
Depends on: vue-lint

### 18. ide-formatting

Reconciled `.editorconfig` (LF everywhere, 2-space for web files) with Prettier. Added `.vscode/settings.json` with format on save and per-language formatter assignments. `npm run lint` and `dotnet format` both produce zero changes.

### behavior-refactor

Each behavior should be a preconfigured list of model, system prompt, available plugins/tools with a name. All behavior details should be separate the agentic core services. craterclaw.json should define behaviors.

### behavior-secrets

Audit behavior definitions for sensitive data — system prompts may reference personal details, internal instructions, or other content that should not be committed. Determine whether behavior definitions (or parts of them) should be stored in user secrets or environment variables rather than craterclaw.json. Implement whatever secret handling approach is appropriate and document the pattern for future behaviors.

### media-library-config

Add a `mediaLibrary` configuration section to `craterclaw.json` defining the network root path and named category directories (e.g. movies, tv). Add FTP server credentials (host, port, username, password) under a separate `ftp` section. Bind both to new options types with validation. No tools yet — config and options types only.

### media-library-tool

SK kernel plugin that operates on the configured local media library. Functions: list files in a category directory, check whether a title already exists anywhere in the library, move a file from a staging location into the correct category directory. Depends on: media-library-config.

### ftp-client-tool

SK kernel plugin for transferring files from a remote FTP server to the local media library. Functions: list files in a remote directory, download a file from a remote path to a local category directory. Uses the configured FTP credentials. Depends on: media-library-config.

### agentic-error-recovery

Investigate and address error handling and recovery patterns across the agentic loop and plugins. To be scoped when the media plugins exist and real failure modes are known.

### media-management-tool

Orchestration behavior tying the media plugins together: download from FTP to the correct library directory, verify the file landed in the library, and delete the corresponding torrent from qBitTorrent if the title is already present in the library. Depends on: media-library-tool, ftp-client-tool, qbittorrent-plugin.

### thinking-mode-ollama

Enable thinking mode by using OllamaPromptExecutionSettings instead of PromptExecutionSettings in SemanticKernelAgenticExecutionService and include "think" true in AdditionalProperties. Thinking should be toggleable by the user.

### web-ux-refactor-2

Refactor the web ux with better placement of providers, models, behavior, chat boxes

### craterclaw-config-override

Add support for an alternate `craterclaw.json` path via a command-line argument or environment variable in both the console and API. Expose this as `--config <path>` in the `craterclaw run` and `craterclaw run --console` commands.
Depends on: powershell-aliases

### linux-aliases

Bash/zsh equivalent of the powershell-aliases module. Shell function file installed via install.sh to ~/.local/share/craterclaw/, sourced from .bashrc/.zshrc. Same craterclaw subcommand interface as the PowerShell module.
Depends on: powershell-aliases

