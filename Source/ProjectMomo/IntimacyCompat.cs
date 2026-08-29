using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Optional soft integration with Intimacy - Friends n' Lovers
    /// (LovelyDovey.Sex.WithEuterpe). Intimacy removes the vanilla lovin' job
    /// giver and routes sex through its own intimacy need and sex jobs, so a
    /// Momo sleeping with a human through it never triggered Project Momo's
    /// essence transfer. This patches Intimacy's SexUtilities.ApplyLovinThoughts
    /// — which runs once per completed sex act with both participants — to have
    /// the Momo drink her fill: the essence moved equals what she needs to fill
    /// her Mana bar, limited by how much essence the partner actually has.
    ///
    /// SOFT DEPENDENCY: nothing in this type references Intimacy's types; the
    /// patch target is resolved by name and only applied when the mod is active
    /// (see ProjectMomoMod's static constructor). No Harmony attributes here:
    /// PatchAll scans this assembly and must never resolve Intimacy types when
    /// the mod is absent.
    /// </summary>
    public static class IntimacyCompat
    {
        public const string IntimacyPackageId = "LovelyDovey.Sex.WithEuterpe";

        /// <summary>
        /// True when Intimacy - Friends n' Lovers is loaded. Matches the
        /// player-facing packageId so workshop (_steam suffixed) copies count,
        /// same as YayoAniCompat.Active.
        /// </summary>
        public static bool Active
        {
            get
            {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    if (string.Equals(mod.PackageIdPlayerFacing, IntimacyPackageId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(mod.PackageId, IntimacyPackageId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Intimacy's sex job defNames (its JobDriver_Sex / JobDriver_SexLead).
        // Looked up by string so this assembly never hard-references the mod.
        private const string SexJobDefName = "SEX_Sex";
        private const string SexLeadJobDefName = "SEX_SexLead";

        /// <summary>
        /// If <paramref name="pawn"/> is currently in an Intimacy sex act, returns
        /// their partner; otherwise null. The driver holds the partner in job
        /// targetA throughout the act, so both participants resolve each other.
        /// Used by YayoAniCompat to animate the act while Yayo's Animation is
        /// loaded (Intimacy ships no pawn animation of its own).
        /// </summary>
        public static Pawn SexPartnerOf(Pawn pawn)
        {
            Job job = pawn?.CurJob;
            if (job == null)
            {
                return null;
            }

            string defName = job.def?.defName;
            if (defName != SexJobDefName && defName != SexLeadJobDefName)
            {
                return null;
            }

            Pawn partner = job.targetA.Pawn;
            return partner != null && partner != pawn ? partner : null;
        }

        /// <summary>
        /// Postfix Intimacy's per-act thought applicator. Must only be called when
        /// Intimacy is active; the target is looked up by name so this assembly
        /// never hard-references the Intimacy dll.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var applyLovinThoughts = AccessTools.Method("LoveyDoveySexWithEuterpe.SexUtilities:ApplyLovinThoughts");
                if (applyLovinThoughts == null)
                {
                    Log.Warning("[Project Momo] Intimacy found, but SexUtilities.ApplyLovinThoughts is missing — intimacy feeding disabled.");
                    return;
                }

                harmony.Patch(applyLovinThoughts, postfix: new HarmonyMethod(typeof(IntimacyCompat), nameof(ApplyLovinThoughtsPostfix)));
                Log.Message("[Project Momo] Intimacy - Friends n' Lovers detected — Momos will feed on their partners during sex.");
            }
            catch (Exception e)
            {
                Log.Warning("[Project Momo] Failed to hook Intimacy — intimacy feeding disabled.\n" + e);
            }
        }

        /// <summary>
        /// Runs after each completed Intimacy sex act. When exactly one participant
        /// is a Momo, she drains enough of her partner's essence to fill her Mana
        /// bar (EssenceTransfer clamps to what the partner can actually give). The
        /// transfer's own lovin' side effects are suppressed: Intimacy has already
        /// applied its thoughts, need gains and cooldowns for this act.
        /// </summary>
        public static void ApplyLovinThoughtsPostfix(Pawn pawn, Pawn partner)
        {
            if (!ProjectMomoModSettings.Settings.IntimacyFeeding)
            {
                return;
            }
            if (pawn == null || partner == null || pawn == partner)
            {
                return;
            }

            Pawn momo = EssenceTransfer.IsMomo(pawn) ? pawn
                : EssenceTransfer.IsMomo(partner) ? partner
                : null;
            if (momo == null)
            {
                return;
            }

            Pawn human = momo == pawn ? partner : pawn;
            if (!EssenceTransfer.CanTransfer(momo, human))
            {
                return;
            }

            // Drink her fill: exactly what the Mana bar is missing. Transfer clamps
            // to the partner's remaining essence, and if the act fires this hook
            // once per participant the second call finds a full bar and moves nothing.
            Need_Mana mana = EssenceTransfer.Mana(momo);
            float deficit = mana.MaxLevel - mana.CurLevel;
            if (deficit <= 0f)
            {
                return;
            }

            EssenceTransfer.Transfer(momo, human, deficit, intimateSideEffects: false);
        }
    }
}
