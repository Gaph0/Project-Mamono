using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Lets a Momo autonomously pour mana into a downed, corruptible woman on the
    /// map — the forced corruption path, driven by the think tree rather than the
    /// player's right-click. A Momo with mana to spare who finds a helpless woman
    /// will begin infusing her. Inserted into the Humanlike think tree by
    /// ThinkTreeInjection, below mental states and emergencies.
    /// </summary>
    public class JobGiver_InfuseMomo : ThinkNode_JobGiver
    {
        // The think-tree giver scans the map at most this often per pawn.
        private const int ScanIntervalTicks = 500;

        protected override Job TryGiveJob(Pawn pawn)
        {
            var settings = ProjectMomoModSettings.Settings;
            if (!settings.CorruptionEnabled || !settings.AutonomousCorruptionEnabled)
            {
                return null;
            }
            if (!CanAct(pawn))
            {
                return null;
            }

            // Throttle the map scan with the same component the proposals use.
            int now = Find.TickManager.TicksGame;
            TransformProposalComponent comp = TransformProposalComponent.Get();
            if (comp != null)
            {
                if (!comp.CanScanNow(pawn, now))
                {
                    return null;
                }
                comp.NoteScan(pawn, now + ScanIntervalTicks);
            }

            Pawn target = FindTarget(pawn);
            if (target == null)
            {
                return null;
            }

            return JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_InfuseMomo, target);
        }

        /// <summary>Cheap gates before any scan runs: a calm, upright, mana-fed Momo.</summary>
        private static bool CanAct(Pawn pawn)
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
            if (!EssenceTransfer.IsMomo(pawn))
            {
                return false;
            }
            // Only a Momo with mana to spare gives it away.
            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana == null || mana.CurLevel < ProjectMomoModSettings.Settings.CorruptionManaCost)
            {
                return false;
            }
            return true;
        }

        /// <summary>The nearest downed woman she is hostile to and can reach and infuse.</summary>
        private static Pawn FindTarget(Pawn momo)
        {
            Pawn best = null;
            float bestDist = float.MaxValue;

            var pawns = momo.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!CorruptionFloatMenuPatch.CanInfuse(momo, candidate, out _))
                {
                    continue;
                }
                if (!momo.CanReach(candidate, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                float dist = momo.Position.DistanceToSquared(candidate.Position);
                if (dist < bestDist)
                {
                    best = candidate;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
