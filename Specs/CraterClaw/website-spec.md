# CraterClaw Website Spec

## Name
- CraterClaw Website

## Purpose
- Define the web-facing experience for CraterClaw, including the C# API boundary and the Vue TypeScript frontend that exposes library capabilities to the user.

## Scope
- Implement a C# API that exposes the required CraterClaw library workflows to a web client.
- Implement a Vue application in TypeScript that consumes that API.
- Support user flows for:
  - Viewing configured provider endpoints and provider status
  - Viewing models and starting model downloads
  - Running interactive model sessions
  - Viewing and managing scheduled or recurring tasks
  - Selecting from curated behavior profiles
  - Viewing configured MCP servers and requesting availability checks
- Reuse library contracts and application services wherever practical so the website does not duplicate core orchestration logic.
- Exclude deployment and network exposure concerns from the initial website scope.

## Interface Notes
- The API should remain aligned with the library's shared contracts.
- Frontend automated tests use Vitest and should focus on client behavior without requiring live provider or MCP dependencies.

## Status
- Planning
