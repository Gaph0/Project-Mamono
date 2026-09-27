using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// A winged mamono ignores terrain movement cost on a map, the same way the VRE Insector wings
    /// gene does it: Pawn_PathFollower.CostToMoveIntoCell is the per-cell price the game uses
    /// while a pawn walks, so replacing it with the pawn's plain ticks-per-move means mud, sand,
    /// snow, slush and roads stop mattering - she keeps her own pace whatever she crosses.
    ///
    /// Mirrors VEF's floating-creature patch (VEF.AnimalBehaviours, used by VREInsector_Hover):
    /// a cell stays unpassable (cost 10000) when its terrain is impassable and dry, or when an
    /// impassable thing stands on it, so walls, doors and buildings still stop her. Water is the
    /// one exemption, again as in VEF: she crosses it at her own pace, which suits a woman who
    /// already leaps over obstacles with her Fly ability.
    ///
    /// Both flight genes count. A downed woman and a prisoner count for nothing, so a captured
    /// flyer cannot outrun her escort.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new[] { typeof(Pawn), typeof(IntVec3) })]
    public static class FlyingPawnTerrainCostPatch
    {
        public static void Postfix(Pawn pawn, IntVec3 c, ref float __result)
        {
            if (pawn?.Map == null)
            {
                return;
            }
            if (pawn.Downed || pawn.IsPrisoner || pawn.genes == null)
            {
                return;
            }
            if (!pawn.genes.HasActiveGene(ProjectMamono_DefOf.PMM_Gene_Flight)
                && !pawn.genes.HasActiveGene(ProjectMamono_DefOf.PMM_Gene_FlightWeak))
            {
                return;
            }

            Map map = pawn.Map;
            TerrainDef terrain = map.terrainGrid.TerrainAt(c);
            float cost = ((c.x != pawn.Position.x && c.z != pawn.Position.z)
                ? pawn.TicksPerMoveDiagonal
                : pawn.TicksPerMoveCardinal);

            if (terrain == null)
            {
                cost = 10000f;
            }
            else if (terrain.passability == Traversability.Impassable && !terrain.IsWater)
            {
                cost = 10000f;
            }

            List<Thing> things = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def.passability == Traversability.Impassable)
                {
                    cost = 10000f;
                }
            }

            __result = cost;
        }
    }
}
