using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Momos don't lose fertility to age. Vanilla models the decline as
    /// StatPart_FertilityByGenderAge on the Fertility stat (Biotech), which
    /// multiplies the stat by the female age curve: 1.0 at 20-28, fading to
    /// 0 by 50. This postfix undoes the curve's multiplier for Momo-carriers,
    /// restoring fertility to its peak (the curve tops out at 1.0, so dividing
    /// by it returns the pre-age value). Hediff-based fertility changes
    /// (StatPart_FertilityByHediffs) are untouched — a sterilized or
    /// fertility-drained Momo still feels those.
    ///
    /// Patched by name (not typeof) so the patch target only resolves when
    /// Biotech is loaded; ProjectMomoMod applies it alongside the other
    /// optional integrations. An extra stat postfix when Biotech is absent
    /// would be dead weight: the Fertility stat itself doesn't exist then.
    /// </summary>
    public static class MomoFertilityPatch
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
                Log.Warning("[Project Momo] Could not find StatPart_FertilityByGenderAge.TransformValue — Momo age-fertility immunity disabled.");
                return;
            }

            var postfix = new HarmonyMethod(typeof(MomoFertilityPatch), nameof(Postfix));
            harmony.Patch(target, postfix: postfix);
            Log.Message("[Project Momo] Biotech detected — Momos are immune to fertility age decline.");
        }

        /// <summary>
        /// After vanilla applies the age curve, restore it for Momo-carriers:
        /// val was multiplied by ageFactor (0..1), so divide it back out. A
        /// factor of 1 (peak years) leaves the value alone; a factor near 0
        /// (post-menopause age) restores full fertility.
        /// </summary>
        public static void Postfix(StatPart_FertilityByGenderAge __instance, StatRequest req, ref float val)
        {
            if (!ProjectMomoModSettings.Settings.MomoFertilityAgeless)
            {
                return;
            }

            if (!(req.Thing is Pawn pawn) || (!EssenceTransfer.IsMomo(pawn) && !Incubisation.IsFullIncubus(pawn)))
            {
                return;
            }

            float ageFactor = AgeFactor(__instance, pawn);
            if (ageFactor >= 1f)
            {
                return; // already at the curve's peak — nothing to restore
            }

            // The curve's maximum is 1.0, so val / ageFactor returns the value
            // the stat had before the age multiplier — i.e. peak fertility.
            val = ageFactor > 0.0001f ? val / ageFactor : val * 10000f;
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
