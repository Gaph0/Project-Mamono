using System;
using HarmonyLib;
using Verse;
using YayoAnimation;
using YayoAnimation.Data;

namespace ProjectMamono
{
    /// <summary>
    /// Optional soft integration with Yayo's Animation (Continued)
    /// (com.yayo.yayoAni.continued). While a Mamono performs the tsugai bonding
    /// action, this plays a romancin' (lovin'-style) bounce on her: Yayo only
    /// runs that animation for pawns in the vanilla Lovin job lying in a bed,
    /// so we postfix AnimationCore.CheckAni and write the motion into the Mamono's
    /// PawnDrawData (position/rotation offsets) ourselves; Yayo's renderer
    /// patches then apply those offsets when drawing her.
    ///
    /// SOFT DEPENDENCY: nothing in this type may be touched unless Yayo's
    /// Animation is active - it is only reached via ProjectMamonoMod's static
    /// constructor behind a ModsConfig.IsActive check. No Harmony attributes
    /// here: PatchAll scans this assembly and must never resolve Yayo types
    /// when the mod is absent.
    /// </summary>
    public static class YayoAniCompat
    {
        public const string YayoPackageId = "com.yayo.yayoAni.continued";

        /// <summary>
        /// True when Yayo's Animation is loaded. We scan RunningMods and match the
        /// player-facing packageId rather than ModsConfig.IsActive(string): the
        /// active-mods config stores workshop mods as &quot;packageid_steam&quot;
        /// (lowercased), so IsActive(&quot;com.yayo.yayoAni.continued&quot;) returns false even
        /// when the mod is clearly active. PackageIdPlayerFacing has no _steam suffix.
        /// </summary>
        public static bool Active
        {
            get
            {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    if (string.Equals(mod.PackageIdPlayerFacing, YayoPackageId, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mod.PackageId, YayoPackageId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Postfix AnimationCore.CheckAni, the per-render-tick entry point Yayo's
        /// own PawnRenderer patch calls. Must only be called when Yayo is active.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var checkAni = AccessTools.Method(typeof(AnimationCore), nameof(AnimationCore.CheckAni));
                if (checkAni == null)
                {
                    Log.Warning("[Project Mamono] Yayo's Animation found, but AnimationCore.CheckAni is missing - bonding animation disabled.");
                    return;
                }

                harmony.Patch(checkAni, postfix: new HarmonyMethod(typeof(YayoAniCompat), nameof(CheckAniPostfix)));
                Log.Message("[Project Mamono] Yayo's Animation detected - tsugai bonding animation enabled.");
            }
            catch (Exception e)
            {
                Log.Warning("[Project Mamono] Failed to hook Yayo's Animation - bonding animation disabled.\n" + e);
            }
        }

        /// <summary>
        /// Runs after Yayo's animation logic each render tick. When the pawn is a
        /// Mamono mid-bond, overwrite her draw data with a continuous romancin'
        /// bounce: a rhythmic thrust along the axis facing her partner plus a slow
        /// swaying tilt. Gated only on Project Mamono's own bonding-animation toggle
        /// (deliberately independent of Yayo's lovin'/faction/zoom settings, which
        /// previously suppressed it silently).
        /// </summary>
        public static void CheckAniPostfix(Pawn pawn, Rot4 rot, PawnDrawData pdd)
        {
            if (pawn == null || pdd == null)
            {
                return;
            }

            // Intimacy sex act: animate both partners while either is in it.
            // Intimacy ships no pawn animation and Yayo only recognizes the vanilla
            // Lovin job name, so its SEX_Sex pawns would otherwise stand frozen.
            if (IntimacyCompat.Active && ProjectMamonoModSettings.Settings.IntimacyAnimation)
            {
                Pawn sexPartner = IntimacyCompat.SexPartnerOf(pawn);
                if (sexPartner != null && pawn.RaceProps.Humanlike)
                {
                    YayoAniShim.ApplyBondAnimation(pawn, sexPartner, rot, pdd);
                    return;
                }
            }

            if (!ProjectMamonoModSettings.Settings.YayoBondingAnimation)
            {
                return;
            }

            // The tsugai bonding job and both essence-feeding jobs play the same
            // romancin' bounce while their action toil is running.
            var driver = pawn.jobs?.curDriver;
            bool active;
            if (driver is JobDriver_FormTsugai tsugai)
            {
                active = tsugai.BondInProgress;
            }
            else if (driver is JobDriver_DrainEssence drain)
            {
                active = drain.FeedInProgress;
            }
            else if (driver is JobDriver_GiveEssence give)
            {
                active = give.FeedInProgress;
            }
            else
            {
                return;
            }

            if (!active)
            {
                return;
            }

            Pawn partner = pawn.CurJob?.targetA.Pawn;
            if (partner == null || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            // The actual bounce lives in YayoAniShim so the partner can be
            // animated through the same code (driven per-tick from the job driver).
            YayoAniShim.ApplyBondAnimation(pawn, partner, rot, pdd);
        }
    }
}
