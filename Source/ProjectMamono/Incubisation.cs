using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// The single entry point for incubisation: a human man's gradual, permanent
    /// change into an incubus through repeated intimate essence transfer with a
    /// Mamono (the male counterpart to mamono corruption). Every transfer that
    /// funnels through <see cref="EssenceTransfer.Transfer"/> - Give/Drain jobs,
    /// tsugai bonding, Intimacy-mod sex - calls <see cref="ApplyDose"/> here.
    ///
    /// Unlike the female path there is no xenotype swap: the man stays human,
    /// and the progress hediff's severity stages simply grant more of the
    /// incubus' perks (essence regen, rest/hunger relief, freedom from age
    /// ailments, lifespan matching his mate). The most recent Mamono whose mana
    /// infused him leaves her MARK on him: once the mark sets, other unbonded
    /// Mamonos will not feed from him (<see cref="MarkProtects"/>).
    /// </summary>
    public static class Incubisation
    {
        // Epsilon for "full severity" comparisons (same tolerance as the corruption path).
        private const float FullEpsilon = 0.0001f;

        private static ProjectMamonoSettings Settings => ProjectMamonoModSettings.Settings;

        /// <summary>
        /// Could this pawn ever be incubised? A living, male, non-monster
        /// humanlike of bondable age. Mirrors MamonoTransformation.CanEverTransform:
        /// the Mamono-gene check doubles as the re-transformation guard (a Mamono can
        /// never become an incubus), and the age gate reuses the bonding minimum.
        /// </summary>
        public static bool CanEverIncubise(Pawn pawn)
        {
            return CanEverIncubise(pawn, out _);
        }

        /// <summary>As <see cref="CanEverIncubise(Pawn)"/>, with a player-facing reason for refusals.</summary>
        public static bool CanEverIncubise(Pawn pawn, out string reason)
        {
            reason = null;
            if (pawn == null || pawn.Dead)
            {
                return false;
            }
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike || pawn.genes == null)
            {
                reason = "not human";
                return false;
            }
            if (EssenceTransfer.IsMamono(pawn))
            {
                reason = "already a monster";
                return false;
            }
            if (pawn.gender != Gender.Male)
            {
                reason = "not male";
                return false;
            }
            if (pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYearsFloat < Settings.BondMinAge)
            {
                reason = "too young";
                return false;
            }
            return true;
        }

        /// <summary>The pawn's incubisation progress hediff, or null if he has none.</summary>
        public static Hediff_Incubisation ProgressOf(Pawn pawn)
        {
            return pawn?.health?.hediffSet?
                .GetFirstHediffOfDef(ProjectMamono_DefOf.ProjectMamono_Incubisation) as Hediff_Incubisation;
        }

        /// <summary>True once the man's incubisation has run its full course.</summary>
        public static bool IsFullIncubus(Pawn pawn)
        {
            Hediff_Incubisation progress = ProgressOf(pawn);
            return progress != null && progress.Severity >= progress.def.maxSeverity - FullEpsilon;
        }

        /// <summary>
        /// True while the man is suffused enough (severity ≥ 0.5) that age-driven
        /// ailments no longer touch him - the frailties of old age are among the
        /// first human limits the mana dissolves. Used by MamonoAgeAilmentPatch.
        /// </summary>
        public static bool PreventsAgeAilments(Pawn pawn)
        {
            Hediff_Incubisation progress = ProgressOf(pawn);
            return progress != null && progress.Severity >= 0.5f;
        }

        /// <summary>
        /// Essence regeneration multiplier from incubisation progress: marked men
        /// produce richer essence (×1.25), near-incubi ×1.75, and a full incubus
        /// the configured amount. 1.0 below the mark threshold.
        /// </summary>
        public static float EssenceRegenMultiplier(Pawn pawn)
        {
            Hediff_Incubisation progress = ProgressOf(pawn);
            if (progress == null || progress.Severity < Settings.IncubisationMarkThreshold)
            {
                return 1f;
            }
            if (progress.Severity >= progress.def.maxSeverity - FullEpsilon)
            {
                return Settings.IncubisationEssenceRegenFull;
            }
            if (progress.Severity >= 0.75f)
            {
                // Halfway from the near-incubus rate to the full rate.
                return (1.5f + Settings.IncubisationEssenceRegenFull) * 0.5f;
            }
            return 1.25f;
        }

        /// <summary>
        /// Willpower multiplier once the shackles start to give (severity ≥ 0.75):
        /// the configured bonus, 1.0 below that stage.
        /// </summary>
        public static float WillpowerMultiplier(Pawn pawn)
        {
            Hediff_Incubisation progress = ProgressOf(pawn);
            if (progress == null || progress.Severity < 0.75f)
            {
                return 1f;
            }
            return 1f + Settings.IncubisationWillpowerBonus;
        }

        /// <summary>
        /// The marking rule: true when the man carries a set mark left by a
        /// DIFFERENT, still-living Mamono - an unbonded <paramref name="mamono"/>
        /// respects the claim and will not feed from him. A Mamono bonded (tsugai)
        /// to the man is exempt: wives feed their husband, and a harem outranks
        /// a stranger's mark. A dead monster's claim fades with her.
        /// </summary>
        public static bool MarkProtects(Pawn mamono, Pawn man)
        {
            Hediff_Incubisation progress = ProgressOf(man);
            if (progress == null || progress.Severity < Settings.IncubisationMarkThreshold)
            {
                return false;
            }
            Pawn marker = progress.Source;
            if (marker == null || marker == mamono || marker.Dead)
            {
                return false;
            }
            if (mamono?.relations != null && man != null
                && mamono.relations.DirectRelationExists(ProjectMamono_DefOf.ProjectMamono_Tsugai, man))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// One feeding's worth of incubisation: scaled by the essence actually
        /// moved, multiplied when the feeding Mamono is bonded to the man, and
        /// daily-capped per man. Creates the progress hediff on first dose and
        /// imprints the feeding Mamono as his most recent claim - last claimant
        /// wins, so a different Mamono feeding a half-changed man overwrites the
        /// mark while keeping his accumulated progress.
        /// </summary>
        public static void ApplyDose(Pawn mamono, Pawn man, float essenceAmount)
        {
            if (!Settings.IncubisationEnabled || essenceAmount <= 0f
                || !EssenceTransfer.IsMamono(mamono) || !CanEverIncubise(man))
            {
                return;
            }

            float dose = essenceAmount * Settings.IncubisationPerEssenceFactor;
            if (mamono.relations != null
                && mamono.relations.DirectRelationExists(ProjectMamono_DefOf.ProjectMamono_Tsugai, man))
            {
                dose *= Settings.IncubisationBondedMultiplier;
            }

            Hediff_Incubisation progress = ProgressOf(man);
            if (progress == null)
            {
                progress = HediffMaker.MakeHediff(ProjectMamono_DefOf.ProjectMamono_Incubisation, man) as Hediff_Incubisation;
                if (progress == null)
                {
                    return;
                }
                man.health.AddHediff(progress);
            }

            // The daily cap may refuse the dose entirely; the mark only moves
            // when mana actually did (whoever truly infused him last claims him).
            if (progress.TryApplyDose(dose))
            {
                progress.Imprint(mamono);
            }
        }
    }
}
