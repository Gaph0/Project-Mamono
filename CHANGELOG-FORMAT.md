# Changelog Format

The rule for every `CHANGELOG.md` in the Project Mamono family. Five mods follow it.

## Shape

```markdown
# Changelog

## Player-facing

- 2026-09-19: Fixed adult insect mamonos failing to roll an age.
- 2026-09-06: Added hive nourishment.

## Internal

- 2026-09-19: Replaced the BasePawn race clones with Human-based races.
```

The rules:

- Flat dated lines. No version headings.
- The date comes first, as `YYYY-MM-DD`.
- One line, one change.
- Newest first, in both lists.
- The version lives in git tags, not in this file. Each mod is tagged on its own, so each mod can ship its own version.

## Which list

**Player-facing** - a change she can see in the game. A new gene, item or event. A balance change. A bug she hit. A new required mod.

**Internal** - a change she cannot see. Build changes, refactors, file moves, new code with no visible effect yet.

One test decides it: would a player notice this? If no, it goes in `Internal`.

## Player-facing lines

1. Start with one verb: **Added**, **Changed**, **Fixed**, **Removed**, **Rebalanced**.
2. The verb is the category. Add no prefix such as `feat:`, `fix:` or `ci:`.
3. Say what changed for the player. Not how.
4. One sentence, about 25 words. Two sentences at most.
5. No file names, no class names, no tick counts, no root cause.
6. Use the name the game already uses.

The cause, the file names and the numbers belong in the commit message.

## Internal lines

Same verbs. One short line. Enough to find the change again later. Never the reason.

## Examples

Player-facing fix:

- Bad: `- 2026-09-01: fix: freed genie no longer beelines for the map edge. Root cause: PMM_GenieWild was missing from the think tree, so she fell through to JobGiver_ExitMapBest. Added her branches ... counts down 10000 ticks (4 in-game hours) ...`
- Good: `- 2026-09-01: Fixed a freed genie walking off the map instead of staying near her lamp.`

Internal change:

- Bad: `- 2026-09-19: Build via dotnet: PMM.SlimeFaction.csproj replaces raw csc`
- Good: `- 2026-09-19: Changed the build to use MSBuild.`

## Never

- Never add a version heading.
- Never add a type prefix.
- Never put two changes on one line. Split it.
- Never rewrite a shipped entry. Add a new line that reverses it.

## Keeping it honest

`changelog-check.sh` checks all five files, and `build-all.sh` runs it first.

- **Structure is fatal.** The two sections must be present and in order. Every line must read `- YYYY-MM-DD: <verb> ...`, with a verb from the list above. A malformed changelog stops the build.
- **Coverage warns.** If a mod has changed files under `Defs`, `Source`, `Patches`, `About`, `Languages` or `Textures`, and its `CHANGELOG.md` is not part of the same change, you get a warning. Add `--strict` to `build-all.sh` (or set `CHANGELOG_STRICT=1`) to make that fatal as well.
- **Long lines are a note, never a failure.** It reports lines over 40 words, because the cap is soft.

The checker only reports. It cannot write the line for you.
