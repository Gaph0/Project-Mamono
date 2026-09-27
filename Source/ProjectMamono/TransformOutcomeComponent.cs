using System.Collections.Generic;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Carries a newly transformed monster's outcome - a join offer to the colony -
    /// to the next game tick, rather than running it inside the infusion job's toil
    /// cleanup. Changing her faction directly from the finish action swaps the
    /// pawn's CurJob/curDriver while the old JobDriver is still mid-Cleanup, which
    /// corrupts its toil list and throws a NullReferenceException (the same bug
    /// BondOutcomeComponent works around for tsugai bonds). Deferring by a tick
    /// avoids that.
    /// </summary>
    public class TransformOutcomeComponent : GameComponent
    {
        private struct PendingOutcome : IExposable
        {
            public Pawn pawn;
            public Pawn source;
            public bool join;
            public float chance;
            public int sourceLevel;
            public int pawnLevel;
            public int tick;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref source, "source");
                Scribe_Values.Look(ref join, "join");
                Scribe_Values.Look(ref chance, "chance");
                Scribe_Values.Look(ref sourceLevel, "sourceLevel");
                Scribe_Values.Look(ref pawnLevel, "pawnLevel");
                Scribe_Values.Look(ref tick, "tick");
            }
        }

        private List<PendingOutcome> pending = new List<PendingOutcome>();

        public TransformOutcomeComponent(Game game)
        {
        }

        public static void Queue(Pawn pawn, Pawn source, bool join, float chance, int sourceLevel, int pawnLevel)
        {
            if (Current.Game == null)
            {
                return;
            }

            var comp = Current.Game.GetComponent<TransformOutcomeComponent>();
            comp?.pending.Add(new PendingOutcome
            {
                pawn = pawn,
                source = source,
                join = join,
                chance = chance,
                sourceLevel = sourceLevel,
                pawnLevel = pawnLevel,
                tick = Find.TickManager.TicksGame + 1
            });
        }

        public override void GameComponentTick()
        {
            if (pending.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].tick > now)
                {
                    continue;
                }

                PendingOutcome po = pending[i];
                pending.RemoveAt(i);

                if (po.join)
                {
                    MamonoTransformation.ExecuteTransformJoin(po.pawn, po.source, po.chance, po.sourceLevel, po.pawnLevel);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pendingTransformOutcomes", LookMode.Deep);
        }
    }
}
