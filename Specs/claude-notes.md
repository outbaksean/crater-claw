# Claude Code Notes

Tips and patterns for getting more value out of Claude Code on this project.

---

## Parallel Agents

Claude Code can run agents in the background in an isolated git worktree while you continue working. This is useful for well-defined tasks that don't depend on your current work.

Example use cases for this project:

- Running `remove-mcp` (a pure deletion across known files) in the background while working on something else
- Generating infrastructure config files (smb.conf, cloud-init YAML) while reviewing a spec
- Running the full test suite in the background after a change

To trigger parallel work, tell Claude explicitly: "run this in the background" or "do these two things in parallel." Claude will use a worktree so changes are isolated until you're ready to review them.

The worktree is automatically cleaned up if no changes are made. If changes are made, Claude reports the worktree path and branch so you can review and merge.

---

## CLAUDE.md is High Leverage

The `CLAUDE.md` file is loaded into every session. Rules and conventions in it are followed consistently without needing to re-explain them. The current setup (spec format, phased planning, contract-first, test-before-implement) is well-configured.

Things worth adding to CLAUDE.md if they come up repeatedly:

- Patterns that Claude gets wrong more than once
- Project-specific conventions (naming, file placement, etc.)
- Anything you find yourself re-explaining across sessions

---

## Generate Config Artifacts, Not Just Prose

For infrastructure work (Proxmox, Jellyfin, Samba, cloud-init), ask Claude to produce the actual config files rather than a checklist of steps. For example:

- "Generate the smb.conf for sharing /media with guest read/write access"
- "Write a cloud-init user-data file that installs Jellyfin and Samba on Debian"
- "Write the Proxmox LXC config snippet to bind-mount /mnt/media"

This gets working artifacts you can use directly rather than instructions you have to translate yourself. Claude can also verify config syntax and flag common pitfalls for specific tools.

---

## Start Planning Sessions with a Concrete Proposal

Open-ended exploration ("what should we do about X?") works but uses a lot of context arriving at a decision. If you have a direction in mind, starting with "here's what I'm thinking, what are the gaps?" gets to a decision faster and keeps more context available for implementation.

---

## Specs as Session Anchors

Starting a session by pointing Claude at the active spec (`Read Specs/foo-spec.md`) immediately restores context about what phase is active, what the contract is, and where implementation left off. More reliable than summarizing from memory.

---

## Memory Files

Claude maintains memory files at `~/.claude/projects/.../memory/`. Preferences, project context, and feedback corrections are stored there and loaded automatically. If Claude keeps doing something wrong, saying "remember not to do X" will persist that correction across sessions. If something important was decided in a session, asking Claude to "save this to memory" ensures it's available next time.
