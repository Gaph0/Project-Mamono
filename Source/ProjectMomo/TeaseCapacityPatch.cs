using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// As tease damage builds on a pawn, their blood pumping and breathing rise
    /// (arousal). Postfixes PawnCapacitiesHandler.GetLevel — the single read point
    /// for a pawn's capacity — and scales the result by the pawn's tease severity.
    /// Applies after the game's own capacity calculation (incl. hediff capMods and
    /// caching), so it stacks cleanly and stays current as tease changes.
    /// </summary>
    [HarmonyPatch(typeof(PawnCapacitiesHandler), "GetLevel")]
    public static class TeaseCapacityPatch
    {
        // PawnCapacitiesHandler.pawn is private; read it via reflection (cached FieldInfo).
        private static readonly System.Reflection.FieldInfo PawnField =
            typeof(PawnCapacitiesHandler).GetField("pawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static void Postfix(PawnCapacitiesHandler __instance, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.BloodPumping && capacity != PawnCapacityDefOf.Breathing)
            {
                return;
            }

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            Hediff tease = pawn.health?.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TeaseDamage);
            if (tease == null)
            {
                return;
            }

            float boost = 1f + tease.Severity * ProjectMomoModSettings.Settings.TeaseCapacityBoostPerSeverity;
            if (float.IsNaN(boost) || float.IsInfinity(boost))
            {
                return;
            }

            __result = Mathf.Max(0f, __result * boost);
        }
    }
}
