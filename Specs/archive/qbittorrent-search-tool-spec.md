# qBitTorrent Search Tool Spec

## Name
- qBitTorrent Search Tool

## Checkpoint
- qbittorrent-search-tool

## Depends On
- qbittorrent-plugin (checkpoint 10)

## Purpose
Add a `SearchTorrents` kernel function to `QBitTorrentPlugin` that queries qBitTorrent's built-in search plugin system and returns matching torrent results for use in the agentic execution loop.

## Scope
- Add `SearchTorrentsAsync` to `QBitTorrentPlugin` as a `[KernelFunction]`.
- The function starts a search job via the qBitTorrent search API, polls until the job finishes (or a timeout is reached), retrieves results, cleans up the job, and returns a JSON array of results.
- Update `GetFunctionDescriptions()` to include the new function.
- Add unit tests in `CraterClaw.Core.Tests`.
- Update README and `current-architecture.md`.

---

## Phase 1: SearchTorrents Kernel Function

**Status: Done**

### Contract

New method on `QBitTorrentPlugin`:

```csharp
[KernelFunction, Description("Search for torrents using installed search plugins. Returns a JSON array of results with fileName, fileUrl, fileSize, nbSeeders, nbLeechers, and siteUrl.")]
public async Task<string> SearchTorrentsAsync(
    [Description("The search query string.")] string query,
    [Description("The search category. Use 'all' for all categories, or a specific category: 'movies', 'music', 'software', 'games', 'anime', 'books'.")] string category = "all",
    [Description("Maximum number of results to return. Defaults to 10.")] int maxResults = 10,
    CancellationToken cancellationToken = default)
```

Return type: `string`. On success, a JSON array. On error, a string starting with `"Error:"`.

Result object shape (each element of the returned array):
```json
{
  "fileName": "string",
  "fileUrl": "string",
  "fileSize": number,
  "nbSeeders": number,
  "nbLeechers": number,
  "siteUrl": "string"
}
```

Result trimming rules applied before serialization:
- `fileName` is truncated to 120 characters if longer.
- `fileUrl` values that are magnet links (start with `magnet:`) have tracker parameters stripped: everything from the first `&tr=` onward is removed, retaining only `magnet:?xt=...&dn=...`. Non-magnet URLs are kept as-is.

`GetFunctionDescriptions()` gains one entry:
```
("SearchTorrents", "Search for torrents using installed search plugins.")
```

### Tests

In `CraterClaw.Core.Tests/QBitTorrentPluginTests.cs`, add tests using a mock `HttpMessageHandler`. Each test must mock the login call as well.

1. `SearchTorrentsAsync_ReturnsResults_WhenSearchCompletesAfterPolling`
   Mock: login succeeds; `start` returns `{ "id": 1 }`; first `status` poll returns `[{ "id": 1, "status": "Running", "total": 0 }]`; second `status` poll returns `[{ "id": 1, "status": "Stopped", "total": 2 }]`; `results` returns two items; `delete` returns 200.
   Assert: returned JSON deserializes to an array of length 2 containing the expected file names.

2. `SearchTorrentsAsync_ReturnsResults_WhenFirstStatusIsStopped`
   Mock: login succeeds; `start` returns `{ "id": 2 }`; first `status` poll immediately returns `[{ "id": 2, "status": "Stopped", "total": 1 }]`; `results` returns one item; `delete` returns 200.
   Assert: returned JSON deserializes to a non-empty array.

3. `SearchTorrentsAsync_ReturnsError_WhenNotConfigured`
   Construct plugin with `BaseUrl` set to empty string.
   Assert: return value starts with `"Error:"`.

4. `SearchTorrentsAsync_ReturnsEmptyArray_WhenResultsAreEmpty`
   Mock: all calls succeed; `results` response contains an empty array.
   Assert: return value is `"[]"` and the `delete` endpoint was called exactly once.

5. `SearchTorrentsAsync_TrimsMagnetTrackers`
   Mock: all calls succeed; `results` returns one item where `fileUrl` is a magnet link containing `&tr=udp://tracker.example.com:1337` after the `&dn=` segment.
   Assert: the `fileUrl` in the returned JSON does not contain `&tr=`.

6. `SearchTorrentsAsync_TruncatesLongFileName`
   Mock: all calls succeed; `results` returns one item where `fileName` is 200 characters long.
   Assert: the `fileName` in the returned JSON is 120 characters long.

### Implementation

`SearchTorrentsAsync` internal flow:

1. If `!IsConfigured`, return `"Error: qBitTorrent is not configured."`.
2. Wrap all logic in `try/catch`; on exception log the error and return `$"Error: {ex.Message}"`.
3. Start the search: POST `/api/v2/search/start` with form fields `pattern=<query>`, `plugins=all`, `category=<category>` via `SendAuthenticatedAsync`. Parse the JSON response body and read the `id` field as an `int`; store as `searchId`.
4. Poll for completion: loop up to 30 times with a 500ms delay between iterations.
   - GET `/api/v2/search/status?id=<searchId>` via `SendAuthenticatedAsync`.
   - Parse response as a JSON array. If any element with matching `id` has `status` equal to `"Stopped"` (case-insensitive), break out of the loop.
5. Fetch results: GET `/api/v2/search/results?id=<searchId>&limit=<maxResults>&offset=0` via `SendAuthenticatedAsync`. Parse the `results` JSON array from the response. Project each element to an anonymous object applying the trimming rules:
   - `fileName`: raw value truncated to 120 characters.
   - `fileUrl`: if the value starts with `magnet:` (case-insensitive), remove everything from the first `&tr=` onward; otherwise keep as-is.
   - `fileSize`, `nbSeeders`, `nbLeechers`, `siteUrl`: taken as-is.
6. Clean up: attempt to POST `/api/v2/search/delete` with form field `id=<searchId>` via `SendAuthenticatedAsync`. If this call throws, log a warning and continue — do not propagate the error.
7. Return `JsonSerializer.Serialize(projectedResults)`.

### README Sync

In the qBitTorrent section of README, add `SearchTorrents` to the list of available plugin functions.

### Current Architecture Sync

Under the `QBitTorrentPlugin` kernel functions list in `current-architecture.md`, add:
- `SearchTorrents` — starts a search job using installed qBitTorrent search plugins, polls until complete, returns a JSON array of results (fileName, fileUrl, fileSize, nbSeeders, nbLeechers, siteUrl). `maxResults` defaults to 10. File names are truncated to 120 characters and magnet link tracker parameters are stripped to reduce response size.

### Manual Verification Plan

Prerequisites:
- qBitTorrent running with WebUI enabled.
- At least one search plugin installed and enabled in qBitTorrent (Plugins > Search Plugins).
- Credentials set in user secrets (`qbittorrent:baseUrl`, `qbittorrent:username`, `qbittorrent:password`).

Steps:
1. Run the console harness.
2. Select an endpoint, a model, and the `qbittorrent-manager` profile.
3. Confirm `SearchTorrents` appears in the listed plugin functions.
4. Enter a task prompt such as "Search for 'ubuntu' and tell me the top result."
5. Confirm the model invokes `SearchTorrents`, streaming output appears, and the tools invoked summary includes `SearchTorrents`.
