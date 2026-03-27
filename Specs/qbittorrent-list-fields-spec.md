# Spec: qbittorrent-list-fields

## Goal

`ListTorrentsAsync` has been extended to return four additional fields per torrent: `amount_left`, `priority`, `size`, and `category`. This spec covers the review fix and test work required to close the checkpoint cleanly.

## Scope

`CraterClaw.Core/QBitTorrentPlugin.cs` and `CraterClaw.Core.Tests/QBitTorrentPluginTests.cs` only.

---

## Phase 1: Fix stale description and update tests

**Status: Done**

### Context

The implementation change is complete. Two gaps remain:

1. `GetFunctionDescriptions()` still returns the old description for `ListTorrents`:
   `"List all torrents with name, hash, status, progress, and size."`
   This must match the `[Description]` attribute on the function.

2. The existing `ListTorrentsAsync_ReturnsJson_WhenAuthenticated` test only feeds the old three fields
   (`name`, `state`, `added_on`) and does not assert the new fields appear in the output.

### Contract

No interface or signature changes. The only changes are:

- `GetFunctionDescriptions()` — `ListTorrents` description updated to:
  `"List all torrents with name, state, added_on, amount_left, priority, size, and category."`

- `ListTorrentsAsync_ReturnsJson_WhenAuthenticated` — test JSON extended to include all seven fields;
  assertions added for each new field.

- New test `ListTorrentsAsync_ProjectsAllFields` — feeds a torrent with all seven fields populated,
  deserialises the result, and asserts each field has the expected value.

- New test `ListTorrentsAsync_HandlesAbsentFields` — feeds a torrent with only `name` present
  (all other fields absent from the raw JSON); asserts the result is valid JSON with no error.

### Tests

Update `ListTorrentsAsync_ReturnsJson_WhenAuthenticated`:

- Feed JSON with all seven fields: `name`, `state`, `added_on`, `amount_left`, `priority`, `size`, `category`.
- Add `Assert.Contains` for each new field name in the serialised output.

Add `ListTorrentsAsync_ProjectsAllFields`:

- Input: single torrent object with all seven fields set to known values.
- Assert: deserialise the result array; verify each field equals its expected value.

Add `ListTorrentsAsync_HandlesAbsentFields`:

- Input: single torrent object with only `"name":"partial"` — all other fields absent.
- Assert: result does not start with `"Error:"` and is parseable JSON.

### Implement

1. Fix `GetFunctionDescriptions()` in `QBitTorrentPlugin.cs` — update the `ListTorrents` description string.
2. Update `ListTorrentsAsync_ReturnsJson_WhenAuthenticated` in `QBitTorrentPluginTests.cs`.
3. Add `ListTorrentsAsync_ProjectsAllFields` test.
4. Add `ListTorrentsAsync_HandlesAbsentFields` test.
5. Run tests: `dotnet test CraterClaw.slnx`

### README Sync

No user-visible changes. README requires no update.

### Current Architecture Sync

Update `current-architecture.md` — revise the `ListTorrentsAsync` field list under the QBitTorrentPlugin section.

---

## Phase 2: Format output values

**Status: Done**

### Context

`ListTorrentsAsync` currently returns raw numeric values for `added_on` (Unix timestamp), `amount_left` (bytes), and `size` (bytes). These are not readable by the model without conversion. This phase formats them at projection time so the model receives human-readable strings.

### Contract

A private `FormatBytes(long bytes) -> string` helper is added to `QBitTorrentPlugin`. The projection in `ListTorrentsAsync` changes three fields:

- `added_on` — `long?` → `string?`: formatted as `"yyyy-MM-dd HH:mm"` (UTC) via `DateTimeOffset.FromUnixTimeSeconds`.
- `amount_left` — `long?` → `string?`: formatted by `FormatBytes`.
- `size` — `long?` → `string?`: formatted by `FormatBytes`.

`FormatBytes` thresholds: >= 1 GiB → `"X.X GB"`, >= 1 MiB → `"X.X MB"`, >= 1 KiB → `"X.X KB"`, otherwise `"N B"`. One decimal place for GiB/MiB/KiB.

The `[Description]` attribute and `GetFunctionDescriptions()` entry for `ListTorrents` are updated to reflect the formatted output.

### Tests

Update `ListTorrentsAsync_ProjectsAllFields`:
- `added_on` assertion changes from `GetInt64()` to `GetString()`, expected value `"2023-11-14 22:13"` (Unix 1700000001 in UTC).
- `amount_left` assertion changes from `GetInt64()` to `GetString()`, expected `"0 B"`.
- `size` assertion changes from `GetInt64()` to `GetString()`, expected `"4.7 GB"`.

Add `ListTorrentsAsync_FormatsBytes` — unit-style test feeding known byte values and asserting formatted strings for GB, MB, KB, and byte ranges.

### Implement

1. Add `FormatBytes` private static method to `QBitTorrentPlugin`.
2. Update the projection in `ListTorrentsAsync` for `added_on`, `amount_left`, `size`.
3. Update the `[Description]` attribute and `GetFunctionDescriptions()`.
4. Update `ListTorrentsAsync_ProjectsAllFields` assertions.
5. Add `ListTorrentsAsync_FormatsBytes` test.
6. Run tests: `dotnet test CraterClaw.slnx`

### README Sync

No user-visible changes.

### Current Architecture Sync

Update `current-architecture.md` — note the formatted types for `added_on`, `amount_left`, and `size`.

### Manual Verification Plan

Same as Phase 1 — trigger `ListTorrents` via the agentic loop and verify the response shows readable dates and sizes.

---

### Manual Verification Plan (Phase 1)

Dependencies: qBitTorrent running and configured in `craterclaw.json`.

1. Start the console harness (`craterclaw run`).
2. Select the qbittorrent behavior profile and run an agentic task that triggers `ListTorrents`.
3. Verify the response includes `amount_left`, `priority`, `size`, and `category` for each torrent.
