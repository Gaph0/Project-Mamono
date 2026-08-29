using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Bonded (tsugai) pairs are perfectly matched: force the compatibility and
    /// romance/lovin' chance factors between them to their maximum. RimWorld reads
    /// these through Pawn_RelationsTracker for romance attempts, lovin', and the
    /// social tab. Applied as postfixes on the three factor methods, checking for
    /// the tsugai relation between the two pawns (reflexive, so direction doesn't
    /// matter). Tunable caps in mod settings.
    /// </summary>
    public static class TsugaiRomancePatch
    {
        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        // Pawn_RelationsTracker.pawn is private.
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn");

        private static Pawn GetPawn(Pawn_RelationsTracker tracker)
        {
            return tracker == null ? null : PawnField?.GetValue(tracker) as Pawn;
        }

        /// <summary>True if the two pawns share the tsugai bond.</summary>
        private static bool IsBonded(Pawn_RelationsTracker tracker, Pawn other)
        {
            Pawn pawn = GetPawn(tracker);
            if (pawn?.relations == null || other == null || pawn == other)
            {
                return false;
            }

            return pawn.relations.DirectRelationExists(ProjectMomo_DefOf.ProjectMomo_Tsugai, other);
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.CompatibilityWith))]
        public static class Patch_CompatibilityWith
        {
            public static void Postfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result)
            {
                if (IsBonded(__instance, otherPawn) && __result < Settings.BondCompatibility)
                {
                    __result = Settings.BondCompatibility;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryRomanceChanceFactor))]
        public static class Patch_SecondaryRomanceChanceFactor
        {
            public static void Postfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result)
            {
                if (IsBonded(__instance, otherPawn) && __result < Settings.BondRomanceFactor)
                {
                    __result = Settings.BondRomanceFactor;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
        public static class Patch_SecondaryLovinChanceFactor
        {
            public static void Postfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result)
            {
                if (IsBonded(__instance, otherPawn) && __result < Settings.BondRomanceFactor)
                {
                    __result = Settings.BondRomanceFactor;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf))]
        public static class Patch_OpinionOf
        {
            public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, ref int __result)
            {
                if (IsBonded(__instance, other) && __result < Settings.BondOpinion)
                {
                    __result = Settings.BondOpinion;
                }
            }
        }

        /// <summary>
        /// Harem-friendly lovin'/romance: bonded partners who share a common tsugai
        /// mate (e.g. several Momos bonded to the same Protagonist) don't take the
        /// vanilla "cheated on me" mood hit when that mate romances or sleeps with one
        /// of the others. TryAddCheaterThought(pawn, cheater) gives the offended pawn
        /// and the partner who strayed; we suppress it when the two share a tsugai
        /// partner. Ordinary (non-harem) jealousy is untouched.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "TryAddCheaterThought")]
        public static class Patch_TryAddCheaterThought
        {
            public static bool Prefix(Pawn pawn, Pawn cheater)
            {
                return !ShareTsugaiPartner(pawn, cheater);
            }

            /// <summary>True if <paramref name="a"/> and <paramref name="b"/> are both tsugai-bonded to the same living third pawn.</summary>
            private static bool ShareTsugaiPartner(Pawn a, Pawn b)
            {
                if (a?.relations == null || b?.relations == null || a == b)
                {
                    return false;
                }

                List<Pawn> aPartners = new List<Pawn>();
                a.relations.GetDirectRelations(ProjectMomo_DefOf.ProjectMomo_Tsugai, ref aPartners);
                if (aPartners.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < aPartners.Count; i++)
                {
                    Pawn shared = aPartners[i];
                    if (shared != null && shared != b && !shared.Dead
                        && b.relations.DirectRelationExists(ProjectMomo_DefOf.ProjectMomo_Tsugai, shared))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
