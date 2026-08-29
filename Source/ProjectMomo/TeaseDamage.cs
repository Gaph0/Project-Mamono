using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Downs a pawn whose Willpower has dropped to/below the knockout threshold,
    /// and revives them when it recovers. Driven by tease-severity changes via
    /// the Harmony postfix below, so the knockdown happens on the same tick the
    /// threshold is crossed rather than on a polled interval.
    /// </summary>
    public static class TeaseKnockout
    {
        public const float KnockoutThreshold = 0.1f;

        // Hysteresis deadband. Vanilla rounds every capacity to the nearest 0.01
        // (GenMath.RoundedHundredth in CalculateCapacityLevel), and Willpower is
        // 1 - teaseSeverity, so near the threshold the rounded capacity value 0.1
        // satisfies BOTH a "knock out at w <= 0.1" test and a too-small recovery test
        // at once. Worse, the tease fade keeps crossing the stage boundary at 0.9,
        // re-firing the two asymmetric Evaluate triggers (severity setter vs break tick)
        // against each other. Together those produced a self-sustaining collapse/stand
        // oscillation with no new hits. The cure is a true deadband: knock out at/below
        // KnockoutThreshold, but only recover once willpower climbs strictly ABOVE
        // KnockoutThreshold + RecoveryHysteresis, so the two conditions never overlap.
        // 0.02 clears the 0.01 rounding grain with room to spare (recover at tease <= 0.88).
        private const float RecoveryHysteresis = 0.02f;

        public static void Evaluate(Pawn pawn, Pawn attacker = null)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return;
            }

            // Raw, unrounded tease severity drives the decision; the rounded Willpower
            // capacity is display-only, and reading it would reintroduce the 0.01-grain
            // ambiguity described above.
            Hediff tease = pawn.health.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TeaseDamage);
            float teaseSeverity = tease?.Severity ?? 0f;
            float willpower = 1f - teaseSeverity;
            Hediff knockout = pawn.health.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_WillpowerBreak);

            // Not yet knocked out: knock out at/below the threshold. Already knocked out:
            // stay down until willpower clearly exceeds it (threshold + hysteresis). In the
            // deadband between the two, a downed pawn simply stays down and an up pawn stays up.
            bool knockedOut = knockout != null
                ? willpower <= KnockoutThreshold + RecoveryHysteresis
                : willpower <= KnockoutThreshold;

            if (knockedOut)
            {
                if (knockout == null)
                {
                    pawn.health.AddHediff(ProjectMomo_DefOf.ProjectMomo_WillpowerBreak);

                    // ISEKAI: the victim suffers a broken-will mood thought, and
                    // the Momo-carrier earns XP for the knockout.
                    IsekaiCompat.ApplyBrokenWillMood(pawn);
                    if (attacker != null)
                    {
                        IsekaiCompat.AwardKnockoutXP(attacker);
                    }
                }

                // Tsugai: a Momo who brings a man to broken willpower begins the
                // bonding action on him. Runs on the melee path (attacker known).
                if (attacker != null)
                {
                    TsugaiFormation.TryStartBondJob(attacker, pawn);
                }

                if (!pawn.Downed)
                {
                    // forceDowned feeds CheckForStateChange, which calls the
                    // private MakeDowned to flip healthState to Down.
                    pawn.health.forceDowned = true;
                    pawn.health.CheckForStateChange(null, null);
                }
            }
            else
            {
                if (knockout != null)
                {
                    pawn.health.RemoveHediff(knockout);
                }

                // The pawn may be downed by the hediff's Moving=0 capMod rather than
                // forceDowned, so always re-evaluate the downed state after recovery:
                // with the knockout hediff gone, ShouldBeDowned flips false and the
                // pawn stands back up. Clearing forceDowned alone would leave a
                // Moving-downed pawn collapsed.
                if (pawn.health.forceDowned)
                {
                    pawn.health.forceDowned = false;
                }
                if (pawn.Downed)
                {
                    pawn.health.CheckForStateChange(null, null);
                }
            }
        }
    }

    /// <summary>
    /// Re-evaluates the knockout state whenever the tease damage hediff's
    /// severity changes (build-up on hit or fade over time).
    /// </summary>
    [HarmonyPatch(typeof(Hediff), "set_Severity")]
    public static class TeaseSeverityPatch
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance.def == ProjectMomo_DefOf.ProjectMomo_TeaseDamage)
            {
                TeaseKnockout.Evaluate(__instance.pawn);
            }
        }
    }
}
