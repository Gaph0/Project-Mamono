using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// The bond-grief debuff a surviving partner carries after a tsugai bond dies.
    /// It is temporary: on add it sets its own disappear duration from the mod
    /// settings (BondLossMinDays..BondLossMaxDays), so it always fades even though
    /// the XML supplies only a fallback range. It also remembers the partners whose
    /// death caused it, so a resurrection can be matched back to the grieving pawn.
    /// </summary>
    public class Hediff_TsugaiLoss : HediffWithComps
    {
        private const int TicksPerDay = 60000;

        private List<int> lostPartnerIds = new List<int>();

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            HediffComp_Disappears comp = this.TryGetComp<HediffComp_Disappears>();
            if (comp == null)
            {
                return;
            }

            float minDays = Mathf.Min(ProjectMamonoModSettings.Settings.BondLossMinDays, ProjectMamonoModSettings.Settings.BondLossMaxDays);
            float maxDays = Mathf.Max(ProjectMamonoModSettings.Settings.BondLossMinDays, ProjectMamonoModSettings.Settings.BondLossMaxDays);
            float days = Rand.Range(minDays, maxDays);
            comp.SetDuration(Mathf.RoundToInt(days * TicksPerDay));
        }

        /// <summary>Records that <paramref name="partnerId"/>'s death contributed to this grief.</summary>
        public void RecordLostPartner(int partnerId)
        {
            if (!lostPartnerIds.Contains(partnerId))
            {
                lostPartnerIds.Add(partnerId);
            }
        }

        /// <summary>True if this grief was caused (at least in part) by the given partner's death.</summary>
        public bool Grieves(int partnerId)
        {
            return lostPartnerIds.Contains(partnerId);
        }

        /// <summary>A resurrected pawn should not still be grieving a broken bond.</summary>
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            if (pawn?.health != null)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lostPartnerIds, "lostPartnerIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars && lostPartnerIds == null)
            {
                lostPartnerIds = new List<int>();
            }
        }
    }
}
