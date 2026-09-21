using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// One shared "momo corpses" line for the whole family.
    ///
    /// Vanilla decides a corpse's category in
    /// ThingDefGenerator_Corpses.GenerateCorpseDef:
    ///
    ///     thingCategories.Add(pawnDef.race.Humanlike
    ///         ? ThingCategoryDefOf.CorpsesHumanlike
    ///         : pawnDef.race.FleshType.corpseCategory);
    ///
    /// The humanlike test comes first, so a momo corpse is filed with human corpses
    /// whatever the flesh type says. No XML field exists for the category, and corpse
    /// defs are generated after XML patching, so no patch can reach them either.
    ///
    /// Nor can this be a one-shot job of our own. Big and Small files its races'
    /// corpses too - RaceFuser.GenerateCorpse uses BS_CorpsesHumanlikeHybrids or
    /// BS_CorpsesHumanlikeAnimals - and the order of mods'
    /// [StaticConstructorOnStartup] constructors is not ours to choose, so B&S could
    /// simply run after us and put the corpses back. That is why this file also
    /// patches StaticConstructorOnStartupUtility.CallAll(): its postfix runs after
    /// every mod's startup constructor, so our move is always the last word.
    ///
    /// PMM_MomoCorpses (Defs/ThingCategoryDefs/ThingCategories_MomoCorpses.xml) is a
    /// CHILD of CorpsesHumanlike, the way vanilla's CorpsesInsect is a child of
    /// CorpsesAnimal. That nesting is what keeps the move safe: a filter that allowed
    /// humanlike corpses was already expanded to hold the corpse defs themselves, and
    /// a filter configured later walks the category's descendants - so the butcher
    /// bill, corpse stockpiles, and the Ideology and Anomaly buildings that want
    /// humanlike corpses all still accept a momo corpse, while the menus show one
    /// "momo corpses" line instead of one entry per race.
    ///
    /// The def lives in THIS mod, not in a species mod, so every momo mod can share
    /// one line. Each species mod registers its own races:
    ///
    ///     [StaticConstructorOnStartup]
    ///     public static class SlimeMomoCorpses
    ///     {
    ///         static SlimeMomoCorpses()
    ///         {
    ///             MomoCorpses.Register("PMM_Race_SlimeMomo", ...);
    ///         }
    ///     }
    ///
    /// Register() moves the corpses straight away, from inside the species mod's own
    /// startup constructor, and the CallAll postfix below moves them again at the very
    /// end. Both passes are safe: Apply() is idempotent.
    /// </summary>
    public static class MomoCorpses
    {
        /// <summary>
        /// Every race registered by a species mod, in registration order. Looked up by
        /// name, so a race from an absent DLC or an uninstalled mod is simply skipped.
        /// </summary>
        private static readonly List<string> RaceDefNames = new List<string>();

        /// <summary>
        /// Categories a momo corpse gets moved out of: vanilla's humanlike line, and
        /// Big and Small's two, in case B&S decided the race is a hybrid (carrying a
        /// B&S race tracker is about all it takes).
        /// </summary>
        private static readonly string[] ReplacedCategories =
        {
            "CorpsesHumanlike",
            "BS_CorpsesHumanlikeHybrids",
            "BS_CorpsesHumanlikeAnimals"
        };

        /// <summary>
        /// Adds race defs to the momo corpses line and moves their corpses right away.
        /// Called once per species mod, from that mod's own [StaticConstructorOnStartup]
        /// constructor. Unknown or absent race names are harmless.
        /// </summary>
        public static void Register(params string[] raceDefNames)
        {
            if (raceDefNames == null)
            {
                return;
            }
            foreach (string raceName in raceDefNames)
            {
                if (!string.IsNullOrEmpty(raceName) && !RaceDefNames.Contains(raceName))
                {
                    RaceDefNames.Add(raceName);
                }
            }
            Apply();
        }

        /// <summary>
        /// Moves every registered momo corpse into PMM_MomoCorpses. Safe to run more than
        /// once, which it does: once per species mod from Register(), and once more from
        /// the CallAll postfix at the bottom of this file.
        /// </summary>
        public static void Apply()
        {
            ThingCategoryDef momoCorpses = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("PMM_MomoCorpses");
            if (momoCorpses == null)
            {
                Log.Error("[PMM] ThingCategoryDef PMM_MomoCorpses is missing, so momo corpses stay "
                    + "under humanlike corpses. Check Defs/ThingCategoryDefs/ThingCategories_MomoCorpses.xml "
                    + "in Project Momo.");
                return;
            }

            var replaced = new List<ThingCategoryDef>();
            foreach (string categoryName in ReplacedCategories)
            {
                ThingCategoryDef category = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(categoryName);
                if (category != null)
                {
                    replaced.Add(category);
                }
            }

            int moved = 0;
            foreach (string raceName in RaceDefNames)
            {
                ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(raceName);
                ThingDef corpse = raceDef?.race?.corpseDef;
                if (corpse == null)
                {
                    continue; // a race from an absent mod or DLC is simply skipped
                }
                Move(corpse, momoCorpses, replaced);
                moved++;

                // The unnatural corpse (Anomaly's rot-and-revive variant) hangs off
                // the real corpse as a virtual def, and belongs on the same line.
                if (corpse.virtualDefs != null)
                {
                    foreach (ThingDef virtualDef in corpse.virtualDefs)
                    {
                        Move(virtualDef, momoCorpses, replaced);
                    }
                }
            }

            if (moved > 0 && Prefs.DevMode)
            {
                Log.Message($"[PMM] Moved {moved} momo corpses into PMM_MomoCorpses.");
            }

            if (moved > 0)
            {
                // Menus draw a category's rows from ThingCategoryDef.SortedChildThingDefs,
                // and membership tests use allChildThingDefsCached. Both are built once, in
                // ResolveReferences, long before mods' static constructors run - so editing
                // thingCategories and childThingDefs alone leaves the new line showing no
                // rows while the old line still lists the corpses. ResolveReferences rebuilds
                // both from the lists we just edited, for every category, because a parent's
                // cached set covers its descendants too.
                foreach (ThingCategoryDef category in DefDatabase<ThingCategoryDef>.AllDefs)
                {
                    category.ResolveReferences();
                }
            }
        }

        private static void Move(ThingDef corpse, ThingCategoryDef momoCorpses, List<ThingCategoryDef> replaced)
        {
            if (corpse == null)
            {
                return;
            }
            if (corpse.thingCategories == null)
            {
                corpse.thingCategories = new List<ThingCategoryDef>();
            }

            // Menus do not read a thing's thingCategories live. ThingCategoryNodeDatabase
            // builds a snapshot once, in FinalizeInit, filling ThingCategoryDef.childThingDefs
            // for every thing def - and that happens before mods' static constructors run.
            // Moving a corpse without editing that snapshot leaves it drawn under the old
            // line, and the new line empty, which is exactly what happened first time.
            foreach (ThingCategoryDef oldCategory in corpse.thingCategories)
            {
                oldCategory?.childThingDefs?.Remove(corpse);
            }

            corpse.thingCategories.RemoveAll(c => c == null || replaced.Contains(c));
            if (!corpse.thingCategories.Contains(momoCorpses))
            {
                corpse.thingCategories.Add(momoCorpses);
            }

            if (momoCorpses.childThingDefs != null && !momoCorpses.childThingDefs.Contains(corpse))
            {
                momoCorpses.childThingDefs.Add(corpse);
            }
        }
    }

    /// <summary>
    /// The last word on the subject: CallAll() runs every mod's
    /// [StaticConstructorOnStartup], so a postfix here runs after all of them -
    /// including Big and Small's, which files its own races' corpses and could
    /// otherwise undo the move the species mods make.
    /// </summary>
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.CallAll))]
    public static class Patch_CallAll_MomoCorpses
    {
        public static void Postfix()
        {
            MomoCorpses.Apply();
        }
    }
}
