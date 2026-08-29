using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Relation worker for the tsugai bond. Provides InRelation so the relation is
    /// recognised, and a baseline generation chance (unused — bonds form via
    /// knockout, not pawn generation).
    /// </summary>
    public class PawnRelationWorker_Tsugai : PawnRelationWorker
    {
        public override bool InRelation(Pawn me, Pawn other)
        {
            if (me == null || other == null)
            {
                return false;
            }

            return me.relations != null
                && me.relations.DirectRelationExists(def, other);
        }

        public override float GenerationChance(Pawn generated, Pawn other, PawnGenerationRequest request)
        {
            return 0f;
        }
    }
}
