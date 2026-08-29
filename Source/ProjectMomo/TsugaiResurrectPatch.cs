using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Detects when a pawn is brought back to life. ResurrectionUtility.TryResurrect
    /// is the single choke point every resurrection path (resurrector serum, death
    /// refusal, anomaly, dev-mode) funnels through, so postfixing it and handing off
    /// to TsugaiFormation lets any grieving former partner swap their bond grief for
    /// a positive moodlet. The bond itself is not re-established.
    /// </summary>
    [HarmonyPatch(typeof(ResurrectionUtility), "TryResurrect")]
    public static class TsugaiResurrectPatch
    {
        public static void Postfix(Pawn pawn, bool __result)
        {
            if (__result && pawn != null)
            {
                TsugaiFormation.HandleBondPartnerResurrected(pawn);
            }
        }
    }
}
