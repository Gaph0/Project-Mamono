using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Inserts JobGiver_ProposeTsugaiBond into the Humanlike think tree at runtime,
    /// from ProjectMomoMod's static constructor.
    ///
    /// Why C# instead of an XML PatchOperation: the proposal giver must sit ABOVE
    /// the LordDuty subtree — a visiting Momo in a visitor lord otherwise always
    /// has a lord job and could never act on her desire. Inserting directly into
    /// thinkRoot.subNodes avoids every PatchOperation ordering ambiguity (and any
    /// interference from other mods' think-tree patches), and runs after the def
    /// database has fully resolved.
    /// </summary>
    public static class ThinkTreeInjection
    {
        private static bool injected;

        public static void Inject()
        {
            if (injected)
            {
                return;
            }

            ThinkTreeDef humanlike = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Humanlike");
            if (humanlike?.thinkRoot == null)
            {
                Log.Warning("[Project Momo] Could not find the Humanlike think tree — voluntary bond proposals disabled.");
                return;
            }

            var root = humanlike.thinkRoot;
            bool haveProposal = false;
            bool haveTransform = false;
            bool haveInfuse = false;
            for (int i = 0; i < root.subNodes.Count; i++)
            {
                ThinkNode node = root.subNodes[i];
                if (node is JobGiver_ProposeTsugaiBond) haveProposal = true;
                else if (node is JobGiver_TransformProposal) haveTransform = true;
                else if (node is JobGiver_InfuseMomo) haveInfuse = true;
            }
            if (haveProposal && haveTransform && haveInfuse)
            {
                injected = true;
                return;
            }

            // Position 1: directly after the MentalStateCritical subtree (index 0)
            // and ahead of LordDuty, so a willing Momo visitor can break off to
            // propose. Mental states and emergencies still preempt proposals and
            // infusions. (The Humanlike root is a ThinkNode_Priority, which walks
            // subNodes in list order, so list position alone sets the priority.)
            int index = root.subNodes.Count > 0 ? 1 : 0;
            if (!haveProposal)
            {
                var proposal = new JobGiver_ProposeTsugaiBond();
                root.subNodes.Insert(index, proposal);
                proposal.ResolveReferences();
            }
            if (!haveTransform)
            {
                var transform = new JobGiver_TransformProposal();
                root.subNodes.Insert(index, transform);
                transform.ResolveReferences();
            }
            if (!haveInfuse)
            {
                var infuse = new JobGiver_InfuseMomo();
                root.subNodes.Insert(index, infuse);
                infuse.ResolveReferences();
            }

            injected = true;
        }
    }
}
