using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// A human (the actor) walks to a Mamono and gives her some of their essence,
    /// restoring her mana. The actor must be a non-Mamono humanlike with an Essence
    /// need; the target must be a Mamono with a Mana need.
    /// </summary>
    public class JobDriver_GiveEssence : JobDriver
    {
        private const int DurationTicks = 900; // ~15 seconds

        private Pawn Mamono => job?.targetA.Thing as Pawn;

        /// <summary>True while the give action is actively being performed (drives the Yayo lovin' animation).</summary>
        public bool FeedInProgress { get; private set; }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !EssenceTransfer.CanTransfer(Mamono, pawn));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            Toil give = Toils_General.WaitWith(TargetIndex.A, DurationTicks, false, true, false, TargetIndex.A, PathEndMode.ClosestTouch);
            give.FailOn(() => !EssenceTransfer.CanTransfer(Mamono, pawn));
            give.WithProgressBarToilDelay(TargetIndex.A);
            give.AddPreInitAction(() => FeedInProgress = true);
            give.AddPreTickAction(PingPartnerAnimation);
            give.AddFinishAction(() =>
            {
                FeedInProgress = false;
                if (EssenceTransfer.CanTransfer(Mamono, pawn))
                {
                    EssenceTransfer.Transfer(Mamono, pawn, float.MaxValue);
                }
            });

            yield return give;
        }

        /// <summary>
        /// Makes the partner bounce in step with the giver, only when Yayo's Animation
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

                Pawn partner = Mamono;
                if (partner == null || !partner.Spawned || !partner.RaceProps.Humanlike)
                {
                    return;
                }

                YayoAniShim.PingPartner(partner, pawn);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Project Mamono] Give partner animation failed (continuing feed): " + e.Message);
            }
        }
    }
}
