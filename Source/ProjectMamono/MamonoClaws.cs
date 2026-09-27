using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Mamono claws: the tease-damage multiplier for a clawed attacker. Queried by
    /// the melee tease path (TeaseDamagePatch) and by the public entry point
    /// (TeaseApplication) so every tease source - melee, psycasts, mod
    /// integrations - scales alike. Returns 1 for non-carriers.
    /// </summary>
    public static class MamonoClaws
    {
        public static float TeaseDealtMultiplier(Pawn attacker)
        {
            if (attacker?.genes != null && attacker.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_MamonoClaws))
            {
                return ProjectMamonoModSettings.Settings.ClawsTeaseMultiplier;
            }
            return 1f;
        }
    }

    /// <summary>
    /// The claws' price: a manipulation penalty for clawed carriers. Postfixes
    /// PawnCapacitiesHandler.GetLevel - the single read point for a pawn's
    /// capacity - exactly like TeaseCapacityPatch: applied after the game's own
    /// calculation (incl. hediff capMods and caching), so no hediff bookkeeping
    /// is needed and the penalty stays current as the gene is added or removed.
    /// </summary>
    [HarmonyPatch(typeof(PawnCapacitiesHandler), "GetLevel")]
    public static class ClawsManipulationPatch
    {
        // PawnCapacitiesHandler.pawn is private; read it via reflection (cached FieldInfo).
        private static readonly FieldInfo PawnField =
            typeof(PawnCapacitiesHandler).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Postfix(PawnCapacitiesHandler __instance, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.Manipulation)
            {
                return;
            }

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }
            if (!(pawn.genes?.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_MamonoClaws) ?? false))
            {
                return;
            }

            float factor = 1f - ProjectMamonoModSettings.Settings.ClawsManipulationPenalty;
            if (float.IsNaN(factor) || float.IsInfinity(factor))
            {
                return;
            }

            __result = Mathf.Max(0f, __result * factor);
        }
    }
}
