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
    /// berserk and attacks a nearby unbonded pawn.
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
