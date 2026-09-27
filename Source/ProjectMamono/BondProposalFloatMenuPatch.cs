using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Adds the "Propose bond" right-click order for voluntary tsugai bonding:
    /// with one of your Mamonos selected, right-click a man she could bond - or
    /// with one of your men selected, right-click a Mamono. The option greys out
    /// with a reason when the pair can't bond (already bonded, not enough
    /// essence, no interest, recent rejection). Acceptance is still rolled from
    /// the target's desire - the player orders the proposal, not the answer.
    /// Mirrors EssenceFloatMenuPatch's hook and decoration style.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class BondProposalFloatMenuPatch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn __instance, Pawn selPawn)
        {
            foreach (FloatMenuOption opt in __result)
            {
                yield return opt;
            }

            Pawn target = __instance;
            if (selPawn == null || target == null || selPawn == target)
            {
                yield break;
            }
            if (!selPawn.Spawned || !target.Spawned || selPawn.Map != target.Map)
            {
                yield break;
            }
            if (!ProjectMamonoModSettings.Settings.VoluntaryBondingEnabled)
            {
                yield break;
            }

            bool selIsMamono = EssenceTransfer.IsMamono(selPawn);
            bool targetIsMamono = EssenceTransfer.IsMamono(target);
            if (selIsMamono == targetIsMamono)
            {
                yield break; // both Mamonos or both humans - no bond between them
            }

            FloatMenuOption proposal = BuildOption(selPawn, target);
            if (proposal != null)
            {
                yield return proposal;
            }
        }

        private static FloatMenuOption BuildOption(Pawn selPawn, Pawn target)
        {
            if (!selPawn.Drafted && !selPawn.IsColonistPlayerControlled)
            {
                return null; // only let the player order their own pawns
            }

            // The selected pawn must be a plausible initiator for its side.
            Pawn man = EssenceTransfer.IsMamono(selPawn) ? target : selPawn;
            if (!TsugaiFormation.IsBondable(man))
            {
                return null;
            }

            const string label = "Propose tsugai bond";
            if (!VoluntaryBonding.CanProposeTo(selPawn, target, out string reason))
            {
                // Offer it greyed-out so the player understands why it's unavailable.
                return new FloatMenuOption(reason != null ? $"{label} ({reason})" : $"{label} (unavailable)", null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, () =>
                {
                    // Re-validates and rolls acceptance inside; on success the
                    // proposal job is ordered onto the selected pawn.
                    VoluntaryBonding.TryPlayerOrderedProposal(selPawn, target);
                }),
                selPawn,
                target);
        }
    }
}
