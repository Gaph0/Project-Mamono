using RimWorld;
using Verse;

namespace ProjectMamono
{
    [DefOf]
    public static class ProjectMamono_DefOf
    {
        public static PawnCapacityDef ProjectMamono_Willpower;
        public static GeneDef ProjectMamono_Mamono;
        public static GeneDef PMM_Gene_Flight;
        public static GeneDef PMM_Gene_FlightWeak;
        public static GeneDef PMM_Gene_LargeFrame;
        public static GeneDef ProjectMamono_MamonoVenom;
        public static GeneDef ProjectMamono_MamonoFiery;
        public static GeneDef ProjectMamono_MamonoClaws;
        public static HediffDef ProjectMamono_TeaseDamage;
        public static HediffDef ProjectMamono_VenomBuildup;
        public static HediffDef ProjectMamono_FieryBondWard;
        public static HediffDef ProjectMamono_WillpowerBreak;
        public static HediffDef ProjectMamono_ManaStarvation;
        public static PawnRelationDef ProjectMamono_Tsugai;
        public static HediffDef ProjectMamono_TsugaiBond;
        public static ThoughtDef ProjectMamono_TsugaiFormed;
        public static ThoughtDef ProjectMamono_TsugaiWilling;
        public static ThoughtDef ProjectMamono_TsugaiRejected;
        public static InteractionDef ProjectMamono_TsugaiProposalAccepted;
        public static InteractionDef ProjectMamono_TsugaiProposalRejected;
        public static JobDef ProjectMamono_FormTsugai;
        public static JobDef ProjectMamono_ProposeTsugaiBond;
        public static HediffDef ProjectMamono_TsugaiLoss;
        public static ThoughtDef ProjectMamono_TsugaiLossThought;
        public static ThoughtDef ProjectMamono_TsugaiRestored;
        public static ThoughtDef ProjectMamono_EssenceShared;
        public static ThoughtDef ProjectMamono_ManaCatharsis;
        public static NeedDef ProjectMamono_Essence;
        public static NeedDef ProjectMamono_Mana;
        public static StatDef PMM_EssenceRecovery;
        public static StatDef PMM_ManaDrain;
        public static JobDef ProjectMamono_GiveEssence;
        public static JobDef ProjectMamono_DrainEssence;
        public static JobDef ProjectMamono_DrainEssenceDry;
        public static JobDef ProjectMamono_EssenceBerserkAttack;
        public static JobDef ProjectMamono_LeaveColony;
        public static MentalStateDef ProjectMamono_ManaFeedingState;
        public static MentalStateDef ProjectMamono_EssenceBerserk;

        // Mamono corruption (female human -> monster transformation).
        public static XenotypeDef ProjectMamono_Xenotype_Mamono;
        public static HediffDef ProjectMamono_MamonoCorruption;
        public static JobDef ProjectMamono_InfuseMamono;
        public static JobDef ProjectMamono_TransformProposal;
        public static ThoughtDef ProjectMamono_MamonoAwakened;
        public static ThoughtDef ProjectMamono_MamonoTurned;
        public static ThoughtDef ProjectMamono_TransformRejected;
        public static InteractionDef ProjectMamono_TransformProposalAccepted;
        public static InteractionDef ProjectMamono_TransformProposalRejected;

        // Incubisation (male human -> incubus gradual transformation).
        public static HediffDef ProjectMamono_Incubisation;
        public static ThoughtDef ProjectMamono_IncubusAwakened;
        public static ThoughtDef ProjectMamono_IncubusTurned;

        // ISEKAI: a husband with the Protagonist trait always wins over a bonded Mamono.
        [MayRequire("JellyCreative.IsekaiLeveling")]
        public static TraitDef Isekai_Protagonist;

        // Arrogant (2026-09-23): a plain personality trait, forced by the reptile dragons'
        // pride gene. Its teeth are in ArrogantTraitPatch.cs, keyed on ISEKAI levels.
        public static TraitDef PMM_Arrogant;

        // Monster Extremists ideology meme: ascended/baseliner social opinions.
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMamono_MonsterExtremistAscended;
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMamono_MonsterExtremistBaseliner;
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMamono_MonsterExtremistAscendedRelaxed;
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMamono_MonsterExtremistAscendedStrict;
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static ThoughtDef ProjectMamono_MonsterExtremistBaselinerStrict;

        static ProjectMamono_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ProjectMamono_DefOf));
        }
    }
}
