# Big & Small Adoption - Agent Handoff

Read this before touching any Big & Small (B&S) work in these repos. It records what is done, what is broken, and the hard-won lessons so you don't repeat my mistakes.

Scope: 5 mods. Core = `Project Mamono` (PMM.Core). Sub-mods: `Project Mamono Reptiles` (PMM.Reptiles), `Project Mamono Slime Faction`, `Project Mamono Elementals`, `Project Mamono Insects`. Plan doc: `BIGSMALL-ADAPTATION.md` (phases 0–7).

## The framework

- Mod: "Big and Small - Framework" v3.0.0, `packageId = RedMattis.BetterPrerequisites` (NOT "BigSmall"). Workshop id **2925432336**. Local install: `~/.steam/.../RimWorld/Mods/Big and Small - Framework`. 1.6 content under `1.6/Base` (always loaded) + `1.6/SimplyRaces` (always loaded) + conditional DLC/Mods folders.
- Assemblies: `1.6/Base/Assemblies/BigAndSmall.dll`, `BSXeno.dll`.
- Wiki: `github.com/RedMattis/BigSmall_Framework/wiki`. Source clone for reading: `/tmp/bs_src/bsfw` (sparse, `1.6/Base/Source`). Framework source is also readable via ILSpy decompile of the local `BigAndSmall.dll`.
- **B&S Genes is a SEPARATE mod** (`RedMattis.BigSmall.Genes`, repo `BigSmall_Genes`). We do NOT depend on it. This matters a lot - see "lamia/wings" below.

## What's DONE and working

- **Phase 0 (About.xml)**: all 5 mods declare `RedMattis.BetterPrerequisites` dependency + `loadAfter` entry. Workshop URL 2925432336 confirmed via Steam API. Requirement line added to each `<description>`.
- **Phase 1 (core infra)**: B&S assembly refs (`Private=false`) in `PMM.Shared.props` (sub-mods, via `$(BigSmallPath)`) and `ProjectMamono.csproj` (core, direct `Reference` with versioned HintPath - B&S is a local Mods-folder install, so the SDK's `RimWorldSteamModDependency` can't find it). This also feeds the XSD generator (`@(Reference)`), so `Defs/DefsSchema.xsd` now covers ~1154 B&S types and our `BigAndSmall.*` XML validates. Core `Defs/BigSmall/`: `ColorTags_PMM.xml` (6 `FlagStringData` color tags: PMM_Body/Hair/Tail/Wing/Glow/ChitinGraphics), `GlobalSettings_PMM.xml` (`PMM_GlobalSettings`, all features OFF). Core C# audit: keep all 84 files - B&S replaces nothing in core (essence/mana, tsugai, willpower, transformation are bespoke).
- **Phase 2 (Reptiles pilot)** - all gene-side, framework-canonical:
  - `PMM_Gene_LamiaTail`: `PawnExtension` `thingDefSwap → BS_Naga` + `renderCacheOff` + `exclusionTags(taur, Tail)`. Covers lamia/medusa/wurm/basilisk/bunyip (all carry the gene). Body fusion brings `BS_SnakeHuman` body + `BS_Naga_Race` tracker (metabolism package).
  - `PMM_Gene_Reptile` (all reptiles): `pawnDiet → BS_Carnivore`.
  - `PMM_GeneTrait_WurmMind`: `statOffsets SM_BodySizeOffset +0.5` (wurm is huge).
  - **Lamia tail RENDERS** (user-confirmed, drawn by the naga tracker). The custom render hediff was deleted 2026-09-20; in-game re-check still pending. See "lessons".
- **B&S morph/vial assessment**: `DarkDragonsBlood.cs` (Malef transformation) kept as C# - B&S morph engine can't reproduce it (no on-ingest trigger, no two-path Apply/ConvertXenotype branch, no mamono exemption fidelity).

## Wings: FIXED and CONFIRMED in-game (2026-09-19)

**Dragons, wyverns and malef dragons render B&S feathered wings.** All fly via core `PMM_Gene_Flight`. B&S ships a complete feathered-wing renderer (12 textures, `BS_FeatheredWing*` path defs, `BS_WingClr*` color defs - all in always-loaded `1.6/SimplyRaces/Defs/Races/WingedHuman/`), but ONLY inside the `BS_HumanoidWithWings_Race` tracker, which fires only for a *racial* winged pawn. On the gene path that tracker hides its wings (`triggerGeneTag Wing/Wings`) expecting the Genes mod's wing gene - which we don't have.

Root cause, confirmed by reading the framework source and decompiled vanilla code:

- The body swap also applies the swap target's **race tracker hediff**. Tracker application keys off `pawn.def.GetRaceExtensions()` (`SimpleRaceExtension.TrackerMissing` → `ApplyHediffToPawn`), and the fused def keeps the target's `RaceExtension`. So a gene-swapped pawn DOES get `BS_HumanoidWithWings_Race`. The old belief that the tracker "fires only for racial pawns" was wrong.
- The tracker owns the wing renderer. Its first alt blanks the wings when the pawn has an active gene tagged `Wing`/`Wings` (`triggerGeneTag` matches gene `exclusionTags`). It skips the blanking only when the pawn raises `ShowBaseWingsRight` / `ShowBaseWingsLeft` (`triggerFlags` blacklist). This hook exists for the separate Genes mod. Our `PMM_Gene_Flight` has `exclusionTags(Wings)` but had no flags - so the tracker blanked all 6 wing nodes.
- The lamia tail works for exactly this reason: `PMM_Gene_LamiaTail` raises `ShowBaseAbdomen` via a `BigAndSmall.Flagger` modExtension. That flag keeps the naga tracker drawing. Our custom render hediffs were never the load-bearing piece.

The final recipe (core `Defs/GeneDefs.xml`, on `PMM_Gene_Flight`, CONFIRMED in-game):
```xml
<li Class="BigAndSmall.PawnExtension">
    <forceThingDefSwap>true</forceThingDefSwap>
    <renderCacheOff>true</renderCacheOff>
    <thingDefSwap>BS_HumanoidWithWings</thingDefSwap>
</li>
<li Class="BigAndSmall.Flagger">
    <flags>
        <ShowBaseWingsRight />
        <ShowBaseWingsLeft />
    </flags>
</li>
```
Three things were each needed: `forceThingDefSwap` (the cautious swap silently refused on dragons - no BS_Wing parts ever appeared), `renderCacheOff` (wings crop against the body atlas without it - same as the lamia tail), and the two flags (the tracker blanks its own wings on Wing/Wings-tagged pawns otherwise).

**Single renderer: the B&S tracker ONLY.** `Defs/HediffDefs_WingRender.xml` (our custom 6-node port) was DELETED 2026-09-19 after in-game confirmation - the tracker draws the wings by itself, and a second renderer would draw the same textures twice. Do NOT recreate a custom wing hediff. The same call was made for the lamia tail: `Project Mamono Reptiles/Defs/HediffDefs/Hediff_LamiaTailRender.xml` was DELETED 2026-09-20 (see below).

Why the 5 earlier fixes all failed: they only touched the custom hediff's XML while (a) the swap never ran (no force) and (b) the tracker blanked its own art (no flags). No hediff edit could fix either.

## HARD-WON LESSONS (read before editing)

- **Fused bodies keep the HUMAN render tree - but the race tracker still applies.** `RaceFuser_Finalize.cs:96`: `newRace.renderTree = sRace.renderTree` where `sRace` = base (human). So `thingDefSwap` grants body *parts*, not the target race's static render tree. BUT the tracker is a hediff, and hediff render nodes are dynamic (`DynamicPawnRenderNodeSetup_Hediffs` in vanilla). The fused def keeps the target's `RaceExtension`, so the tracker hediff lands on the pawn and its render nodes draw. Gene-path body art comes from the tracker, gated by `ShowBase*` flags.
- **The `ShowBase*` flag pattern (the gene-path recipe).** B&S race trackers hide their body art when the pawn carries a gene whose `exclusionTags` match the tracker's `triggerGeneTag` (taur, Wing/Wings, ...). Raise the matching flag via `<li Class="BigAndSmall.Flagger"><flags>...</flags></li>` on the gene to keep the tracker drawing: `ShowBaseAbdomen` (lamia tail), `ShowBaseWingsRight`+`ShowBaseWingsLeft` (wings). This is what the Genes mod's own genes do.
- **B&S Genes ≠ Framework.** B&S's own lamia/wing gene art lives in `LS_SnakeTail` / wing genes in the **Genes** mod, which we don't depend on. We reproduce the pattern with swap + flags + force. Ship NO custom render hediffs for swapped bodies - the tracker is the single renderer. Both custom hediffs are now gone (wing 2026-09-19, lamia tail 2026-09-20).
- **`renderNodeProperties` is a VANILLA type** (`PawnRenderNodeProperties`), not B&S. Hediff render nodes draw via vanilla's pipeline.
- **The working lamia tail recipe** (CONFIRMED in-game): `thingDefSwap → BS_Naga` (no force needed - lamias start as plain humans, cautious fuse lands) + `exclusionTags(taur, Tail)` + `Flagger(ShowBaseAbdomen)` + `renderCacheOff`. The flag makes the naga tracker draw the tail. **The custom render hediff was removed 2026-09-20** - `applyPartHediff → PMM_Hediff_LamiaTailRender` was a duplicate of the tracker's own `BS_SnekGraphicSet` node, and the wings case proved the tracker draws a gene-swapped body alone. Only loss from the deletion: the tracker has no "part missing" alt, so a lamia whose `BS_SnakeBody` is destroyed still shows tail art (B&S's own racial naga behaves the same way). **Not yet re-tested in-game.**
- **forceThingDefSwap is half-wired in B&S.** `GeneRequestThingSwap` (gene-added path) never reads it - only xenotype-generation paths honor it. For xenotype-carried genes that's enough; a gene ADDED to a live pawn (dev mode, gene extractor) still swaps cautiously.
- **`statOffsets` is NOT a `PawnExtension` field.** Only `statFactors`/`statOffsets` nested under `ConditionalStatAffecter`. Putting `statOffsets` on a `PawnExtension` modExtension is silently ignored. For a plain stat offset use vanilla `statOffsets` on the `GeneDef` (proven by `PMM_Gene_Reptile`).
- **Filter priority** (all B&S filter lists), low→high: `acceptlist < whitelist < blacklist < allowlist < banlist`.
- **Core About.xml is overwritten by builds** (SDK `GenerateAboutXml`). Hand edits go in the committed file; restore with `git checkout -- About/About.xml` before `sync.sh`. The B&S dependency entry lives there.
- **Full race pattern vs xenotypes.** Reptiles is xenotype-only (right call - mamono inheritance depends on Biotech xenotypes; a B&S *race* is a body type, not inheritable). Slimes/Elementals use real race ThingDefs (`ParentName="Human"` + `BigAndSmall.RaceExtension` + race-tracker hediff `ParentName="BS_DefaultRaceTracker"`). Don't convert Reptiles to the race pattern.

## How to build / validate

- Whole solution: `cd "Project Mamono" && ./build-all.sh` (builds all 5, copies each mod's own dll to its `Assemblies/`). 0 errors currently.
- XML well-formed: `python3 -c "import xml.dom.minidom; xml.dom.minidom.parse('<file>')"`.
- XSD check for a root type: `grep -oE 'name="BigAndSmall\.[A-Za-z]+"' "Project Mamono/Defs/DefsSchema.xsd"`.
- Player.log: `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`.

## How to verify the wing fix in-game

1. Spawn a fresh dragon (or wyvern). Look at her facing south and east: the front wing draws at layer 92 on those facings and is easy to see.
2. If wings still do not show, check application, not rendering: Health tab should list two `BS_Wing` parts, and the pawn should carry the `BS_HumanoidWithWings_Race` hediff. If the parts are missing, the swap was refused (`forceThingDefSwap` is false). If the tracker is missing, the fused def lost its `RaceExtension`.
3. `PRN_Ultimate.GraphicFor` (`UltimateRender_Static.cs`) returns `BS_Blank` when the BSCache is missing or no path resolves; it logs a once-per-node warning in Player.log. No warning + no wings means the nodes never entered the tree.

## How to verify the lamia tail still works (after the 2026-09-20 deletion)

The tail now comes from the tracker alone. Check it the same way as the wings:

1. Spawn a **fresh** lamia, medusa or ryuu.
2. The pawn must have the `BS_Naga_Race` tracker hediff and a `BS_SnakeBody` part. If the tracker is there, the tail node is in the render tree - the gene's `ShowBaseAbdomen` flag keeps it visible.
3. If the tail is gone: check whether the tracker is missing. That would mean the fused def lost its `RaceExtension`, which no current evidence supports. Re-adding `applyPartHediff` would only work if you also restore the hediff def.

**Save warning:** the deleted hediff is baked into any save where the gene was already applied. Loading that save will log a missing-`HediffDef` error for each affected pawn. Test with a fresh spawn or a new save, or strip the hediff in dev mode before removing the def.

## Reference file paths

- Working tail: gene in `Project Mamono Reptiles/Defs/GeneDefs/Genes_Reptile.xml` (`PMM_Gene_LamiaTail`). No hediff file - the `BS_Naga_Race` tracker draws the tail. The old `Defs/HediffDefs/Hediff_LamiaTailRender.xml` is deleted - do not recreate it.
- Wings (fixed, tracker-only): gene in `Project Mamono/Defs/GeneDefs.xml` (`PMM_Gene_Flight`). The old `Defs/HediffDefs_WingRender.xml` is deleted - do not recreate it.
- B&S examples (local mod): `.../Big and Small - Framework/1.6/SimplyRaces/Defs/Races/{LamiaBody,WingedHuman}/`.
- B&S fusion logic: `/tmp/bs_src/bsfw/1.6/Base/Source/BigSmallFramework/DefPatches/RaceFuser/RaceFuser_Finalize.cs`.
- B&S hediff-apply loop: `.../SimpleCustomRaces/SimpleRaceCache.cs` (`hediffsToParts → TryAddToAllMatchingParts`, `hediffsToBody → GetOrAddHediff`).
