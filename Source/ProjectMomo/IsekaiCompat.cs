using IsekaiLeveling;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Hard-dependency bridge to the ISEKAI RPG LEVELING mod (JellyCreative.IsekaiLeveling).
    /// Maps ISEKAI's RPG stats onto Project Momo's Willpower / tease mechanics.
    /// All tunables are read from mod settings (Options &gt; Mod Settings &gt; Project Momo).
    /// Per-point scaling follows ISEKAI's own convention:
    /// multiplier = 1 + (stat - BASE_STAT_VALUE) * perPoint, clamped.
    /// </summary>
    public static class IsekaiCompat
    {
        private const int BaseStat = IsekaiStatAllocation.BASE_STAT_VALUE; // 5

        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        /// <summary>Get the pawn's Isekai component, or null if it has none.</summary>
        public static IsekaiComponent Comp(Pawn pawn)
        {
            if (pawn == null) return null;
            return IsekaiComponent.GetCached(pawn);
        }

        /// <summary>
        /// Get a pawn's Isekai level regardless of faction. Player-faction pawns use
        /// IsekaiComponent.Level; other (mob/hostile) pawns use MobRankComponent.
        /// Returns 1 if the pawn has no Isekai level.
        /// </summary>
        public static int GetLevel(Pawn pawn)
        {
            if (pawn == null) return 1;

            var playerComp = Comp(pawn);
            if (playerComp != null)
            {
                return playerComp.Level;
            }

            var mobComp = pawn.TryGetComp<IsekaiLeveling.MobRanking.MobRankComponent>();
            if (mobComp != null)
            {
                return mobComp.currentLevel;
            }

            return 1;
        }

        /// <summary>Essence recharge multiplier from the average of STR and VIT.</summary>
        public static float EssenceRechargeMultiplier(Pawn pawn)
        {
            var comp = Comp(pawn);
            if (comp?.stats == null) return 1f;
            float avg = (comp.stats.strength + comp.stats.vitality) * 0.5f;
            float mult = 1f + (avg - BaseStat) * Settings.StrVitEssenceRegenPerPoint;
            return Mathf.Clamp(mult, 0.5f, 3f);
        }

        /// <summary>Mana drain multiplier from the average of WIS and INT. Higher mental stats = slower drain (below 1).</summary>
        public static float ManaConservationMultiplier(Pawn pawn)
        {
            var comp = Comp(pawn);
            if (comp?.stats == null) return 1f;
            float avg = (comp.stats.wisdom + comp.stats.intelligence) * 0.5f;
            float mult = 1f - (avg - BaseStat) * Settings.WisIntManaDrainPerPoint;
            return Mathf.Clamp(mult, 0.3f, 1.2f);
        }

        /// <summary>Tease damage multiplier from the ATTACKER's beauty. Vanilla beauty runs -2..+2, so a beautiful Momo teases harder.</summary>
        public static float TeaseBeautyMultiplier(Pawn attacker)
        {
            if (attacker == null) return 1f;
            float beauty = attacker.GetStatValue(StatDefOf.PawnBeauty);
            float mult = 1f + beauty * Settings.BeautyTeasePerPoint;
            return Mathf.Clamp(mult, 0.3f, 3f);
        }

        /// <summary>Willpower multiplier from VIT. 1.0 at base (5), higher with more VIT.</summary>
        public static float WillpowerMultiplier(Pawn pawn)
        {
            var comp = Comp(pawn);
            if (comp?.stats == null) return 1f;
            float mult = 1f + (comp.stats.vitality - BaseStat) * Settings.VitWillpowerPerPoint;
            return Mathf.Clamp(mult, 0.5f, 3f);
        }

        /// <summary>Tease damage multiplier from the ATTACKER's CHA.</summary>
        public static float TeaseDealtMultiplier(Pawn attacker)
        {
            var comp = Comp(attacker);
            if (comp?.stats == null) return 1f;
            float mult = 1f + (comp.stats.charisma - BaseStat) * Settings.ChaTeasePerPoint;
            return Mathf.Clamp(mult, 0.5f, 3f);
        }

        /// <summary>Tease damage multiplier from the VICTIM's WIS (higher WIS = less tease taken).</summary>
        public static float TeaseResistMultiplier(Pawn victim)
        {
            var comp = Comp(victim);
            if (comp?.stats == null) return 1f;
            float mult = 1f - (comp.stats.wisdom - BaseStat) * Settings.WisTeaseResistPerPoint;
            return Mathf.Clamp(mult, 0.3f, 1.2f);
        }

        /// <summary>Award Isekai XP to a Momo-carrier for breaking a victim's will.</summary>
        public static void AwardKnockoutXP(Pawn attacker)
        {
            var comp = Comp(attacker);
            if (comp == null) return;
            comp.GainXP(Settings.KnockoutXP, "Tease Knockout");
        }

        /// <summary>Award Isekai XP proportional to the tease severity actually applied.</summary>
        public static void AwardTeaseDamageXP(Pawn attacker, float severityApplied)
        {
            var comp = Comp(attacker);
            if (comp == null || severityApplied <= 0f) return;
            int xp = Mathf.RoundToInt(severityApplied * Settings.XPPerTeaseSeverity);
            if (xp > 0)
            {
                comp.GainXP(xp, "Tease Damage");
            }
        }

        /// <summary>Give the victim Isekai's broken-will mood thought.</summary>
        public static void ApplyBrokenWillMood(Pawn victim)
        {
            IsekaiMoods.Gain(victim, IsekaiMoods.BrokenWill);
        }

        /// <summary>Award Isekai XP to a Momo for essence consumed (give/drain).</summary>
        public static void AwardEssenceXP(Pawn momo, float essenceConsumed)
        {
            var comp = Comp(momo);
            if (comp == null || essenceConsumed <= 0f) return;
            int xp = Mathf.RoundToInt(essenceConsumed * Settings.XPPerEssence);
            if (xp > 0)
            {
                comp.GainXP(xp, "Essence");
            }
        }
    }
}
