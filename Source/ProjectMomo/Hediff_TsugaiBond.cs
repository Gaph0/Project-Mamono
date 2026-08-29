using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The tsugai bond marker. Each bond the pawn forms is recorded individually
    /// (partner id, name, and date), so multiple bonds stack cleanly — the willpower
    /// bonus scales with the number of recorded bonds. Severity itself stays fixed
    /// at 1; it is not used as a counter.
    /// </summary>
    public class Hediff_TsugaiBond : HediffWithComps
    {
        /// <summary>One recorded tsugai bond. Ids (not pawn references) are stored so the record survives the partner leaving the map or dying.</summary>
        public class BondRecord : IExposable
        {
            public int partnerId;
            public string partnerName;
            public int formedTick = -1;

            public void ExposeData()
            {
                Scribe_Values.Look(ref partnerId, "partnerId");
                Scribe_Values.Look(ref partnerName, "partnerName");
                Scribe_Values.Look(ref formedTick, "formedTick", -1);
            }
        }

        private List<BondRecord> bonds = new List<BondRecord>();

        // Stacks migrated from the old severity-as-counter saves (pre individual tracking).
        private int legacyStacks;

        /// <summary>Number of bonds this pawn carries (at least 1 while the hediff exists).</summary>
        public int BondCount => Mathf.Max(1, bonds.Count + legacyStacks);

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Severity = 1f;
        }

        /// <summary>
        /// A dead pawn should never keep the bond marker — the corpse has no living
        /// bond, and the marker must not persist through a resurrection. The partner's
        /// side (severing the relation, their grief) is handled separately by
        /// TsugaiFormation.HandleBondPartnerDied.
        /// </summary>
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (pawn?.health != null)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        /// <summary>Records a new bond with the given partner, respecting the stack cap.</summary>
        public void AddBond(Pawn partner)
        {
            if (partner == null)
            {
                return;
            }

            int cap = ProjectMomoModSettings.Settings.BondMaxStacks;
            if (cap < 1)
            {
                cap = 1;
            }
            if (bonds.Count + legacyStacks >= cap)
            {
                return;
            }

            bonds.Add(new BondRecord
            {
                partnerId = partner.thingIDNumber,
                partnerName = partner.Name?.ToStringShort ?? partner.LabelShort,
                formedTick = Find.TickManager.TicksGame
            });
        }

        /// <summary>Adds the bond marker to <paramref name="pawn"/> (or reuses the existing one) and records the bond to <paramref name="partner"/>.</summary>
        public static void AddBondTo(Pawn pawn, Pawn partner)
        {
            if (pawn?.health == null || partner == null)
            {
                return;
            }

            if (!(pawn.health.hediffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TsugaiBond) is Hediff_TsugaiBond bond))
            {
                bond = pawn.health.AddHediff(ProjectMomo_DefOf.ProjectMomo_TsugaiBond) as Hediff_TsugaiBond;
            }
            bond?.AddBond(partner);
        }

        /// <summary>
        /// Removes only the bond to <paramref name="partner"/> from <paramref name="pawn"/>,
        /// keeping every other living bond. The hediff is removed outright only when no
        /// bonds remain. Use this (not RemoveHediff) when one partner of a harem dies, so
        /// the survivor keeps the rest of their bonds.
        /// </summary>
        public static void RemoveBondFrom(Pawn pawn, Pawn partner)
        {
            if (pawn?.health == null || partner == null)
            {
                return;
            }

            if (!(pawn.health.hediffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TsugaiBond) is Hediff_TsugaiBond bond))
            {
                return;
            }

            bond.bonds.RemoveAll(b => b.partnerId == partner.thingIDNumber);

            // Only drop the hediff entirely once no bonds (recorded or legacy) remain.
            if (bond.bonds.Count == 0 && bond.legacyStacks == 0)
            {
                pawn.health.RemoveHediff(bond);
            }
        }

        /// <summary>Total tsugai bonds on the pawn, or 0 if it has none.</summary>
        public static int CountBonds(HediffSet diffSet)
        {
            if (diffSet == null)
            {
                return 0;
            }
            if (diffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TsugaiBond) is Hediff_TsugaiBond bond)
            {
                return bond.BondCount;
            }
            return 0;
        }

        public override string LabelInBrackets => BondCount > 1 ? $"x{BondCount}" : base.LabelInBrackets;

        public override string TipStringExtra
        {
            get
            {
                if (bonds.Count == 0)
                {
                    return base.TipStringExtra;
                }

                StringBuilder sb = new StringBuilder();
                if (!base.TipStringExtra.NullOrEmpty())
                {
                    sb.AppendLine(base.TipStringExtra);
                }
                sb.AppendLine("Bonded to:");
                foreach (BondRecord bond in bonds)
                {
                    string date = bond.formedTick >= 0 ? GenDate.DateFullStringAt(bond.formedTick, Find.WorldGrid.LongLatOf(pawn?.MapHeld?.Tile ?? 0)) : null;
                    sb.AppendLine(date != null ? $"  - {bond.partnerName} ({date})" : $"  - {bond.partnerName}");
                }
                return sb.ToString().TrimEndNewlines();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref bonds, "bonds", LookMode.Deep);
            Scribe_Values.Look(ref legacyStacks, "legacyStacks", 0);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (bonds == null)
                {
                    bonds = new List<BondRecord>();
                }

                // Migrate old saves where severity itself was the stack counter.
                int severityCount = Mathf.RoundToInt(Severity);
                if (severityCount > 1 && bonds.Count == 0 && legacyStacks == 0)
                {
                    legacyStacks = severityCount - 1;
                }
                if (Severity != 1f)
                {
                    Severity = 1f;
                }
            }
        }
    }
}
