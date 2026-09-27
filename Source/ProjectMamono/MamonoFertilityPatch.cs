using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Mamono fertility protection, in three layers (all Biotech-only):
    ///
    /// 1. Ageless fertility: Mamonos don't lose fertility
    ///    to age. Vanilla models the decline as StatPart_FertilityByGenderAge on
    ///    the Fertility stat, which multiplies the stat by the female age curve:
    ///    1.0 at 20-28, fading to 0 by 50. The postfix undoes the curve's
    ///    multiplier for Mamono-carriers, restoring fertility to its peak (the
    ///    curve tops out at 1.0, so dividing by it returns the pre-age value).
    ///
    /// 2. Always fertile: a postfix on StatExtension
    ///    .GetStatValue floors an adult Mamono's Fertility at 1.0, so no source -
    ///    sterilized, fertility-drained or removed ovaries (StatPart_
    ///    FertilityByHediffs), gene/trait offsets, or age - can push her below
    ///    100%. Boosts above 100% are untouched.
    ///
    /// 3. Sterility gate: Pawn.Sterile() also blocks
    ///    reproduction when a hediff has preventsPregnancy (Core's Sterilized -
    ///    and pregnancy itself) or a gene has sterilize (Biotech's Sterile).
    ///    A postfix reports adult Mamono-carriers as not-sterile - except while
    ///    pregnant, so an active pregnancy still suppresses re-conception the
    ///    way vanilla expects. Children keep vanilla sterility in every layer.
    ///
    /// Exceptions, added 2026-09-26: the guarantee stands aside for a Mamono whose barrenness is
    /// deliberate - vanilla's own other two sterility sources, a hediff that prevents pregnancy
    /// or a gene with `sterilize`. Both are statements about that one pawn rather than a number
    /// to protect, so all three layers skip her and vanilla answers on its own: the stat reads
    /// 0%, Sterile() returns true and no pregnancy can start. Without this, the floor below held
    /// a barren Mamono's Fertility at 100% whatever her gene or hediff said - which is exactly what
    /// the abaddon did.
    ///
    /// Patched by name (not typeof) so the patch target only resolves when
    /// Biotech is loaded; ProjectMamonoMod applies it alongside the other
    /// optional integrations. An extra stat postfix when Biotech is absent
    /// would be dead weight: the Fertility stat itself doesn't exist then.
    /// </summary>
    public static class MamonoFertilityPatch
    {
        /// <summary>True while Biotech's Fertility stat (and its age curve) exists.</summary>
        public static bool Active => BiotechFertilityStat != null;

        private static StatDef biotechFertilityStat;

        private static StatDef BiotechFertilityStat
        {
            get
            {
                if (biotechFertilityStat == null)
                {
                    // GetNamedSilentFail (not StatDefOf.Fertility) so the lookup
                    // is lazy and safe when Biotech is absent: StatDefOf would
                    // throw on first touch instead of returning null.
                    biotechFertilityStat = DefDatabase<StatDef>.GetNamedSilentFail("Fertility");
                }
                return biotechFertilityStat;
            }
        }

        public static void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(StatPart_FertilityByGenderAge), nameof(StatPart_FertilityByGenderAge.TransformValue));
            if (target == null)
            {
                Log.Warning("[Project Mamono] Could not find StatPart_FertilityByGenderAge.TransformValue - Mamono age-fertility immunity disabled.");
            }
            else
            {
                var postfix = new HarmonyMethod(typeof(MamonoFertilityPatch), nameof(Postfix));
                harmony.Patch(target, postfix: postfix);
            }

            // Layer 3: the Sterile() gate. Sterile() is plain Verse (it self-gates
            // on ModsConfig.BiotechActive), so this target always resolves.
            var sterileTarget = AccessTools.Method(typeof(Pawn), nameof(Pawn.Sterile));
            if (sterileTarget == null)
            {
                Log.Warning("[Project Mamono] Could not find Pawn.Sterile - Mamono sterility immunity disabled.");
            }
            else
            {
                harmony.Patch(sterileTarget, postfix: new HarmonyMethod(typeof(MamonoFertilityPatch), nameof(SterilePostfix)));
            }

            // Layer 2: the stat floor. StatExtension.GetStatValue is one method
            // with optional parameters, so pin the full signature explicitly.
            var getStatTarget = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue),
                new[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) });
            if (getStatTarget == null)
            {
                Log.Warning("[Project Mamono] Could not find StatExtension.GetStatValue - Mamono fertility floor disabled.");
                return;
            }

            harmony.Patch(getStatTarget, postfix: new HarmonyMethod(typeof(MamonoFertilityPatch), nameof(GetStatValuePostfix)));
            Log.Message("[Project Mamono] Biotech detected - Mamonos are immune to fertility age decline and sterility.");
        }

        /// <summary>
        /// Layer 3: adult Mamono-carriers are never sterile. Only the sterility
        /// sources are overridden - vanilla's lifestage and humanlike gates
        /// stand (children stay sterile), and an active pregnancy still
        /// suppresses re-conception so the pregnancy system can't be retriggered
        /// mid-term.
        /// </summary>
        public static void SterilePostfix(Pawn __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (__instance == null || (!EssenceTransfer.IsMamono(__instance) && !Incubisation.IsFullIncubus(__instance)))
            {
                return;
            }

            if (DeliberatelySterile(__instance))
            {
                return;
            }

            if (__instance.ageTracker?.CurLifeStage?.reproductive != true
                || __instance.RaceProps == null || !__instance.RaceProps.Humanlike)
            {
                return;
            }

            if (PregnancyUtility.GetPregnancyHediff(__instance) != null)
            {
                return;
            }

            __result = false;
        }

        /// <summary>
        /// Layer 2: an adult Mamono's Fertility never reads below 100%, whatever
        /// reduced it (hediffs, genes, traits, age). Children keep their natural
        /// zero. Runs on every stat read, so the stat-def reference compare
        /// comes first - it rejects virtually every call.
        /// </summary>
        public static void GetStatValuePostfix(Thing thing, StatDef stat, ref float __result)
        {
            if (BiotechFertilityStat == null || stat != BiotechFertilityStat || __result >= 1f)
            {
                return;
            }

            if (!(thing is Pawn pawn) || (!EssenceTransfer.IsMamono(pawn) && !Incubisation.IsFullIncubus(pawn)))
            {
                return;
            }

            if (DeliberatelySterile(pawn))
            {
                return;
            }

            if (pawn.ageTracker?.CurLifeStage?.reproductive != true)
            {
                return;
            }

            __result = 1f;
        }

        /// <summary>
        /// After vanilla applies the age curve, restore it for Mamono-carriers:
        /// val was multiplied by ageFactor (0..1), so divide it back out. A
        /// factor of 1 (peak years) leaves the value alone; a factor near 0
        /// (post-menopause age) restores full fertility.
        /// </summary>
        public static void Postfix(StatPart_FertilityByGenderAge __instance, StatRequest req, ref float val)
        {
            if (!(req.Thing is Pawn pawn) || (!EssenceTransfer.IsMamono(pawn) && !Incubisation.IsFullIncubus(pawn)))
            {
                return;
            }

            if (DeliberatelySterile(pawn))
            {
                return;
            }

            float ageFactor = AgeFactor(__instance, pawn);
            if (ageFactor >= 1f)
            {
                return; // already at the curve's peak - nothing to restore
            }

            // The curve's maximum is 1.0, so val / ageFactor returns the value
            // the stat had before the age multiplier - i.e. peak fertility.
            val = ageFactor > 0.0001f ? val / ageFactor : val * 10000f;
        }

        /// <summary>
        /// True when vanilla already has a reason of its own to call this pawn sterile, other than
        /// the Fertility number the floor protects: a hediff that prevents pregnancy (surgery's
        /// Sterilized, or a mod's own) or a gene that says so (Biotech's Sterile, which the
        /// abaddon carries). Both are deliberate statements about that one pawn, so the
        /// Mamono guarantee does not apply to her and every layer skips her. `SterileGenes` is
        /// vanilla's own test, so these two together are exactly the reasons `Pawn.Sterile()`
        /// would return true if the floor were not standing in the way.
        /// </summary>
        private static bool DeliberatelySterile(Pawn pawn)
        {
            return pawn.health?.hediffSet != null
                && (pawn.health.hediffSet.HasHediffPreventsPregnancy() || pawn.SterileGenes());
        }

        // The gender curves are protected fields on the stat part.
        private static readonly FieldInfo FemaleCurveField =
            AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "femaleFertilityAgeFactor");
        private static readonly FieldInfo MaleCurveField =
            AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "maleFertilityAgeFactor");

        /// <summary>The curve value vanilla used for this pawn (gender + biological age).</summary>
        private static float AgeFactor(StatPart_FertilityByGenderAge part, Pawn pawn)
        {
            if (pawn.ageTracker == null)
            {
                return 1f;
            }

            FieldInfo field = pawn.gender == Gender.Female ? FemaleCurveField : MaleCurveField;
            if (!(field?.GetValue(part) is SimpleCurve curve))
            {
                return 1f;
            }
            return curve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
        }
    }
}
