# CraterClaw Library Configuration and Secret Management Spec

## Name
- CraterClaw Library Configuration and Secret Management

## Purpose
- Adopt `Microsoft.Extensions.Configuration` as the unified configuration backbone, replacing direct `JsonSerializer`-based config loading with a layered system that separates structure (config file), secrets (user secrets), and deployment overrides (environment variables).

## Scope
- Replace `FileProviderConfigurationService` and `IProviderConfigurationService` with `IOptions<ProviderOptions>` bound from `IConfiguration`.
- Consolidate `provider-config.json` and `mcp-config.json` into a single `craterclaw.json` config file with named dictionary sections.
- Change all array-structured config (endpoints list, servers list) to dictionary sections keyed by name. The name is the dictionary key, not a field inside the object. This makes user secret paths stable regardless of ordering.
- Layer configuration sources in order: `craterclaw.json` → user secrets → OS environment variables. Each layer overrides the previous.
- Config is read-only at runtime. Remove write-back capability (`SetActiveEndpointAsync`, `SaveAsync`). Active provider is set in the config file directly or selected per-session by the console without persisting.
- Add `IValidateOptions<ProviderOptions>` to enforce existing validation rules (unique names, valid URIs) at startup rather than at load time.
- Add `UserSecretsId` to the Console project so `dotnet user-secrets` can be used during development.
- Add `.gitignore` entries as a safety net against accidentally committed secret files.

## Config Schema

`craterclaw.json` is the single committed config file. Secret values (MCP env entries) are left as empty strings in this file; user secrets or OS environment variables supply the real values.

```json
{
  "providers": {
    "active": "local",
    "endpoints": {
      "local": {
        "baseUrl": "http://localhost:11434"
      },
      "wslWithLocalhostIp": {
        "baseUrl": "http://localhost:11435"
      }
    }
  },
  "mcpServers": {
    "qbittorrent": {
      "label": "qBitTorrent",
      "transport": "stdio",
      "command": "uvx",
      "args": ["--from", "git+https://github.com/jmagar/yarr-mcp", "qbittorrent-mcp-server"],
      "env": {
        "QBITTORRENT_URL": "",
        "QBITTORRENT_USER": "",
        "QBITTORRENT_PASS": "",
        "QBITTORRENT_MCP_TRANSPORT": "stdio"
      },
      "enabled": true
    }
  }
}
```

## Setting Secrets (Development)

User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\{id}\secrets.json` — outside the workspace and repository:

```
dotnet user-secrets set "McpServers:qbittorrent:Env:QBITTORRENT_URL" "http://192.168.1.x:8080"
dotnet user-secrets set "McpServers:qbittorrent:Env:QBITTORRENT_USER" "admin"
dotnet user-secrets set "McpServers:qbittorrent:Env:QBITTORRENT_PASS" "your-password"
```

Because server names are dictionary keys, these paths are stable — adding or reordering servers in `craterclaw.json` does not invalidate stored secrets.

## Setting Secrets (Deployment / Production)

OS environment variables override both the config file and user secrets. Set the same keys using environment variable naming (`:` becomes `__` on platforms that do not support `:` in env var names):

```
MCPSERVERS__QBITTORRENT__ENV__QBITTORRENT_URL=http://192.168.1.x:8080
```

Windows supports `:` directly in environment variable names, so the IConfiguration key format works as-is.

## Options Types

Options classes use mutable properties for IConfiguration binding. Named entries are represented as `Dictionary<string, T>`:

- `ProviderOptions`: `Active` (string?), `Endpoints` (Dictionary\<string, ProviderEndpointOptions\>)
- `ProviderEndpointOptions`: `BaseUrl` (string)
- `McpOptions` and `McpServerOptions` are defined in the mcp-config spec.

Validation is implemented via `IValidateOptions<ProviderOptions>` and registered at startup. Invalid configuration fails fast on first access.

## Active Provider Selection

- `providers:active` in `craterclaw.json` sets the default endpoint name for the session.
- The console shows the default and allows per-session override. The selection is not persisted.
- To change the default permanently, edit `craterclaw.json` directly.

## Contract Notes
- `IProviderConfigurationService` is removed. Consumers that need provider endpoint data inject `IOptions<ProviderOptions>` directly.
- `ProviderEndpoint` (Name, BaseUrl) is retained as a lightweight runtime record constructed from options at the call site.
- `ProviderConfiguration` is removed. Its validation logic moves to `IValidateOptions<ProviderOptions>`.
- `AddCraterClawCore()` accepts `IConfiguration` instead of file path strings.

## .gitignore Safety Net
- `.env`
- `*.secrets.json`
- `*.local.json`

## Out of Scope
- Windows Credential Manager or DPAPI integration
- Encryption of secrets at rest
- Secret rotation or expiry
- Web API or ASP.NET Core hosting

## Status
- Planning
