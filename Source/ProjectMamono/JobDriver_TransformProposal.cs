using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// The voluntary transformation ceremony: the Mamono walks up to the willing
    /// woman and performs a short (~10 second) infusion, then the woman awakens
    /// as the Mamono's xenotype. Unlike the forced path, the woman stands and takes
    /// part - she is given a Wait job for the duration, and the ceremony quietly
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
        // ~10 seconds of real time - consent is quicker than conquest.
        private const int CeremonyDurationTicks = 600;

        // The partner must stay roughly adjacent; walking off cancels the ceremony.
        private const float StayWithinCells = 2.5f;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned
                || !MamonoTransformation.CanEverTransform(Target));

            // Walk to the willing woman and stand next to her.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Perform the infusion ceremony (~10s), reusing the pink progress bar.
            // The finish action completes the transformation.
            Toil infuse = Toils_General.WaitWith(TargetIndex.A, CeremonyDurationTicks, false, false, false, TargetIndex.A, PathEndMode.Touch);
            // EducationCompat: a woman who goes mute during the walk can no longer
            // consent, so the ceremony gives up rather than completing.
            infuse.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned
                || !Target.Position.InHorDistOf(pawn.Position, StayWithinCells)
                || !MamonoTransformation.CanEverTransform(Target)
                || EducationCompat.IsMute(Target));

            int startTick = -1;
            MoteProgressBar bar = null;
            infuse.AddPreInitAction(() =>
            {
                startTick = Find.TickManager.TicksGame;

                // Face-to-face now: record the accepted proposal in both pawns'
                // character logs (rejected proposals never get this far).
                VoluntaryTransformation.LogProposal(pawn, Target, accepted: true);

                // The willing woman stops and waits for the ceremony to finish - and is
                // freed by the finish action below however this ends.
                HoldPartner(Target, CeremonyDurationTicks);
            });
            infuse.AddPreTickAction(() => TickBondProgress(pawn, ref bar, startTick, CeremonyDurationTicks));
            infuse.AddFinishAction(() =>
            {
                if (Target != null && !Target.Dead && !Target.Downed && Target.Spawned
                    && Target.Position.InHorDistOf(pawn.Position, StayWithinCells))
                {
                    // The Mamono is the actor; her xenotype is what the woman becomes.
                    MamonoTransformation.ApplyXenotype(Target, MamonoTransformation.XenotypeFor(pawn), pawn);
                }
            });
            // Registered after the infusion action so the bar vanishes the moment
            // the ceremony completes (or ends for any reason).
            infuse.AddFinishAction(() => DestroyBondBar(ref bar));
            infuse.AddFinishAction(() => ReleasePartner(Target));

            yield return infuse;
        }
    }
}
