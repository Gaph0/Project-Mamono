using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// The ~30-second action a Momo performs on the man she has knocked out to form
    /// the tsugai bond. The bond itself only completes when the action finishes —
    /// interrupting it (or the target dying/being rescued) prevents the bond.
    /// Shows a pink, vanilla-sized progress bar under the Momo for the duration:
    /// WaitWith's built-in bar is anchored to the target and only renders for
    /// player-faction actors, so we drive a MoteBondProgressBar ourselves.
    /// </summary>
    public class JobDriver_FormTsugai : JobDriver
    {
        // ~30 seconds of real time.
        private const int DurationTicks = 1800;

        // Vanilla progress bars float just above the pawn's center.
        private const float BarOffsetZ = -0.5f;

        // Protected so the voluntary proposal driver (JobDriver_ProposeTsugaiBond)
        // can reuse the same plumbing for its own shorter ceremony.
        protected Pawn Target => job?.targetA.Thing as Pawn;

        /// <summary>True while the bonding action is actively being performed (drives the Yayo bonding animation).</summary>
        public bool BondInProgress { get; protected set; }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Target == null || Target.Dead || Target.Destroyed);

            // Walk to the downed man and stand over him (as close as pathing allows,
            // the same convention vanilla uses to carry a downed pawn).
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // Perform the bonding action (~30s). No built-in progress bar — we add
            // our own under the Momo below. The finish action forms the bond.
            Toil bond = Toils_General.WaitWith(TargetIndex.A, DurationTicks, false, false, false, TargetIndex.A, PathEndMode.ClosestTouch);
            bond.FailOn(() => Target == null || Target.Dead || Target.Destroyed);

            int startTick = -1;
            MoteProgressBar bar = null;
            bond.AddPreInitAction(() =>
            {
                startTick = Find.TickManager.TicksGame;
                BondInProgress = true;
            });
            bond.AddPreTickAction(() => TickBondProgress(pawn, ref bar, startTick, DurationTicks));
            bond.AddPreTickAction(PingPartnerAnimation);
            bond.AddFinishAction(() =>
            {
                BondInProgress = false;
                if (Target != null && !Target.Dead && !Target.Destroyed)
                {
                    TsugaiFormation.TryBond(pawn, Target);
                }
            });
            // Registered after the bond action so the bar vanishes the moment the
            // action completes (or ends for any reason), rather than lingering until
            // the mote's solid time runs out.
            bond.AddFinishAction(() => DestroyBondBar(ref bar));

            yield return bond;
        }

        /// <summary>Removes the progress bar as soon as the bonding action ends.</summary>
        protected static void DestroyBondBar(ref MoteProgressBar bar)
        {
            if (bar != null && !bar.Destroyed)
            {
                bar.Destroy();
            }

            bar = null;
        }

        /// <summary>
        /// Makes the downed partner bounce in step with the Momo, but only when
        /// Yayo's Animation is loaded. Guarded by YayoAniCompat.Active so the Yayo
        /// types in YayoAniShim are never resolved when Yayo is absent. Wrapped in a
        /// try/catch: an exception here would otherwise propagate into DriverTick's
        /// catch-and-end-job handler and silently abort the bond before it completes.
        /// </summary>
        private void PingPartnerAnimation()
        {
            try
            {
                if (!YayoAniCompat.Active)
                {
                    return;
                }

                Pawn partner = Target;
                if (partner == null || !partner.Spawned || !partner.Downed || !partner.RaceProps.Humanlike)
                {
                    return;
                }

                YayoAniShim.PingPartner(partner, pawn);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Project Momo] Bonding partner animation failed (continuing bond): " + e.Message);
            }
        }

        /// <summary>
        /// Keeps the pink, vanilla-sized progress bar floating over the Momo in step
        /// with the bonding action's elapsed time. Uses ProjectMomo_MoteBondProgressBar
        /// (pink fill, exact vanilla 0.68×0.12 dimensions) at the vanilla offset;
        /// alwaysShow keeps it visible at any zoom. Spawns lazily, then Maintains
        /// (keeps alive) and updates it each tick. The duration is a parameter so
        /// the voluntary ceremony (shorter) reuses the same bar.
        /// </summary>
        protected static void TickBondProgress(Pawn momo, ref MoteProgressBar bar, int startTick, int durationTicks)
        {
            if (momo == null || !momo.Spawned || momo.Map == null)
            {
                return;
            }

            if (bar != null && bar.Destroyed)
            {
                bar = null;
            }

            if (bar == null)
            {
                ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("ProjectMomo_MoteBondProgressBar");
                if (moteDef == null)
                {
                    return;
                }

                MoteBondProgressBar pink = MoteMaker.MakeAttachedOverlay(momo, moteDef, new Vector3(0f, 0f, BarOffsetZ)) as MoteBondProgressBar;
                if (pink == null)
                {
                    return;
                }

                // Exact vanilla bar dimensions; MakeAttachedOverlay defaults scale to 1.
                pink.SetVanillaBarSize();

                bar = pink;
                bar.offsetZ = BarOffsetZ;
                bar.alwaysShow = true;
            }

            int elapsed = Find.TickManager.TicksGame - startTick;
            bar.progress = Mathf.Clamp01((float)elapsed / durationTicks);
            bar.Maintain();
        }
    }
}
