# IDE Formatting Spec

## Name
- ide-formatting

## Checkpoint
- ide-formatting

## Purpose
Ensure VS Code format on save, `dotnet format`, and `npm run lint:fix` all produce identical output. Currently `.editorconfig` sets `end_of_line = crlf` globally, which conflicts with Prettier's `endOfLine: lf`. Fix the conflict and add VS Code workspace settings so the correct formatter runs automatically on save for each file type.

## Design

### Line Endings

LF everywhere. The codebase targets Linux deployment and git should not be converting line endings. `.editorconfig` global `end_of_line` changes from `crlf` to `lf`. Prettier already has `endOfLine: lf`.

### VS Code Settings

`.vscode/settings.json` (new file) sets:
- `"editor.formatOnSave": true`
- Prettier as the default formatter for JS, TS, Vue, JSON, and MJS files
- The C# extension as the default formatter for C# files
- `"editor.defaultFormatter"` scoped per language

### dotnet format

`dotnet format` is already available. No additional configuration needed — it reads from `.editorconfig` for C# style rules.

---

## Phase 1: Reconcile Line Endings and Add VS Code Settings

**Status: Done**

### Contract

Changes:
- `.editorconfig` — change global `end_of_line` from `crlf` to `lf`; add overrides for JS/TS/Vue/MJS files with `indent_size = 2`
- `.vscode/settings.json` — new file with formatter and format-on-save settings
- `CraterClaw.Web/.prettierrc.json` — no change needed, already has `endOfLine: lf`

### Tests

No automated tests. Manual verification only.

### Implement

1. Update `.editorconfig`:
   - Change global `end_of_line = crlf` to `end_of_line = lf`
   - Add section for web files (`*.{js,ts,mjs,vue}`) with `indent_size = 2`

2. Create `.vscode/settings.json`:
```json
{
    "editor.formatOnSave": true,
    "editor.defaultFormatter": "esbenp.prettier-vscode",
    "[csharp]": {
        "editor.defaultFormatter": "ms-dotnettools.csharp"
    }
}
```

3. Run `npm run lint:fix` in `CraterClaw.Web` to reformat all web files to LF with correct indentation.

4. Run `dotnet format CraterClaw.slnx` to reformat C# files.

### README Sync

Add a `## Formatting` section noting:
- C#: `dotnet format .\CraterClaw.slnx`
- Vue/TS: `npm run lint:fix` from `CraterClaw.Web`
- VS Code formats on save automatically when the Prettier and C# extensions are installed

### Current Architecture Sync

Update `.editorconfig` description: LF line endings everywhere, 2-space indent for web files, 4-space for C#/JSON.

### Manual Verification Plan

Prerequisites: Prettier (`esbenp.prettier-vscode`) and C# (`ms-dotnettools.csharp`) VS Code extensions installed.

1. Run `npm run lint` from `CraterClaw.Web` — zero errors or warnings.
2. Open a `.vue` file in VS Code, make a whitespace change, save — file should be reformatted by Prettier without introducing CRLF.
3. Open a `.cs` file, make a whitespace change, save — file should be reformatted by the C# extension.
4. Run `dotnet format .\CraterClaw.slnx` — should report no changes needed.
