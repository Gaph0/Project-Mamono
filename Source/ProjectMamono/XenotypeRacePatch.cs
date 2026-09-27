using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Applying a xenotype to a LIVING pawn also gives her that species' body.
    ///
    /// Big &amp; Small reads a xenotype's declared race
    /// (BigAndSmall.XenotypeExtension.setRace) while a pawn is being GENERATED, so every
    /// path that sets a xenotype afterwards used to leave a hole: the pawn wore the
    /// species' genes on the body it already had. No four arms for an abaddon folk, no
    /// wings, no chitin, no species size - none of that lives in genes.
    ///
    /// MamonoTransformation.ApplyXenotypeRace closes that hole for our own transformation
    /// paths (mana corruption, xenogerms, a conversion). This postfix closes it for
    /// everything else, and the one people actually reach for when testing a new species
    /// is vanilla's dev "apply xenotype" action, which is
    /// DebugToolsPawns.SetXenotype -> Pawn_GeneTracker.SetXenotype and nothing more.
    ///
    /// Deliberately narrow:
    /// <list type="bullet">
    /// <item>no XenotypeExtension, or no setRace on it: nothing happens. That is every
    /// vanilla xenotype and the base Mamono.</item>
    /// <item>the pawn is already that race: nothing happens, so ordinary pawn generation
    /// (where Big &amp; Small does its own swap) is not swapped a second time.</item>
    /// <item>animals are skipped. A monster xenotype belongs on a humanlike woman;
    /// swapping a muffalo onto a humanlike race def would only wreck its body.</item>
    /// </list>
    /// </summary>
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype))]
    public static class XenotypeRacePatch
    {
        public static void Postfix(Pawn_GeneTracker __instance)
        {
            Pawn pawn = __instance?.pawn;
            if (pawn == null || pawn.Dead || pawn.def == null
                || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            BigAndSmall.XenotypeExtension extension =
                pawn.genes?.Xenotype?.GetModExtension<BigAndSmall.XenotypeExtension>();
            if (extension?.setRace == null || extension.setRace == pawn.def)
            {
                return;
            }

            MamonoTransformation.ApplyXenotypeRace(pawn);
        }
    }
}
