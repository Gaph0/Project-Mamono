using RimWorld;
using Verse;

namespace ProjectMomo
{
    [DefOf]
    public static class ProjectMomo_DefOf
    {
        public static PawnCapacityDef ProjectMomo_Willpower;
        public static GeneDef ProjectMomo_Momo;
        public static GeneDef PMM_Gene_Flight;
        public static GeneDef ProjectMomo_MomoVenom;
        public static GeneDef ProjectMomo_MomoFiery;
        public static GeneDef ProjectMomo_MomoClaws;
        public static HediffDef ProjectMomo_TeaseDamage;
        public static HediffDef ProjectMomo_VenomBuildup;
        public static HediffDef ProjectMomo_FieryBondWard;
        public static HediffDef ProjectMomo_WillpowerBreak;
        public static HediffDef ProjectMomo_ManaStarvation;
        public static PawnRelationDef ProjectMomo_Tsugai;
        public static HediffDef ProjectMomo_TsugaiBond;
        public static ThoughtDef ProjectMomo_TsugaiFormed;
        public static ThoughtDef ProjectMomo_TsugaiWilling;
        public static ThoughtDef ProjectMomo_TsugaiRejected;
        public static InteractionDef ProjectMomo_TsugaiProposalAccepted;
        public static InteractionDef ProjectMomo_TsugaiProposalRejected;
        public static JobDef ProjectMomo_FormTsugai;
        public static JobDef ProjectMomo_ProposeTsugaiBond;
        public static HediffDef ProjectMomo_TsugaiLoss;
        public static ThoughtDef ProjectMomo_TsugaiLossThought;
        public static ThoughtDef ProjectMomo_TsugaiRestored;
        public static ThoughtDef ProjectMomo_EssenceShared;
        public static ThoughtDef ProjectMomo_ManaCatharsis;
        public static NeedDef ProjectMomo_Essence;
        public static NeedDef ProjectMomo_Mana;
        public static JobDef ProjectMomo_GiveEssence;
        public static JobDef ProjectMomo_DrainEssence;
        public static JobDef ProjectMomo_EssenceBerserkAttack;
        public static JobDef ProjectMomo_LeaveColony;
        public static MentalStateDef ProjectMomo_ManaFeedingState;
        public static MentalStateDef ProjectMomo_EssenceBerserk;

        // Mamono corruption (female human -> monster transformation).
        public static XenotypeDef ProjectMomo_Xenotype_Momo;
        public static HediffDef ProjectMomo_MomoCorruption;
        public static JobDef ProjectMomo_InfuseMomo;
        public static JobDef ProjectMomo_TransformProposal;
        public static ThoughtDef ProjectMomo_MomoAwakened;
        public static ThoughtDef ProjectMomo_MomoTurned;
        public static ThoughtDef ProjectMomo_TransformRejected;
        public static InteractionDef ProjectMomo_TransformProposalAccepted;
        public static InteractionDef ProjectMomo_TransformProposalRejected;

        // Incubisation (male human -> incubus gradual transformation).
        public static HediffDef ProjectMomo_Incubisation;
        public static ThoughtDef ProjectMomo_IncubusAwakened;
        public static ThoughtDef ProjectMomo_IncubusTurned;

        // ISEKAI: a husband with the Protagonist trait always wins over a bonded Momo.
        [MayRequire("JellyCreative.IsekaiLeveling")]
        public static TraitDef Isekai_Protagonist;

        // Monster Extremists ideology meme: ascended/baseliner social opinions.
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMomo_MonsterExtremistAscended;
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMomo_MonsterExtremistBaseliner;

        static ProjectMomo_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ProjectMomo_DefOf));
        }
    }
}
