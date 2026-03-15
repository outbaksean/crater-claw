# CraterClaw Library Agentic Execution Spec

## Name
- CraterClaw Library Agentic Execution

## Purpose
- Define the orchestration layer that runs a model in a tool-use loop: the model receives a task and a set of MCP-backed tools, calls tools as needed, and produces a final text result when it signals completion or a turn limit is reached.

## Scope
- Add tool-use contracts extending the interactive execution contracts:
  - `ToolDefinition`: immutable record with tool name, description, and a JSON Schema object describing its parameters; passed to the model at request time
  - `ToolCall`: immutable record with tool name and a key-value argument map; returned by the model when it requests a tool
  - `ToolResult`: immutable record with tool name and string content; fed back to the model after execution
  - `AgenticRequest`: immutable record with model name, initial messages, available tool definitions, and a maximum iteration count
  - `AgenticResponse`: immutable record with the final assistant content, the number of iterations consumed, and the finish reason (Completed, IterationLimitReached)
- Add MCP protocol contracts:
  - `IMcpSession`: connect to a single MCP server, retrieve its tool list as `ToolDefinition` records, and invoke a named tool with arguments to produce a `ToolResult`
  - `IMcpSessionFactory`: create an `IMcpSession` for a given `McpServerDefinition`
- Add Ollama-backed agentic execution:
  - Extend `IModelExecutionService` or introduce `IAgenticExecutionService` to run the tool-use loop
  - On each iteration: send messages and tool definitions to Ollama `/api/chat`; if the response contains tool calls, execute each via the appropriate `IMcpSession` and append results; if the response contains only text content, return it as the final result
  - Enforce the iteration limit and return `IterationLimitReached` if it is hit before a text response
- Add MCP session implementations:
  - Http: connect to an HTTP-based MCP server using JSON-RPC 2.0 over the Streamable HTTP transport; call `initialize`, list tools, and invoke tools
  - Stdio: spawn the server process, communicate via JSON-RPC 2.0 over stdin/stdout; call `initialize`, list tools, and invoke tools
- Wire the console harness to:
  - Accept a selected behavior profile and a task prompt
  - Resolve permitted MCP servers from configuration that match the profile's allowed names and are enabled
  - Open sessions for each permitted server and collect tool definitions
  - Run the agentic loop and stream or display the final response
  - Report iteration count and which tools were called
- Add automated tests for the execution loop logic using fake MCP sessions and fake model responses; do not test against live Ollama or live MCP endpoints.

## Contract Notes
- `IAgenticExecutionService` depends on `IModelExecutionService` for the model call and `IMcpSessionFactory` for tool execution; it does not own HTTP or process I/O directly.
- Tool definitions passed to the model must come from the MCP servers that the selected behavior profile permits. The agentic execution layer enforces this by filtering sessions to the profile's `AllowedMcpServerNames`.
- MCP sessions are opened per agentic task and closed when the task completes. Do not share sessions across tasks.
- Argument maps in `ToolCall` use `JsonElement` or `Dictionary<string, object?>` to avoid tying the contract to a specific JSON library version.
- If a tool invocation fails, append an error `ToolResult` and continue the loop rather than aborting; the model may recover or choose a different tool.
- The stdio MCP implementation must not leave orphaned processes if the task is cancelled or an exception occurs.

## Decomposition
- This spec is too broad for a single plan. Before implementation begins, decompose into child specs:
  - `agentic-execution/tool-contracts-spec.md`: `ToolDefinition`, `ToolCall`, `ToolResult`, `AgenticRequest`, `AgenticResponse`, and the `IMcpSession`/`IMcpSessionFactory` interfaces
  - `agentic-execution/mcp-sessions-spec.md`: Http and Stdio `IMcpSession` implementations and their JSON-RPC framing
  - `agentic-execution/execution-loop-spec.md`: `IAgenticExecutionService` orchestration loop, Ollama tool-call parsing, and console harness wiring

## Manual Verification
- Dependencies: at least one model with tool-use support downloaded (e.g., `llama3.1` or `qwen2.5`); at least one MCP server running and matching a behavior profile.
- Run the console harness, select a profile with at least one permitted MCP server, enter a task prompt, and confirm the agentic loop runs.
- Confirm tool calls appear in the output as they occur.
- Confirm the final assistant response is displayed.
- Confirm the iteration count is reported.

## Status
- Planning
