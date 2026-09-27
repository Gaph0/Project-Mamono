using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Computes Willpower as a pawn capacity shown on the Health tab (alongside
    /// Consciousness, Manipulation, etc.). Mamono-carriers get a 1.5x bonus; bonded
    /// (tsugai) pawns get a configurable bonus; tease damage reduces it by its
    /// severity (Willpower = base * (1 - teaseSeverity)).
    /// </summary>
    public class PawnCapacityWorker_Willpower : PawnCapacityWorker
    {
        private const float MamonoBonus = 1.5f;

        public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            Pawn pawn = diffSet?.pawn;

            float level = 1f;

            if (pawn != null && pawn.genes != null && pawn.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_Mamono))
            {
                level *= MamonoBonus;
            }

            // ISEKAI: VIT boosts willpower.
            level *= IsekaiCompat.WillpowerMultiplier(pawn);

            // The tsugai bond steels both partners' resolve - one stack per bond.
            float bonusPerBond = ProjectMamonoModSettings.Settings.BondWillpowerBonus;
            if (bonusPerBond != 0f)
            {
                int bonds = Hediff_TsugaiBond.CountBonds(diffSet);
                if (bonds > 0)
                {
                    level *= 1f + bonusPerBond * bonds;
                }
            }

            // A loosening of the shackles: once incubisation passes the
            // near-incubus stage, the man's resolve hardens with it.
            level *= Incubisation.WillpowerMultiplier(pawn);

            // Losing bonded mates crushes the survivor's willpower. Each grief
            // hediff adds a penalty, but the total is floored so even several
            // broken bonds can't reduce willpower past a set fraction.
            float penaltyPerLoss = ProjectMamonoModSettings.Settings.BondLossWillpowerPenalty;
            if (penaltyPerLoss != 0f && diffSet != null)
            {
                int griefs = 0;
                foreach (Hediff h in diffSet.hediffs)
                {
                    if (h.def == ProjectMamono_DefOf.ProjectMamono_TsugaiLoss)
                    {
                        griefs++;
                    }
                }

                if (griefs > 0)
                {
                    float totalPenalty = Math.Min(penaltyPerLoss * griefs, ProjectMamonoModSettings.Settings.BondLossWillpowerPenaltyCap);
                    level *= 1f - totalPenalty;
                }
            }

            Hediff tease = diffSet?.GetFirstHediffOfDef(ProjectMamono_DefOf.ProjectMamono_TeaseDamage);
            if (tease != null)
            {
                // Tease erodes willpower multiplicatively (Willpower = base * (1 - severity)),
                // so VIT / bond / gene bonuses are scaled down along with the base rather than
                // discarded. A higher-willpower pawn therefore genuinely resists teasing: it
                // takes more tease to push them under the knockout threshold, while anyone can
                // still be broken once severity climbs high enough (level -> 0 as severity -> 1).
                level *= Mathf.Max(0f, 1f - tease.Severity);
            }

            if (level < 0f)
            {
                level = 0f;
            }

            return level;
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return true;
        }
    }
}
