using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Pawns with the Isekai RPG LEVELING Protagonist trait are not limited to a
    /// single lover or spouse. This file covers marriage and romance-success
    /// tweaks. The Free-Love-style break-up/cheater suppression lives in
    /// ProtagonistAffairPatch.cs.
    /// </summary>

    /// <summary>
    /// Prevents marrying a Protagonist from demoting existing spouses to ex-spouses.
    /// </summary>
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.ChangeSpouseRelationsToExSpouse))]
    public static class ProtagonistKeepSpousesPatch
    {
        public static bool Prefix(Pawn pawn)
        {
            if (pawn == null) return true;
            if (ProjectMamono_DefOf.Isekai_Protagonist == null) return true;
            return pawn.story?.traits?.HasTrait(ProjectMamono_DefOf.Isekai_Protagonist) != true;
        }
    }

    /// <summary>
    /// Romance attempts keep succeeding for Protagonists even after they already
    /// have a lover. This patches InteractionWorker_RomanceAttempt.SuccessChance
    /// and boosts the result when the initiator is a Protagonist.
    /// </summary>
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "SuccessChance")]
    public static class ProtagonistRomanceSuccessPatch
    {
        private static bool IsProtagonist(Pawn pawn)
        {
            return pawn != null
                && ProjectMamono_DefOf.Isekai_Protagonist != null
                && pawn.story?.traits?.HasTrait(ProjectMamono_DefOf.Isekai_Protagonist) == true;
        }

        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (!IsProtagonist(initiator)) return;
            if (recipient == null) return;

            // Only boost if the interaction is currently being suppressed
            // (vanilla/Isekai may zero it out because the initiator already has
            // a lover). We never reduce the chance here.
            if (__result <= 0f || __result < 0.75f)
            {
                __result = 0.999f;
            }
        }
    }
}
