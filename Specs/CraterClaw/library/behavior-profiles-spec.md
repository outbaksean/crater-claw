# CraterClaw Library Behavior Profiles Spec

## Name
- CraterClaw Library Behavior Profiles

## Purpose
- Define a fixed, application-owned catalog of curated behavior profiles that combine a task description, model guidance, and a permitted set of MCP servers, so that agentic tasks always run within a predefined and reviewable configuration.

## Scope
- Add behavior profile contracts:
  - `BehaviorProfile`: immutable record with a stable identifier, display name, description, recommended model tags (hints for model selection, not hard requirements), and the list of MCP server names the profile is permitted to use
  - `IBehaviorProfileService`: retrieve all profiles and retrieve a profile by identifier
- Implement the profile catalog as hardcoded application data in `CraterClaw.Core`; no file or database backing.
- Define an initial catalog with the following profiles:
  - `no-tools`: no MCP servers, general-purpose conversation and reasoning
  - `qbittorrent-manager`: qBitTorrent MCP, querying and managing downloads
- Wire the console harness to list available profiles and select one for use in an agentic task.
- Add automated tests confirming the catalog is complete, identifiers are unique, and profile retrieval by identifier behaves correctly.

## Contract Notes
- Profile identifiers are stable constants. Renaming or removing an identifier is a breaking change and requires updating any persisted references in configuration.
- Recommended model tags are advisory strings (e.g., `reasoning`, `code`, `vision`). They are metadata for the caller to use when choosing a model; the execution layer does not enforce them.
- `AllowedMcpServerNames` references MCP server names as defined in `McpServerDefinition.Name`. The behavior profile service does not validate that those servers exist in configuration; that cross-check is the responsibility of the agentic execution layer.
- Users select a profile; they do not compose or modify one. There is no profile authoring surface in this spec.
- Do not add prompt templates or system prompt content to this spec. If profiles require model-specific prompting, that is defined in the agentic execution spec.

## Console Integration
- Display the profile list with identifier, name, and description.
- After profile selection, display which MCP servers the profile permits.
- Store the selected profile identifier in session state for use when launching an agentic task.

## Manual Verification
- Run the console harness and navigate to the profile selection flow.
- Confirm all catalog profiles appear with correct names and descriptions.
- Select a profile and confirm the permitted MCP server list is displayed.

## Status
- Planning
