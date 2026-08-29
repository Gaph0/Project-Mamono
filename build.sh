#!/bin/bash
# Builds Assemblies/ProjectMomo.dll with mcs against RimWorld + workshop mod references.
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

csc -nologo -target:library \
  Source/ProjectMomo/*.cs -out:Assemblies/ProjectMomo.dll \
  -r:"$M/Assembly-CSharp.dll" -r:"$M/UnityEngine.CoreModule.dll" \
  -r:"$M/UnityEngine.IMGUIModule.dll" -r:"$M/UnityEngine.TextRenderingModule.dll" \
  -r:"$M/netstandard.dll" \
  -r:"$H/0Harmony.dll" -r:"$IK/IsekaiLeveling.dll" -r:"$Y/yayoAni.dll" \
  -r:"$VPE/VanillaPsycastsExpanded.dll" -r:"$VEF/VEF.dll"

echo "Built Assemblies/ProjectMomo.dll"
