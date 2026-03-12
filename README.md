# CraterClaw

CraterClaw is an application I'm making as a solo dev to play with AI. I'm both playing with ai coding subscriptions and using ollama for some MCP agentic stuff. With the coding I'm trying out spec driven development and contract first development (see CLAUDE.md). The application is indended to be a fully local claw ai assistant.

## Current State

Bootstrap library, console harness, and tests for validating Ollama connectivity.

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

## Configure Endpoint

The console harness accepts an Ollama base URL in one of two ways:

- Command-line argument
- Interactive prompt when no argument is provided

Examples of base URLs:

- `http://localhost:11434`
- `http://my-server:11434`

## Run the Console Harness

Run with an endpoint argument:

```powershell
dotnet run --project .\CraterClaw.Console -- http://localhost:11434
```

Run without an argument (you will be prompted):

```powershell
dotnet run --project .\CraterClaw.Console
```

## Expected Output

- Reachable endpoint:
	- `Reachable: <base-url>`
- Unreachable or invalid endpoint:
	- `Unreachable: <base-url>`
	- Error detail on the next line

## Manual Verification Flow

1. Start Ollama or identify a known reachable endpoint.
2. Run the console harness with that endpoint and confirm it prints `Reachable`.
3. Run the console harness with an invalid or unreachable endpoint (for example `http://localhost:1`) and confirm it prints `Unreachable` plus an error message.