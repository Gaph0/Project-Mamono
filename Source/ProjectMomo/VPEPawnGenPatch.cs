using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Postfix on PawnGenerator.GeneratePawn for the optional Vanilla Psycasts
    /// Expanded integration (see VPECompat). Applied manually by VPECompat.Apply
    /// and only while VPE is active — no Harmony attributes here: PatchAll scans
    /// this assembly and must never route pawn generation through the psycast
    /// grant when the mod is absent.
    /// </summary>
    public static class VPEPawnGenPatch
    {
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            // VPE's own pawn-gen patch skips newborns; psycasts at birth are
            // meaningless (grown Momos gaining them is a separate concern).
            if (__result == null || request.AllowedDevelopmentalStages.Newborn())
            {
                return;
            }

            VPECompat.TryGrantSpawnPsycasts(__result);
        }
    }
}
