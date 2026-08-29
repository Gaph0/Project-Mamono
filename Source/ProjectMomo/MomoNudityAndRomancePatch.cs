using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// General Momo behaviour tweaks:
    /// 1. Momos have no opinion on nudity — they ignore mood and social thoughts
    ///    from being naked, being clothed as a nudist, or seeing/being seen with
    ///    uncovered body parts. Also suppresses the thought onlookers would get
    ///    from a naked Momo, so Momos can move around uncovered without creating
    ///    social friction.
    /// 2. Age imposes no negative multiplier on Momo romance/lovin' chances.
    ///    The under-age hard gates in SecondaryLovinChanceFactor are left alone.
    /// </summary>
    [HarmonyPatch]
    public static class MomoNuditySelfPatch
    {
        private static bool IsMomo(Pawn pawn)
        {
            return pawn?.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo) ?? false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Base-game self thoughts about nakedness / nudism.
            yield return AccessTools.Method(typeof(ThoughtWorker_PsychologicallyNude), "CurrentStateInternal");
            yield return AccessTools.Method(typeof(ThoughtWorker_NudistNude), "CurrentStateInternal");

            // Ideology precept self thoughts about uncovered body parts.
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinUncovered), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinOrChestUncovered), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinChestOrHairUncovered), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinChestHairOrFaceUncovered), "ShouldHaveThought");
        }

        public static bool Prefix(Pawn p, ref ThoughtState __result)
        {
            if (IsMomo(p))
            {
                __result = ThoughtState.Inactive;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class MomoNuditySocialPatch
    {
        private static bool IsMomo(Pawn pawn)
        {
            return pawn?.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo) ?? false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Ideology precept social thoughts about uncovered body parts.
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinUncovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinOrChestUncovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinChestOrHairUncovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_GroinChestHairOrFaceUncovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_AnyBodyPartCovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_AnyBodyPartButGroinCovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered_Social), "ShouldHaveThought");
            yield return AccessTools.Method(typeof(ThoughtWorker_Precept_FaceCovered_Social), "ShouldHaveThought");
        }

        public static bool Prefix(Pawn p, Pawn otherPawn, ref ThoughtState __result)
        {
            // Momos do not form opinions about nudity, and others do not form
            // nudity opinions about Momos.
            if (IsMomo(p) || IsMomo(otherPawn))
            {
                __result = ThoughtState.Inactive;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
    public static class MomoLovinAgePatch
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn");

        private static bool IsMomo(Pawn pawn)
        {
            return pawn?.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo) ?? false;
        }

        public static void Postfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result)
        {
            if (__result == 1f)
            {
                return;
            }

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (IsMomo(pawn) || IsMomo(otherPawn))
            {
                __result = 1f;
            }
        }
    }
}
