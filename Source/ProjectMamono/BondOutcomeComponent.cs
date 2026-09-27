using System.Collections.Generic;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Carries a bonded Mamono's outcome - joining the colony or kidnapping her
    /// husband - to the next game tick, rather than running it inside the bonding
    /// job's toil cleanup. Changing her faction or starting a new job directly from
    /// the finish action swaps the pawn's CurJob/curDriver while the old JobDriver
    /// is still mid-Cleanup, which corrupts its toil list and throws a
    /// NullReferenceException. Deferring by a tick avoids that.
    /// </summary>
    public class BondOutcomeComponent : GameComponent
    {
        private struct PendingOutcome : IExposable
        {
            public Pawn mamono;
            public Pawn man;
            public bool join;
            public float chance;
            public int manLevel;
            public int mamonoLevel;
            public int tick;

            public void ExposeData()
            {
                Scribe_References.Look(ref mamono, "mamono");
                Scribe_References.Look(ref man, "man");
                Scribe_Values.Look(ref join, "join");
                Scribe_Values.Look(ref chance, "chance");
                Scribe_Values.Look(ref manLevel, "manLevel");
                Scribe_Values.Look(ref mamonoLevel, "mamonoLevel");
                Scribe_Values.Look(ref tick, "tick");
            }
        }

        private List<PendingOutcome> pending = new List<PendingOutcome>();

        public BondOutcomeComponent(Game game)
        {
        }

        public static void Queue(Pawn mamono, Pawn man, bool join, float chance, int manLevel, int mamonoLevel)
        {
            if (Current.Game == null)
            {
                return;
            }

            var comp = Current.Game.GetComponent<BondOutcomeComponent>();
            comp?.pending.Add(new PendingOutcome
            {
                mamono = mamono,
                man = man,
                join = join,
                chance = chance,
                manLevel = manLevel,
                mamonoLevel = mamonoLevel,
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
                    TsugaiFormation.ExecuteJoin(po.mamono, po.man, po.chance, po.manLevel, po.mamonoLevel);
                }
                else
                {
                    TsugaiFormation.ExecuteKidnap(po.mamono, po.man);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pendingBondOutcomes", LookMode.Deep);
        }
    }
}
