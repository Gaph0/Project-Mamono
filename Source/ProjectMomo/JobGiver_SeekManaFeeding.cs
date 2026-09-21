using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Lets a bonded Momo autonomously seek out her tsugai partner and feed when
    /// her Mana runs low — the mana equivalent of vanilla's JobGiver_GetFood.
    /// No mental break, no letter: she simply walks to her mate and drains
    /// essence, then goes back to her day. A captive Momo is shut out of the break
    /// system and cannot leave, so when no bonded mate is available she feeds on a
    /// fellow prisoner instead — the same act, without the intimate side effects.
    /// Only fires for a Momo at or below the configured seek threshold; if no one
    /// suitable is around, the low-mana break system remains the fallback for the
    /// free Momos it still covers.
    /// Inserted into the Humanlike think tree by ThinkTreeInjection, below
    /// mental states and emergencies but above LordDuty, so a visiting Momo
    /// bonded to a colonist can still break off to feed.
    ///
    /// When enabled in settings, the pair's lovin' MTB scales the whole
    /// cadence: vanilla's lovin' MTB starts from a 12-hour base, so
    /// 12 / GetLovinMtbHours(pawn, partner) is a "drive" multiplier — 1 for an
    /// average couple, higher for a high-libido pair, lower for a low-libido
    /// one. A high-drive Momo starts seeking at a higher Mana level, re-checks
    /// sooner, retries sooner after a failed feed and may feed more times per
    /// day; a low-drive Momo seeks less than the configured rates.
    /// </summary>
    public class JobGiver_SeekManaFeeding : ThinkNode_JobGiver
    {
        // The think-tree giver re-checks a pawn at most this often (the
        // reachability check in GetLivingBondPartner is the expensive part).
        private const int ScanIntervalTicks = 500;

        // Vanilla's lovin' MTB is computed from this base (hours); drive is the
        // ratio between it and the pair's actual MTB.
        private const float BaseLovinMtbHours = 12f;

        // The hunger gate is widened by at most this factor either way, so a
        // high-drive Momo tops up early but never feeds from a full bar.
        private const float MaxThresholdDriveScale = 1.5f;

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
            if (comp != null && !comp.CanScanNow(pawn, now))
            {
                return null;
            }

            // Lovin' drive: the pair's lovin' MTB as a seeking-frequency
            // multiplier (1 = the configured rates; the toggle off = fixed rates).
            float drive = ResolveLovinDrive(pawn, settings, comp, now);

            // The precise hunger gate, widened (or narrowed) by her drive: a
            // high-drive Momo goes for top-up feeds, a low-drive one waits.
            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana.CurLevel > ScaledThreshold(settings.AutonomousFeedThreshold, drive))
            {
                return null;
            }

            if (comp != null)
            {
                // A high-drive Momo re-checks sooner; a low-drive one drifts.
                comp.NoteScan(pawn, now + (int)(ScanIntervalTicks / drive));
                if (!comp.CanAttemptNow(pawn, now))
                {
                    return null;
                }
                // Natural feeding is capped per day — once she has fed her fill,
                // low Mana falls back to the break system as usual. Her drive
                // scales the cap, never below a single feed.
                if (!comp.UnderDailyCap(pawn, ScaledDailyCap(settings.AutonomousFeedMaxPerDay, drive), now))
                {
                    return null;
                }
            }

            // Her living, reachable tsugai partner. Unbonded Momos keep their
            // feral flavor — no autonomous feeding, only the berserk break. The one
            // exception is a captive: no mate to reach, no way out of the cell and no
            // break to fall back on, so a fellow prisoner is her only meal.
            Pawn partner = LowManaBreak.GetLivingBondPartner(pawn);
            bool captiveFeed = partner == null && pawn.IsPrisoner;
            if (captiveFeed)
            {
                partner = EssenceTransfer.FindFellowPrisonerToDrain(pawn);
            }
            if (partner == null)
            {
                return null;
            }

            if (!EssenceTransfer.CanTransfer(pawn, partner))
            {
                // Her mate is dry: back off for the (drive-scaled) retry cooldown
                // so his essence can recover, instead of re-checking every scan.
                NoteAttempt(pawn, comp, now, settings, drive);
                return null;
            }

            // Issuing the job counts as an attempt: if the walk or the drain is
            // interrupted (the mate drafted off, a door locked mid-path), the
            // cooldown keeps her from re-issuing the same failing job every
            // think tick.
            NoteAttempt(pawn, comp, now, settings, drive);
            // Feeding on a fellow prisoner uses the dry variant: no lovin' memory, no lovin' job.
            return JobMaker.MakeJob(
                captiveFeed ? ProjectMomo_DefOf.ProjectMomo_DrainEssenceDry : ProjectMomo_DefOf.ProjectMomo_DrainEssence,
                partner);
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
            // Coarse hunger gate: only a Momo anywhere near her seek threshold
            // is worth scanning at all. The drive multiplier can widen the
            // threshold by at most MaxThresholdDriveScale, so the precise gate
            // runs in TryGiveJob once her drive is known.
            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana == null || mana.CurLevel > settings.AutonomousFeedThreshold * MaxThresholdDriveScale)
            {
                return false;
            }
            return true;
        }

        /// <summary>Starts the (drive-scaled) retry cooldown, if the bookkeeping component is available.</summary>
        private static void NoteAttempt(Pawn pawn, ManaFeedingComponent comp, int now, ProjectMomoSettings settings, float drive)
        {
            comp?.NoteAttempt(pawn, now + (int)(settings.AutonomousFeedRetryCooldownHours / drive * GenDate.TicksPerHour));
        }

        /// <summary>
        /// The pair's lovin' drive as a feeding-frequency multiplier:
        /// BaseLovinMtbHours / GetLovinMtbHours(pawn, partner), so 1 for an
        /// average couple, above 1 for a high-libido pair and below 1 for a
        /// low-libido one — clamped to [1/max, max]. Neutral (1) when the
        /// feature is off, she is unbonded, or the pair currently can't
        /// (bleeding, in labor). Cached per pawn for an hour: the MTB walk
        /// (hediffs, capacities, relations) is too costly to run every think cycle.
        /// </summary>
        private static float ResolveLovinDrive(Pawn pawn, ProjectMomoSettings settings, ManaFeedingComponent comp, int now)
        {
            if (!settings.AutonomousFeedLovinDriven || comp == null || pawn.relations == null)
            {
                return 1f;
            }
            if (comp.TryGetCachedDrive(pawn, now, out float cached))
            {
                return cached;
            }

            float drive = 1f;
            // Any living on-map bond partner will do for the reading — this is
            // a libido proxy, not the reachability-resolved feed target.
            Pawn partner = pawn.relations.GetFirstDirectRelationPawn(
                ProjectMomo_DefOf.ProjectMomo_Tsugai,
                p => p != null && !p.Dead && p.Spawned && p.Map == pawn.Map);
            if (partner != null)
            {
                float mtbHours = LovePartnerRelationUtility.GetLovinMtbHours(pawn, partner);
                if (mtbHours > 0f)
                {
                    float max = Mathf.Max(1f, settings.AutonomousFeedLovinDriveMaxEffect);
                    drive = Mathf.Clamp(BaseLovinMtbHours / mtbHours, 1f / max, max);
                }
            }
            comp.NoteDrive(pawn, now, drive);
            return drive;
        }

        /// <summary>The hunger gate widened by drive, clamped to the Mana bar.</summary>
        private static float ScaledThreshold(float baseThreshold, float drive)
        {
            return Mathf.Clamp01(baseThreshold * Mathf.Clamp(drive, 1f / MaxThresholdDriveScale, MaxThresholdDriveScale));
        }

        /// <summary>The per-day cap scaled by drive (0 stays uncapped; a scaled cap never rounds down to 0).</summary>
        private static int ScaledDailyCap(int baseCap, float drive)
        {
            return baseCap <= 0 ? baseCap : Mathf.Max(1, Mathf.RoundToInt(baseCap * drive));
        }
    }
}
