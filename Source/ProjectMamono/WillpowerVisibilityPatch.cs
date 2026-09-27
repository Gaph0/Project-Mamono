using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Hides the Willpower capacity on the Health tab for Mamono-carriers (for whom
    /// it is always maxed). The capacity still functions normally - tease erosion
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
                && __instance == ProjectMamono_DefOf.ProjectMamono_Willpower
                && WillpowerOverviewPatch.IsMamono(p))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Hides the Willpower row on the Health tab's <b>Overview</b> for Mamono-carriers.
    /// The overview builds its capacity list with
    /// DefDatabase&lt;PawnCapacityDef&gt;.AllDefs.Where(d =&gt; d.showOnHumanlikes) and never
    /// consults CanShowOnPawn, so the postfix above can't reach it. Because the
    /// capacity rows are drawn inline mid-method, we can't remove one cleanly -
    /// instead we clear the def's showOnHumanlikes flag for the duration of the
    /// draw when the pawn is a Mamono, then restore it in a finally.
    /// </summary>
    [HarmonyPatch(typeof(HealthCardUtility), "DrawOverviewTab")]
    public static class WillpowerOverviewPatch
    {
        public static bool IsMamono(Pawn p)
        {
            return p?.genes != null && p.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_Mamono);
        }

        public static void Prefix(Pawn pawn, ref bool __state)
        {
            PawnCapacityDef def = ProjectMamono_DefOf.ProjectMamono_Willpower;
            __state = IsMamono(pawn) && def != null && def.showOnHumanlikes;
            if (__state)
            {
                def.showOnHumanlikes = false;
            }
        }

        public static void Postfix(Pawn pawn, bool __state)
        {
            if (__state)
            {
                ProjectMamono_DefOf.ProjectMamono_Willpower.showOnHumanlikes = true;
            }
        }
    }
}
