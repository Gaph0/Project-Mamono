using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Implements an Ideology-style "Free Love" behavior for pawns with the Isekai
    /// RPG LEVELING Protagonist trait. When a Protagonist has multiple lovers,
    /// sleeping with or romancing one of them does not generate affair pop-ups,
    /// cheater thoughts, or "CheatedOnMe" memories on the Protagonist's other
    /// partners. Existing lovers are also left intact when a new romance succeeds.
    /// </summary>
    public static class ProtagonistAffairPatch
    {
        private static bool IsProtagonist(Pawn pawn)
        {
            return pawn != null
                && ProjectMomo_DefOf.Isekai_Protagonist != null
                && pawn.story?.traits?.HasTrait(ProjectMomo_DefOf.Isekai_Protagonist) == true;
        }

        /// <summary>
        /// Skips the vanilla break-up logic for Protagonists. When a Protagonist
        /// gains a new lover/fiancé, their existing lovers/fiancés are left intact.
        /// This avoids the infinite "no longer in a relationship" message spam.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "BreakLoverAndFianceRelations")]
        public static class Patch_BreakLoverAndFianceRelations
        {
            public static bool Prefix(Pawn pawn, ref List<Pawn> oldLoversAndFiances)
            {
                if (!IsProtagonist(pawn))
                {
                    return true;
                }

                // Ensure the out list is initialized (vanilla expects a valid list
                // even if empty) so the caller can iterate without null checks.
                if (oldLoversAndFiances == null)
                {
                    oldLoversAndFiances = new List<Pawn>();
                }

                return false;
            }
        }

        /// <summary>
        /// Suppresses the "CheatedOnMe" thought that vanilla adds to a pawn when
        /// their lover sleeps with someone else, if the cheater is a Protagonist.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "TryAddCheaterThought")]
        public static class Patch_TryAddCheaterThought
        {
            public static bool Prefix(Pawn pawn, Pawn cheater)
            {
                return !IsProtagonist(cheater);
            }
        }

        /// <summary>
        /// Suppresses the "Affair" social thought between a Protagonist's partners.
        /// The thought fires when one partner sees the other as having cheated.
        /// </summary>
        [HarmonyPatch(typeof(ThoughtWorker_Affair), "CurrentSocialStateInternal")]
        public static class Patch_ThoughtWorker_Affair
        {
            public static bool Prefix(Pawn p, Pawn otherPawn, ref ThoughtState __result)
            {
                if (IsProtagonist(p) || IsProtagonist(otherPawn))
                {
                    __result = ThoughtState.Inactive;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Replaces the entire new-lovers letter for Protagonists so the vanilla
        /// affair branch is never taken. Generates the same positive letter that
        /// vanilla uses when neither pawn has existing lovers.
        /// </summary>
        [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "GetNewLoversLetter")]
        public static class Patch_GetNewLoversLetter
        {
            public static bool Prefix(Pawn initiator, Pawn recipient, List<Pawn> initiatorOldLoversAndFiances, List<Pawn> recipientOldLoversAndFiances, bool createdBond, ref string letterText, ref string letterLabel, ref LetterDef letterDef, ref LookTargets lookTargets)
            {
                if (!IsProtagonist(initiator) && !IsProtagonist(recipient))
                {
                    return true;
                }

                // Use the real vanilla translation keys for the positive new-lovers letter.
                letterLabel = "LetterLabelNewLovers".Translate();
                letterDef = LetterDefOf.PositiveEvent;

                string text = "LetterNewLovers".Translate(initiator.Named("PAWN1"), recipient.Named("PAWN2"));

                if (createdBond)
                {
                    text = text + "\n\n" + "LetterNewLoversBonded".Translate(initiator.Named("PAWN1"), recipient.Named("PAWN2"));
                }

                letterText = text;
                lookTargets = new LookTargets(new Pawn[] { initiator, recipient });

                return false;
            }
        }
    }
}
