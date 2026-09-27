using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Inserts the voluntary-behavior JobGivers (mana feeding, tsugai bond
    /// proposals, transformation offers, corruption infusions) into the
    /// Humanlike think tree at runtime, from ProjectMamonoMod's static constructor.
    ///
    /// Why C# instead of an XML PatchOperation: these givers must sit ABOVE
    /// the LordDuty subtree - a visiting Mamono in a visitor lord otherwise always
    /// has a lord job and could never act on her hunger or desire. Inserting
    /// directly into thinkRoot.subNodes avoids every PatchOperation ordering
    /// ambiguity (and any interference from other mods' think-tree patches), and
    /// runs after the def database has fully resolved.
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
                Log.Warning("[Project Mamono] Could not find the Humanlike think tree - voluntary bond proposals disabled.");
                return;
            }

            var root = humanlike.thinkRoot;
            bool haveProposal = false;
            bool haveTransform = false;
            bool haveInfuse = false;
            bool haveFeeding = false;
            for (int i = 0; i < root.subNodes.Count; i++)
            {
                ThinkNode node = root.subNodes[i];
                if (node is JobGiver_ProposeTsugaiBond) haveProposal = true;
                else if (node is JobGiver_TransformProposal) haveTransform = true;
                else if (node is JobGiver_InfuseMamono) haveInfuse = true;
                else if (node is JobGiver_SeekManaFeeding) haveFeeding = true;
            }
            if (haveProposal && haveTransform && haveInfuse && haveFeeding)
            {
                injected = true;
                return;
            }

            // Position 1: directly after the MentalStateCritical subtree (index 0)
            // and ahead of LordDuty, so a willing Mamono visitor can break off to
            // feed or propose. Mental states and emergencies still preempt feeding,
            // proposals and infusions. (The Humanlike root is a ThinkNode_Priority,
            // which walks subNodes in list order, so list position alone sets the
            // priority: the last giver inserted at the index lands highest.)
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
                var infuse = new JobGiver_InfuseMamono();
                root.subNodes.Insert(index, infuse);
                infuse.ResolveReferences();
            }
            if (!haveFeeding)
            {
                // Feeding goes in last so it lands on top of the stack: hunger is
                // seen to before courtship or corruption, just as vanilla's GetFood
                // outranks the social job givers.
                var feeding = new JobGiver_SeekManaFeeding();
                root.subNodes.Insert(index, feeding);
                feeding.ResolveReferences();
            }

            injected = true;
        }
    }
}
