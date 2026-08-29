using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The broken-will knockout marker. The hediff is added by TeaseKnockout.Evaluate
    /// when willpower hits the floor, but natural tease recovery does NOT reliably
    /// re-run Evaluate: tease severity fades through HediffComp_SeverityPerDay, which
    /// adjusts severity via a ref parameter (not the Severity property setter the
    /// TeaseSeverityPatch hooks), and the tease hediff is removed outright at min
    /// severity. So the knockout would never be re-evaluated and this hediff would
    /// linger forever. Ticking here lets the broken-will marker remove itself once
    /// the victim's willpower climbs back above the knockout threshold.
    /// </summary>
    public class Hediff_WillpowerBreak : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // Rare-interval re-check is plenty (willpower recovers over days, not ticks).
            if (pawn != null && pawn.IsHashIntervalTick(250, delta))
            {
                TeaseKnockout.Evaluate(pawn);
            }
        }
    }
}
