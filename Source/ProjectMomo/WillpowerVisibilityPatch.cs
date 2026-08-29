using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Hides the Willpower capacity on the Health tab for Momo-carriers (for whom
    /// it is always maxed). The capacity still functions normally — tease erosion
    /// and knockout are unaffected; this only removes the display row. The health
    /// card calls PawnCapacityDef.CanShowOnPawn(pawn) per capacity when building
    /// the list, so this postfix simply vetoes it for carriers.
    /// </summary>
    [HarmonyPatch(typeof(PawnCapacityDef), "CanShowOnPawn")]
    public static class WillpowerVisibilityPatch
    {
        public static void Postfix(PawnCapacityDef __instance, Pawn p, ref bool __result)
        {
            if (__result
                && __instance == ProjectMomo_DefOf.ProjectMomo_Willpower
                && WillpowerOverviewPatch.IsMomo(p))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Hides the Willpower row on the Health tab's <b>Overview</b> for Momo-carriers.
    /// The overview builds its capacity list with
    /// DefDatabase&lt;PawnCapacityDef&gt;.AllDefs.Where(d =&gt; d.showOnHumanlikes) and never
    /// consults CanShowOnPawn, so the postfix above can't reach it. Because the
    /// capacity rows are drawn inline mid-method, we can't remove one cleanly —
    /// instead we clear the def's showOnHumanlikes flag for the duration of the
    /// draw when the pawn is a Momo, then restore it in a finally.
    /// </summary>
    [HarmonyPatch(typeof(HealthCardUtility), "DrawOverviewTab")]
    public static class WillpowerOverviewPatch
    {
        public static bool IsMomo(Pawn p)
        {
            return p?.genes != null && p.genes.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo);
        }

        public static void Prefix(Pawn pawn, ref bool __state)
        {
            PawnCapacityDef def = ProjectMomo_DefOf.ProjectMomo_Willpower;
            __state = IsMomo(pawn) && def != null && def.showOnHumanlikes;
            if (__state)
            {
                def.showOnHumanlikes = false;
            }
        }

        public static void Postfix(Pawn pawn, bool __state)
        {
            if (__state)
            {
                ProjectMomo_DefOf.ProjectMomo_Willpower.showOnHumanlikes = true;
            }
        }
    }
}
