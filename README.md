Config mod for Project Momo.

## Gene

The `Momo` gene (`ProjectMomo_Momo`) forces its geneholder to be female through
`ProjectMomo.Gene_Momo` in `Assemblies/ProjectMomo.dll`. It is available when the
Biotech expansion is active.

## Willpower and tease damage

`ProjectMomo_Willpower` is a pawn capacity shown on the Health tab (alongside
Consciousness, Manipulation, etc.). Momo-carriers get 1.5x willpower; pawns
carrying tease damage lose willpower proportional to its severity
(`Willpower x (1 - teaseSeverity)`, via `PawnCapacityWorker_Willpower`).

When a Momo-carrier lands a melee hit on a pawn without the Momo gene, the
victim gains `ProjectMomo_TeaseDamage` on the brain (0.05 severity per hit,
stacking up to 1, fading at 0.5/day). When the victim's willpower reaches 0%,
the `HediffComp_TeaseKnockout` comp applies `ProjectMomo_WillpowerBreak`
(Consciousness capped at 10%), downing the pawn without killing them; the
knockout is removed automatically once willpower recovers.

## Rebuilding the assembly

To rebuild after editing anything under `Source/ProjectMomo/` (run from
anywhere — the first line selects the Desktop workspace):

```bash
cd "$HOME/Desktop/Project Momo"
M=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
H=~/.steam/steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies
IK=~/.steam/steam/steamapps/workshop/content/294100/3657580708/Assemblies
Y=~/.steam/steam/steamapps/workshop/content/294100/2877292196/1.6/Assemblies
mcs -sdk:4.8 -target:library \
  -out:"$PWD/Assemblies/ProjectMomo.dll" \
  -r:"$M/Assembly-CSharp.dll" -r:"$M/UnityEngine.CoreModule.dll" -r:"$M/UnityEngine.IMGUIModule.dll" \
  -r:"$M/UnityEngine.TextRenderingModule.dll" \
  -r:"$M/netstandard.dll" \
  -r:"$H/0Harmony.dll" -r:"$IK/IsekaiLeveling.dll" -r:"$Y/yayoAni.dll" \
  Source/ProjectMomo/*.cs
```

Build output stays in this workspace (`Assemblies/ProjectMomo.dll`). Deployment
into the game's `Mods` folder is a separate manual step — never overwrite the
DLL while RimWorld is running, or the loaded assembly's metadata gets
corrupted.