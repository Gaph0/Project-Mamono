using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Detects when a bonded (tsugai) partner dies. RimWorld calls
    /// Pawn_RelationsTracker.Notify_PawnKilled on the dead pawn's own tracker, so
    /// we postfix it and hand off to TsugaiFormation, which strips the survivors'
    /// bond hediffs and applies the bond-grief debuff and mood penalty. The
    /// tracker's pawn field is private, so it is read by reflection once.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "Notify_PawnKilled")]
    public static class TsugaiLossPatch
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_RelationsTracker), "pawn");

        public static void Postfix(Pawn_RelationsTracker __instance)
        {
            Pawn dead = PawnField?.GetValue(__instance) as Pawn;
            if (dead != null)
            {
                TsugaiFormation.HandleBondPartnerDied(dead);
            }
        }
    }
}
