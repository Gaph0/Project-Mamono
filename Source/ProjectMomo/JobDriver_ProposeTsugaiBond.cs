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

        // How long a stalled walk is tolerated before the proposal gives up.
        private const int WalkGiveUpTicks = 2500;

        // The partner must stay roughly adjacent; walking off cancels the ceremony.
        private const float StayWithinCells = 2.5f;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Downed || !Target.Spawned);

            // Walk to the willing partner and stand next to them. Vanilla's goto toil only
            // completes on arrival, and a pawn with no path simply stands where he is, so give
            // up after a while rather than standing there for the rest of the game. The order
            // itself is refused up front when the pair cannot be reached (CanProposeTo), but
            // the partner can walk somewhere unreachable while the proposal is under way.
            int walkStart = -1;
            Toil walk = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            walk.AddPreInitAction(() => walkStart = Find.TickManager.TicksGame);
            walk.FailOn(() => walkStart > 0
                && (pawn.pather == null || !pawn.pather.Moving)
                && Find.TickManager.TicksGame - walkStart > WalkGiveUpTicks);
            yield return walk;

            // Face to face at last: the answer is rolled HERE, beside the target, so a
            // rejection's effects are applied next to her and never remotely. See
            // AcceptsProposal for why the verdict is not carried on the job.
            Toil resolve = new Toil();
            resolve.initAction = () =>
            {
                if (!VoluntaryBonding.AcceptsProposal(pawn, Target))
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

                // The willing partner stops and waits for the ceremony to finish — and is
                // freed by the finish action below however this ends.
                HoldPartner(Target, ProposeDurationTicks);
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
            bond.AddFinishAction(() => ReleasePartner(Target));

            yield return bond;
        }
    }
}
