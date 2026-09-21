using HarmonyLib;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// A visiting Momo arrives with a full mana bar.
    ///
    /// Every need starts on half a bar (Verse.Need.SetInitialLevel sets
    /// CurLevelPercentage to 0.5), and a guest drains at GuestManaDrainFactor of the normal
    /// rate — a quarter by default, so one full bar is eight days but the half bar she is
    /// generated with is only four. Any visit longer than that leaves her dry and wearing
    /// the mana-starvation hediff for the tail of her stay: not a danger (see the guest
    /// branch in LowManaBreak, which never lets a visitor break) but no way for a guest to
    /// arrive. Filling her the moment she spawns keeps every visitor fed — vanilla
    /// visitors, trader caravans, and the guests each Momo mod sends — instead of leaving
    /// every mod to remember it.
    ///
    /// Skipped when the pawn is respawning after a load, so saving and reloading cannot be
    /// used to refill a guest mid-visit. Colonists, wild Momos, prisoners and raiders are
    /// untouched: IsVisitingGuest excludes them all, and a raider *should* drain.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class VisitingGuestManaPatch
    {
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad || !EssenceTransfer.IsVisitingGuest(__instance))
            {
                return;
            }

            Need_Mana mana = EssenceTransfer.Mana(__instance);
            if (mana != null)
            {
                mana.CurLevelPercentage = 1f;
            }
        }
    }
}
