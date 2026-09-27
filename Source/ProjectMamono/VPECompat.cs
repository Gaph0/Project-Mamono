using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Optional soft integration with Vanilla Psycasts Expanded
    /// (VanillaExpanded.VPsycastsE). Monster xenotypes opt in via
    /// MamonoPsycastExtension on their XenotypeDef; while VPE is active this
    /// patches PawnGenerator.GeneratePawn so spawned Mamonos of those xenotypes
    /// receive a psylink, VPE's psycast implant, one unlocked psycaster path and
    /// a few random psycasts - mirroring how VPE equips its own caster pawn
    /// kinds (Empire_Caster_* etc.).
    ///
    /// SOFT DEPENDENCY: this type never references VPE types. All VPE/VEF-typed
    /// code lives in VPEShim, which is only touched when VPE is active (the same
    /// lazy-load guard as YayoAniShim). The pawn-generation patch is applied
    /// manually from ProjectMamonoMod's static constructor and carries no Harmony
    /// attributes, so PatchAll never resolves VPE types when the mod is absent.
    /// </summary>
    public static class VPECompat
    {
        public const string VpePackageId = "VanillaExpanded.VPsycastsE";

        /// <summary>
        /// True when Vanilla Psycasts Expanded is loaded. Matches the
        /// player-facing packageId so workshop (_steam suffixed) copies count,
        /// same as IntimacyCompat.Active.
        /// </summary>
        public static bool Active
        {
            get
            {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    if (string.Equals(mod.PackageIdPlayerFacing, VpePackageId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mod.PackageId, VpePackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static ProjectMamonoSettings Settings => ProjectMamonoModSettings.Settings;

        /// <summary>Applies the pawn-generation patch. Called only when VPE is active.</summary>
        public static void Apply(Harmony harmony)
        {
            var generatePawn = AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) });
            if (generatePawn == null)
            {
                Log.Error("[Project Mamono] VPE compat: could not find PawnGenerator.GeneratePawn to patch.");
                return;
            }

            harmony.Patch(generatePawn, postfix: new HarmonyMethod(typeof(VPEPawnGenPatch), nameof(VPEPawnGenPatch.Postfix)));
        }

        /// <summary>
        /// If the pawn's xenotype opted in via MamonoPsycastExtension, roll the
        /// spawn chance and grant VPE psycasts. Called from the GeneratePawn
        /// postfix, so the pawnkind's xenotype roll is already final.
        /// </summary>
        public static void TryGrantSpawnPsycasts(Pawn pawn)
        {
            try
            {
                if (!Settings.VPEPsycastsEnabled) return;
                if (pawn?.genes == null || pawn.genes.UniqueXenotype) return;

                var extension = pawn.genes.Xenotype?.GetModExtension<MamonoPsycastExtension>();
                if (extension == null) return;
                if (extension.pathDefNames.NullOrEmpty()) return;
                if (!Rand.Chance(extension.spawnChance)) return;

                VPEShim.Grant(pawn, extension);
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"[Project Mamono] VPE compat: failed to grant psycasts to {pawn}: {e}", pawn?.thingIDNumber ?? 0);
            }
        }
    }
}
