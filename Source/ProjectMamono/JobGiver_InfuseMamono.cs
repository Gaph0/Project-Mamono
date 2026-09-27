using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Lets a Mamono autonomously pour mana into a downed, corruptible woman on the
    /// map - the forced corruption path, driven by the think tree rather than the
    /// player's right-click. A Mamono with mana to spare who finds a helpless woman
    /// will begin infusing her. Inserted into the Humanlike think tree by
    /// ThinkTreeInjection, below mental states and emergencies.
    /// </summary>
    public class JobGiver_InfuseMamono : ThinkNode_JobGiver
    {
        // The think-tree giver scans the map at most this often per pawn.
        private const int ScanIntervalTicks = 500;

        protected override Job TryGiveJob(Pawn pawn)
        {
            var settings = ProjectMamonoModSettings.Settings;
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

            return JobMaker.MakeJob(ProjectMamono_DefOf.ProjectMamono_InfuseMamono, target);
        }

        /// <summary>
        /// Cheap gates before any scan runs: a calm, upright, mana-fed raider
        /// Mamono. Only Mamonos of a faction hostile to the player infuse on their
        /// own - colonists never do (the player orders infusions by hand), and
        /// wild (factionless) or visiting (non-hostile guest) Mamonos stay feral.
        /// </summary>
        private static bool CanAct(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            // Only hostile raiders act on their own. Colonists, wild Mamonos (no
            // faction) and visiting Mamonos never autonomously infuse.
            Faction faction = pawn.Faction;
            if (faction == null || !faction.HostileTo(Faction.OfPlayer))
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
            if (!EssenceTransfer.IsMamono(pawn))
            {
                return false;
            }
            // Only a Mamono with mana to spare gives it away.
            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana == null || mana.CurLevel < ProjectMamonoModSettings.Settings.CorruptionManaCost)
            {
                return false;
            }
            return true;
        }

        /// <summary>The nearest downed woman she can reach and infuse.</summary>
        private static Pawn FindTarget(Pawn mamono)
        {
            Pawn best = null;
            float bestDist = float.MaxValue;

            var pawns = mamono.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!CorruptionFloatMenuPatch.CanInfuse(mamono, candidate, out _))
                {
                    continue;
                }
                // A raider Mamono only preys on her enemies (e.g. your colonists);
                // your own colony Mamonos answer to you and take any valid target.
                if (!mamono.IsColonist && !mamono.HostileTo(candidate))
                {
                    continue;
                }
                if (!mamono.CanReach(candidate, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    continue;
                }

                float dist = mamono.Position.DistanceToSquared(candidate.Position);
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
