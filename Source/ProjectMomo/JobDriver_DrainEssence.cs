using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// A Momo (the actor) walks to a human and drains some of their essence,
    /// restoring her mana. The actor must be a Momo with a Mana need; the target
    /// must be a non-Momo humanlike with an Essence need.
    /// </summary>
    public class JobDriver_DrainEssence : JobDriver
    {
        private const int DurationTicks = 900; // ~15 seconds

        private Pawn Human => job?.targetA.Thing as Pawn;

        /// <summary>True while the drain action is actively being performed (drives the Yayo lovin' animation).</summary>
        public bool FeedInProgress { get; private set; }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !EssenceTransfer.CanTransfer(pawn, Human));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            Toil drain = Toils_General.WaitWith(TargetIndex.A, DurationTicks, false, true, false, TargetIndex.A, PathEndMode.ClosestTouch);
            drain.FailOn(() => !EssenceTransfer.CanTransfer(pawn, Human));
            drain.WithProgressBarToilDelay(TargetIndex.A);
            drain.AddPreInitAction(() => FeedInProgress = true);
            drain.AddPreTickAction(PingPartnerAnimation);
            drain.AddFinishAction(() =>
            {
                FeedInProgress = false;
                if (EssenceTransfer.CanTransfer(pawn, Human))
                {
                    EssenceTransfer.Transfer(pawn, Human, float.MaxValue);
                    // Only a completed feed counts toward the daily cap, and only
                    // when she sought him out calmly — break-driven feeds (the
                    // desperation fallback) are exempt, so the cap can never lock
                    // a starving Momo out of the berserk/feeding break path.
                    if (!pawn.InMentalState)
                    {
                        ManaFeedingComponent.Get()?.NoteFeed(pawn, Find.TickManager.TicksGame);
                    }
                }
            });

            yield return drain;
        }

        /// <summary>
        /// Makes the partner bounce in step with the Momo, only when Yayo's Animation
        /// is loaded. Guarded like the bonding driver so Yayo types are never resolved
        /// when absent, and so an exception can never abort the feed.
        /// </summary>
        private void PingPartnerAnimation()
        {
            try
            {
                if (!YayoAniCompat.Active)
                {
                    return;
                }

                Pawn partner = Human;
                if (partner == null || !partner.Spawned || !partner.RaceProps.Humanlike)
                {
                    return;
                }

                YayoAniShim.PingPartner(partner, pawn);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Project Momo] Drain partner animation failed (continuing feed): " + e.Message);
            }
        }
    }
}
