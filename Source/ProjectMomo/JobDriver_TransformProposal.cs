using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// The voluntary transformation ceremony: the Momo walks up to the willing
    /// woman and performs a short (~10 second) infusion, then the woman awakens
    /// as the Momo's xenotype. Unlike the forced path, the woman stands and takes
    /// part — she is given a Wait job for the duration, and the ceremony quietly
    /// fails if she moves away, goes down, or dies before it completes. A willing
    /// body does not resist, so the transformation completes in one step: no
    /// gradual corruption and no mana cost.
    ///
    /// Subclasses JobDriver_FormTsugai so the pink progress bar plumbing is
    /// shared. BondInProgress is deliberately never set, so the Yayo bonding
    /// animation does not play during the ceremony.
    /// </summary>
    public class JobDriver_TransformProposal : JobDriver_FormTsugai
    {
        // ~10 seconds of real time — consent is quicker than conquest.
        private const int CeremonyDurationTicks = 600;

        // The partner must stay roughly adjacent; walking off cancels the ceremony.
        private const float StayWithinCells = 2.5f;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned
                || !MomoTransformation.CanEverTransform(Target));

            // Walk to the willing woman and stand next to her.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Perform the infusion ceremony (~10s), reusing the pink progress bar.
            // The finish action completes the transformation.
            Toil infuse = Toils_General.WaitWith(TargetIndex.A, CeremonyDurationTicks, false, false, false, TargetIndex.A, PathEndMode.Touch);
            infuse.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned
                || !Target.Position.InHorDistOf(pawn.Position, StayWithinCells)
                || !MomoTransformation.CanEverTransform(Target));

            int startTick = -1;
            MoteProgressBar bar = null;
            infuse.AddPreInitAction(() =>
            {
                startTick = Find.TickManager.TicksGame;

                // Face-to-face now: record the accepted proposal in both pawns'
                // character logs (rejected proposals never get this far).
                VoluntaryTransformation.LogProposal(pawn, Target, accepted: true);

                // The willing woman stops and waits for the ceremony to finish.
                if (Target?.jobs != null && Target.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    Target.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), JobCondition.InterruptForced);
                }
            });
            infuse.AddPreTickAction(() => TickBondProgress(pawn, ref bar, startTick, CeremonyDurationTicks));
            infuse.AddFinishAction(() =>
            {
                if (Target != null && !Target.Dead && !Target.Downed && Target.Spawned
                    && Target.Position.InHorDistOf(pawn.Position, StayWithinCells))
                {
                    // The Momo is the actor; her xenotype is what the woman becomes.
                    MomoTransformation.ApplyXenotype(Target, MomoTransformation.XenotypeFor(pawn), pawn);
                }
            });
            // Registered after the infusion action so the bar vanishes the moment
            // the ceremony completes (or ends for any reason).
            infuse.AddFinishAction(() => DestroyBondBar(ref bar));

            yield return infuse;
        }
    }
}
