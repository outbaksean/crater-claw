# CraterClaw

CraterClaw is an application I'm making as a solo dev to play with AI. I'm both playing with ai coding subscriptions and using ollama for some MCP agentic stuff. With the coding I'm trying out spec driven development and contract first development (see CLAUDE.md). The application is indended to be a fully local claw ai assistant.

## Current State

Bootstrap and provider configuration slices are implemented. The library supports file-backed provider endpoint configuration, active endpoint selection, and connectivity checks through the console harness.

## Prerequisites

- .NET SDK 10.x
- Optional: Ollama running locally or remotely for reachable-endpoint manual verification

Check SDK version:

```powershell
dotnet --version
```

## Project Layout

- `CraterClaw.slnx`: solution file
- `CraterClaw.Core`: library contracts and provider status service
- `CraterClaw.Console`: console harness for manual connectivity checks
- `CraterClaw.Core.Tests`: xUnit tests for contracts and service behavior

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

The test suite uses mocked HTTP behavior and does not require a real Ollama instance.

## Configure Providers

The console harness reads providers from a JSON config file. By default it uses `./provider-config.json` in the current directory.

Example config file:

```json
{
	"endpoints": [
		{ "name": "local", "baseUrl": "http://localhost:11434" },
		{ "name": "lan", "baseUrl": "http://192.168.1.50:11434" }
	],
	"activeProviderName": "local"
}
```

## Run the Console Harness

Run with a config file argument:

```powershell
dotnet run --project .\CraterClaw.Console -- .\provider-config.json
```

Run without an argument (you will be prompted for config file path, blank uses default):

```powershell
dotnet run --project .\CraterClaw.Console
```

### VS Code Task

You can also run the console using the workspace task:

1. Open `Terminal` -> `Run Task...`
2. Choose `Run CraterClaw Console (with args)`
3. Enter arguments when prompted (default: `.\provider-config.json`)

The task runs:

```powershell
dotnet run --project .\CraterClaw.Console -- <your-args>
```

## Expected Output

- Config load and endpoint list are printed.
- Endpoints are numbered (`1`, `2`, etc.) and you can choose by number, or press Enter to keep the current active endpoint.
- Reachable endpoint:
  - `Reachable: <base-url>`
- Unreachable or invalid endpoint:
  - `Unreachable: <base-url>`
  - Error detail on the next line

## Manual Verification Flow

1. Start Ollama or identify a known reachable endpoint.
2. Create a `provider-config.json` with at least two endpoints.
3. Run the console harness and select endpoint `1`.
4. Confirm endpoint A is used for the status check.
5. Run again, select endpoint `2`, and confirm endpoint B is used.
6. Confirm `activeProviderName` in the JSON file persists the latest selection.