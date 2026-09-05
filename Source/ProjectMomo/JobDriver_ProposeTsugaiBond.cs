using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// The voluntary tsugai ceremony: the initiator (a Momo or a man) walks up to
    /// their willing partner and performs a short (~10 second) bonding action, then
    /// the bond forms. Unlike the forced path, the partner stands and takes part —
    /// they are given a Wait job for the duration, and the ceremony quietly fails
    /// if they move away, go down, or die before it completes.
    ///
    /// Subclasses JobDriver_FormTsugai so the pink bond progress bar plumbing is
    /// shared and YayoAniCompat's <c>driver is JobDriver_FormTsugai</c> check picks
    /// up the bonding animation for this driver too.
    /// </summary>
    public class JobDriver_ProposeTsugaiBond : JobDriver_FormTsugai
    {
        // ~10 seconds of real time — consent is quicker than conquest.
        private const int ProposeDurationTicks = 600;

        // The partner must stay roughly adjacent; walking off cancels the ceremony.
        private const float StayWithinCells = 2.5f;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned);

            // Walk to the willing partner and stand next to them.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Face to face at last: reveal the answer rolled when the job was created
            // (carried on job.playerForced). A rejection applies its effects here —
            // beside the target — so nobody is ever "remotely seduced" from across
            // the map, and the job ends immediately without the ceremony.
            Toil resolve = new Toil();
            resolve.initAction = () =>
            {
                if (!pawn.CurJob.playerForced)
                {
                    VoluntaryBonding.ApplyFaceToFaceRejection(pawn, Target);
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            yield return resolve;

            // Perform the bonding ceremony (~10s), reusing the pink progress bar.
            // The finish action forms the bond.
            Toil bond = Toils_General.WaitWith(TargetIndex.A, ProposeDurationTicks, false, false, false, TargetIndex.A, PathEndMode.Touch);
            bond.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned
                || !Target.Position.InHorDistOf(pawn.Position, StayWithinCells));

            int startTick = -1;
            MoteProgressBar bar = null;
            bond.AddPreInitAction(() =>
            {
                startTick = Find.TickManager.TicksGame;
                BondInProgress = true;

                // Face-to-face now: record the accepted proposal in both pawns'
                // character logs (rejected proposals never get this far).
                VoluntaryBonding.LogProposal(pawn, Target, accepted: true);

                // The willing partner stops and waits for the ceremony to finish.
                if (Target?.jobs != null && Target.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    Target.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), JobCondition.InterruptForced);
                }
            });
            bond.AddPreTickAction(() => TickBondProgress(pawn, ref bar, startTick, ProposeDurationTicks));
            bond.AddFinishAction(() =>
            {
                BondInProgress = false;
                if (Target != null && !Target.Dead && !Target.Downed && Target.Spawned
                    && Target.Position.InHorDistOf(pawn.Position, StayWithinCells))
                {
                    // Either side may have initiated — sort out who is who.
                    Pawn momo = EssenceTransfer.IsMomo(pawn) ? pawn : Target;
                    Pawn man = EssenceTransfer.IsMomo(pawn) ? Target : pawn;
                    TsugaiFormation.TryBond(momo, man, voluntary: true);
                }
            });
            // Registered after the bond action so the bar vanishes the moment the
            // ceremony completes (or ends for any reason).
            bond.AddFinishAction(() => DestroyBondBar(ref bar));

            yield return bond;
        }
    }
}
