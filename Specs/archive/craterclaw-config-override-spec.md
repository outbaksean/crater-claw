# Spec: craterclaw-config-override

## Goal

Add support for an alternate `craterclaw.json` path in the `craterclaw run` commands. When running the console harness or API, an explicit config file can be specified without editing the default committed file. The PowerShell module passes the resolved absolute path to the .NET app via a CLI argument.

## Checkpoint Deliverable

- `CraterClaw.Console` and `CraterClaw.Api` both accept an optional `--config <path>` CLI argument. When present, the specified file is loaded instead of the default `craterclaw.json` in the application base directory.
- `craterclaw run` accepts a `-Config <path>` parameter (relative or absolute). The path is resolved to an absolute path in PowerShell before being passed to the .NET app.
- `-Config` applies to the console harness and the API. It has no effect when used with `-WebOnly`.
- When `--config` is not supplied, existing behavior is unchanged.

---

## Phase 1: Config path override in .NET apps and PowerShell

**Status:** Done

### Contract

**Console (`CraterClaw.Console/Program.cs`)**

Parse `--config <path>` from `args` before building configuration. If present and non-empty, use the specified path. Otherwise fall back to `Path.Combine(AppContext.BaseDirectory, "craterclaw.json")`.

```csharp
// extract --config <path> from args
static string ResolveConfigPath(string[] args, string defaultPath)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == "--config")
            return args[i + 1];
    return defaultPath;
}
```

The extracted `--config` argument does not need to be stripped from `args` since the console does not pass `args` to any framework that would misinterpret it.

**API (`CraterClaw.Api/Program.cs`)**

Parse and strip `--config <path>` from `args` before passing to `WebApplication.CreateBuilder`. Otherwise the extra argument may cause `AddCommandLine` to fail or produce unexpected config keys.

```csharp
// strip --config <path> from args, return (filteredArgs, configPath)
static (string[] Args, string ConfigPath) ParseArgs(string[] args, string defaultPath)
{
    var filtered = new List<string>();
    string configPath = defaultPath;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--config" && i + 1 < args.Length)
        {
            configPath = args[i + 1];
            i++; // skip value
        }
        else
        {
            filtered.Add(args[i]);
        }
    }
    return (filtered.ToArray(), configPath);
}
```

**PowerShell (`tools/CraterClaw.psm1`)**

Add `-Config` string parameter to the `craterclaw` function and `Invoke-CcRun`. Resolve to absolute path using `Resolve-Path`. Pass as `-- --config <absolutePath>` to `dotnet run` for applicable modes.

```powershell
# console mode
& dotnet run --project $consolePath -- --config $resolvedConfig

# api in separate window
Start-Process $psExe -ArgumentList '-NoExit', '-Command',
    "dotnet run --project `"$apiPath`" -- --config `"$resolvedConfig`""
```

When `-Config` is not supplied, the `-- --config` argument is omitted entirely (existing behavior preserved).

### Tests

**`CraterClaw.Api.Tests`** — add a test class `ConfigOverrideTests` in the existing test project. Write a temporary `craterclaw.json` with a known provider name, pass `--config <tempPath>` via `WebApplicationFactory`, and assert the provider appears in `GET /api/providers`.

```csharp
[Fact]
public async Task Config_override_loads_specified_file()
{
    // Arrange: write temp json with a distinct endpoint name
    // Act: factory = new WebApplicationFactory<Program>()
    //          .WithWebHostBuilder(b => b.UseSetting(...))
    //      -- but use the args mechanism instead
    // Assert: GET /api/providers returns the endpoint from temp file
}
```

Note: `WebApplicationFactory` does not natively pass CLI args. The override is applied via the `ASPNETCORE_` env var mechanism or a custom `IConfiguration` override in `WithWebHostBuilder`. The test should verify config override by injecting the path via `WebHostBuilder.UseSetting("configOverridePath", tempPath)` — or, more directly, by refactoring `ParseArgs` into a small static helper that can be called from test setup. See implementation section for the chosen approach.

> Implementation note: the cleanest testable surface is a helper `ParseArgs(string[] args, string defaultPath)` extracted to a file-scoped internal static class in `CraterClaw.Api`. The test instantiates the helper directly via `[assembly: InternalsVisibleTo("CraterClaw.Api.Tests")]`.

Alternatively — and more simply — the test can start the API with an environment variable `CRATERCLAW_CONFIG` set to the temp path and have the API check that env var as the override source. This avoids modifying the app's startup arg handling and is easier to inject in tests.

**Chosen approach: `ASPNETCORE_` config key override is not needed — use `WebHostBuilder.UseEnvironment` + inline `IConfiguration` override.**

Actually the simplest testable approach: both apps check environment variable `CRATERCLAW_CONFIG` first, then `--config` arg, then the default path. In tests, set `Environment.SetEnvironmentVariable("CRATERCLAW_CONFIG", tempPath)` before constructing the factory. Clear it in teardown.

**Revised contract:**

Priority order for config path resolution (highest to lowest):
1. `--config <path>` CLI argument
2. `CRATERCLAW_CONFIG` environment variable
3. Default: `Path.Combine(AppContext.BaseDirectory, "craterclaw.json")`

The PowerShell module uses the CLI argument path (not the env var). The env var exists to support test injection without arg manipulation.

### Implementation

1. **`CraterClaw.Console/Program.cs`**: add `ResolveConfigPath` static local function; call it before `ConfigurationBuilder`. No change to existing config sources.

2. **`CraterClaw.Api/Program.cs`**: add `ResolveConfigPath` static local function checking env var then `args`; call it before `builder.Configuration.AddJsonFile`. Strip the `--config` pair from `args` before `WebApplication.CreateBuilder(args)` to avoid `AddCommandLine` failures.

3. **`CraterClaw.Api.Tests/ConfigOverrideTests.cs`** (new): write temp config, set `CRATERCLAW_CONFIG` env var, create `WebApplicationFactory<Program>`, call `GET /api/providers`, assert expected endpoint present. Clear env var in `Dispose`.

4. **`tools/CraterClaw.psm1`**: add `-Config [string]` parameter; resolve to absolute path when supplied; thread through `Invoke-CcRun`; append `-- --config <path>` to `dotnet run` calls when non-empty.

5. Update `Write-CcUsage` to document `-Config`.

### README Sync

- Add `-Config <path>` to the commands table under `craterclaw run`.
- Add a note in the Configuration section: "To run with an alternate config file without changing the committed `craterclaw.json`, use `craterclaw run -Config <path>`."

### Current Architecture Sync

Update `current-architecture.md`:
- Console Harness Flow: note that `--config <path>` overrides the default config file path.
- PowerShell module: document `-Config` parameter.
- Configuration: note the `CRATERCLAW_CONFIG` env var override.

### Manual Verification Plan

**Dependencies:** none beyond normal build.

1. Copy `craterclaw.json` to `my-test.json`. Change a provider name to something unique (e.g. `"test-override"`).
2. Run `craterclaw run -Console -Config .\my-test.json` — verify the provider list shows `test-override`.
3. Run `craterclaw run -ApiOnly -Config .\my-test.json` — verify `GET /api/providers` returns `test-override`.
4. Run `craterclaw run -Console` (no `-Config`) — verify original provider names appear unchanged.
5. Delete `my-test.json`.
