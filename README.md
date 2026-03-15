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

Configuration is layered: `craterclaw.json` (committed, no secrets) -> dotnet user secrets (dev) -> OS environment variables (deployment). Sensitive values such as MCP server credentials are stored outside the repository.

## Prerequisites

- .NET SDK 10.x
- Ollama running locally or on the LAN for provider connectivity (optional for tests)
- `uv` on PATH for stdio MCP server availability checks (optional)

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

### MCP servers

MCP server definitions live under `mcp.servers` in `craterclaw.json`. Sensitive env values are left as empty strings in the file:

```json
{
    "mcp": {
        "servers": {
            "qbittorrent": {
                "label": "qBitTorrent",
                "transport": "Stdio",
                "command": "uvx",
                "args": [ "--from", "git+https://github.com/jmagar/yarr-mcp", "qbittorrent-mcp-server" ],
                "env": {
                    "QBITTORRENT_URL": "",
                    "QBITTORRENT_USER": "",
                    "QBITTORRENT_PASS": "",
                    "QBITTORRENT_MCP_TRANSPORT": "stdio"
                },
                "enabled": true
            }
        }
    }
}
```

Set the real values using dotnet user secrets so they are never committed:

```powershell
dotnet user-secrets set "mcp:servers:qbittorrent:env:QBITTORRENT_URL"  "http://192.168.1.x:8080" --project .\CraterClaw.Console
dotnet user-secrets set "mcp:servers:qbittorrent:env:QBITTORRENT_USER" "admin"                    --project .\CraterClaw.Console
dotnet user-secrets set "mcp:servers:qbittorrent:env:QBITTORRENT_PASS" "your-password"            --project .\CraterClaw.Console
```

User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\craterclaw-console\secrets.json` on Windows, outside the repository.

For deployment, use OS environment variables instead (`:` becomes `__` on platforms that do not support `:` in variable names; Windows supports `:` directly):

```powershell
$env:mcp__servers__qbittorrent__env__QBITTORRENT_URL  = "http://192.168.1.x:8080"
$env:mcp__servers__qbittorrent__env__QBITTORRENT_PASS = "your-password"
```

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
