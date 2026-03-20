# CraterClaw Library Bootstrap Spec

## Name
- CraterClaw Library Bootstrap

## Purpose
- Define the smallest implementation slice that produces a testable CraterClaw library foundation, a runnable console harness, and a verified basic Ollama connectivity path.

## Scope
- Create the initial C# solution structure for the CraterClaw library workstream.
- Use .NET 10 for all bootstrap projects in this slice.
- Name the solution `CraterClaw.slnx`.
- Scaffold at least:
  - A core library project named `CraterClaw.Core` for shared contracts and services
  - A console application project named `CraterClaw.Console` that acts as the manual test harness
  - An xUnit test project named `CraterClaw.Core.Tests` for library tests
- Define the minimum provider-facing contracts needed for an Ollama-backed status check.
- Implement a basic Ollama provider path that can:
  - Accept configured endpoint information
  - Attempt a connectivity or health-style request against the configured Ollama instance
  - Return a normalized success or failure result through library contracts
- Implement a minimal console flow that:
  - Loads or accepts a configured Ollama endpoint
  - Invokes the library connectivity check
  - Prints a clear success or failure result for manual verification
- Add automated tests for the contracts and service behavior using doubles or mocked HTTP behavior rather than a real Ollama instance.

## Acceptance Focus
- The solution can be restored and built.
- The solution and all projects target .NET 10.
- The automated tests can run without a real Ollama instance.
- The console harness can be run manually and exercise the basic Ollama connectivity flow.
- The library surface is small but structured so later provider, model, behavior-profile, scheduling, and MCP work can build on it without rewriting the bootstrap contracts.

## Out Of Scope
- Model listing or model download support
- Interactive prompt execution
- Scheduled or recurring task execution
- Behavior profile selection
- MCP configuration or availability checks beyond leaving room in the contracts for future work
- Multi-provider support beyond designing the contracts so it can be added later
- Web API or Vue frontend work

## Contract Notes
- The first contract set should stay minimal and testable. It only needs enough shape to represent provider endpoint configuration and a provider status result.
- The console harness should remain a thin shell over library services and should not contain provider logic.
- If Semantic Kernel is included in the bootstrap, it should only be included where it materially supports the connectivity slice. It should not force speculative abstractions before they are needed.

## Manual Verification
- Configure the console harness with an Ollama endpoint.
- Run the console harness and trigger the connectivity check.
- Confirm the console output distinguishes a reachable endpoint from an unreachable or invalid endpoint.

## Status
- Done
