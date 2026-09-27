using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    [StaticConstructorOnStartup]
    public static class ProjectMamonoMod
    {
        static ProjectMamonoMod()
        {
            Harmony harmony = new Harmony("PMM.Presets.Mamono");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Voluntary tsugai bonding: add the proposal giver to the Humanlike
            // think tree now that every def has resolved.
            ThinkTreeInjection.Inject();

            // Optional: ride Yayo's Animation for the tsugai bonding animation.
            // Guarded so the Yayo types in YayoAniCompat are never touched when
            // the mod is absent (soft dependency).
            bool yayoActive = YayoAniCompat.Active;
            Log.Message($"[Project Mamono] Startup: Yayo's Animation active = {yayoActive}.");
            if (yayoActive)
            {
                YayoAniCompat.Apply(harmony);
            }

            // Optional: Mamonos feed through Intimacy - Friends n' Lovers sex acts.
            // Guarded so no Intimacy types are resolved when the mod is absent.
            bool intimacyActive = IntimacyCompat.Active;
            Log.Message($"[Project Mamono] Startup: Intimacy - Friends n' Lovers active = {intimacyActive}.");
            if (intimacyActive)
            {
                IntimacyCompat.Apply(harmony);
            }

            // Optional: Mamonos are immune to fertility age decline. Guarded so the
            // Biotech stat part type is only resolved when the Fertility stat exists.
            bool fertilityActive = MamonoFertilityPatch.Active;
            Log.Message($"[Project Mamono] Startup: Biotech fertility active = {fertilityActive}.");
            if (fertilityActive)
            {
                MamonoFertilityPatch.Apply(harmony);
            }

            // Optional: Big & Small's romance compatibility layer off (user's call 2026-09-26).
            // One system decides who can pair up, and it is ours - B&S's shared-tag compatibility
            // is skipped in favour of vanilla plus this mod's own romance patches. Guarded so no
            // B&S type is resolved when the mod is absent.
            bool bsRomanceActive = BigAndSmallRomanceDisablePatch.Active;
            Log.Message($"[Project Mamono] Startup: Big & Small romance compatibility active = {bsRomanceActive}.");
            if (bsRomanceActive)
            {
                BigAndSmallRomanceDisablePatch.Apply(harmony);
            }

            // Optional: Mamono xenotypes with psycast data spawn as Vanilla Psycasts
            // Expanded psycasters. Guarded so no VPE/VEF types are resolved when
            // the mod is absent (soft dependency).
            bool vpeActive = VPECompat.Active;
            Log.Message($"[Project Mamono] Startup: Vanilla Psycasts Expanded active = {vpeActive}.");
            if (vpeActive)
            {
                VPECompat.Apply(harmony);
            }

            // Optional: a mute pawn (Progression: Education's speech track) cannot
            // propose or accept a tsugai bond or a transformation. Nothing here
            // references that mod's assembly - the mute trait is resolved by
            // defName, so this is a no-op when the mod is absent (soft dependency).
            bool educationActive = EducationCompat.Active;
            Log.Message($"[Project Mamono] Startup: Progression: Education active = {educationActive}, mute trait found = {EducationCompat.MuteTrait != null}.");
        }
    }

    /// <summary>
    /// Disables family-relation generation for pawns carrying the Mamono gene.
    /// The gene forces all carriers female, but RimWorld's relation workers
    /// require male parents, which causes a NullReferenceException during
    /// pawn group (settlement) generation. Skipping relations avoids that.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    public static class Patch_GeneratePawnRelations
    {
        private static readonly GeneDef MamonoGene = DefDatabase<GeneDef>.GetNamedSilentFail("ProjectMamono_Mamono");

        public static bool Prefix(Pawn pawn)
        {
            if (pawn == null || MamonoGene == null || pawn.genes == null)
            {
                return true;
            }

            if (pawn.genes.HasActiveGene(MamonoGene))
            {
                return false;
            }

            return true;
        }
    }
}
