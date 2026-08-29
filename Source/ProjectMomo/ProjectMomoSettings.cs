using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Persisted mod settings. Values are adjustable in Options &gt; Mod Settings &gt; Project Momo.
    /// Defaults match the original hard-coded balance.
    /// </summary>
    public class ProjectMomoSettings : ModSettings
    {
        // VIT -> Willpower capacity multiplier (per stat point above base 5).
        public float VitWillpowerPerPoint = 0.02f;
        // Attacker CHA -> tease damage dealt (per stat point above base 5).
        public float ChaTeasePerPoint = 0.05f;
        // Victim WIS -> tease damage resisted (per stat point above base 5).
        public float WisTeaseResistPerPoint = 0.03f;
        // Avg(STR, VIT) -> essence recharge rate (per stat point above base 5).
        public float StrVitEssenceRegenPerPoint = 0.03f;
        // Avg(WIS, INT) -> mana drain reduction (per stat point above base 5).
        public float WisIntManaDrainPerPoint = 0.03f;
        // BloodPumping/Breathing boost per point of tease severity (0.5 = +50% at full tease).
        public float TeaseCapacityBoostPerSeverity = 0.5f;
        // Tease damage per point of the attacker's beauty (vanilla beauty -2..+2).
        public float BeautyTeasePerPoint = 0.25f;
        // Lump XP awarded on a tease knockout.
        public int KnockoutXP = 25;
        // XP per full point of tease severity dealt.
        public float XPPerTeaseSeverity = 40f;
        // XP per full point of essence consumed by a Momo (give/drain).
        public float XPPerEssence = 50f;

        // Mana starvation: days for the decline to fully escalate while Mana is empty.
        public float StarvationDaysToMax = 1.5f;
        // Mana restored per point of food nutrition a Momo eats (0.15 = a 0.9-nutrition
        // meal restores ~13% of her mana bar). Food is only a supplement — essence
        // feeding remains the real source of mana. 0 = food gives no mana.
        public float ManaFromFoodPerNutrition = 0.15f;
        // Mana drain multiplier for WILD Momo (wild men): below 1 drains slower than tamed/colonist
        // Momo, so a wild Momo roaming the map doesn't starve as fast. 1 = same as tamed.
        public float WildManaDrainFactor = 0.25f;
        // Mana level (fraction) below which a Momo can randomly suffer a feeding break.
        public float LowManaBreakThreshold = 0.1f;
        // Mental-break mean-time-between (days) while Mana is below the threshold.
        public float LowManaBreakMtbDays = 0.5f;

        // Base chance a Momo who bonds to a colonist joins the colony (equal levels).
        public float BondJoinBaseChance = 0.25f;
        // Join-chance shift per level the husband outlevels (or is outleveled by) the Momo.
        public float BondJoinChancePerLevel = 0.05f;
        // Upper cap on the bond join chance.
        public float BondJoinMaxChance = 0.9f;

        // Compatibility floor between bonded (tsugai) pawns (vanilla max ~1.0).
        public float BondCompatibility = 1.0f;
        // Romance/lovin' chance-factor floor between bonded pawns (vanilla max ~1.0).
        public float BondRomanceFactor = 1.0f;
        // Opinion floor between bonded pawns (vanilla cap is 100).
        public int BondOpinion = 100;
        // Minimum biological age a male must be to be bonded to.
        public float BondMinAge = 16f;
        // Willpower bonus per tsugai bond (stacks with each bond; 0.25 = +25% each).
        public float BondWillpowerBonus = 0.25f;
        // Hard cap on how many tsugai bonds one pawn can stack (matches the hediff's max severity).
        public int BondMaxStacks = 10;
        // Willpower penalty per broken tsugai bond being grieved (0.5 = -50% each).
        public float BondLossWillpowerPenalty = 0.5f;
        // Floor on the total bond-loss willpower penalty — stacked griefs can never
        // reduce willpower past this fraction (0.75 = always keeps at least 25%).
        public float BondLossWillpowerPenaltyCap = 0.75f;
        // Days a bond-grief hediff lasts before it fades (min..max range).
        public float BondLossMinDays = 5f;
        public float BondLossMaxDays = 10f;

        // Voluntary (consensual) tsugai bonding: master switch.
        public bool VoluntaryBondingEnabled = true;
        // Let Momos autonomously propose bonds to men they want.
        public bool VoluntaryBondMomoProposals = true;
        // Let men autonomously propose bonds to Momos they want.
        public bool VoluntaryBondManProposals = true;
        // Essence a voluntary bond costs the man — he must have at least this much
        // to offer a bond. Essence is the bonding budget: it keeps one man from
        // collecting Momos faster than he can regenerate.
        public float VoluntaryBondEssenceCost = 0.5f;
        // Minimum desire score for a pawn to act on a voluntary bond (0..1).
        public float VoluntaryBondDesireThreshold = 0.6f;
        // Flat join-chance bonus for a willingly formed bond with a colonist.
        public float VoluntaryBondJoinBonus = 0.25f;
        // Hours before a pawn can make another proposal attempt.
        public float VoluntaryBondAttemptCooldownHours = 6f;
        // Hours a rejected pair must wait before either may propose to the other again.
        public float VoluntaryBondRejectionCooldownHours = 24f;

        // Play Yayo's lovin' (romancin') animation on the Momo during tsugai bonding.
        public bool YayoBondingAnimation = true;

        // Intimacy - Friends n' Lovers: a Momo having sex through the Intimacy mod
        // drains enough essence from her partner to fill her Mana bar.
        public bool IntimacyFeeding = true;

        // Intimacy + Yayo's Animation: play Yayo's romancin' bounce on both
        // partners for the duration of an Intimacy sex act.
        public bool IntimacyAnimation = true;

        // Momos don't lose fertility to age (Biotech's fertility age curve is
        // cancelled for Momo-carriers).
        public bool MomoFertilityAgeless = true;

        // Hidden debug tab enabler.
        public bool DebugTabEnabled = false;

        // Disable incest prevention for tsugai-bonded / Momo-related romance/lovin'.
        public bool DisableIncestPrevention = false;

        // Mamono corruption: master switch for the female-human-to-monster
        // transformation system (mana infusion of will-broken women).
        public bool CorruptionEnabled = true;
        // Corruption progress from one completed infusion (0.25 = four infusions to transform).
        public float CorruptionSeverityPerInfusion = 0.25f;
        // Corruption lost per day while the victim's willpower is above the knockout threshold.
        public float CorruptionDecayPerDay = 0.2f;
        // Mana one infusion costs the Momo (fraction of her mana bar).
        public float CorruptionManaCost = 0.2f;
        // Minimum biological age a woman must be to be corrupted.
        public float CorruptionMinAge = 16f;

        // Base chance a non-colonist corrupted by one of your colonists joins the colony (equal levels).
        public float CorruptionJoinBaseChance = 0.25f;
        // Join-chance shift per level the corruptor outlevels (or is outleveled by) the victim.
        public float CorruptionJoinChancePerLevel = 0.05f;
        // Upper cap on the corruption join chance.
        public float CorruptionJoinMaxChance = 0.9f;

        // Voluntary (consensual) transformation: master switch.
        public bool VoluntaryCorruptionEnabled = true;
        // Let Momos autonomously offer a transformation to women they want.
        public bool VoluntaryCorruptionMomoProposals = true;
        // Minimum desire score for a Momo to offer a transformation (0..1).
        public float VoluntaryCorruptionDesireThreshold = 0.6f;
        // Flat join-chance bonus when a woman willingly accepts the change.
        public float VoluntaryCorruptionJoinBonus = 0.25f;
        // Hours before a Momo can make another transformation offer.
        public float VoluntaryCorruptionAttemptCooldownHours = 6f;
        // Hours a refused Momo must wait before offering to the same woman again.
        public float VoluntaryCorruptionRejectionCooldownHours = 24f;

        // Let Momos autonomously infuse downed, corruptible women (the forced path).
        public bool AutonomousCorruptionEnabled = true;

        // VPE integration: allow Momo xenotypes to spawn with VPE psycasts.
        public bool VPEPsycastsEnabled = true;

        public void ResetToDefaults()
        {
            VitWillpowerPerPoint = 0.02f;
            ChaTeasePerPoint = 0.05f;
            WisTeaseResistPerPoint = 0.03f;
            StrVitEssenceRegenPerPoint = 0.03f;
            WisIntManaDrainPerPoint = 0.03f;
            TeaseCapacityBoostPerSeverity = 0.5f;
            BeautyTeasePerPoint = 0.25f;
            KnockoutXP = 25;
            XPPerTeaseSeverity = 40f;
            XPPerEssence = 50f;
            StarvationDaysToMax = 1.5f;
            ManaFromFoodPerNutrition = 0.15f;
            LowManaBreakThreshold = 0.1f;
            LowManaBreakMtbDays = 0.5f;
            BondJoinBaseChance = 0.25f;
            BondJoinChancePerLevel = 0.05f;
            BondJoinMaxChance = 0.9f;
            BondCompatibility = 1.0f;
            BondRomanceFactor = 1.0f;
            BondOpinion = 100;
            BondMinAge = 16f;
            BondWillpowerBonus = 0.25f;
            BondMaxStacks = 10;
            BondLossWillpowerPenalty = 0.5f;
            BondLossWillpowerPenaltyCap = 0.75f;
            BondLossMinDays = 5f;
            BondLossMaxDays = 10f;
            VoluntaryBondingEnabled = true;
            VoluntaryBondMomoProposals = true;
            VoluntaryBondManProposals = true;
            VoluntaryBondEssenceCost = 0.5f;
            VoluntaryBondDesireThreshold = 0.6f;
            VoluntaryBondJoinBonus = 0.25f;
            VoluntaryBondAttemptCooldownHours = 6f;
            VoluntaryBondRejectionCooldownHours = 24f;
            YayoBondingAnimation = true;
            IntimacyFeeding = true;
            IntimacyAnimation = true;
            MomoFertilityAgeless = true;
            DebugTabEnabled = false;
            DisableIncestPrevention = false;
            CorruptionEnabled = true;
            CorruptionSeverityPerInfusion = 0.25f;
            CorruptionDecayPerDay = 0.2f;
            CorruptionManaCost = 0.2f;
            CorruptionMinAge = 16f;
            CorruptionJoinBaseChance = 0.25f;
            CorruptionJoinChancePerLevel = 0.05f;
            CorruptionJoinMaxChance = 0.9f;
            VoluntaryCorruptionEnabled = true;
            VoluntaryCorruptionMomoProposals = true;
            VoluntaryCorruptionDesireThreshold = 0.6f;
            VoluntaryCorruptionJoinBonus = 0.25f;
            VoluntaryCorruptionAttemptCooldownHours = 6f;
            VoluntaryCorruptionRejectionCooldownHours = 24f;
            AutonomousCorruptionEnabled = true;
            VPEPsycastsEnabled = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref VitWillpowerPerPoint, "VitWillpowerPerPoint", 0.02f);
            Scribe_Values.Look(ref ChaTeasePerPoint, "ChaTeasePerPoint", 0.05f);
            Scribe_Values.Look(ref WisTeaseResistPerPoint, "WisTeaseResistPerPoint", 0.03f);
            Scribe_Values.Look(ref StrVitEssenceRegenPerPoint, "StrVitEssenceRegenPerPoint", 0.03f);
            Scribe_Values.Look(ref WisIntManaDrainPerPoint, "WisIntManaDrainPerPoint", 0.03f);
            Scribe_Values.Look(ref TeaseCapacityBoostPerSeverity, "TeaseCapacityBoostPerSeverity", 0.5f);
            Scribe_Values.Look(ref BeautyTeasePerPoint, "BeautyTeasePerPoint", 0.25f);
            Scribe_Values.Look(ref KnockoutXP, "KnockoutXP", 25);
            Scribe_Values.Look(ref XPPerTeaseSeverity, "XPPerTeaseSeverity", 40f);
            Scribe_Values.Look(ref XPPerEssence, "XPPerEssence", 50f);
            Scribe_Values.Look(ref StarvationDaysToMax, "StarvationDaysToMax", 1.5f);
            Scribe_Values.Look(ref ManaFromFoodPerNutrition, "ManaFromFoodPerNutrition", 0.15f);
            Scribe_Values.Look(ref LowManaBreakThreshold, "LowManaBreakThreshold", 0.1f);
            Scribe_Values.Look(ref LowManaBreakMtbDays, "LowManaBreakMtbDays", 0.5f);
            Scribe_Values.Look(ref BondJoinBaseChance, "BondJoinBaseChance", 0.25f);
            Scribe_Values.Look(ref BondJoinChancePerLevel, "BondJoinChancePerLevel", 0.05f);
            Scribe_Values.Look(ref BondJoinMaxChance, "BondJoinMaxChance", 0.9f);
            Scribe_Values.Look(ref BondCompatibility, "BondCompatibility", 1.0f);
            Scribe_Values.Look(ref BondRomanceFactor, "BondRomanceFactor", 1.0f);
            Scribe_Values.Look(ref BondOpinion, "BondOpinion", 100);
            Scribe_Values.Look(ref BondMinAge, "BondMinAge", 7f);
            Scribe_Values.Look(ref BondWillpowerBonus, "BondWillpowerBonus", 0.25f);
            Scribe_Values.Look(ref BondMaxStacks, "BondMaxStacks", 10);
            Scribe_Values.Look(ref BondLossWillpowerPenalty, "BondLossWillpowerPenalty", 0.5f);
            Scribe_Values.Look(ref BondLossWillpowerPenaltyCap, "BondLossWillpowerPenaltyCap", 0.75f);
            Scribe_Values.Look(ref BondLossMinDays, "BondLossMinDays", 5f);
            Scribe_Values.Look(ref BondLossMaxDays, "BondLossMaxDays", 10f);
            Scribe_Values.Look(ref VoluntaryBondingEnabled, "VoluntaryBondingEnabled", true);
            Scribe_Values.Look(ref VoluntaryBondMomoProposals, "VoluntaryBondMomoProposals", true);
            Scribe_Values.Look(ref VoluntaryBondManProposals, "VoluntaryBondManProposals", true);
            Scribe_Values.Look(ref VoluntaryBondEssenceCost, "VoluntaryBondEssenceCost", 0.5f);
            Scribe_Values.Look(ref VoluntaryBondDesireThreshold, "VoluntaryBondDesireThreshold", 0.6f);
            Scribe_Values.Look(ref VoluntaryBondJoinBonus, "VoluntaryBondJoinBonus", 0.25f);
            Scribe_Values.Look(ref VoluntaryBondAttemptCooldownHours, "VoluntaryBondAttemptCooldownHours", 6f);
            Scribe_Values.Look(ref VoluntaryBondRejectionCooldownHours, "VoluntaryBondRejectionCooldownHours", 24f);
            Scribe_Values.Look(ref YayoBondingAnimation, "YayoBondingAnimation", true);
            Scribe_Values.Look(ref IntimacyFeeding, "IntimacyFeeding", true);
            Scribe_Values.Look(ref IntimacyAnimation, "IntimacyAnimation", true);
            Scribe_Values.Look(ref MomoFertilityAgeless, "MomoFertilityAgeless", true);
            Scribe_Values.Look(ref DebugTabEnabled, "DebugTabEnabled", false);
            Scribe_Values.Look(ref DisableIncestPrevention, "DisableIncestPrevention", false);
            Scribe_Values.Look(ref CorruptionEnabled, "CorruptionEnabled", true);
            Scribe_Values.Look(ref CorruptionSeverityPerInfusion, "CorruptionSeverityPerInfusion", 0.25f);
            Scribe_Values.Look(ref CorruptionDecayPerDay, "CorruptionDecayPerDay", 0.2f);
            Scribe_Values.Look(ref CorruptionManaCost, "CorruptionManaCost", 0.2f);
            Scribe_Values.Look(ref CorruptionMinAge, "CorruptionMinAge", 16f);
            Scribe_Values.Look(ref CorruptionJoinBaseChance, "CorruptionJoinBaseChance", 0.25f);
            Scribe_Values.Look(ref CorruptionJoinChancePerLevel, "CorruptionJoinChancePerLevel", 0.05f);
            Scribe_Values.Look(ref CorruptionJoinMaxChance, "CorruptionJoinMaxChance", 0.9f);
            Scribe_Values.Look(ref VoluntaryCorruptionEnabled, "VoluntaryCorruptionEnabled", true);
            Scribe_Values.Look(ref VoluntaryCorruptionMomoProposals, "VoluntaryCorruptionMomoProposals", true);
            Scribe_Values.Look(ref VoluntaryCorruptionDesireThreshold, "VoluntaryCorruptionDesireThreshold", 0.6f);
            Scribe_Values.Look(ref VoluntaryCorruptionJoinBonus, "VoluntaryCorruptionJoinBonus", 0.25f);
            Scribe_Values.Look(ref VoluntaryCorruptionAttemptCooldownHours, "VoluntaryCorruptionAttemptCooldownHours", 6f);
            Scribe_Values.Look(ref VoluntaryCorruptionRejectionCooldownHours, "VoluntaryCorruptionRejectionCooldownHours", 24f);
            Scribe_Values.Look(ref AutonomousCorruptionEnabled, "AutonomousCorruptionEnabled", true);
            Scribe_Values.Look(ref VPEPsycastsEnabled, "VPEPsycastsEnabled", true);

            // Self-heal an inverted grief-duration range (e.g. a hand-edited config
            // file): min must never exceed max.
            if (BondLossMinDays > BondLossMaxDays)
            {
                BondLossMinDays = BondLossMaxDays;
            }
        }
    }
}
