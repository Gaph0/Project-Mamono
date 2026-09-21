using System;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Optional soft integration with Progression: Education
    /// (ferny.ProgressionEducation). That mod's speech track gives every pawn a
    /// tier, and the lowest tier (PE_MuteTier) applies the trait
    /// PE_MuteProficiency - "no understanding of language". A mute pawn cannot
    /// speak, so she cannot propose a tsugai bond or a transformation, and cannot
    /// accept either one.
    ///
    /// SOFT DEPENDENCY: nothing here references the ProgressionEducation
    /// assembly. The mute trait is resolved by defName, so with that mod absent
    /// MuteTrait stays null, IsMute is always false, and the mod behaves exactly
    /// as it did before. No patch, no shim, no load order requirement.
    ///
    /// Known deviation: Progression: Education stops enforcing its own mute rules
    /// when the player switches speech proficiency off in its settings. This check
    /// keeps working in that case, because the trait stays on the pawn.
    /// </summary>
    public static class EducationCompat
    {
        public const string PackageId = "ferny.ProgressionEducation";

        private const string MuteTraitDefName = "PE_MuteProficiency";

        private static TraitDef muteTrait;
        private static bool muteTraitResolved;

        /// <summary>
        /// The trait Progression: Education's mute tier applies, or null when that
        /// mod is absent. Cached, and safe to cache: the first caller is the
        /// ProjectMomoMod constructor, which runs after every def has loaded.
        /// </summary>
        public static TraitDef MuteTrait
        {
            get
            {
                if (!muteTraitResolved)
                {
                    muteTrait = DefDatabase<TraitDef>.GetNamedSilentFail(MuteTraitDefName);
                    muteTraitResolved = true;
                }

                return muteTrait;
            }
        }

        /// <summary>True when Progression: Education is loaded. Used for the startup line.</summary>
        public static bool Active
        {
            get
            {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    if (string.Equals(mod.PackageIdPlayerFacing, PackageId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mod.PackageId, PackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// True when the pawn's speech tier is mute. False for animals, and false
        /// everywhere when Progression: Education is not loaded.
        /// </summary>
        public static bool IsMute(Pawn pawn)
        {
            if (pawn?.story?.traits == null)
            {
                return false;
            }

            TraitDef trait = MuteTrait;
            return trait != null && pawn.story.traits.HasTrait(trait);
        }

        /// <summary>
        /// A short reason for the float menu, in the same style as the reasons
        /// already used there ("she has no interest" and friends).
        /// </summary>
        public static string MuteReason(Pawn pawn)
        {
            return pawn != null && pawn.gender == Gender.Male ? "he is mute" : "she is mute";
        }
    }
}
