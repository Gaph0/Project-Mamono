using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// "Blood rush" (PMM_Gene_BloodRush): the gene's speed gifts only apply while
    /// her blood is high, so the buff has to be read at stat time rather than
    /// written on the gene.
    ///
    /// Why a class and not XML: vanilla ships ConditionalStatAffecter for light,
    /// clothing, pain, vacuum, pollution, temperature and a few others - and NOT
    /// for any resource. A gene can hold a conditional affecter in its
    /// `conditionalStatAffecters` list, so the hook exists, but the condition has
    /// to be ours. That is this class; the stats themselves stay in the XML on the
    /// affecter, exactly as vanilla's own ones do.
    ///
    /// The threshold is 0.75, the same one Hemogenic paints on its own gizmo, so
    /// the bar the player watches and the bar the gene reads are the same bar.
    /// Hemogen is a 0..1 resource (see the vanilla defs: resourceLossPerDay 0.08 is
    /// described as "an additional 8 hemogen per day"), hence the literal.
    ///
    /// The gene is also the check that the pawn HAS it: an affecter listed on a gene
    /// is only consulted while that gene is active, but a belt-and-braces test costs
    /// nothing and keeps the class safe if it is ever reused elsewhere.
    /// </summary>
    public class ConditionalStatAffecter_HemogenHigh : ConditionalStatAffecter
    {
        private const float Threshold = 0.75f;

        public override bool Applies(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !(pawn.genes?.HasActiveGene(ProjectMamono_DefOf.PMM_Gene_BloodRush) ?? false))
            {
                return false;
            }

            Gene_Hemogen hemogen = pawn.genes.GetFirstGeneOfType<Gene_Hemogen>();
            return hemogen != null && hemogen.Value >= Threshold;
        }

        /// <summary>
        /// Deliberately EMPTY. The engine appends this in brackets wherever it
        /// shows the affecter's contribution - the pawn's Stats tab reads
        /// "Blood rush (while her hemogen is 75% or more): x115%" with a label, and
        /// the user wanted the bare "Blood rush: x115%" there (2026-09-27).
        ///
        /// The 75% threshold therefore lives in the gene's description instead,
        /// which is where the family's prose belongs anyway (see the gene's own
        /// comment). If it turns out the engine prints empty brackets for an empty
        /// label, the fallback is a very short label such as "75%+" - one line.
        ///
        /// `Label` is the abstract member in 1.6; there is no ExplanationPart.
        /// Vanilla's own affecters return Keyed translation keys here ("in
        /// sunlight", "clothed"); a literal is used because the family ships
        /// English only.
        /// </summary>
        public override string Label => string.Empty;
    }
}
