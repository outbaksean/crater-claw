# CraterClaw Library qBitTorrent Plugin Plan

## Decisions

- `QBitTorrentPlugin` is registered as a singleton so the session cookie (`SID`) is cached across calls. DI uses a factory lambda to construct it with a dedicated `HttpClient` from `IHttpClientFactory`.
- Cookies are managed manually: `HttpClient` does not use a `CookieContainer`. The `SID` value is parsed from the `Set-Cookie` response header on login and stored as a nullable field. Requests include `Cookie: SID=<value>` as a header.
- On 403, the plugin clears `_sid`, re-authenticates once, and retries the original request.
- All kernel functions return `string`. Success responses return JSON from the qBitTorrent API directly. Failure responses return a plain-text error string prefixed with `"Error: "`.
- `QBitTorrentOptions` validation is conditional: if `BaseUrl` is null or empty, the options are treated as unconfigured (no validation failure). If `BaseUrl` is set, `Username` and `Password` are also required.
- `QBitTorrentPlugin` exposes a `static IReadOnlyList<(string Name, string Description)> GetFunctionDescriptions()` method so Program.cs can list functions without taking a dependency on SK types directly.
- The MCP tool listing loop (iterating `IMcpClientProvider`) is removed from Program.cs. `IMcpClientProvider` and `McpClientProvider` remain registered for future use.

---

## Phase 1: Options, Plugin Implementation, and Tests

### Status
- Done

### Goal
- Implement `QBitTorrentOptions`, its validator, and `QBitTorrentPlugin` with all six kernel functions and session auth. Register in DI. Cover with unit tests using a mock `HttpMessageHandler`.

### Contract

**`QBitTorrentOptions`** (public, `CraterClaw.Core`):
```csharp
public sealed class QBitTorrentOptions
{
    public string? BaseUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
```

**`QBitTorrentPlugin`** (public, `CraterClaw.Core`):
```csharp
[KernelPlugin(Name = "qbittorrent", Description = "Manage qBitTorrent downloads")]
public sealed class QBitTorrentPlugin(
    HttpClient httpClient,
    IOptions<QBitTorrentOptions> options,
    ILogger<QBitTorrentPlugin> logger)
{
    [KernelFunction, Description("List all torrents with name, hash, status, progress, and size.")]
    public Task<string> ListTorrentsAsync(CancellationToken cancellationToken = default);

    [KernelFunction, Description("Add a torrent from a magnet link or HTTP URL.")]
    public Task<string> AddTorrentByUrlAsync(
        [Description("The magnet link or HTTP URL.")] string url,
        CancellationToken cancellationToken = default);

    [KernelFunction, Description("Pause a torrent by its hash.")]
    public Task<string> PauseTorrentAsync(
        [Description("The torrent hash.")] string hash,
        CancellationToken cancellationToken = default);

    [KernelFunction, Description("Resume a paused torrent by its hash.")]
    public Task<string> ResumeTorrentAsync(
        [Description("The torrent hash.")] string hash,
        CancellationToken cancellationToken = default);

    [KernelFunction, Description("Delete a torrent by its hash, with optional file deletion.")]
    public Task<string> DeleteTorrentAsync(
        [Description("The torrent hash.")] string hash,
        [Description("Whether to also delete downloaded files.")] bool deleteFiles = false,
        CancellationToken cancellationToken = default);

    [KernelFunction, Description("Get current transfer statistics including speeds and session totals.")]
    public Task<string> GetTransferStatsAsync(CancellationToken cancellationToken = default);

    public static IReadOnlyList<(string Name, string Description)> GetFunctionDescriptions();
}
```

### qBitTorrent WebUI API Endpoints Used

| Operation | Method | Path | Body |
|---|---|---|---|
| Login | POST | `/api/v2/auth/login` | form: `username=&password=` |
| List torrents | GET | `/api/v2/torrents/info` | - |
| Add torrent | POST | `/api/v2/torrents/add` | form: `urls=<url>` |
| Pause | POST | `/api/v2/torrents/pause` | form: `hashes=<hash>` |
| Resume | POST | `/api/v2/torrents/resume` | form: `hashes=<hash>` |
| Delete | POST | `/api/v2/torrents/delete` | form: `hashes=<hash>&deleteFiles=true\|false` |
| Transfer stats | GET | `/api/v2/transfer/info` | - |

Login response: body is `"Ok."` on success or `"Fails."` on bad credentials. `Set-Cookie: SID=<value>` is set on success. Parse the SID value from the header with a string split on `=` and `;`.

### Tasks

**`CraterClaw.Core`**
- Add `QBitTorrentOptions.cs`.
- Add `QBitTorrentOptionsValidator.cs` (internal, sealed): no failure if `BaseUrl` is null/empty; otherwise require valid URI, non-empty `Username`, non-empty `Password`.
- Add `QBitTorrentPlugin.cs`:
  - Private fields: `_client` (HttpClient), `_options` (QBitTorrentOptions), `_logger`, `_sid` (string?, initially null).
  - Private `Task EnsureAuthenticatedAsync(CancellationToken)`: skips if `_sid` is not null; POSTs login form; parses SID from `Set-Cookie`; throws `InvalidOperationException` if response body is not `"Ok."`.
  - Private `Task<HttpResponseMessage> SendAuthenticatedAsync(HttpRequestMessage, CancellationToken)`: calls `EnsureAuthenticatedAsync`, adds `Cookie: SID=<_sid>`, sends; on 403 clears `_sid`, re-authenticates once, retries.
  - Six kernel functions each calling `SendAuthenticatedAsync` with the appropriate request and returning the response body as a string (or an `"Error: "` prefix string on failure).
  - Static `GetFunctionDescriptions()` returns a hardcoded list of `(Name, Description)` tuples matching the six functions.
- Update `ServiceCollectionExtensions.AddCraterClawCore()`:
  - `services.AddOptions<QBitTorrentOptions>().Bind(configuration.GetSection("qbittorrent")).ValidateOnStart()`
  - `services.AddSingleton<IValidateOptions<QBitTorrentOptions>, QBitTorrentOptionsValidator>()`
  - `services.AddHttpClient("qbittorrent")`
  - Factory singleton: `services.AddSingleton(sp => new QBitTorrentPlugin(sp.GetRequiredService<IHttpClientFactory>().CreateClient("qbittorrent"), sp.GetRequiredService<IOptions<QBitTorrentOptions>>(), sp.GetRequiredService<ILogger<QBitTorrentPlugin>>()))`

**`CraterClaw.Core.Tests`**
- Add `QBitTorrentPluginTests.cs` using the `DelegatingTestHandler` pattern from existing tests.

### Tests

`QBitTorrentPluginTests`:
- `ListTorrentsAsync_ReturnsJson_WhenAuthenticated`: handler returns login "Ok." then torrent list JSON; assert result contains torrent data.
- `ListTorrentsAsync_ReauthenticatesAndRetries_On403`: handler returns login "Ok.", then 403, then login "Ok." again, then torrent list JSON; assert result contains torrent data (not an error).
- `AddTorrentByUrlAsync_ReturnsSuccess_WhenOkResponse`: handler returns login "Ok." then 200 "Ok."; assert result does not start with "Error:".
- `PauseTorrentAsync_ReturnsSuccess`: handler returns login "Ok." then 200; assert no error.
- `ResumeTorrentAsync_ReturnsSuccess`: handler returns login "Ok." then 200; assert no error.
- `DeleteTorrentAsync_ReturnsSuccess`: handler returns login "Ok." then 200; assert no error.
- `GetTransferStatsAsync_ReturnsJson`: handler returns login "Ok." then stats JSON; assert result contains speed fields.
- `ListTorrentsAsync_ReturnsError_WhenLoginFails`: handler returns login "Fails."; assert result starts with "Error:".

### Manual Verification
- `dotnet build CraterClaw.slnx` succeeds.
- `dotnet test CraterClaw.slnx --no-build` passes with new tests included.

---

## Phase 2: Console Update and Config

### Status
- Done

### Goal
- Update `craterclaw.json` with a `qbittorrent` config section, remove the MCP tool listing loop from Program.cs, and add plugin function listing for the `qbittorrent-manager` profile.

### Tasks

**`CraterClaw.Console/craterclaw.json`**
- Add a `qbittorrent` section with empty placeholder values:
  ```json
  "qbittorrent": {
      "baseUrl": "",
      "username": "",
      "password": ""
  }
  ```

**`CraterClaw.Console/Program.cs`**
- Add `var qBitTorrentPlugin = provider.GetRequiredService<QBitTorrentPlugin>();` to the DI resolution block.
- Remove the `foreach (var server in permittedServers)` MCP tool listing loop (the `McpClient` block).
- Replace the removed block with: if `permittedServers.Count > 0`, call `QBitTorrentPlugin.GetFunctionDescriptions()` and print each function as `{i}. {name} - {description}`.
- Keep the `permitted`/`permittedServers` filter logic; the count check gates whether any listing occurs.

**`README.md`**
- Update Prerequisites to replace the MCP clone note with user secrets instructions for `qbittorrent:*`.
- Update Configuration section: add `qbittorrent` secrets block:
  ```powershell
  dotnet user-secrets set "qbittorrent:baseUrl"  "http://192.168.1.x:8080" --project .\CraterClaw.Console
  dotnet user-secrets set "qbittorrent:username" "admin"                    --project .\CraterClaw.Console
  dotnet user-secrets set "qbittorrent:password" "your-password"            --project .\CraterClaw.Console
  ```
- Update Console Flow: replace step 9 to reflect plugin function listing instead of MCP tool listing.

### Tests
- No new tests required.

### Manual Verification Plan
- Prerequisites: qBitTorrent running with WebUI enabled; `qbittorrent:*` credentials set in user secrets.
- Run the console harness and select the `qbittorrent-manager` profile.
- Confirm the six plugin functions are printed by name and description.
- Select the `no-tools` profile and confirm no functions are listed.

---

## Completion Criteria
- Both phase statuses are marked Done.
- All automated tests pass.
- Manual verification confirms function listing for the qbittorrent-manager profile.
- `qbittorrent-plugin-spec.md` Status is updated to Done.
