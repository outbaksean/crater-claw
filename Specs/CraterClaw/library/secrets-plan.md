# CraterClaw Library Configuration and Secret Management Plan

## Decisions

- `craterclaw.json` replaces `provider-config.json`. The `mcp-config.json` file is not yet migrated in this plan; it is absorbed when the mcp-config spec is implemented fresh against IOptions.
- Options classes are mutable POCOs (not records) to satisfy IConfiguration binding requirements.
- `ProviderEndpoint` record is retained as a runtime type. It is constructed at the call site by pairing the dictionary key (name) with the `ProviderEndpointOptions.BaseUrl`.
- `IProviderConfigurationService` and `FileProviderConfigurationService` are removed. Their consumers are updated to use `IOptions<ProviderOptions>` directly.
- `ProviderConfiguration` record is removed. Its validation moves to `ProviderOptionsValidator : IValidateOptions<ProviderOptions>`, registered via `services.AddOptions<ProviderOptions>().ValidateOnStart()`.
- Active provider selection is per-session only. The console reads `providers:active` from options as the default, allows the user to override for the session, but makes no write calls.
- `AddCraterClawCore(IConfiguration configuration)` replaces the current string path parameters.
- `UserSecretsId` is added to `CraterClaw.Console.csproj`. The ID is `craterclaw-console`.
- Existing file-based service tests are removed and replaced with options-based unit tests that construct `IOptions<T>` directly via `Options.Create(...)`.

## Overview
- This plan migrates the configuration system in two phases.
- Phase 1 adds packages, defines options types, validation, and DI wiring.
- Phase 2 removes old services, updates the console, and updates tests.

---

## Phase 1: Packages, Options Types, Validation, and DI

### Status
- Done

### Goal
- Introduce `IConfiguration` and `IOptions<ProviderOptions>` as the new configuration backbone and wire everything through DI. The old services remain in place until Phase 2.

### Contract
- `ProviderOptions` class in `CraterClaw.Core`:
  - `string? Active`
  - `Dictionary<string, ProviderEndpointOptions> Endpoints`
- `ProviderEndpointOptions` class in `CraterClaw.Core`:
  - `string BaseUrl`
- `ProviderOptionsValidator` (internal, sealed) implementing `IValidateOptions<ProviderOptions>` in `CraterClaw.Core`:
  - Each endpoint name must be non-empty (guaranteed by dictionary key, but validate the BaseUrl is a valid absolute URI)
  - `Active`, if set, must match a key in `Endpoints`
- `AddCraterClawCore(IConfiguration configuration)` updated signature in `ServiceCollectionExtensions`
  - Registers `IOptions<ProviderOptions>` bound to the `providers` section
  - Registers `ProviderOptionsValidator` and calls `.ValidateOnStart()`
  - Retains existing service registrations unchanged for this phase

### Tasks
- Add NuGet packages to `CraterClaw.Core.csproj`:
  - `Microsoft.Extensions.Configuration.Abstractions` (10.0.0)
  - `Microsoft.Extensions.Options.ConfigurationExtensions` (10.0.0)
- Add NuGet packages to `CraterClaw.Console.csproj`:
  - `Microsoft.Extensions.Configuration.Json` (10.0.0)
  - `Microsoft.Extensions.Configuration.UserSecrets` (10.0.0)
  - `Microsoft.Extensions.Configuration.EnvironmentVariables` (10.0.0)
- Add `<UserSecretsId>craterclaw-console</UserSecretsId>` to `CraterClaw.Console.csproj`.
- Add `ProviderOptions.cs` and `ProviderEndpointOptions.cs` in `CraterClaw.Core`.
- Add `ProviderOptionsValidator.cs` (internal, sealed) in `CraterClaw.Core`.
- Update `ServiceCollectionExtensions.AddCraterClawCore()` to accept `IConfiguration` and register `IOptions<ProviderOptions>`.
- Update `CraterClaw.Console/Program.cs` to build `IConfiguration`:
  ```csharp
  var configuration = new ConfigurationBuilder()
      .AddJsonFile(configPath, optional: false)
      .AddUserSecrets<Program>()
      .AddEnvironmentVariables()
      .Build();
  services.AddCraterClawCore(configuration);
  ```
- Create `craterclaw.json` in the Console project output directory (copy existing endpoints from `provider-config.json` into the new schema).
- Add to `.gitignore`:
  ```
  # Secret files
  .env
  *.secrets.json
  *.local.json
  ```
- Add `ProviderOptionsValidatorTests.cs` in `CraterClaw.Core.Tests`.

### Tests
`ProviderOptionsValidatorTests`:
- Valid options with multiple named endpoints and a matching `Active` passes validation.
- Invalid `BaseUrl` (not an absolute URI) produces a validation failure.
- `Active` set to a name not in `Endpoints` produces a validation failure.
- Empty `Endpoints` dictionary produces a validation failure.

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes.
- Console starts without error using `craterclaw.json`.

---

## Phase 2: Remove Old Services, Update Console, and Update Tests

### Status
- Done

### Goal
- Remove `IProviderConfigurationService`, `FileProviderConfigurationService`, and `ProviderConfiguration`. Update all consumers to use `IOptions<ProviderOptions>`. Update the console to use per-session active selection. Remove file-based tests.

### Contract
- No new public surface.
- `ProviderEndpoint` record is unchanged and remains the runtime type passed to `IProviderStatusService` and `IModelListingService`.
- Console resolves the active endpoint by reading `ProviderOptions.Active` as the default, displaying it, and allowing the user to override for the session.

### Tasks
- Delete `IProviderConfigurationService.cs`, `FileProviderConfigurationService.cs`, and `ProviderConfiguration.cs` from `CraterClaw.Core`.
- Update `ServiceCollectionExtensions.AddCraterClawCore()` to remove `IProviderConfigurationService` registration.
- Update `CraterClaw.Console/Program.cs`:
  - Inject `IOptions<ProviderOptions>` instead of `IProviderConfigurationService`.
  - Build the endpoint list from `options.Value.Endpoints` (each key-value pair becomes a `ProviderEndpoint`).
  - Mark the entry matching `options.Value.Active` as default in the numbered list.
  - Resolve the selected `ProviderEndpoint` in-session; do not call any save/persist method.
- Delete `FileProviderConfigurationServiceTests` class from `CraterClaw.Core.Tests`.
- Delete `ProviderConfigurationContractTests` class from `CraterClaw.Core.Tests` (validation now covered by `ProviderOptionsValidatorTests`).
- Update `OllamaProviderStatusServiceTests` and `OllamaModelListingServiceTests` if they reference removed types (unlikely — they test services that take `ProviderEndpoint` directly).

### Tests
- No new tests required; Phase 1 tests cover the options validation surface.

### Manual Verification Plan
- Run the console with `craterclaw.json` containing two endpoints and `active` set to one of them.
- Confirm the active endpoint is shown as the default in the numbered list.
- Select a different endpoint and confirm status check and model listing use the selected endpoint.
- Confirm that editing `providers:active` in `craterclaw.json` and restarting the console changes the default.
- Set a user secret (`dotnet user-secrets set "Providers:Active" "wslWithLocalhostIp"`) and confirm it overrides the file value.

---

## Completion Criteria
- Both phase statuses are marked Done.
- `CraterClaw.Core`, `CraterClaw.Console`, and `CraterClaw.Core.Tests` build successfully with no warnings.
- All automated tests pass.
- `IProviderConfigurationService`, `FileProviderConfigurationService`, and `ProviderConfiguration` do not exist in the codebase.
- `dotnet user-secrets` overrides config file values correctly.
- `secrets-spec.md` Status is updated to Done.
