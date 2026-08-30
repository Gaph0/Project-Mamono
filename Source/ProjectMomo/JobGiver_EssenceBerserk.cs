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

            // She found her mate: a living tsugai bond exists (her drained victim may
            // already have been carried off to a bed). The hunt's purpose is fulfilled
            // — snap out of the berserk state instead of hunting new prey or fleeing
            // the colony over having "no viable bonding targets". Runs before the
            // fleeing check so a Momo who bonded while the flee flag was set still
            // recovers. MentalStateTick does the same one tick later; doing it here
            // too closes the same-tick race where the flee job would otherwise start.
            if (TsugaiFormation.HasBondedPartner(pawn))
            {
                state.RecoverFromState();
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
                // Downed and still unbonded (e.g. a rescue hauled him off mid-ceremony
                // and he was set down again): go finish the bonding instead of
                // attacking — the chase job refuses downed prey (IsValidPrey), so
                // chasing him would only churn instantly-failing jobs.
                if (prey.Downed)
                {
                    if (TsugaiFormation.CanBond(pawn, prey))
                    {
                        return JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_FormTsugai, prey);
                    }
                    return MakeWanderJob(pawn);
                }
                return MakeChaseJob(prey);
            }

            // No unbonded humanlike is reachable right now. Flee the map once there
            // is genuinely nothing on it; otherwise wander so the Momo keeps moving
            // instead of standing idle. Either way we always return a job here, so no
            // lower-priority think node ever takes over an essence-berserk Momo.
            if (prey == null)
            {
                // A bondable pawn being carried somewhere on the map (her interrupted
                // victim hauled toward a bed) is only temporarily unavailable —
                // prowl until he is set down again instead of abandoning the colony.
                if (AnyBondableBeingCarried(pawn))
                {
                    return MakeWanderJob(pawn);
                }
                state.fleeing = true;
                return MakeFleeJob(pawn);
            }
            return MakeWanderJob(pawn);
        }

        /// <summary>True while any spawned pawn on the Momo's map is carrying a
        /// bondable pawn (e.g. rescuing her interrupted victim to a bed). A carried
        /// pawn is despawned, so FindNearbyUnbondedPawn never sees him — without this
        /// check the Momo would flee the moment her victim is picked up.</summary>
        private static bool AnyBondableBeingCarried(Pawn momo)
        {
            if (momo?.Map == null)
            {
                return false;
            }
            System.Collections.Generic.IReadOnlyList<Pawn> pawns = momo.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.carryTracker?.CarriedThing is Pawn carried && TsugaiFormation.IsBondable(carried))
                {
                    return true;
                }
            }
            return false;
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
