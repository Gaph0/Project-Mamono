using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Drives a Momo in the essence-starvation berserk state toward the nearest
    /// unbonded humanlike and attacks them — a hunt that works across the whole map,
    /// unlike vanilla berserk which only notices pawns within 40 tiles and then
    /// wanders in place. When no unbonded prey exists anywhere on the map, she
    /// flees for the map edge instead.
    /// </summary>
    public class JobGiver_EssenceBerserk : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.MentalState is MentalState_EssenceBerserk state))
            {
                // Not an essence berserk (vanilla berserk etc.): yield so the vanilla
                // JobGiver_Berserk / wander nodes below handle it.
                return null;
            }

            // Already fleeing: keep heading for the map edge.
            if (state.fleeing)
            {
                return MakeFleeJob(pawn);
            }

            Pawn prey = state.currentPrey;
            if (!IsValidPrey(pawn, prey))
            {
                prey = LowManaBreak.FindNearbyUnbondedPawn(pawn);
                state.currentPrey = prey;
            }

            if (prey != null && pawn.CanReach(prey, PathEndMode.Touch, Danger.Deadly))
            {
                return MakeChaseJob(prey);
            }

            // No unbonded humanlike is reachable right now. Flee the map once there
            // is genuinely nothing on it; otherwise wander so the Momo keeps moving
            // instead of standing idle. Either way we always return a job here, so no
            // lower-priority think node ever takes over an essence-berserk Momo.
            if (prey == null)
            {
                state.fleeing = true;
                return MakeFleeJob(pawn);
            }
            return MakeWanderJob(pawn);
        }

        /// <summary>Live, spawned, on the same map, visible, not downed, and bondable —
        /// the berserk Momo only hunts pawns she could actually take as a husband, so
        /// women, children and fellow Momos are never prey. IsBondable also excludes
        /// already-bonded (tsugai) pawns.</summary>
        public static bool IsValidPrey(Pawn momo, Pawn prey)
        {
            if (prey == null || prey == momo || prey.Dead || !prey.Spawned || prey.Downed || prey.Map != momo.Map)
            {
                return false;
            }
            if (prey.RaceProps == null || !prey.RaceProps.Humanlike)
            {
                return false;
            }
            if (InvisibilityUtility.IsPsychologicallyInvisible(prey))
            {
                return false;
            }
            if (!TsugaiFormation.IsBondable(prey))
            {
                return false;
            }
            return true;
        }

        private static Job MakeChaseJob(Pawn prey)
        {
            // Whole-map chase via the dedicated driver (GotoThing PathEndMode.Touch),
            // not vanilla JobDriver_AttackMelee — see JobDriver_EssenceBerserkAttack.
            Job job = JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_EssenceBerserkAttack, prey);
            job.canBashDoors = true;
            return job;
        }

        /// <summary>Wander while an unreachable prey exists somewhere on the map — the
        /// Momo keeps prowling until the target becomes reachable, instead of idling.
        /// Used only when a prey exists but cannot currently be reached; if there is
        /// no prey at all she flees instead (see TryGiveJob).</summary>
        private static Job MakeWanderJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(JobDefOf.GotoWander, pawn.Position);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            job.expiryInterval = 600;
            return job;
        }

        /// <summary>Walk to the nearest map edge and leave the map. Colonists also
        /// leave the player faction (vanilla "leave colony" break); NPCs just flee.</summary>
        private static Job MakeFleeJob(Pawn pawn)
        {
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                Job leave = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ProjectMomo_LeaveColony"));
                leave.locomotionUrgency = LocomotionUrgency.Sprint;
                return leave;
            }

            IntVec3 exitCell = pawn.Position;
            if (pawn.Map != null)
            {
                if (!RCellFinder.TryFindBestExitSpot(pawn, out exitCell, TraverseMode.ByPawn))
                {
                    // Fallback: project the pawn's position onto the nearest map edge.
                    Rot4 edge = CellRect.WholeMap(pawn.Map).GetClosestEdge(pawn.Position);
                    exitCell = pawn.Position;
                    if (edge == Rot4.North) exitCell.z = pawn.Map.Size.z - 2;
                    else if (edge == Rot4.South) exitCell.z = 1;
                    else if (edge == Rot4.East) exitCell.x = pawn.Map.Size.x - 2;
                    else exitCell.x = 1;
                    if (!exitCell.Standable(pawn.Map))
                    {
                        exitCell = CellFinder.RandomClosewalkCellNear(exitCell, pawn.Map, 10);
                    }
                }
            }
            Job job = JobMaker.MakeJob(JobDefOf.Flee, exitCell);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            // Required for JobDriver_Flee to actually call ExitMap: its tick only
            // exits when job.exitMapOnArrival is set AND the pawn stands on an exit
            // cell. Without this flag she walks to the edge and idles there forever.
            job.exitMapOnArrival = true;
            return job;
        }
    }
}
