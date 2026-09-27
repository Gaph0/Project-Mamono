using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// The ~20-second action a Mamono performs on a will-broken woman to pour mana
    /// into her, advancing her mamono corruption (Hediff_MamonoCorruption) by one
    /// dose (CorruptionSeverityPerInfusion). The dose - and the mana cost - only
    /// lands when the action completes, so interrupting the infusion wastes
    /// nothing. Each completed infusion also imprints the infuser's xenotype on
    /// the corruption: the last Mamono to infuse her decides what she becomes.
    ///
    /// Subclasses JobDriver_FormTsugai to reuse the pink progress bar plumbing.
    /// BondInProgress is deliberately never set, so the Yayo bonding animation
    /// does not play during infusions.
    /// </summary>
    public class JobDriver_InfuseMamono : JobDriver_FormTsugai
    {
        // ~20 seconds of real time per infusion.
        private const int InfuseDurationTicks = 1200;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Destroyed || !MamonoTransformation.CanEverTransform(Target));

            // Walk to the will-broken woman and stand over her (the same
            // convention the bonding job uses to carry a downed man).
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Pour mana into her (~20s). No built-in progress bar - the pink one
            // from the bonding plumbing is driven below. The finish action lands
            // the dose; the job silently fails if she stops being corruptible
            // (e.g. another Mamono finished her first).
            Toil infuse = Toils_General.WaitWith(TargetIndex.A, InfuseDurationTicks, false, false, false, TargetIndex.A, PathEndMode.ClosestTouch);
            infuse.FailOn(() => Target == null || Target.Dead || Target.Destroyed || !MamonoTransformation.CanEverTransform(Target));

            int startTick = -1;
            MoteProgressBar bar = null;
            infuse.AddPreInitAction(() => startTick = Find.TickManager.TicksGame);
            infuse.AddPreTickAction(() => TickBondProgress(pawn, ref bar, startTick, InfuseDurationTicks));
            infuse.AddFinishAction(() =>
            {
                if (Target != null && !Target.Dead && !Target.Destroyed)
                {
                    MamonoTransformation.ApplyInfusion(pawn, Target);
                }
            });
            // Registered after the infusion action so the bar vanishes the moment
            // it completes (or ends for any reason).
            infuse.AddFinishAction(() => DestroyBondBar(ref bar));

            yield return infuse;
        }
    }
}
