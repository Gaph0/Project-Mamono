using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Drives the low-mana mental break. While a Momo's mana is below the threshold
    /// (default 10%), she can randomly break. If she has a living tsugai bond she
    /// seeks out that partner and initiates essence feeding; if unbonded she goes
    /// berserk and attacks a nearby unbonded pawn. Wild Momos never berserk —
    /// a starving wild Momo simply leaves the map to hunt elsewhere.
    /// </summary>
    public static class LowManaBreak
    {
        /// <summary>Called each need interval with the Momo's current mana level (0..1).</summary>
        public static void CheckBreak(Pawn pawn, float manaLevel)
        {
            var settings = ProjectMomoModSettings.Settings;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Downed || pawn.InMentalState)
            {
                return;
            }
            if (manaLevel > settings.LowManaBreakThreshold)
            {
                return;
            }
            if (!Rand.MTBEventOccurs(settings.LowManaBreakMtbDays, 60000f, 150f))
            {
                return;
            }

            Trigger(pawn);
        }

        /// <summary>
        /// Starts the appropriate break right now: bonded Momos seek their mate and feed;
        /// unbonded go berserk and attack a nearby unbonded pawn if one is around. Shared
        /// by the low-mana threshold and the mana-starvation-desperate hediff.
        /// </summary>
        public static void Trigger(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            Pawn partner = GetLivingBondPartner(pawn);
            if (partner != null)
            {
                // Bonded: enter the feeding break (letter/duration only), then start the
                // Drain Essence job. The job is issued AFTER the mental state starts —
                // TryStartMentalState stops the pawn's current job on entry, so issuing
                // it inside the state's PostStart would be cancelled.
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    ProjectMomo_DefOf.ProjectMomo_ManaFeedingState,
                    reason: "overcome by a desperate hunger for essence",
                    forced: true,
                    forceWake: true,
                    causedByMood: false,
                    otherPawn: partner);

                if (EssenceTransfer.CanTransfer(pawn, partner) && pawn.jobs != null)
                {
                    Job feed = JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_DrainEssence, partner);
                    pawn.jobs.StartJob(feed, JobCondition.InterruptForced);
                }
            }
            else if (EssenceTransfer.IsVisitingGuest(pawn))
            {
                // An unbonded visiting Momo never goes essence-berserk on the colony map:
                // the berserk hunt exists to win her a mate, and a guest taking (then
                // kidnapping) a colonist is exactly what a visit must not produce. Her
                // slowed guest mana drain keeps her from starving this far during a normal
                // visit; if she does run dry she simply stays hungry until she leaves.
            }
            else if (pawn.IsWildMan())
            {
                // A starving wild Momo never goes berserk on the colony map: she has
                // no stake in this place, so she slips away to hunt elsewhere. A plain
                // flee job (no mental state) keeps the wild-man think tree running, so
                // she still wanders off naturally even if the flee somehow fails.
                Job flee = MakeLeaveMapJob(pawn);
                if (flee != null && pawn.jobs != null)
                {
                    pawn.jobs.StartJob(flee, JobCondition.InterruptForced);
                }
            }
            else
            {
                // Unbonded: essence berserk — hunt the nearest unbonded pawn. The
                // pre-picked victim is handed to the state via otherPawn so she heads
                // straight for them instead of wandering.
                Pawn victim = FindNearbyUnbondedPawn(pawn);
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    ProjectMomo_DefOf.ProjectMomo_EssenceBerserk,
                    reason: "driven wild by essence starvation",
                    forced: true,
                    forceWake: true,
                    causedByMood: false,
                    otherPawn: victim);
            }
        }

        /// <summary>
        /// Builds the walk-to-the-map-edge job a starving wild Momo uses to slip away.
        /// exitMapOnArrival is required: JobDriver_Flee only calls ExitMap when the flag
        /// is set and she stands on an exit cell — without it she would idle at the edge.
        /// </summary>
        private static Job MakeLeaveMapJob(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            IntVec3 exitCell = pawn.Position;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out exitCell, TraverseMode.ByPawn))
            {
                // Fallback: project the pawn's position onto the nearest map edge.
                Rot4 edge = CellRect.WholeMap(pawn.Map).GetClosestEdge(pawn.Position);
                if (edge == Rot4.North) exitCell.z = pawn.Map.Size.z - 2;
                else if (edge == Rot4.South) exitCell.z = 1;
                else if (edge == Rot4.East) exitCell.x = pawn.Map.Size.x - 2;
                else exitCell.x = 1;
                if (!exitCell.Standable(pawn.Map))
                {
                    exitCell = CellFinder.RandomClosewalkCellNear(exitCell, pawn.Map, 10);
                }
            }

            Job job = JobMaker.MakeJob(JobDefOf.Flee, exitCell);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.exitMapOnArrival = true;
            return job;
        }

        /// <summary>The Momo's living, reachable tsugai partner, or null if she is unbonded or her bond is inaccessible.</summary>
        public static Pawn GetLivingBondPartner(Pawn pawn)
        {
            if (pawn?.relations == null || pawn.Map == null)
            {
                return null;
            }

            List<Pawn> partners = new List<Pawn>();
            pawn.relations.GetDirectRelations(ProjectMomo_DefOf.ProjectMomo_Tsugai, ref partners);
            for (int i = 0; i < partners.Count; i++)
            {
                Pawn partner = partners[i];
                if (partner == null || partner.Dead || !partner.Spawned || partner.Map != pawn.Map)
                {
                    continue;
                }

                // The bond is only accessible if the Momo can actually reach it.
                if (pawn.CanReach(partner, PathEndMode.Touch, Danger.Deadly))
                {
                    return partner;
                }
            }
            return null;
        }

        /// <summary>A nearby spawned, living, unbonded humanlike the berserk Momo can attack, or null.</summary>
        public static Pawn FindNearbyUnbondedPawn(Pawn momo)
        {
            if (momo?.Map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestDistSq = float.MaxValue;
            System.Collections.Generic.IReadOnlyList<Pawn> pawns = momo.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                // Downed pawns are valid berserk targets too — only the dead or
                // unspawned are skipped.
                if (p == null || p == momo || p.Dead || !p.Spawned)
                {
                    continue;
                }
                if (p.RaceProps == null || !p.RaceProps.Humanlike)
                {
                    continue;
                }
                // Skip anyone she could never bond (women, children, other Momos) —
                // berserk hunger only drives her toward bondable men.
                if (!TsugaiFormation.IsBondable(p))
                {
                    continue;
                }

                float dSq = p.Position.DistanceToSquared(momo.Position);
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    best = p;
                }
            }
            return best;
        }
    }
}
