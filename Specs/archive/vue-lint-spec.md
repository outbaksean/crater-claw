# Vue Lint Spec

## Name
- vue-lint

## Checkpoint
- vue-lint

## Purpose
Add ESLint to `CraterClaw.Web` with Vue and TypeScript support so that `npm run lint` and `npm run lint:fix` work end to end. Required before the `powershell-aliases` checkpoint so the `craterclaw format` command has a real lint target.

## Design

Use the flat config format (`eslint.config.js`) introduced in ESLint 9. Install:
- `eslint`
- `eslint-plugin-vue`
- `@typescript-eslint/eslint-plugin`
- `@typescript-eslint/parser`
- `globals` (for environment globals in flat config)

Configure Vitest globals in the ESLint config so test files do not produce false positives for undefined globals (`describe`, `it`, `expect`, etc.).

---

## Phase 1: ESLint Setup

**Status: Done**

### Contract

No C# changes. Deliverables:
- `CraterClaw.Web/eslint.config.js`
- Updated `CraterClaw.Web/package.json` with new dev dependencies and scripts

New scripts in `package.json`:
```json
"lint": "eslint src",
"lint:fix": "eslint src --fix"
```

### Tests

No new automated tests. `npm run lint` passing on the existing source is the verification.

### Implement

1. Install dev dependencies:
   ```
   npm install --save-dev eslint eslint-plugin-vue @typescript-eslint/eslint-plugin @typescript-eslint/parser globals
   ```

2. Create `eslint.config.js`:
   - Apply `eslint-plugin-vue` recommended rules for `.vue` files
   - Apply `@typescript-eslint` recommended rules for `.ts` files
   - Register Vitest globals (`describe`, `it`, `test`, `expect`, `beforeEach`, `afterEach`, `vi`) for test files so they are not flagged as undefined

3. Add `lint` and `lint:fix` scripts to `package.json`.

4. Run `npm run lint` and fix any errors surfaced in existing source files. Warnings may be left as-is.

### README Sync

Add `npm run lint` and `npm run lint:fix` to the Vue Frontend section of the README.

### Current Architecture Sync

Update `CraterClaw.Web` section to note ESLint is configured with Vue and TypeScript plugins, flat config format, Vitest globals registered for test files.

### Manual Verification Plan

1. Run `npm run lint` from `CraterClaw.Web` — should complete with no errors.
2. Introduce a deliberate lint error (e.g. an unused variable), run `npm run lint` — should report it.
3. Run `npm run lint:fix` — should fix auto-fixable issues.
4. Run `npm test` — existing Vitest tests should still pass.
