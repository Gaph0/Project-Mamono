using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Lets a bonded Momo autonomously seek out her tsugai partner and feed when
    /// her Mana runs low — the mana equivalent of vanilla's JobGiver_GetFood.
    /// No mental break, no letter: she simply walks to her mate and drains
    /// essence, then goes back to her day. Only fires for a Momo at or below the
    /// configured seek threshold; if her mate is off-map, unreachable, or dry,
    /// it stays silent and the low-mana break system remains the fallback.
    /// Inserted into the Humanlike think tree by ThinkTreeInjection, below
    /// mental states and emergencies but above LordDuty, so a visiting Momo
    /// bonded to a colonist can still break off to feed.
    /// </summary>
    public class JobGiver_SeekManaFeeding : ThinkNode_JobGiver
    {
        // The think-tree giver re-checks a pawn at most this often (the
        // reachability check in GetLivingBondPartner is the expensive part).
        private const int ScanIntervalTicks = 500;

        protected override Job TryGiveJob(Pawn pawn)
        {
            var settings = ProjectMomoModSettings.Settings;
            if (!settings.AutonomousFeedingEnabled)
            {
                return null;
            }
            if (!CanAct(pawn, settings))
            {
                return null;
            }

            int now = Find.TickManager.TicksGame;
            ManaFeedingComponent comp = ManaFeedingComponent.Get();
            if (comp != null)
            {
                if (!comp.CanScanNow(pawn, now))
                {
                    return null;
                }
                comp.NoteScan(pawn, now + ScanIntervalTicks);
                if (!comp.CanAttemptNow(pawn, now))
                {
                    return null;
                }
                // Natural feeding is capped per day — once she has fed her fill,
                // low Mana falls back to the break system as usual.
                if (!comp.UnderDailyCap(pawn, settings.AutonomousFeedMaxPerDay, now))
                {
                    return null;
                }
            }

            // Her living, reachable tsugai partner. Unbonded Momos keep their
            // feral flavor — no autonomous feeding, only the berserk break.
            Pawn partner = LowManaBreak.GetLivingBondPartner(pawn);
            if (partner == null)
            {
                return null;
            }

            if (!EssenceTransfer.CanTransfer(pawn, partner))
            {
                // Her mate is dry: back off for the retry cooldown so his essence
                // can recover, instead of re-checking every scan interval.
                NoteAttempt(pawn, comp, now, settings);
                return null;
            }

            // Issuing the job counts as an attempt: if the walk or the drain is
            // interrupted (the mate drafted off, a door locked mid-path), the
            // cooldown keeps her from re-issuing the same failing job every
            // think tick.
            NoteAttempt(pawn, comp, now, settings);
            return JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_DrainEssence, partner);
        }

        /// <summary>Cheap gates before any scan runs: a calm, upright, hungry Momo.</summary>
        private static bool CanAct(Pawn pawn, ProjectMomoSettings settings)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (!pawn.Awake() || pawn.Drafted || pawn.InMentalState)
            {
                return false;
            }
            if (pawn.mindState?.enemyTarget != null)
            {
                return false;
            }
            // Wild Momo keep their feral flavor: they drain far slower and go
            // berserk when starved rather than walking up to someone for a meal.
            if (pawn.IsWildMan())
            {
                return false;
            }
            if (!EssenceTransfer.IsMomo(pawn))
            {
                return false;
            }
            // Only a Momo whose Mana has fallen to the seek threshold goes
            // looking for her mate.
            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana == null || mana.CurLevel > settings.AutonomousFeedThreshold)
            {
                return false;
            }
            return true;
        }

        /// <summary>Starts the retry cooldown, if the bookkeeping component is available.</summary>
        private static void NoteAttempt(Pawn pawn, ManaFeedingComponent comp, int now, ProjectMomoSettings settings)
        {
            comp?.NoteAttempt(pawn, now + (int)(settings.AutonomousFeedRetryCooldownHours * GenDate.TicksPerHour));
        }
    }
}
