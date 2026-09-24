using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The "essence" need on ordinary humans (the resource Momos feed on). Gated to
    /// humanlikes via minIntelligence, and removed from Momo-carriers by the gene's
    /// disablesNeeds. It slowly regenerates over time.
    /// </summary>
    public class Need_Essence : Need
    {
        // Full refill over ~1 day (60000 ticks).
        private const float GainPerTick = 1f / 60000f;

        public Need_Essence(Pawn pawn) : base(pawn)
        {
            threshPercents = new System.Collections.Generic.List<float> { 0.2f, 0.5f };
        }

        public override void NeedInterval()
        {
            if (IsFrozen)
            {
                return;
            }

            // ISEKAI: the average of STR and VIT speeds essence recovery.
            // Incubisation: a mana-touched man's essence grows ever richer.
            // Gear: a metal that wards essence (dragonium) adds a flat bonus per piece
            // worn. PMM_EssenceRecovery is an OFFSET stat fed from worn apparel, so
            // three pieces give three times what one piece gives, never a product.
            float gearBonus = pawn.GetStatValue(ProjectMomo_DefOf.PMM_EssenceRecovery);
            CurLevel += GainPerTick * 150f * IsekaiCompat.EssenceRechargeMultiplier(pawn) * Incubisation.EssenceRegenMultiplier(pawn) * (1f + gearBonus);
        }
    }
}
