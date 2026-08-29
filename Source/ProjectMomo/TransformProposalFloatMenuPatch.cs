using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Adds the "Offer transformation" right-click order for voluntary
    /// transformation: with one of your Momos selected, right-click a woman she
    /// could remake. The option greys out with a reason when the offer is
    /// unavailable (not female, too young, already a monster, no interest,
    /// recent refusal). Acceptance is still rolled from the woman's desire —
    /// the player orders the offer, not the answer. This is the consensual
    /// path; the forced path (infusing a downed woman) is CorruptionFloatMenuPatch.
    /// Mirrors BondProposalFloatMenuPatch's hook and decoration style.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class TransformProposalFloatMenuPatch
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
            if (!ProjectMomoModSettings.Settings.CorruptionEnabled
                || !ProjectMomoModSettings.Settings.VoluntaryCorruptionEnabled)
            {
                yield break;
            }

            // Only a Momo offers, and never to another monster.
            if (!EssenceTransfer.IsMomo(selPawn) || EssenceTransfer.IsMomo(target))
            {
                yield break;
            }

            // An upright woman can only be offered the change; a downed one is
            // handled by the forced infusion path instead.
            if (target.Downed)
            {
                yield break;
            }

            FloatMenuOption proposal = BuildOption(selPawn, target);
            if (proposal != null)
            {
                yield return proposal;
            }
        }

        private static FloatMenuOption BuildOption(Pawn momo, Pawn target)
        {
            if (!momo.Drafted && !momo.IsColonistPlayerControlled)
            {
                return null; // only let the player order their own pawns
            }

            const string label = "Offer transformation";
            if (!VoluntaryTransformation.CanProposeTo(momo, target, out string reason))
            {
                // Offer it greyed-out so the player understands why it's unavailable.
                return new FloatMenuOption(reason != null ? $"{label} ({reason})" : $"{label} (unavailable)", null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, () =>
                {
                    // Re-validates and rolls acceptance inside; on success the
                    // ceremony job is ordered onto the selected Momo.
                    VoluntaryTransformation.TryPlayerOrderedProposal(momo, target);
                }),
                momo,
                target);
        }
    }
}
