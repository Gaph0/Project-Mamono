using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The mamono-corruption progress hediff: a Momo's mana slowly rewriting a
    /// woman into a monster. Severity grows only through completed mana infusions
    /// (MomoTransformation.ApplyInfusion); while the victim is on her feet she
    /// fights the corruption off and it decays (CorruptionDecayPerDay), so
    /// partial corruption is reversible. At full severity the transformation
    /// fires through MomoTransformation.ApplyXenotype and this hediff removes
    /// itself.
    ///
    /// Last-corruptor-wins: the hediff remembers the xenotype imprinted by the
    /// most recent infuser. A different-xenotype Momo infusing a half-corrupted
    /// woman overwrites the imprint (severity is kept — the corruption is
    /// mana-type-agnostic, only the imprint changes), so whoever finishes the
    /// corruption decides what she becomes.
    /// </summary>
    public class Hediff_MomoCorruption : HediffWithComps
    {
        // defName of the xenotype the most recent infuser imprinted (null/invalid -> base Momo).
        private string targetXenotypeDefName;

        // The Momo who most recently infused her (drives messages now, join offers later).
        private Pawn source;

        /// <summary>The Momo whose mana most recently infused the victim.</summary>
        public Pawn Source => source;

        /// <summary>The xenotype she will become when the corruption completes.</summary>
        public XenotypeDef TargetXenotype
        {
            get
            {
                XenotypeDef def = targetXenotypeDefName != null
                    ? DefDatabase<XenotypeDef>.GetNamedSilentFail(targetXenotypeDefName)
                    : null;
                if (MomoTransformation.IsMonsterXenotype(def))
                {
                    return def;
                }
                return ProjectMomo_DefOf.ProjectMomo_Xenotype_Momo;
            }
        }

        /// <summary>Imprints the infuser's xenotype (overwriting any previous one) and remembers her.</summary>
        public void Imprint(Pawn infuser)
        {
            source = infuser;
            targetXenotypeDefName = MomoTransformation.XenotypeFor(infuser)?.defName;
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // Rare-interval re-check is plenty (corruption moves over days, not ticks).
            if (pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250, delta))
            {
                return;
            }

            // Completion is normally fired by the infusion job the moment the last
            // dose lands; this re-check catches dev-mode severity edits.
            if (Severity >= def.maxSeverity - 0.0001f)
            {
                CompleteTransformation();
                return;
            }

            // An upright woman fights the corruption off; a downed one cannot.
            // Decay is driven by being downed, not by tease-driven willpower:
            // tease fades in hours while a gunshot keeps her down for days, and
            // any downed woman is infusable (see CorruptionFloatMenuPatch).
            if (!pawn.Downed)
            {
                float decayed = Severity - ProjectMomoModSettings.Settings.CorruptionDecayPerDay * (250f / 60000f);
                if (decayed <= 0.001f)
                {
                    // She shook the corruption off entirely.
                    pawn.health.RemoveHediff(this);
                }
                else
                {
                    Severity = decayed;
                }
            }
        }

        /// <summary>
        /// Fires the transformation and cleans up, whatever the outcome. If the
        /// guards refuse (e.g. she was transformed by other means in the meantime),
        /// the hediff is removed anyway so a stale full-severity corruption can
        /// never sit there re-firing — the same self-resolving pattern as
        /// Hediff_WillpowerBreak.
        /// </summary>
        public void CompleteTransformation()
        {
            Pawn victim = pawn;
            if (victim == null || victim.Dead)
            {
                return;
            }

            // ApplyXenotype removes this hediff on success.
            if (!MomoTransformation.ApplyXenotype(victim, TargetXenotype, source) && victim.health != null)
            {
                victim.health.RemoveHediff(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetXenotypeDefName, "targetXenotypeDefName");
            Scribe_References.Look(ref source, "source");
        }
    }
}
