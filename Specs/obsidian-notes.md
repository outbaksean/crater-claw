# Obsidian Vault Structure — Investigation Notes

**Status:** Pre-investigation — options and research questions only

This file is the working document for the `investigate-obsidian` checkpoint. The goal is to settle on a vault layout that works well for both human navigation in Obsidian and deterministic file access by AI agents. Output is a decision recorded at the bottom of this file.

---

## Constraints

- The scaffold step creates the vault structure deterministically from story inputs — no AI involved.
- Agent system prompts reference files by known, hard-coded paths. The structure must be stable and unambiguous.
- The Context Helper's job is to read relevant files and summarise them before a specialist runs. The structure should make it easy to retrieve "everything about characters" or "the current outline" without listing the whole vault.
- Agents run on local models with limited context windows. Files should be scoped — one concern per file rather than one giant file per story.
- The structure must work for both short stories and novels without being redesigned between them.

---

## Structural Options

### Option A: Flat by category

One file per concern at the root level. Simple, easy to reference.

```
/vault-root/
  premise.md
  characters.md
  setting.md
  plot.md
  theme.md
  outline.md
  draft.md
  review.md
  notes.md
```

Pros:
- Every file path is predictable and short.
- Easy for agents to reference: "read `characters.md`".
- Scaffold step is trivial — create each file with a header and any initial content from story inputs.

Cons:
- `draft.md` becomes very large for novels — context window pressure.
- No way to work on chapter 2 without loading chapter 1.
- `characters.md` with 10+ characters is a large, unstructured blob.

Suitable for: short stories. Probably not novels.

---

### Option B: Hierarchical by category, flat within

Subdirectory per concern, one file per entity or section within it.

```
/vault-root/
  premise.md
  outline.md
  characters/
    [name].md       (one file per character)
  setting/
    overview.md
    locations/
      [name].md     (one file per location)
  plot/
    acts.md
    scenes.md
  theme/
    themes.md
  drafts/
    chapter-01.md
    chapter-02.md
  reviews/
    chapter-01.md
  notes.md
```

Pros:
- Scales to novels — chapters and characters are separate files.
- Context Helper can retrieve "all files in `characters/`" for a targeted summary.
- Humans navigating in Obsidian get a sensible sidebar structure.
- Agents can reference specific entities: "read `characters/elara.md`".

Cons:
- Scaffold step is more complex — needs to create directories and populate character/location files from inputs.
- Agent system prompts need to know the naming convention for dynamically created files (e.g. how character names map to file names).
- Path references in system prompts need to handle variable file names (e.g. "list files in `characters/` and read each one").

Suitable for: short stories and novels. Recommended starting point.

---

### Option C: Phase-based

Organised around pipeline stages rather than content type.

```
/vault-root/
  inputs/
    premise.md
    parameters.md     (story type, length, etc.)
  world/
    characters.md
    setting.md
    theme.md
  structure/
    outline.md
    plot.md
  output/
    drafts/
      chapter-01.md
    reviews/
      chapter-01.md
```

Pros:
- Maps clearly to pipeline stages — each agent knows which directory is its domain.
- Clean separation between immutable inputs and evolving output.

Cons:
- Less intuitive for human navigation in Obsidian.
- "World" is an odd grouping that mixes characters, setting, and theme.
- Doesn't obviously improve on Option B for agent access patterns.

Suitable for: possibly, but Option B seems more natural for both humans and agents.

---

## Obsidian-Specific Questions

### Frontmatter

Obsidian supports YAML frontmatter in every file:

```markdown
---
type: character
status: draft
tags: [protagonist, hero]
---
```

Potential uses:
- `status` field lets the Reviewer mark a draft as reviewed without editing the content.
- `type` field lets the Context Helper filter files by concern without reading content.
- Agents can update frontmatter independently of prose content.

Research question: does frontmatter add enough value to justify the parsing overhead in agent prompts, or does a clean directory structure make it redundant?

### Wikilinks

Obsidian renders `[[character-name]]` as a clickable link in the UI. Agents writing prose could use wikilinks to cross-reference characters and locations, which would make the vault more navigable for humans.

Research question: do wikilinks survive round-trips through agent writes without being mangled? Are they worth the convention overhead?

### Dataview Plugin

Obsidian's Dataview plugin can query frontmatter fields across the vault (e.g. "list all characters where status = active"). This is powerful for human navigation but agents cannot run Dataview queries — they read files directly.

Verdict: probably not relevant for the agent pipeline. Worth having for human use if the convention is already there.

---

## File Naming Conventions

For dynamically created files (characters, locations, chapters), the naming convention needs to be consistent enough that agents can construct paths without listing the directory first.

Options:
- **Slugified name**: `characters/elara-voss.md` — readable, predictable, but requires a slug function in the scaffold step.
- **Lowercase with hyphens**: same as above, just stated explicitly as the rule.
- **Numbered**: `characters/01-elara.md` — preserves order but adds noise to names.
- **Exact input value lowercased**: simplest to implement in the scaffold step, fragile if the input contains special characters.

---

## Research Questions

1. **Short story vs novel threshold** — at what length does Option A break down and Option B become necessary? Is it worth having two structure variants, or should Option B be the universal default?

2. **Frontmatter value for agents** — try giving an agent a vault with and without frontmatter and see whether it improves retrieval accuracy in Context Helper tasks.

3. **Directory listing as navigation** — can `ListFiles(directory)` in the `MarkdownFilePlugin` reliably substitute for wikilinks and Dataview for agent navigation? If yes, wikilinks are optional decoration.

4. **Context window impact** — measure roughly how many tokens a typical `characters/` directory consumes when all files are concatenated. This determines whether the Context Helper step is necessary or whether specialists can load the full vault directly.

5. **Write consistency** — when an agent appends to or rewrites a file, does it reliably preserve frontmatter and existing structure, or does it tend to reformat or drop metadata? Test before committing to frontmatter conventions.

6. **Obsidian sync** — if the vault lives on the local filesystem and is opened in Obsidian simultaneously, are there any file locking or sync conflicts to be aware of when agents write files?

---

## Decision

*(To be filled in after investigation)*

- Chosen structure: 
- Frontmatter: yes / no / optional
- Wikilinks: yes / no / optional
- File naming convention: 
- Notes:
