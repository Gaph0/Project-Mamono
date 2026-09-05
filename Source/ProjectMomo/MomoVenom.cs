using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Public entry point for applying momo-venom build-up from anywhere
    /// (melee now, abilities or mod integrations later). Mirrors the tease
    /// system's TeaseApplication: create-or-add on the victim, clamp to the
    /// hediff's severity range, return the amount actually applied. The venom
    /// is non-lethal by def construction — the hediff only suppresses the
    /// Moving capacity — so no knockout evaluation is needed here; the
    /// severity-change patch below keeps the victim's downed state current.
    /// </summary>
    public static class MomoVenomApplication
    {
        /// <summary>
        /// Applies <paramref name="severity"/> venom to the victim and returns
        /// the amount actually applied after clamping (0 when the victim has no
        /// health tracker, is already at max, or the severity is not finite).
        /// </summary>
        public static float TryApplyVenom(Pawn victim, Pawn attacker, float severity)
        {
            if (victim?.health?.hediffSet == null || victim.Dead)
            {
                return 0f;
            }
            if (float.IsNaN(severity) || float.IsInfinity(severity) || severity <= 0f)
            {
                return 0f;
            }

            float applied;
            Hediff venom = victim.health.hediffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_VenomBuildup);
            if (venom != null)
            {
                float before = venom.Severity;
                venom.Severity = Mathf.Clamp(venom.Severity + severity, venom.def.minSeverity, venom.def.maxSeverity);
                applied = venom.Severity - before;
            }
            else
            {
                // Whole-body hediff: the venom is systemic, not a wound.
                venom = HediffMaker.MakeHediff(ProjectMomo_DefOf.ProjectMomo_VenomBuildup, victim);
                venom.Severity = Mathf.Clamp(severity, venom.def.minSeverity, venom.def.maxSeverity);
                victim.health.AddHediff(venom);
                applied = venom.Severity;
            }

            return applied;
        }
    }

    /// <summary>
    /// Re-evaluates the victim's downed state whenever venom severity changes —
    /// build-up on hit or fade over time. A capacity-downed pawn (Moving below
    /// the 0.15 capability floor) is only re-checked when the game runs
    /// CheckForStateChange, and a severity change on an existing hediff does
    /// not trigger one on its own. Driven by the same Harmony postfix pattern
    /// as TeaseSeverityPatch, so the pin-down and the stand-up happen on the
    /// same tick the threshold is crossed rather than on a polled interval.
    /// </summary>
    [HarmonyPatch(typeof(Hediff), "set_Severity")]
    public static class VenomSeverityPatch
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance.def != ProjectMomo_DefOf.ProjectMomo_VenomBuildup)
            {
                return;
            }

            Pawn pawn = __instance.pawn;
            if (pawn?.health == null || pawn.Dead)
            {
                return;
            }

            pawn.health.CheckForStateChange(null, null);
        }
    }
}
