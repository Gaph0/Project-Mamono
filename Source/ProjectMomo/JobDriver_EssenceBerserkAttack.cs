using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// The essence-berserk chase: the Momo paths straight to her hunted prey and
    /// melee-attacks it. This replaces using vanilla JobDriver_AttackMelee, whose
    /// FollowAndMeleeAttack toil paths toward the target's (nonexistent) interaction
    /// cell and gives up re-pathing except on a slow hash interval — so a berserk
    /// Momo would only ever engage prey that wandered within a few dozen tiles.
    /// GotoThing with PathEndMode.Touch walks her across the whole map like the
    /// essence-feeding jobs do.
    /// </summary>
    public class JobDriver_EssenceBerserkAttack : JobDriver
    {
        private Pawn Prey => job?.targetA.Thing as Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Stop if the prey is gone, dead, downed, or no longer valid prey.
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !JobGiver_EssenceBerserk.IsValidPrey(pawn, Prey));

            // Walk all the way to the prey (whole-map chase), re-pathing if it moves.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOn(() => !JobGiver_EssenceBerserk.IsValidPrey(pawn, Prey));

            // Melee the prey until it goes down (or the mental state ends). The
            // (targetInd, hitAction) overload uses stand position None, so it paths
            // with PathEndMode.Touch and engages the adjacent pawn directly. The
            // hit action is invoked once the pawn is in melee range — it must NOT be
            // null (the toil calls hitAction.Invoke() unconditionally), so mirror the
            // vanilla JobDriver_AttackMelee strike.
            yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, MeleeStrike)
                .FailOn(() => !JobGiver_EssenceBerserk.IsValidPrey(pawn, Prey));
        }

        /// <summary>The melee strike the follow toil performs in range — same call
        /// vanilla JobDriver_AttackMelee makes for its hitAction.</summary>
        private void MeleeStrike()
        {
            Thing target = job?.GetTarget(TargetIndex.A).Thing;
            if (target != null)
            {
                pawn.meleeVerbs.TryMeleeAttack(target, job.verbToUse);
            }
        }
    }
}
