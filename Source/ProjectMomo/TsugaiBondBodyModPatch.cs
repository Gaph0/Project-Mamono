using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Prevents body-purist and body-modder (transhumanist) ideology opinions from
    /// firing because of the tsugai bond hediff. The bond is a natural, consensual
    /// union, not an artificial body modification, so it should not trigger
    /// "disgusted by artificial body" or "appreciates artificial body" thoughts.
    /// </summary>
    [HarmonyPatch]
    public static class TsugaiBondBodyModPatch
    {
        internal static bool IsBonded(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }

            return pawn.health.hediffSet.HasHediff(ProjectMomo_DefOf.ProjectMomo_TsugaiBond);
        }

        internal static bool IsMomo(Pawn pawn)
        {
            return pawn?.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo) ?? false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Body purist disgust / transhumanist appreciation (social opinion).
            yield return AccessTools.Method(typeof(ThoughtWorker_BodyPuristDisgust), "CurrentSocialStateInternal");
            yield return AccessTools.Method(typeof(ThoughtWorker_TranshumanistAppreciation), "CurrentSocialStateInternal");
        }

        public static bool Prefix(Pawn p, Pawn other, ref ThoughtState __result)
        {
            if (IsBonded(p) || IsBonded(other) || IsMomo(p) || IsMomo(other))
            {
                __result = ThoughtState.Inactive;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Companion patch for the self-directed prosthetic precept thought workers.
    /// These target ShouldHaveThought(Pawn p), which has no second parameter, so
    /// they cannot share the social-opinion Prefix above (Harmony requires every
    /// patch parameter to match a parameter on the target method).
    /// </summary>
    [HarmonyPatch]
    public static class TsugaiBondProstheticPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_HasProsthetic), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_HasNoProsthetic), "ShouldHaveThought");
        }

        public static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            if (TsugaiBondBodyModPatch.IsBonded(p) || TsugaiBondBodyModPatch.IsMomo(p))
            {
                __result = ThoughtState.Inactive;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Companion patch for the social prosthetic precept thought workers. Their
    /// second parameter is named "otherPawn" (unlike "other" on the body-purist
    /// workers), so they need their own Prefix signature.
    /// </summary>
    [HarmonyPatch]
    public static class TsugaiBondProstheticSocialPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_HasProsthetic_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_HasNoProsthetic_Social), "ShouldHaveThought");
        }

        public static bool Prefix(Pawn p, Pawn otherPawn, ref ThoughtState __result)
        {
            if (TsugaiBondBodyModPatch.IsBonded(p) || TsugaiBondBodyModPatch.IsBonded(otherPawn)
                || TsugaiBondBodyModPatch.IsMomo(p) || TsugaiBondBodyModPatch.IsMomo(otherPawn))
            {
                __result = ThoughtState.Inactive;
                return false;
            }

            return true;
        }
    }
}
