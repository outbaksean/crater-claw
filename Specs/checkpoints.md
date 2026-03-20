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

## Planned

### 12. vue-frontend

Vue TypeScript frontend (`CraterClaw.Web`) consuming the Web API. Mirrors the console harness user flows in a browser UI.
Depends on: web-api (checkpoint 11)

### behavior-refactor

Each behavior should be a preconfigured list of model, system prompt, available plugins/tools with a name.

### powershell-aliases

Create and document usefull powershell aliases for running, testing, linting etc.

### ide-formatting

Verify automatic formatting commands do the same as vscode on save formatting

### qbittorrent-search-tool

Allow qBittorrent searchesasdf

### ide-debugging

Configure vscode to allow C# debugging

