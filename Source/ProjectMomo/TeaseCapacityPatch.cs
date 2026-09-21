using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// As tease damage builds on a pawn, their blood pumping and breathing rise
    /// (arousal). Postfixes PawnCapacityUtility.CalculateCapacityLevel — the one
    /// function every capacity read funnels through — and scales the result by the
    /// pawn's tease severity. Applies after the game's own calculation (incl.
    /// hediff capMods, genes and their caching), so it stacks cleanly and stays
    /// current as tease changes.
    ///
    /// The game leaves that function by two routes:
    ///   * PawnCapacitiesHandler.GetLevel — cached; gameplay and stat capacity
    ///     offsets read the pawn through it, and
    ///   * the direct calls in HealthCardUtility.GetEfficiencyLabel and
    ///     GetPawnCapacityTip — the Health tab's number and its tooltip.
    /// Patching GetLevel alone (as this class originally did) left the Health tab
    /// showing the untouched value, because the tab's number is computed from this
    /// static call and never passes through the handler. Hooking the shared
    /// function covers both routes and still applies exactly once.
    /// </summary>
    [HarmonyPatch(typeof(PawnCapacityUtility), "CalculateCapacityLevel")]
    public static class TeaseCapacityPatch
    {
        public static void Postfix(HediffSet diffSet, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.BloodPumping && capacity != PawnCapacityDefOf.Breathing)
            {
                return;
            }

            Pawn pawn = diffSet?.pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            Hediff tease = diffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TeaseDamage);
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

    /// <summary>
    /// Keeps the arousal boost current between stage changes. GetLevel caches
    /// whatever CalculateCapacityLevel returns, and Hediff.Severity only notifies
    /// the game when the hediff crosses a stage boundary — enough for vanilla,
    /// whose capacity modifiers are per stage, but not for a severity-scaled boost.
    /// Dropping the cache on every tease severity change (build-up on a hit and the
    /// daily fade alike) leaves the cached value matching the current severity.
    /// </summary>
    [HarmonyPatch(typeof(Hediff), "set_Severity")]
    public static class TeaseCapacityCachePatch
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance.def == ProjectMomo_DefOf.ProjectMomo_TeaseDamage)
            {
                __instance.pawn?.health?.capacities?.Notify_CapacityLevelsDirty();
            }
        }
    }
}
