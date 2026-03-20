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
- `CraterClaw.Core.Tests`: xUnit unit tests (no live Ollama required)

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

## Configuration

All configuration lives in `CraterClaw.Console/craterclaw.json`. The file is committed with placeholder values for any secrets. Real values are supplied at runtime via dotnet user secrets (development) or OS environment variables (deployment).

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

### MCP servers

MCP server definitions live under `mcp.servers` in `craterclaw.json`. No servers are configured by default. Add entries here if future MCP servers are needed.

## Run the Console Harness

```powershell
dotnet run --project .\CraterClaw.Console
```

### VS Code Task

1. Open `Terminal` -> `Run Task...`
2. Choose `Run CraterClaw Console (with args)`

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
