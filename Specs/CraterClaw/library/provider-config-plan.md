# CraterClaw Library Provider Configuration Plan

## Decisions

- Endpoint configuration is stored in a single JSON file with a small schema suitable for local development and test fixtures.
- The library owns configuration parsing and active selection logic. The console harness only calls library contracts.
- Existing provider status checks are reused; no new connectivity implementation is introduced in this plan.

## Overview
- This plan implements the provider configuration spec in three phases.
- Each phase is scoped to be completed in one AI session.
- Phases execute in order: contracts first, implementation second, console wiring and verification third.

---

## Phase 1: Configuration Contracts and Schema

### Status
- Not Started

### Goal
- Define provider configuration contracts and JSON schema types for configured endpoints and active endpoint selection.

### Contract
- `ProviderConfiguration` contract with:
  - `Endpoints` collection of named endpoints
  - `ActiveProviderName` optional string
- `IProviderConfigurationService` with methods to:
  - Load configuration
  - Save configuration
  - Get active endpoint
  - Set active endpoint by name
- Contract-level validation rules:
  - Endpoint names are unique (case-insensitive)
  - Endpoint base URLs are required absolute URLs
  - Active provider name must match a configured endpoint when present

### Tasks
- Add new configuration contracts and interfaces in `CraterClaw.Core`.
- Add contract tests for valid and invalid configuration states.
- Keep implementation out of this phase beyond what is required for contract tests.

### Tests
- Valid configuration with multiple endpoints and a valid active provider.
- Invalid configuration with duplicate endpoint names.
- Invalid configuration with malformed or empty endpoint base URL.
- Invalid configuration where active provider name is missing from endpoints.

### Manual Verification
- Build and run tests to confirm contract definitions and validation tests pass.

---

## Phase 2: File-Backed Configuration Service and Unit Tests

### Status
- Not Started

### Goal
- Implement a file-backed `IProviderConfigurationService` and verify behavior through automated unit tests.

### Contract
- No new public surface beyond Phase 1.
- Internal implementation handles JSON read/write, validation, and active selection updates.
- Errors are returned with actionable messages for invalid configuration content.

### Tasks
- Implement JSON serialization/deserialization for provider configuration.
- Implement active provider resolution and selection update logic.
- Register `IProviderConfigurationService` in `AddCraterClawCore()`.
- Add isolated unit tests using temporary files (no external services).

### Tests
- Load valid configuration from JSON file.
- Save configuration and re-load with consistent values.
- Set active provider to a valid name and persist update.
- Reject set-active requests for unknown endpoint names.
- Return meaningful failure for invalid JSON content.

### Manual Verification
- Verify behavior through automated tests only in this phase.

---

## Phase 3: Console Selection Flow and End-to-End Verification

### Status
- Not Started

### Goal
- Wire the console harness to list configured endpoints, select active endpoint, and run connectivity checks using the selected endpoint.

### Contract
- No changes to library contracts from Phases 1-2.
- Console accepts configuration file path from argument or prompt (final mechanism to be documented if changed).
- Console output clearly identifies selected active endpoint and connectivity result.

### Tasks
- Add console flow to load configuration and show endpoint choices.
- Accept endpoint selection by name and persist active selection.
- Invoke existing `IProviderStatusService` against the active endpoint.
- Handle and report configuration errors without silent failure.

### Tests
- No new automated console tests required in this phase.
- Library tests from Phase 2 cover selection and persistence behavior.

### Manual Verification Plan
- Run console with a config file containing two endpoints.
- Select endpoint A and confirm connectivity check uses endpoint A.
- Select endpoint B and confirm connectivity check uses endpoint B.
- Restart console and confirm the latest active selection persists.

### Spec Sync
- If the console input approach changes (args vs prompt), update this plan and spec to match final behavior.

---

## Completion Criteria
- All three phase statuses are marked Done.
- `CraterClaw.Core` and `CraterClaw.Console` build successfully.
- Automated tests for configuration contracts and service behavior pass.
- Manual verification confirms endpoint selection and persistence behavior.
- `provider-config-spec.md` Status is updated to Done.
