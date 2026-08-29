using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    [StaticConstructorOnStartup]
    public static class ProjectMomoMod
    {
        static ProjectMomoMod()
        {
            Harmony harmony = new Harmony("PMM.Presets.Momo");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Voluntary tsugai bonding: add the proposal giver to the Humanlike
            // think tree now that every def has resolved.
            ThinkTreeInjection.Inject();

            // Optional: ride Yayo's Animation for the tsugai bonding animation.
            // Guarded so the Yayo types in YayoAniCompat are never touched when
            // the mod is absent (soft dependency).
            bool yayoActive = YayoAniCompat.Active;
            Log.Message($"[Project Momo] Startup: Yayo's Animation active = {yayoActive}.");
            if (yayoActive)
            {
                YayoAniCompat.Apply(harmony);
            }

            // Optional: Momos feed through Intimacy - Friends n' Lovers sex acts.
            // Guarded so no Intimacy types are resolved when the mod is absent.
            bool intimacyActive = IntimacyCompat.Active;
            Log.Message($"[Project Momo] Startup: Intimacy - Friends n' Lovers active = {intimacyActive}.");
            if (intimacyActive)
            {
                IntimacyCompat.Apply(harmony);
            }

            // Optional: Momos are immune to fertility age decline. Guarded so the
            // Biotech stat part type is only resolved when the Fertility stat exists.
            bool fertilityActive = MomoFertilityPatch.Active;
            Log.Message($"[Project Momo] Startup: Biotech fertility active = {fertilityActive}.");
            if (fertilityActive)
            {
                MomoFertilityPatch.Apply(harmony);
            }

            // Optional: Momo xenotypes with psycast data spawn as Vanilla Psycasts
            // Expanded psycasters. Guarded so no VPE/VEF types are resolved when
            // the mod is absent (soft dependency).
            bool vpeActive = VPECompat.Active;
            Log.Message($"[Project Momo] Startup: Vanilla Psycasts Expanded active = {vpeActive}.");
            if (vpeActive)
            {
                VPECompat.Apply(harmony);
            }
        }
    }

    /// <summary>
    /// Disables family-relation generation for pawns carrying the Momo gene.
    /// The gene forces all carriers female, but RimWorld's relation workers
    /// require male parents, which causes a NullReferenceException during
    /// pawn group (settlement) generation. Skipping relations avoids that.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    public static class Patch_GeneratePawnRelations
    {
        private static readonly GeneDef MomoGene = DefDatabase<GeneDef>.GetNamedSilentFail("ProjectMomo_Momo");

        public static bool Prefix(Pawn pawn)
        {
            if (pawn == null || MomoGene == null || pawn.genes == null)
            {
                return true;
            }

            if (pawn.genes.HasActiveGene(MomoGene))
            {
                return false;
            }

            return true;
        }
    }
}
