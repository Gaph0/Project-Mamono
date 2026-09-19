# Build unification plan

Goal: one solution file, one shared build config, and one `dotnet build` for all five Project Momo mods.

## Status

- [x] Phase 0 — safety. Dlls backed up (`Assemblies/*.bak-preunify`).
- [x] Phase 1 — core. `PMM.Shared.props`, `ProjectMomo.sln` (core + Insects), `build-all.sh` created. Core builds green.
- [x] Phase 2 — pilot: Insects. `PMM.Insects.csproj` builds green; output dll has all 16 types from the old `csc` build, none missing, none added. `build.sh` now wraps `dotnet build`; `sync.sh` and `.gitignore` exclude `bin/`, `obj/`, `*.csproj`. Fix applied during rollout: SDK auto-includes all `Source/**/*.cs`, so no explicit `Compile` glob is needed in sub-mod csproj files.
- [x] Phase 3 — Slime Faction. `PMM.SlimeFaction.csproj` (VEF + IsekaiLeveling refs) builds green; 94/94 old types present, only Roslyn embedded attributes added. `build.sh` wraps dotnet, `sync.sh`/`.gitignore` exclude bin/obj/csproj. Added to `ProjectMomo.sln`.
- [x] Phase 4 — Reptiles. `PMM.Reptiles.csproj` (VEF ref) builds green; 35/35 old types present, only Roslyn attributes added. Scripts updated, added to `ProjectMomo.sln`.
- [x] Phase 5 — Elementals. `PMM.Elementals.csproj` (no extra refs) builds green; 58/58 old types present, only Roslyn attributes added. Scripts updated, added to `ProjectMomo.sln`.
- [x] Phase 6 — all 5 projects in `ProjectMomo.sln`; `./build-all.sh` builds everything in ~2.5s and restores About.xml.

## How to build now

- Build everything: run `./build-all.sh` in the `Project Momo` folder.
- Build one mod: run `./build.sh` in that mod's folder, like before.
- The old per-mod `csc` scripts are gone. All mods now build with `dotnet`.
- `sync.sh` and `release.sh` work the same as before.

## Why

Today each mod builds alone. The core uses `dotnet build` with the Zlepper SDK. The four sub-mods use `build.sh` with raw `csc`. Each sub-mod script builds the core first if its dll is missing. This works, but build order is maintained by hand, and there is no single command that builds everything.

## Decisions

1. **`ProjectMomo.sln` and `PMM.Shared.props` live in this repo** (Project Momo, the core). The core is already the root mod. No new repo is needed.
2. **Sub-mods import the shared props with an explicit `<Import>`.** We do not use a real `Directory.Build.props`. That file only auto-applies to projects below it. The only shared parent folder is `~/Desktop`, and a props file there would leak into every .NET project on the Desktop.
3. **Sub-mods get plain SDK-style csproj files (net48).** The Zlepper SDK stays core-only. The sub-mods keep their hand-written About.xml files, so the SDK's About.xml generation would only cause trouble.
4. **Sub-mods reference the core with `<ProjectReference>`.** This gives correct build order and editor IntelliSense for free.
5. **The built dll is copied to `Assemblies/` after build.** The csproj default output stays in `bin/`. Only the mod dll is copied. This keeps `Assemblies/` exactly like today — no stray game dlls.
6. **`build.sh` stays as a thin wrapper** that calls `dotnet build`. `sync.sh` and `release.sh` keep working.
7. **Rollout: pilot first.** Insects first (fewest references), then Slime, Reptiles, Elementals.

## Target layout

```
Project Momo/
  ProjectMomo.sln          <- new: lists all 5 projects
  PMM.Shared.props         <- new: shared paths + compiler settings
  ProjectMomo.csproj       <- unchanged (Zlepper SDK)
  build-all.sh             <- new: build sln + restore About.xml
Project Momo Insects/
  PMM.Insects.csproj       <- new (pilot)
Project Momo Slime Faction/
  PMM.SlimeFaction.csproj  <- new
Project Momo Reptiles/
  PMM.Reptiles.csproj      <- new
Project Momo Elementals/
  PMM.Elementals.csproj    <- new
```

## `PMM.Shared.props` contents

- `RimWorldPath` = `/home/gapho/.steam/debian-installation/steamapps/common/RimWorld`
- `RimWorldManagedPath` = `$(RimWorldPath)/RimWorldLinux_Data/Managed`
- `SteamModContentFolder` = `/home/gapho/.steam/debian-installation/steamapps/workshop/content/294100`
- Compiler settings: `LangVersion=latest`, `TargetFramework=net48`, `Nullable=disable`, `ImplicitUsings=disable`, `DebugType=none`
- Shared references, all with `<Private>false</Private>` (never copied): `Assembly-CSharp`, `UnityEngine.CoreModule`, `UnityEngine.IMGUIModule`, `UnityEngine.TextRenderingModule`, `netstandard`, `0Harmony`
- A `CopyToModAssemblies` target: after build, copy only `$(AssemblyName).dll` to the mod's own `Assemblies/` folder

## Sub-mod csproj template

About 25 lines per mod. Import the props, set `AssemblyName` (must match today's dll names exactly: `PMM_Insects`, `PMM_SlimeFaction`, `PMM_Reptiles`, `PMM_Elementals`), add `ProjectReference` to `../Project Momo/ProjectMomo.csproj` with `<Private>false</Private>`, and add per-mod extra references:

- Slime Faction: `VEF.dll`, `IsekaiLeveling.dll`
- Reptiles: `VEF.dll`
- Insects, Elementals: none

## Phases

**Phase 0 — safety.** Commit everything in all five repos. No deletions of old scripts until the end.

**Phase 1 — core.** Add `PMM.Shared.props` and `ProjectMomo.sln` (core only for now). Verify `dotnet build` still works and still regenerates the XSD. No behavior change.

**Phase 2 — pilot: Insects.** Add `PMM.Insects.csproj`. Build it. Check that `Assemblies/PMM_Insects.dll` appears and the old `csc` output is replaced. Keep a `.bak` of the old dll. Update `build.sh` to call `dotnet build`. Add `--exclude=/bin --exclude=/obj` to `sync.sh`. Test in game; check Player.log with rwforge.

**Phase 3 — Slime Faction.** Same, plus the two extra references. Test in game (this is the biggest C# mod).

**Phase 4 — Reptiles.** Same, plus `VEF.dll`.

**Phase 5 — Elementals.** Same, no extra references.

**Phase 6 — wire the solution.** Add all sub-projects to `ProjectMomo.sln`. Add `build-all.sh`:

```
dotnet build ProjectMomo.sln
git checkout -- About/About.xml   # the SDK overwrites it on every build
```

Then update README files, HANDOFF.md, and PLAN.md files to mention the new build.

## Verification (each phase)

1. `dotnet build` finishes green.
2. The dll in `Assemblies/` has the exact old name.
3. The list of compiled `.cs` files matches the old `build.sh` glob.
4. `sync.sh` deploys a clean folder (no `bin/`, no `obj/`).
5. In-game smoke test: mod loads, no red errors in Player.log.

## Risks

- **Slow core rebuilds.** The Zlepper XSD step runs whenever the core project rebuilds, and building a sub-project can trigger the core. Measure this in the pilot. If it hurts, we can gate the XSD step behind a property.
- **About.xml overwrite.** Known issue: every core build overwrites `About/About.xml`. `build-all.sh` restores it. The per-mod `build.sh` wrappers keep the same warning.
- **Duplicate dlls.** If any reference is copied into `Assemblies/`, RimWorld may load it twice. The plan prevents this with `<Private>false</Private>` everywhere and the copy-target that copies only the mod dll.
- **Steam path drift.** The csproj and the old scripts use two different Steam roots (`debian-installation` vs `steam`). Both exist today (one is a symlink). The props file picks the csproj one, so MSBuild uses a single root.

## What this does not change

- About.xml files, Defs, Patches, Languages: untouched.
- The Zlepper SDK setup in the core: untouched.
- `release.sh` and GitHub releases: untouched.
- Load order in game: still enforced by `loadAfter` in each About.xml, exactly like today.
