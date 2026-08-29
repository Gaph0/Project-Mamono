using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// The bonded low-mana break: the Momo seeks out her living tsugai partner and
    /// initiates essence feeding. On start she begins the Drain Essence job toward
    /// her mate — that job walks to him, transfers essence (restoring her mana), and
    /// then starts the vanilla lovin' job when a bed is available. The state recovers
    /// once the feeding is underway or her partner is gone.
    /// </summary>
    public class MentalState_ManaFeeding : MentalState
    {
        // The feed job is issued by LowManaBreak AFTER this state starts (issuing it in
        // PostStart would be cancelled by the mental-state entry stopping the current job).
        // This state just shows the letter and ends once the feed/lovin' job has run and
        // finished, the mate is gone, or the recovery time elapses.

        // Tracks whether the feed/lovin' job has actually begun (CurJob observed) —
        // StartJob doesn't assign CurJob until a later tick, so we can't end the state
        // until we've seen the job running at least once.
        private bool feedStarted;

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            Pawn partner = LowManaBreak.GetLivingBondPartner(pawn);
            if (partner == null)
            {
                // Partner died or left the map: the feeding break can no longer
                // resolve. Clear the desperate-break lock so Need_Mana can
                // re-evaluate and immediately send her into berserk/flee on the
                // next interval instead of waiting for the random MTB roll.
                (pawn.needs?.TryGetNeed(ProjectMomo_DefOf.ProjectMomo_Mana) as Need_Mana)?.ResetDesperateTriggered();
                RecoverFromState();
                return;
            }

            Job cur = pawn.CurJob;
            bool onFeedJob = cur != null && (cur.def == ProjectMomo_DefOf.ProjectMomo_DrainEssence || cur.def == JobDefOf.Lovin);
            if (onFeedJob)
            {
                // The feed job is actually running now; from here on, ending it ends the state.
                feedStarted = true;
                return;
            }

            // Recover only after the feed job has run and finished — never before it began.
            if (feedStarted)
            {
                RecoverFromState();
            }
        }
    }
}
