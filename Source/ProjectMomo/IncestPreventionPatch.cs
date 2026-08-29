using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// When the "Disable incest prevention" debug setting is enabled, blood-family
    /// relations no longer block romance for pawns that are subject to Project
    /// Momo's systems (Momo gene carriers, tsugai-bonded pawns, or pawns who share
    /// a tsugai partner). Both the numeric romance chance and the social-tab
    /// "Cannot Romance (Incestuous)" tooltip are suppressed.
    /// </summary>
    public static class IncestPreventionPatch
    {
        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        private static readonly GeneDef MomoGene = DefDatabase<GeneDef>.GetNamedSilentFail("ProjectMomo_Momo");

        /// <summary>
        /// True if either pawn is a Momo gene carrier, or if either pawn is
        /// tsugai-bonded to the other or to a third pawn (i.e. part of the
        /// Project Momo relationship web).
        /// </summary>
        private static bool IsAffected(Pawn a, Pawn b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (MomoGene != null)
            {
                if ((a.genes != null && a.genes.HasActiveGene(MomoGene))
                    || (b.genes != null && b.genes.HasActiveGene(MomoGene)))
                {
                    return true;
                }
            }

            if (a.relations != null && b.relations != null)
            {
                if (a.relations.DirectRelationExists(ProjectMomo_DefOf.ProjectMomo_Tsugai, b)
                    || b.relations.DirectRelationExists(ProjectMomo_DefOf.ProjectMomo_Tsugai, a))
                {
                    return true;
                }

                if (ShareTsugaiPartner(a, b))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShareTsugaiPartner(Pawn a, Pawn b)
        {
            if (a.relations == null || b.relations == null || a == b)
            {
                return false;
            }

            var partners = new System.Collections.Generic.List<Pawn>();
            a.relations.GetDirectRelations(ProjectMomo_DefOf.ProjectMomo_Tsugai, ref partners);
            for (int i = 0; i < partners.Count; i++)
            {
                Pawn shared = partners[i];
                if (shared != null && shared != b && !shared.Dead
                    && b.relations.DirectRelationExists(ProjectMomo_DefOf.ProjectMomo_Tsugai, shared))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the incest multiplier from the numeric romance chance.
        /// Vanilla computes:
        ///   result = SecondaryLovinChanceFactor(...) * romanceFactor * psychicFactor
        /// where romanceFactor is the product of all shared relations' romanceChanceFactor.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
        public static class Patch_SecondaryRomanceChanceFactor
        {
            public static void Postfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result)
            {
                if (!Settings.DisableIncestPrevention)
                {
                    return;
                }

                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null || !IsAffected(pawn, otherPawn))
                {
                    return;
                }

                foreach (PawnRelationDef relation in pawn.GetRelations(otherPawn))
                {
                    if (relation != null && relation.familyByBloodRelation && relation.romanceChanceFactor != 0f)
                    {
                        __result /= relation.romanceChanceFactor;
                    }
                }
            }
        }

        /// <summary>
        /// Stops the social tab from showing "Cannot Romance (Incestuous)" for
        /// affected pawns. The vanilla incest check simply looks for any relation
        /// whose romanceChanceFactor is not 1; we lie and say no such relation
        /// exists when the setting is on.
        /// </summary>
        [HarmonyPatch(typeof(RelationsUtility), "Incestuous", MethodType.Normal)]
        public static class Patch_RelationsUtility_Incestuous
        {
            public static void Postfix(Pawn one, Pawn two, ref bool __result)
            {
                if (!Settings.DisableIncestPrevention)
                {
                    return;
                }

                if (__result && IsAffected(one, two))
                {
                    __result = false;
                }
            }
        }

        /// <summary>
        /// Removes the "Incestuous" social thought/opinion that appears between
        /// blood-related lovers when the incest-prevention setting is disabled.
        /// </summary>
        [HarmonyPatch(typeof(ThoughtWorker_Incestuous), "CurrentSocialStateInternal")]
        public static class Patch_ThoughtWorker_Incestuous
        {
            public static bool Prefix(Pawn pawn, Pawn other, ref ThoughtState __result)
            {
                if (!Settings.DisableIncestPrevention)
                {
                    return true;
                }

                if (IsAffected(pawn, other))
                {
                    __result = ThoughtState.Inactive;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Zeroes out the numeric incest opinion offset used in social calculations
        /// for affected pawns when the incest-prevention setting is disabled.
        /// </summary>
        [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.IncestOpinionOffsetFor))]
        public static class Patch_IncestOpinionOffsetFor
        {
            public static void Postfix(Pawn other, Pawn pawn, ref float __result)
            {
                if (!Settings.DisableIncestPrevention)
                {
                    return;
                }

                if (__result != 0f && IsAffected(pawn, other))
                {
                    __result = 0f;
                }
            }
        }
    }
}
