# CraterClaw MCP Spec

## Name
- CraterClaw MCP Integration

## Purpose
- Define how CraterClaw represents external MCP servers in configuration and how it verifies their availability for curated behavior profiles.

## Scope
- Define configuration requirements for MCP server entries consumed by CraterClaw.
- Define the data needed to identify an MCP server, describe its location, and indicate whether it is enabled.
- Define the availability-check behavior that CraterClaw can run on demand against configured MCP servers.
- Define how curated behavior profiles reference the subset of MCP servers they are allowed to use.
- Initial MCP candidates to evaluate include:
  - qBitTorrent
  - Web search via SearXNG
  - Media management
  - Obsidian
  - Writing assistant workflows
- Exclude MCP deployment, updates, hosting, and arbitrary user-defined MCP composition.

## Constraints
- MCP servers are external dependencies and may be hosted anywhere reachable by the CraterClaw environment.
- Availability checks should be explicit user- or system-triggered operations rather than an implicit requirement for all application startup paths.

## Status
- Planning
