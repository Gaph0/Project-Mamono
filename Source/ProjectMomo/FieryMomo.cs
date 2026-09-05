using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Fire immunity for fiery momos, and the lesser flame resist on their
    /// bonded partners. Postfixes Thing.PreApplyDamage — the single choke point
    /// every damage passes through before armour — and checks the Heat armour
    /// category rather than a def list, so fire, burns and any modded lava
    /// damage (which sensibly uses the Heat category) are all covered at once.
    /// A fiery-momo carrier takes nothing; a fire-warded bond partner takes
    /// the configured fraction. Ignition itself never reaches this patch for
    /// the carrier: her gene's Flammability x0 stat factor makes vanilla
    /// fire-attach refuse outright.
    /// </summary>
    [HarmonyPatch(typeof(Thing), "PreApplyDamage")]
    public static class FireImmunityPatch
    {
        // DamageArmorCategoryDefOf has no Heat field in this game version, so
        // resolve the Heat armour category by name. The static ctor runs long
        // after def load (first damage tick at the earliest), so GetNamed is safe.
        private static readonly DamageArmorCategoryDef HeatArmorCategory =
            DefDatabase<DamageArmorCategoryDef>.GetNamed("Heat");

        public static void Postfix(Thing __instance, ref DamageInfo dinfo)
        {
            if (!(__instance is Pawn pawn) || pawn.Dead)
            {
                return;
            }
            if (dinfo.Def?.armorCategory != HeatArmorCategory)
            {
                return;
            }

            if (pawn.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_MomoFiery) ?? false)
            {
                // SetAmount(0) also covers the -1 "use default damage" case:
                // 0 is unambiguous no-damage.
                dinfo.SetAmount(0f);
                return;
            }

            if ((pawn.health?.hediffSet?.HasHediff(ProjectMomo_DefOf.ProjectMomo_FieryBondWard) ?? false)
                && dinfo.Amount > 0f)
            {
                dinfo.SetAmount(dinfo.Amount * ProjectMomoModSettings.Settings.FieryWardFlameFactor);
            }
        }
    }

    /// <summary>
    /// Grants and strips the fire-ward hediff, once per in-game hour. A pawn is
    /// warded while they have a living tsugai partner carrying the fiery-momo
    /// gene — resolved live from the tsugai relation (the same pattern the
    /// genie's BondedMaster uses), so the ward unlocks the moment the bond
    /// forms and drops again when the partner dies. Implemented as a polled
    /// GameComponent rather than a self-managing hediff comp: a comp must
    /// never remove its own hediff mid-HealthTickInterval (vanilla foreach-
    /// enumerates hediffs there), and an hourly sweep keeps every edge case —
    /// bond formed, bond broken, partner died — in one place.
    /// </summary>
    public class FireWardComponent : GameComponent
    {
        private const int ScanIntervalTicks = 2500; // one in-game hour

        public FireWardComponent(Game game)
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % ScanIntervalTicks != 0)
            {
                return;
            }
            RefreshWards();
        }

        private static void RefreshWards()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                if (pawn?.health?.hediffSet == null || pawn.RaceProps?.Humanlike != true)
                {
                    continue;
                }

                Hediff ward = pawn.health.hediffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_FieryBondWard);
                if (IsBondedToFieryMomo(pawn))
                {
                    if (ward == null)
                    {
                        pawn.health.AddHediff(ProjectMomo_DefOf.ProjectMomo_FieryBondWard);
                    }
                }
                else if (ward != null)
                {
                    pawn.health.RemoveHediff(ward);
                }
            }
        }

        private static bool IsBondedToFieryMomo(Pawn pawn)
        {
            if (pawn.relations == null)
            {
                return false;
            }
            return pawn.relations.GetFirstDirectRelationPawn(
                ProjectMomo_DefOf.ProjectMomo_Tsugai,
                partner => partner != null && !partner.Dead
                    && (partner.genes?.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_MomoFiery) ?? false)) != null;
        }
    }
}
