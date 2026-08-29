using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Lets a pawn who wants a bond — a Momo or a bondable man — autonomously
    /// propose one to their best willing partner on the map. All scoring, gating,
    /// cooldowns and the acceptance roll live in VoluntaryBonding so the same
    /// logic serves this giver and the player-ordered float menu option.
    /// Inserted into the Humanlike think tree by ThinkTreeInjection.
    /// </summary>
    public class JobGiver_ProposeTsugaiBond : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            return VoluntaryBonding.TryCreateProposalJob(pawn);
        }
    }
}
