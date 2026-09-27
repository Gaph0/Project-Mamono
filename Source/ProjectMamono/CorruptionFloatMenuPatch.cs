using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Adds the "Infuse with mana" right-click order for mamono corruption: with
    /// one of your Mamonos selected, right-click a downed woman. The option greys
    /// out with a reason when the pair can't infuse (not female, too young,
    /// already a monster, not downed, not enough mana). This is the forced path -
    /// the target must be downed (from a tease knockout or any other cause); a
    /// willing, consensual path comes separately.
    /// Mirrors BondProposalFloatMenuPatch's hook and decoration style.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class CorruptionFloatMenuPatch
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
            if (!ProjectMamonoModSettings.Settings.CorruptionEnabled)
            {
                yield break;
            }

            // Only a Mamono infuses, and never another monster.
            if (!EssenceTransfer.IsMamono(selPawn) || EssenceTransfer.IsMamono(target))
            {
                yield break;
            }

            FloatMenuOption infusion = BuildOption(selPawn, target);
            if (infusion != null)
            {
                yield return infusion;
            }
        }

        private static FloatMenuOption BuildOption(Pawn mamono, Pawn target)
        {
            if (!mamono.Drafted && !mamono.IsColonistPlayerControlled)
            {
                return null; // only let the player order their own pawns
            }

            const string label = "Infuse with mana";
            if (!CanInfuse(mamono, target, out string reason))
            {
                // Offer it greyed-out so the player understands why it's unavailable.
                return new FloatMenuOption(reason != null ? $"{label} ({reason})" : $"{label} (unavailable)", null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, () =>
                {
                    // Re-validate on click: she may have stood back up (or the
                    // Mamono's mana drained) since the menu was built.
                    if (CanInfuse(mamono, target, out _))
                    {
                        MamonoTransformation.TryStartInfusionJob(mamono, target);
                    }
                }),
                mamono,
                target);
        }

        /// <summary>
        /// All requirements for one infusion: a corruptible woman (female,
        /// humanlike, of age, not already a monster) who is currently downed, and
        /// a Mamono with enough mana to pay for the dose.
        /// </summary>
        public static bool CanInfuse(Pawn mamono, Pawn target, out string reason)
        {
            if (!MamonoTransformation.CanEverTransform(target, out reason))
            {
                return false;
            }

            // The forced path only: she must be downed - from a tease knockout or
            // any other cause. An upright woman resists the infusion.
            if (!target.Downed)
            {
                reason = "not downed";
                return false;
            }

            Need_Mana mana = EssenceTransfer.Mana(mamono);
            if (mana == null || mana.CurLevel < ProjectMamonoModSettings.Settings.CorruptionManaCost)
            {
                reason = "not enough mana";
                return false;
            }

            return true;
        }
    }
}
