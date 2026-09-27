using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// A Mamono (the actor) walks to a human and drains some of their essence,
    /// restoring her mana. The actor must be a Mamono with a Mana need; the target
    /// must be a non-Mamono humanlike with an Essence need.
    /// </summary>
    public class JobDriver_DrainEssence : JobDriver
    {
        private const int DurationTicks = 900; // ~15 seconds

        private Pawn Human => job?.targetA.Thing as Pawn;

        /// <summary>
        /// False for the dry variant (ProjectMamono_DrainEssenceDry), which is used whenever the
        /// meal is a captive: a fellow prisoner, or a prisoner or slave of the colony. Feeding
        /// on a captive is not an intimate act, so the lovin' memory and the follow-up lovin'
        /// job are skipped. Mana, the shared mood buff, the catharsis and the incubisation dose
        /// all still happen - see EssenceTransfer.Transfer. The def also picks the gate: a dry
        /// job answers to <see cref="EssenceTransfer.IsCaptiveFeedTarget"/> (exempt from the bond
        /// rule and the incubation mark) instead of <see cref="EssenceTransfer.CanTransfer"/>.
        /// Unknown or missing defs stay intimate, the old behaviour.
        /// </summary>
        private bool IntimateSideEffects => job?.def != ProjectMamono_DefOf.ProjectMamono_DrainEssenceDry;

        /// <summary>
        /// The gate for this job's own def, re-checked on every toil so a target that stops
        /// being valid (drained dry, hauled away, forbidding door) ends the walk instead of
        /// finishing a feed nobody allows any more.
        /// </summary>
        private bool CanFeed()
        {
            return IntimateSideEffects
                ? EssenceTransfer.CanTransfer(pawn, Human)
                : EssenceTransfer.IsCaptiveFeedTarget(pawn, Human);
        }

        /// <summary>True while the drain action is actively being performed (drives the Yayo lovin' animation).</summary>
        public bool FeedInProgress { get; private set; }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !CanFeed());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            Toil drain = Toils_General.WaitWith(TargetIndex.A, DurationTicks, false, true, false, TargetIndex.A, PathEndMode.ClosestTouch);
            drain.FailOn(() => !CanFeed());
            drain.WithProgressBarToilDelay(TargetIndex.A);
            drain.AddPreInitAction(() => FeedInProgress = true);
            drain.AddPreTickAction(PingPartnerAnimation);
            drain.AddFinishAction(() =>
            {
                FeedInProgress = false;
                if (CanFeed())
                {
                    EssenceTransfer.Transfer(pawn, Human, float.MaxValue, IntimateSideEffects);
                    // Only a completed feed counts toward the daily cap, and only
                    // when she sought him out calmly - break-driven feeds (the
                    // desperation fallback) are exempt, so the cap can never lock
                    // a starving Mamono out of the berserk/feeding break path.
                    if (!pawn.InMentalState)
                    {
                        ManaFeedingComponent.Get()?.NoteFeed(pawn, Find.TickManager.TicksGame);
                    }
                }
            });

            yield return drain;
        }

        /// <summary>
        /// Makes the partner bounce in step with the Mamono, only when Yayo's Animation
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
                Log.Warning("[Project Mamono] Drain partner animation failed (continuing feed): " + e.Message);
            }
        }
    }
}
