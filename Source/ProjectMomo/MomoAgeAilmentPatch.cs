using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Makes the Momo gene a cure for old age. Vanilla hands out the classic
    /// age ailments (bad back, frail, cataract, hearing loss, dementia,
    /// Alzheimer's, asthma, carcinoma, artery blockage, heart attack) through
    /// HediffGiver_Birthday and HediffGiver_RandomAgeCurved, which both funnel
    /// into HediffGiver.TryApply — so one prefix there silently prevents every
    /// age-driven application on a Momo-carrier. RemoveAgeAilments then strips
    /// the ailments a carrier already had when she gained the gene. No letters,
    /// no messages: the diseases simply cease to exist.
    /// </summary>
    [HarmonyPatch(typeof(HediffGiver), "TryApply")]
    public static class MomoAgeAilmentPatch
    {
        /// <summary>
        /// Prevention: age-driven givers never fire on Momo-carriers, nor on
        /// men whose incubisation has suffused them (the frailties of age are
        /// among the first human limits the mana dissolves). Other givers
        /// sharing TryApply (drug overdoses, hypothermia, events, ...) are
        /// untouched — a Momo can still suffer a go-juice heart attack.
        /// </summary>
        public static bool Prefix(HediffGiver __instance, Pawn pawn, ref bool __result)
        {
            if (pawn == null || !IsAgeGiver(__instance))
            {
                return true;
            }

            if (!EssenceTransfer.IsMomo(pawn) && !Incubisation.PreventsAgeAilments(pawn))
            {
                return true;
            }

            __result = false;
            return false;
        }

        /// <summary>True for the two vanilla age-driven givers (and any modded subclasses).</summary>
        public static bool IsAgeGiver(HediffGiver giver)
        {
            return giver is HediffGiver_Birthday || giver is HediffGiver_RandomAgeCurved;
        }

        /// <summary>
        /// Silently removes every purely-age-related ailment from the pawn.
        /// Hediffs that non-age givers can also cause (a heart attack from an
        /// artery blockage drug side-effect, say) are left alone — the patch
        /// above already handles the age side of those.
        /// </summary>
        public static void RemoveAgeAilments(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i].def != null && AgeAilmentDefs.Contains(hediffs[i].def))
                {
                    pawn.health.RemoveHediff(hediffs[i]);
                }
            }
        }

        /// <summary>
        /// Every hediff an age giver can apply, minus any hediff a non-age
        /// giver can also apply. Built lazily from all HediffGiverSetDefs and
        /// hediff-nested givers so modded age ailments count too.
        /// </summary>
        private static HashSet<HediffDef> ageAilmentDefs;

        private static HashSet<HediffDef> AgeAilmentDefs
        {
            get
            {
                if (ageAilmentDefs == null)
                {
                    HashSet<HediffDef> fromAge = new HashSet<HediffDef>();
                    HashSet<HediffDef> fromOther = new HashSet<HediffDef>();

                    foreach (HediffGiverSetDef setDef in DefDatabase<HediffGiverSetDef>.AllDefs)
                    {
                        Collect(setDef.hediffGivers, fromAge, fromOther);
                    }

                    foreach (HediffDef hediffDef in DefDatabase<HediffDef>.AllDefs)
                    {
                        Collect(hediffDef.hediffGivers, fromAge, fromOther);
                    }

                    fromAge.ExceptWith(fromOther);
                    ageAilmentDefs = fromAge;
                }
                return ageAilmentDefs;
            }
        }

        private static void Collect(List<HediffGiver> givers, HashSet<HediffDef> fromAge, HashSet<HediffDef> fromOther)
        {
            if (givers == null)
            {
                return;
            }

            foreach (HediffGiver giver in givers)
            {
                if (giver?.hediff == null)
                {
                    continue;
                }

                (IsAgeGiver(giver) ? fromAge : fromOther).Add(giver.hediff);
            }
        }
    }
}
