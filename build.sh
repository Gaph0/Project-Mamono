#!/bin/bash
# Builds Assemblies/ProjectMamono.dll with csc against RimWorld + workshop mod references.
# Exits non-zero if compilation fails (also used by the pre-push hook chain).
set -e
cd "$(dirname "$0")"

WS=~/.steam/steam/steamapps/workshop/content/294100
M=~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed
H=$WS/2009463077/Current/Assemblies   # Harmony
IK=$WS/3657580708/Assemblies          # Isekai Leveling
Y=$WS/2877292196/1.6/Assemblies       # yayo's Animation
VPE=$WS/2842502659/1.6/Assemblies     # Vanilla Psycasts Expanded
VEF=$WS/2023507013/1.6/Assemblies     # Vanilla Expanded Framework
# Big and Small Framework, from its Steam workshop folder (id 2925432336), the same way
# ProjectMamono.csproj and PMM.Shared.props reference it: the copy in the local Mods folder
# was pruned on 2026-09-27 when the game's mods were updated, and a Steam update cannot
# delete this one out from under the build. A hard dependency of all five PMM mods (see
# BIGSMALL-ADAPTATION.md); the core transformation calls its xenotype-race API.
BS="$WS/2925432336/1.6/Base/Assemblies"

csc -nologo -target:library \
  Source/ProjectMamono/*.cs -out:Assemblies/ProjectMamono.dll \
  -r:"$M/Assembly-CSharp.dll" -r:"$M/UnityEngine.CoreModule.dll" \
  -r:"$M/UnityEngine.IMGUIModule.dll" -r:"$M/UnityEngine.TextRenderingModule.dll" \
  -r:"$M/netstandard.dll" \
  -r:"$H/0Harmony.dll" -r:"$IK/IsekaiLeveling.dll" -r:"$Y/yayoAni.dll" \
  -r:"$VPE/VanillaPsycastsExpanded.dll" -r:"$VEF/VEF.dll" \
  -r:"$BS/BigAndSmall.dll"

echo "Built Assemblies/ProjectMamono.dll"
