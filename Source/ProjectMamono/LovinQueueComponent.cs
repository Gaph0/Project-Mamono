using System.Collections.Generic;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Defers starting the vanilla lovin' job to the next game tick. EssenceTransfer
    /// runs inside the Drain/Give job's finish action (toil Cleanup); calling
    /// jobs.StartJob there interrupts the very job being cleaned up and throws a
    /// NullReferenceException in Pawn_JobTracker.CleanupCurrentJob. Queuing the
    /// lovin' start one tick out avoids that, exactly like BondOutcomeComponent does
    /// for the bond outcome.
    /// </summary>
    public class LovinQueueComponent : GameComponent
    {
        private struct PendingLovin : IExposable
        {
            public Pawn mamono;
            public Pawn human;
            public int tick;

            public void ExposeData()
            {
                Scribe_References.Look(ref mamono, "mamono");
                Scribe_References.Look(ref human, "human");
                Scribe_Values.Look(ref tick, "tick");
            }
        }

        private List<PendingLovin> pending = new List<PendingLovin>();

        public LovinQueueComponent(Game game)
        {
        }

        public static void Queue(Pawn mamono, Pawn human)
        {
            if (Current.Game == null)
            {
                return;
            }

            var comp = Current.Game.GetComponent<LovinQueueComponent>();
            comp?.pending.Add(new PendingLovin
            {
                mamono = mamono,
                human = human,
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

                PendingLovin pl = pending[i];
                pending.RemoveAt(i);
                EssenceTransfer.StartLovinNow(pl.mamono, pl.human);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pendingLovin", LookMode.Deep);
        }
    }
}
