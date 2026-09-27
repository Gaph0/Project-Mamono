using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// What the arrogant trait does in play, beyond its XML (Defs/TraitDefs_Mamono.xml):
    /// she thinks less of anyone standing below her ISEKAI level, and she is quicker to
    /// insult them than to make small talk.
    ///
    /// It takes code because a TraitDef cannot read another pawn's state - no trait
    /// field compares two pawns. The comparison itself is cheap: ISEKAI RPG LEVELING is
    /// a hard dependency of this mod, and IsekaiCompat.GetLevel already covers both
    /// sides of it (a player-faction pawn's IsekaiComponent, a mob's MobRankComponent,
    /// and 1 for a pawn with no level at all), so a pawn without levels is level 1 and
    /// never gets looked down on for it.
    ///
    /// The numbers are the constants below, so tuning is one edit each.
    /// </summary>
    public static class ArrogantTrait
    {
        /// <summary>Opinion lost per level the other pawn stands below her.</summary>
        public const int OpinionPenaltyPerLevel = 2;

        /// <summary>Ceiling on that penalty, so one high-level dragon does not end up
        /// hating the whole map at the worst possible opinion.</summary>
        public const int OpinionPenaltyCap = 12;

        /// <summary>How much likelier she is to pick an insult over small talk.</summary>
        public const float InsultWeightFactor = 1.5f;

        // A relations tracker's pawn field is private (the same field TsugaiLossPatch
        // reads), and opinion is asked for constantly - every Social tab row, every
        // interaction check - so it is read through one cached field reference rather
        // than reflection per call.
        private static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> TrackerOwner =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

        public static Pawn OwnerOf(Pawn_RelationsTracker tracker) =>
            tracker == null ? null : TrackerOwner(tracker);

        public static bool IsArrogant(Pawn pawn) =>
            pawn?.story?.traits != null && pawn.story.traits.HasTrait(ProjectMamono_DefOf.PMM_Arrogant);

        /// <summary>
        /// Levels she stands above the other pawn: positive when she is above, negative
        /// when the other is above her, and 0 for herself or a missing pawn.
        /// </summary>
        public static int LevelsAbove(Pawn self, Pawn other)
        {
            if (self == null || other == null || self == other)
            {
                return 0;
            }
            return IsekaiCompat.GetLevel(self) - IsekaiCompat.GetLevel(other);
        }

        /// <summary>
        /// Opinion she loses on this pawn. 0 when she is not arrogant, when the other pawn
        /// is not below her, or when it is her own opinion of herself - so the total and
        /// the line explaining the total go quiet together while a collar is on.
        /// </summary>
        public static int OpinionPenalty(Pawn self, Pawn other) =>
            IsArrogant(self)
                ? Mathf.Min(Mathf.Max(LevelsAbove(self, other), 0) * OpinionPenaltyPerLevel, OpinionPenaltyCap)
                : 0;
    }

    /// <summary>She thinks less of anyone below her level.</summary>
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf))]
    public static class ArrogantOpinionPatch
    {
        public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, ref int __result)
        {
            __result -= ArrogantTrait.OpinionPenalty(ArrogantTrait.OwnerOf(__instance), other);
        }
    }

    /// <summary>
    /// And she says so. Vanilla builds the Social tab's opinion breakdown by listing the
    /// contributions it knows about - relations it can look up, thoughts, hediff stages - and
    /// it has never heard of ours, so the penalty moved the number without ever appearing in
    /// the list. This appends the missing line in the shape the engine uses for its own
    /// (" - Arrogant (they are 6 levels lower): -6", the same signed-number helper), so a player
    /// can see both the amount and the reason for it where they go looking.
    ///
    /// The line is only added while the penalty is live, which is why a wedding collar takes
    /// it off the screen at the same time as it takes it out of the total.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionExplanation))]
    public static class ArrogantOpinionExplanationPatch
    {
        public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, ref string __result)
        {
            if (__result.NullOrEmpty())
            {
                return;
            }
            Pawn self = ArrogantTrait.OwnerOf(__instance);
            int penalty = ArrogantTrait.OpinionPenalty(self, other);
            if (penalty <= 0)
            {
                return;
            }
            __result += "\n - " + ReasonFor(self, other) + ": " + (-penalty).ToStringWithSign();
        }

        /// <summary>
        /// The reason as a player should read it. Vanilla's own lines say why a pawn feels the
        /// way they do ("Insulted x2", "Relationship"), so a bare trait name would answer half
        /// the question: this says what the penalty is for - how far below her the other pawn
        /// stands. The trait label comes from the def, so renaming the trait renames the line,
        /// and the wording is a keyed translation like the rest of this mod.
        /// </summary>
        private static string ReasonFor(Pawn self, Pawn other)
        {
            string traitLabel = TraitLabel();
            int gap = ArrogantTrait.LevelsAbove(self, other);
            return gap == 1
                ? "PMM_ArrogantOpinionLineOne".Translate(traitLabel)
                : "PMM_ArrogantOpinionLine".Translate(traitLabel, gap);
        }

        /// <summary>
        /// The trait's name as her trait list shows it. A TraitDef carries two kinds of label -
        /// its own, and one per degree - and the degree is the one a player reads. A def that
        /// states only the degree has an empty Def.LabelCap, which is how this line first went
        /// out reading " (they are 197 levels lower)" with no trait named in it. Reading the
        /// degree here means the line names the trait whether or not the def's own label is
        /// filled in, and renaming the degree renames the line.
        /// </summary>
        private static string TraitLabel()
        {
            TraitDef def = ProjectMamono_DefOf.PMM_Arrogant;
            if (def.degreeDatas != null && def.degreeDatas.Count > 0)
            {
                return def.degreeDatas[0].label.CapitalizeFirst();
            }
            return def.LabelCap;
        }
    }

    /// <summary>And she is quicker to insult them.</summary>
    [HarmonyPatch(typeof(InteractionWorker_Insult), nameof(InteractionWorker_Insult.RandomSelectionWeight))]
    public static class ArrogantInsultPatch
    {
        public static void Postfix(InteractionWorker __instance, Pawn initiator, Pawn recipient, ref float __result)
        {
            // RandomSelectionWeight is declared on InteractionWorker, and this patch asks
            // for it through the insult worker. When the insult worker has no override of
            // its own, Harmony resolves it to the base method, which every interaction
            // shares - so this type check is what keeps the boost to insults alone.
            if (__result <= 0f || !(__instance is InteractionWorker_Insult))
            {
                return;
            }
            if (!ArrogantTrait.IsArrogant(initiator) || ArrogantTrait.LevelsAbove(initiator, recipient) <= 0)
            {
                return;
            }
            __result *= ArrogantTrait.InsultWeightFactor;
        }
    }
}
