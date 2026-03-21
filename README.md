# CraterClaw

CraterClaw is an application I'm making as a solo dev to play with AI. I'm both playing with ai coding subscriptions and using ollama for some MCP agentic stuff. With the coding I'm trying out spec driven development and contract first development (see CLAUDE.md). The application is intended to be a fully local claw ai assistant.

## Current State

The console harness supports:

- Provider endpoint selection from `craterclaw.json` with a numbered list and default marker
- Provider status check (Ollama connectivity)
- Model listing from the active endpoint
- Interactive model execution (single prompt/response)
- MCP server listing with transport type and enabled status
- MCP server availability checks (HTTP GET for http servers; PATH walk for stdio servers)
- Behavior profile selection (`no-tools`, `qbittorrent-manager`)
- Plugin function listing: after selecting a profile, the console displays the available kernel functions by name and description
- Agentic task execution: after selecting a profile and model, enter a task prompt to run the SK agentic loop with the permitted plugins; tool invocations and the final response are displayed

Configuration is layered: `craterclaw.json` (committed, no secrets) -> dotnet user secrets (dev) -> OS environment variables (deployment). Sensitive values such as MCP server credentials are stored outside the repository.

## Prerequisites

- .NET SDK 10.x
- Node.js 20.x and npm 10.x (for `CraterClaw.Web`)
- Ollama running locally or on the LAN for provider connectivity (optional for tests)
- `uv` on PATH for stdio MCP server availability checks (optional)
- qBitTorrent running with WebUI enabled for qBitTorrent plugin features (optional)

Check SDK version:

```powershell
dotnet --version
```

## Project Layout

- `CraterClaw.slnx`: solution file
- `CraterClaw.Core`: library contracts, options types, and service implementations
- `CraterClaw.Console`: console harness for manually exercising library workflows
- `CraterClaw.Api`: ASP.NET Core minimal API exposing library workflows over HTTP
- `CraterClaw.Web`: Vue 3 TypeScript frontend consuming the API
- `CraterClaw.Core.Tests`: xUnit unit tests for the core library (no live Ollama required)
- `CraterClaw.Api.Tests`: xUnit integration tests for the web API

## Formatting

VS Code formats on save automatically when the Prettier (`esbenp.prettier-vscode`) and C# (`ms-dotnettools.csharp`) extensions are installed.

To format from the command line:

```powershell
dotnet format .\CraterClaw.slnx
```

```powershell
cd .\CraterClaw.Web
npm run lint:fix
```

## Restore and Build

From the repository root:

```powershell
dotnet restore .\CraterClaw.slnx
dotnet build .\CraterClaw.slnx
```

## Run Tests

```powershell
dotnet test .\CraterClaw.slnx
```

The test suite uses mocked HTTP and does not require a real Ollama instance or MCP server.

## Run the Vue Frontend

```powershell
cd .\CraterClaw.Web
npm install
npm run dev
```

The dev server runs at `http://localhost:5173` by default. It expects `CraterClaw.Api` to be running at `http://localhost:5000`. To use a different API URL, set the `VITE_API_BASE_URL` environment variable before starting the dev server.

Run Vue unit tests:

```powershell
cd .\CraterClaw.Web
npm test
```

Lint Vue source:

```powershell
cd .\CraterClaw.Web
npm run lint
npm run lint:fix
```

## Run the Web API

```powershell
dotnet run --project .\CraterClaw.Api
```

The API listens on the default ASP.NET Core ports (http://localhost:5000 by default). Endpoints are available under `/api`.

The API reads its own `CraterClaw.Api/craterclaw.json`. Configure provider endpoints there in the same format as the console harness. User secrets for the API use the project flag `--project .\CraterClaw.Api`.

## Configuration

A single `craterclaw.json` lives at the repository root and is shared by both `CraterClaw.Console` and `CraterClaw.Api`. It is copied to each project's output directory at build time. The file is committed with placeholder values for any secrets. Real values are supplied at runtime via dotnet user secrets (development) or OS environment variables (deployment).

### Provider endpoints

Edit `craterclaw.json` to add or change endpoints and set the default active one:

```json
{
    "providers": {
        "active": "local",
        "endpoints": {
            "local": { "baseUrl": "http://localhost:11434" },
            "lan":   { "baseUrl": "http://192.168.1.50:11434" }
        }
    }
}
```

To override the active endpoint without editing the file, use a user secret:

```powershell
dotnet user-secrets set "providers:active" "lan" --project .\CraterClaw.Console
```

### qBitTorrent plugin

The `qbittorrent-manager` behavior profile enables the following kernel functions:

- `ListTorrents` - list all torrents with name, hash, status, progress, and size
- `AddTorrentByUrl` - add a torrent from a magnet link or HTTP URL
- `PauseTorrent` - pause a torrent by hash
- `ResumeTorrent` - resume a paused torrent by hash
- `DeleteTorrent` - delete a torrent by hash with optional file deletion
- `GetTransferStats` - current download/upload speeds and session totals
- `SearchTorrents` - search for torrents using installed qBitTorrent search plugins; requires at least one search plugin enabled in qBitTorrent (Plugins > Search Plugins)

qBitTorrent credentials live under `qbittorrent` in `craterclaw.json`. Leave the file values empty and supply real values via user secrets:

```powershell
dotnet user-secrets set "qbittorrent:baseUrl"  "http://192.168.1.x:8080" --project .\CraterClaw.Console
dotnet user-secrets set "qbittorrent:username" "admin"                    --project .\CraterClaw.Console
dotnet user-secrets set "qbittorrent:password" "your-password"            --project .\CraterClaw.Console
```

Note: the `--project` flag is required. Running `dotnet user-secrets` from the repository root without it will fail because there are multiple projects in the solution.

User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\craterclaw-console\secrets.json` on Windows, outside the repository.

For deployment, use OS environment variables:

```powershell
$env:qbittorrent__baseUrl  = "http://192.168.1.x:8080"
$env:qbittorrent__username = "admin"
$env:qbittorrent__password = "your-password"
```

### AI logging

`aiLogging.enabled` (bool, default `false`) — when true, full Ollama request and response detail is written to a dedicated rolling log file separate from the main log.

`aiLogging.path` (string, optional) — path for the AI log. Can be a directory path (`C:\ollama-logs`) or a file prefix (`C:\ollama-logs\ai-.log`). When a directory is given, files are written as `ai-{date}.log` inside it. When empty, defaults to `logs/ai-{date}.log` relative to the application directory. Can be absolute or relative to the app directory.

The main log never contains message content or request JSON. The AI log only contains AI traffic detail.

Enable via user secrets:

```powershell
dotnet user-secrets set "aiLogging:enabled" "true" --project .\CraterClaw.Console
```

To write the AI log to a dedicated directory:

```powershell
dotnet user-secrets set "aiLogging:path" "C:\ollama-logs" --project .\CraterClaw.Console
```

When AI logging is enabled, the console prints `AI log file: {path}` at startup alongside the main log directory.

### MCP servers

MCP server definitions live under `mcp.servers` in `craterclaw.json`. No servers are configured by default. Add entries here if future MCP servers are needed.

## Run the Console Harness

```powershell
dotnet run --project .\CraterClaw.Console
```

### VS Code Task

1. Open `Terminal` -> `Run Task...`
2. Choose `Run CraterClaw Console (with args)`

### VS Code Debugger

1. Open the Run and Debug panel (`Ctrl+Shift+D`)
2. Select `Debug CraterClaw.Console` from the dropdown
3. Press `F5`

The console runs in the integrated terminal so interactive prompts work normally while breakpoints are active. To debug the API instead, select `Debug CraterClaw.Api`.

## Console Flow

1. **Endpoint selection** - numbered list with `(default)` marker; press Enter to use the default.
2. **Status check** - reports reachable or unreachable.
3. **Model listing** - numbered list of downloaded models (if endpoint is reachable).
4. **Model selection** - press Enter to skip execution.
5. **Prompt** - enter a prompt and receive a response.
6. **MCP server listing** - numbered list of configured servers with transport and enabled status.
7. **Availability check** - select a server number or press Enter to skip.
8. **Profile selection** - numbered list of behavior profiles; press Enter to skip.
9. **Plugin function listing** - for profiles with permitted plugins (e.g. `qbittorrent-manager`), lists the available kernel functions by name and description. Profiles with no plugins (e.g. `no-tools`) display a "no functions available" message.
10. **Task prompt** - enter a task and the agentic loop runs with the selected model and profile plugins. Each tool called is shown as `Tool: {name}`. The final response is shown under `Response:` followed by a tool invocation count. If the model hit the iteration limit before finishing, `(iteration limit reached)` is displayed.
