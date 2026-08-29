using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// When a Momo eats, part of her Mana is restored, scaled by the nutrition
    /// actually gained. Ordinary food is only a supplement — it slows the drain,
    /// but essence feeding (or a bonded partner) remains the real source of mana.
    ///
    /// Hooks the virtual Need.CurLevel setter and reacts only to upward moves of
    /// a pawn's FOOD need — which is exactly what eating triggers (via
    /// Need_Food.NutritionWanted in the FinalizeIngest toil, so only completed
    /// meals count; interrupted meals never raise Food). Malnutrition is driven
    /// by NeedInterval drain, not by writes to the setter, so it grants nothing.
    /// </summary>
    [HarmonyPatch(typeof(Need), nameof(Need.CurLevel), MethodType.Setter)]
    public static class MomoEatingPatch
    {
        // Need.pawn is protected; read it via a compiled field accessor.
        private static readonly AccessTools.FieldRef<Need, Pawn> NeedPawn =
            AccessTools.FieldRefAccess<Need, Pawn>("pawn");

        public static void Postfix(Need __instance, float value)
        {
            if (__instance.def != NeedDefOf.Food)
            {
                return;
            }

            float eaten = value - __instance.CurLevel;
            if (eaten <= 0f)
            {
                return;
            }

            Pawn pawn = NeedPawn.Invoke(__instance);
            if (!EssenceTransfer.IsMomo(pawn))
            {
                return;
            }

            Need_Mana mana = EssenceTransfer.Mana(pawn);
            if (mana == null)
            {
                return;
            }

            float factor = ProjectMomoModSettings.Settings?.ManaFromFoodPerNutrition ?? 0.15f;
            if (factor <= 0f)
            {
                return;
            }

            // Need.CurLevel clamps itself to [0, MaxLevel].
            mana.CurLevel += eaten * factor;
        }
    }
}
