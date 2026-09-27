using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Lets a Mamono who wants to convert a woman autonomously offer her the mana.
    /// All scoring, gating, cooldowns and the acceptance roll live in
    /// VoluntaryTransformation so the same logic serves this giver and the
    /// player-ordered float menu option. Inserted into the Humanlike think tree
    /// by ThinkTreeInjection.
    /// </summary>
    public class JobGiver_TransformProposal : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            return VoluntaryTransformation.TryCreateProposalJob(pawn);
        }
    }
}
