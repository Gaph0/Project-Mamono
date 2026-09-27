# Big & Small Framework - Adaptation Plan

Status: **approved direction, plan stage** (2026-09-19)
Scope: all five PMM mods.

## Decisions (user-approved)

- Big & Small (B&S) becomes a **hard dependency in all five mods**.
- **Core first.** Shared B&S setup goes in Project Mamono core. Per-mod work follows.
- Races convert fully to the **B&S race pattern** (`RaceExtension` + race-tracker hediff).
- All feature areas are in scope: non-humanoid bodies, size system, rendering, gameplay plumbing, and replacing custom C# where B&S already does it.
- **No save compatibility needed.** Clean break is fine.

## Key facts

- B&S packageId is `RedMattis.BetterPrerequisites`. (Not "BigSmall". Easy to get wrong.)
- B&S loads after all DLC and Harmony. It loads **before** HAR and VEF.
- Local copy: `Mods/Big and Small - Framework`, v3.0.0, RimWorld 1.6 folder at `1.6/Base`.
- Assemblies for C# references: `BigAndSmall.dll`, `BSXeno.dll` (set `Private=false`, like VEF and IsekaiLeveling).
- B&S race pattern (from `BS_Naga` example):
  1. `ThingDef ParentName="Human"` with modExtension `BigAndSmall.RaceExtension` → `<raceHediff>`.
  2. Race hediff: `HediffDef ParentName="BS_DefaultRaceTracker"`. Holds stat stages, `CompProperties_Race`, `renderNodeProperties` (PawnRenderNode_Ultimate → `replacementDef` = GraphicSetDef, drawData offsets), and `PawnExtension` modExts.
  3. Optional: `BigAndSmall.GraphicSetDef` for conditional textures and color channels.
- Full PawnExtension field list: source `ModExtensions/PawnExtension.cs` in the B&S repo. Notable extras beyond the wiki: `isAmorphous`, `empVulnerable`, `lockedNeeds`, `meatOverride` + `butcherProducts`, `disabledWorkTypes`, `clampedSkills`, `hiddenGenes`, `randomSkinGenes`/`randomHairGenes`, metamorphosis (`transformGene`, `morphTargets`, `metamorphAtAge`, `metamorphIfNight`/`Day`), `internalDamageDivisor`.
- Filter priority, low to high: **acceptlist < whitelist < blacklist < allowlist < banlist**. Same shape for gene, trait, hediff, hair, and surgery filters.
- A xenotype's declared race (`XenotypeExtension.setRace`, a `ThingDef`, plus `forceRace`) is applied by B&S **while a pawn is generated**. Anything that sets a xenotype afterwards has to ask for it: `MamonoTransformation.ApplyXenotypeRace` for our own paths (corruption, xenogerms, conversions), and since 2026-09-25 `XenotypeRacePatch` - a postfix on `Pawn_GeneTracker.SetXenotype` covering every other path, including vanilla's dev "apply xenotype" action. Without it a pawn wears the species' genes on its old body: no size, no wings, no extra limbs.
- SimplyRaces ships ready bodies and graphic sets: `BS_Naga` (lamia), Centaur, Spider, WingedHuman, FourArms, SixArms, TailedHuman, Yukkuri, Mech.

---

## Phase 0 - About.xml wiring (all five mods) - DONE 2026-09-19

Added to every `About/About.xml`:

1. `modDependencies` entry for `RedMattis.BetterPrerequisites` (display name "Big and Small - Framework", workshop **2925432336** - confirmed via Steam API, matches local PublishedFileId).
2. `loadAfter` entry: `<li>RedMattis.BetterPrerequisites</li>` (after PMM.Core in sub-mods, so core code sees B&S types first).
3. One-line requirement note added to each description.

Per-mod notes:

- **Core**: also add to `loadAfter` so B&S is up before PMM code runs.
- **Reptiles**: B&S loads before VEF, and Reptiles loads after VEF. No conflict. Just add the entry.
- **Gotcha (core only)**: builds overwrite `About/About.xml` with the generated minimal version. The B&S entry goes in the hand-written file. Keep restoring it with `git checkout -- About/About.xml` before sync, as today. Long-term fix: teach the SDK wiring to keep our About.xml. Out of scope here.

## Phase 1 - Core: shared B&S infrastructure

Goal: one home for everything the sub-mods share.

1. **C# references** - DONE: `BigAndSmall.dll` + `BSXeno.dll` (`Private=false`) added to both `PMM.Shared.props` (sub-mods, via `$(BigSmallPath)`) and `ProjectMamono.csproj` (core, direct Reference since B&S is a local mod, not workshop). XSD regenerated: 1154 B&S types covered, so `BigAndSmall.*` XML now validates.
2. **Shared defs** - DONE: new folder `Defs/BigSmall/` in core.
   - `ColorTags_PMM.xml`: `FlagStringData` labels for six shared tags - `PMM_BodyGraphics`, `PMM_HairGraphics`, `PMM_TailGraphics`, `PMM_WingGraphics`, `PMM_GlowGraphics`, `PMM_ChitinGraphics`. Sub-mods reference these in their GraphicSetDefs.
   - `GlobalSettings_PMM.xml`: `PMM_GlobalSettings` with all features off (wiring proof; flip in Phase 5).
   - Shared `PawnDiet` defs deferred until a concrete diet is designed (Phase 3+).
3. **GlobalSettings**: core enables only what a sub-mod actually needs later. Nothing enabled by default yet. Sapient Animals/Mechanoids stays off until Insects decides (see Phase 5).
4. **C# audit (core)**: DONE 2026-09-19. Verdict: **keep all 84 files; B&S replaces nothing in core.** Core systems are bespoke and have no B&S equivalent: essence/mana needs, tsugai bond, willpower, tease damage, transformation/incubisation, IsekaiLeveling and sex-mod shims. Look-alikes checked and rejected:
   - `Gene_Mamono` gender force - B&S `forceGender` exists, but ours also restyles hair and re-randomizes femininity on apply. Keep. (Minor: could later delegate the gender flip to PawnExtension, but the hair logic stays.)
   - `MamonoEatingPatch` - restores the Mana *need* from food; B&S diets gate *what* pawns eat, not custom need gain. Keep.
   - `FieryMamono` fire immunity - patches `PreApplyDamage` with a bonded-partner ward variant; B&S has no conditional damage-immunity hook. Keep.
   No forced-hediff, thought-suppression, size, or render code exists in core (those live in sub-mods, audited in their own phases).
5. Smoke test: game loads with only Core + B&S active. No red errors. **(manual, pending)**
6. **Race-tracker rows CANNOT be hidden (tried 2026-09-26, reverted within the hour)** - the
   obvious patch is to force `BigAndSmall.RaceTracker.Visible` (hardcoded `=> true`) to false for
   PMM pawns, and it does remove the row. It also removes the pawn's **wings** (and the abaddon
   folk's lower arms): B&S draws tracker art through the hediff's own render nodes and only
   installs them **while the hediff is `Visible`** (gate `h.Visible && def.HasDefinedGraphicProperties`
   in B&S's dynamic hediff render setup). Row and art are one switch, so the row stays. The patch
   file was deleted and its changelog line removed; nothing else was touched. Anyone tempted again:
   it would mean moving every tracker's render nodes onto a second, visible hediff - a second row
   for the same information.

## Phase 2 - Reptiles (first race conversion) - DONE 2026-09-19

Best pilot: xenotype-only today, and `BS_Naga` is a ready lamia body. All reptile content is gene-driven (pawnkinds are gear-only, no forced-trait boilerplate to migrate), so the whole phase landed on two genes.

1. **Lamia body** - `PMM_Gene_LamiaTail` now carries a `BigAndSmall.PawnExtension` with `thingDefSwap → BS_Naga` (plus `renderCacheOff`, `exclusionTags taur`/`Tail`). This mirrors B&S's own snake-tail gene (`LoS_Snake_Tail`): the race fuser applies the `BS_SnakeHuman` body + `BS_Naga_Race` tracker (serpent metabolism + tail renderer). Covers lamia, medusa, and ryuu xenotypes at once, since all three carry the tail gene.
   - Design change mid-flight: an earlier draft used a custom `PMM_Lamia_Race` tracker to keep baseline stats. Dropped - B&S's snake-tail gene supplies the whole package on its own, and a custom tracker would fight it. We take the framework's stat package.
2. **Diet** - `PMM_Gene_Reptile` (shared by every reptile xenotype) got a second PawnExtension: `pawnDiet → BS_Carnivore`. All reptiles are carnivorous hunters; processed food and paste still work.
3. **No migration needed** - pawnkinds carry gear/stats only; nothing to move into PawnExtensions.
4. Build clean (0 errors), XML valid. In-game render/hybrid test pending.

Files: `Defs/GeneDefs/Genes_Reptile.xml` (both genes). No new def files - the framework draws the lamia tail.

### Lamia graphics fix (2026-09-19, second pass)

First pass swapped the race to snake-person but drew **no tail**. Root cause (confirmed in source): when B&S fuses a body via a gene, `RaceFuser_Finalize.cs` copies the **human** render tree onto the new race, so the naga tracker's tail render node never fires. B&S's own lamia tail art is drawn by the `LS_SnakeTail` hediff - which lives in the separate **BigSmall_Genes** mod we don't depend on. Fix (chose PMM-own renderer over adding a Genes dependency):

- **The custom hediff is gone (2026-09-20).** The framework draws the tail by itself. The gene now only adds `BigAndSmall.Flagger` with `ShowBaseAbdomen` (same as B&S's snake-tail gene). This flag tells the `BS_Naga_Race` tracker to keep its tail art.
- Why the hediff was wrong: the fused body keeps the `BS_Naga` `RaceExtension`, so the `BS_Naga_Race` tracker lands on the pawn. That tracker already holds the same `BS_SnekGraphicSet` tail node. Our hediff drew the same art a second time. The wings fix proved the same point.

Pending: spawn a **fresh** lamia/medusa and check the tail still shows.

## Phase 3 - Slime Faction

Six race ThingDefs on a vanilla `Human` base today.

1. Convert `PMM_SlimeMamonoRaceBase` children to the B&S race pattern (race hediff per color, or one shared tracker + per-color GraphicSetDef alts - decide during work).
2. Use `isAmorphous` and the three-color transparent shader (`BS_TransparentThreeColor`) for slime translucency.
3. Slime-specific quirks (wastepack eating, paralytic tentacles): keep as PMM comps, but trigger through PawnExtension conditionals where possible.
4. Diet: slime-jelly food rules as a `PawnDiet` def in core or here.

## Phase 4 - Elementals

Eight race ThingDefs on a vanilla `Human` base today.

1. Same conversion as slimes: `RaceExtension` + tracker hediff per element.
2. Rendering: `bodyMaterial`/`headMaterial` for elemental looks (ember skin for Ignis, translucent for Undine).
3. Element diets via `PawnDiet` + `NewFoodCategory` (e.g. Ignis burns chemfuel - the wiki's exact example).
4. `isUnliving`-adjacent tags only where lore says so. Genie lamp binding stays PMM C#.

## Phase 5 - Insects - STARTED 2026-09-19 (Route A, Devil Bug pilot)

**Route A chosen.** Route B (Sapient Animals) rejected: it would downgrade already-humanlike races into generated `HL_` pawns, and most genes stop rendering.

**Pilot: Devil Bug converted** (`Project Mamono Insects/Defs/ThingDefs/Races_InsectMamono_BS.xml`):

- `ThingDef ParentName="Human"` - race-level data stays 1:1 (`baseBodySize 0.2`, `baseHealthScale 0.4`, `baseHungerRate 0.10`, insect flesh/blood/meat, melee tools) + `BigAndSmall.RaceExtension` → `PMM_RaceTracker_DevilBug`.
- Tracker (`ParentName="BS_DefaultRaceTracker"`): armor stat stage, `CompProperties_Race (canSwapAwayFrom)`, PawnExtension with insect romance tags. No render nodes yet - chitin art later via the reserved `PMM_ChitinGraphics` tag.
- Old BasePawn Devil Bug clone deleted; other five species untouched until pilot proves out. Build green, synced.
- B&S's scaler reads `baseBodySize` natively (`HumanoidPawnScaler`: >1.49 gets `renderCacheOff` free) - the 1:1 stat clone carries over intact.

Test gate: devil bug renders small, keeps armor, hive nourishment works, wild-man recipe still functions on a Human-based race. Then convert Giant Ant → Soldier Beetle → Greenworm → Vamp Mosquito → Abaddon (Abaddon is the real prize: 4.5 body size with working render scale).

## Phase 6 - C# cleanup pass (all mods)

After races convert, re-audit each `Source/` for code now dead because B&S does it (forced hediffs/trait enforcement/thought suppression/render swaps). Delete or shrink. Keep what B&S cannot do (IsekaiLeveling hooks, sex-mod integration, tsugai/essence systems).

## Phase 7 - Validation and docs

1. `rw_log_analyze` on a fresh Player.log after each phase.
2. In-game checklist per mod: pawn spawns, renders right (all 4 rotations), diet gates work, apparel rules work, faction raids spawn correctly.
3. Update each mod's README and CHANGELOG (simple English). New requirement line: "Needs Big and Small - Framework."

## Open questions (fill in as we hit them)

- [x] B&S workshop ID: **2925432336** (confirmed).
- [ ] Reptiles: use `BS_Naga` as-is or custom ThingDef with our own art?
- [ ] Slimes: one tracker + color alts, or six trackers?
- [ ] Insects: Route A vs Route B (decided by test).
- [ ] Which core C# systems does B&S replace? (Phase 1 audit output.)
