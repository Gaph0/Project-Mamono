using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Shared ascended/baseliner classification for the Monster Extremists
    /// ideology meme ("All men should be drained, and all women should be
    /// transformed!"). Ascended = a transformed woman (any Momo) or a claimed
    /// man (living tsugai bond). Baseliner = an adult humanlike who is neither.
    /// Believers form no opinion about children, and "drained" is flavour only:
    /// no essence or incubisation check is involved. Every def that uses these
    /// classes is gated behind MayRequire Ideology, so they are only ever
    /// instantiated when the DLC is active.
    /// </summary>
    public static class MonsterExtremist
    {
        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        /// <summary>True for pawns the meme approves of: any Momo, or any pawn with a living tsugai bond.</summary>
        public static bool IsAscended(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            return EssenceTransfer.IsMomo(pawn) || TsugaiFormation.HasBondedPartner(pawn);
        }

        /// <summary>True for adult humanlikes who are neither transformed nor bonded — the meme's "baseliners".</summary>
        public static bool IsBaseliner(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYearsFloat < Settings.CorruptionMinAge)
            {
                return false;
            }
            return !IsAscended(pawn);
        }
    }

    /// <summary>Positive social opinion of ascended pawns, granted by the ascension precept.</summary>
    public class ThoughtWorker_MonsterExtremistAscendedSocial : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return MonsterExtremist.IsAscended(otherPawn);
        }
    }

    /// <summary>Negative social opinion of untransformed, unbonded adults, granted by the ascension precept.</summary>
    public class ThoughtWorker_MonsterExtremistBaselinerSocial : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            return MonsterExtremist.IsBaseliner(otherPawn);
        }
    }

    /// <summary>
    /// The situational social thought behind both Monster Extremist opinions.
    /// The opinion offset itself is read from mod settings (per thought def) so
    /// players can tune how zealously believers judge each other.
    /// </summary>
    public class Thought_MonsterExtremistSocial : Thought_SituationalSocial
    {
        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        public override float OpinionOffset()
        {
            if (ThoughtUtility.ThoughtNullified(pawn, def))
            {
                return 0f;
            }
            if (def == ProjectMomo_DefOf.ProjectMomo_MonsterExtremistAscended)
            {
                return Settings.MonsterExtremistAscendedOpinion;
            }
            return Settings.MonsterExtremistBaselinerOpinion;
        }
    }
}
