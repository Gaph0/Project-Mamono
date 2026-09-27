using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Turns Big &amp; Small's romance compatibility layer off (user's call 2026-09-26), so Project
    /// Mamono's own romance rules are the only ones in play.
    ///
    /// B&S decides who may romance whom from `romanceTags`: `GetHighestSharedTag` returns 0 when the
    /// two pawns share no tag - i.e. compatibility zero, nobody pairs - and otherwise the best
    /// shared tag's chance x factor. It applies that from six methods on
    /// `BigAndSmall.RomancePatches`, all of which carry a `[HarmonyPatch]` attribute:
    ///
    ///   MarriageProposalPrefix        marriage chance
    ///   CompatibilityWith_Postfix     Pawn_RelationsTracker.CompatibilityWith
    ///   RomanceEligiblePairPostfix    RelationsUtility.RomanceEligiblePair
    ///   RomanceFactorPostfix          Pawn_RelationsTracker.SecondaryRomanceChanceFactor
    ///   LovingFactorPostfix           Pawn_RelationsTracker.SecondaryLovinChanceFactor
    ///   LovingFactor_Transpiler       Pawn_RelationsTracker.SecondaryLovinChanceFactor
    ///
    /// All six are skipped here. What remains is vanilla's romance code plus core's own patches
    /// (`TsugaiRomancePatch`, `IncestPreventionPatch`, the `BondRomanceFactor` floor), which is the
    /// point of the exercise: one system decides, and it is ours.
    ///
    /// How, and why this way:
    ///  - Each of those methods gets a prefix from us that skips its body. Patching the patch
    ///    methods themselves is load-order proof: it does not matter whether B&S applied its
    ///    patches before or after this mod, because B&S's body never runs when its method is
    ///    called.
    ///  - The transpiler is the one special case. Harmony calls it to build the patched IL, so our
    ///    prefix hands back the instructions it was given, untouched.
    ///  - Nothing is hard-coded to a method name beyond the class name: if B&S adds a seventh
    ///    romance patch, it is skipped as well, and a helper method in the same class that carries
    ///    no `[HarmonyPatch]` attribute is left alone.
    ///  - Resolved by name with `AccessTools.TypeByName`, like the other optional integrations, so
    ///    no B&S type is touched when the mod is absent.
    ///
    /// One side effect worth knowing: B&S's `RomanceEligiblePair` postfix is what makes some of its
    /// non-humanlike animal-people romance-eligible at all. With it off they follow vanilla
    /// eligibility. Mamono are humanlike, so nothing changes for them.
    /// </summary>
    public static class BigAndSmallRomanceDisablePatch
    {
        private const string RomancePatchesTypeName = "BigAndSmall.RomancePatches";

        private static Type romancePatches;

        /// <summary>True when Big &amp; Small is loaded and exposes its romance patch class.</summary>
        public static bool Active => RomancePatches != null;

        private static Type RomancePatches
        {
            get
            {
                if (romancePatches == null)
                {
                    romancePatches = AccessTools.TypeByName(RomancePatchesTypeName);
                }
                return romancePatches;
            }
        }

        public static void Apply(Harmony harmony)
        {
            int skipped = 0;
            foreach (MethodInfo method in RomancePatches.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<HarmonyPatch>() == null)
                {
                    continue;
                }

                HarmonyMethod prefix = ReturnsInstructions(method)
                    ? new HarmonyMethod(AccessTools.Method(typeof(BigAndSmallRomanceDisablePatch), nameof(SkipTranspiler)))
                    : new HarmonyMethod(AccessTools.Method(typeof(BigAndSmallRomanceDisablePatch), nameof(SkipPatchBody)));

                harmony.Patch(method, prefix: prefix);
                skipped++;
            }

            Log.Message($"[Project Mamono] Startup: Big & Small romance compatibility disabled ({skipped} patch methods skipped).");
        }

        /// <summary>A transpiler takes instructions in and returns instructions.</summary>
        private static bool ReturnsInstructions(MethodInfo method)
        {
            return typeof(IEnumerable<CodeInstruction>).IsAssignableFrom(method.ReturnType);
        }

        /// <summary>
        /// Skips one of B&S's prefix or postfix bodies. Returning false leaves whatever value the
        /// method was going to change exactly as vanilla computed it.
        /// </summary>
        private static bool SkipPatchBody()
        {
            return false;
        }

        /// <summary>
        /// Skips B&S's transpiler and hands the instructions back unchanged. The parameter is named
        /// `instructions` because Harmony injects by name, and B&S names its transpiler argument the
        /// same way; `__result` is what a skipped transpiler returns.
        /// </summary>
        private static bool SkipTranspiler(IEnumerable<CodeInstruction> instructions, ref IEnumerable<CodeInstruction> __result)
        {
            __result = instructions;
            return false;
        }
    }
}
