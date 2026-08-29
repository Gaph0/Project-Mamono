using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Persistent cooldown bookkeeping for voluntary (consensual) transformation
    /// proposals: per-pawn scan throttling (keeps the think-tree giver cheap),
    /// per-pawn attempt cooldowns, per-pair rejection cooldowns, and short
    /// reservations so two Momos never walk to the same woman at once. Follows the
    /// same auto-added GameComponent pattern as VoluntaryBondComponent. Expired
    /// entries are purged once a day so long games don't accumulate dead pawn ids.
    /// </summary>
    public class TransformProposalComponent : GameComponent
    {
        private Dictionary<int, int> nextScanTick = new Dictionary<int, int>();
        private Dictionary<int, int> nextAttemptTick = new Dictionary<int, int>();
        private Dictionary<int, int> targetReservedUntilTick = new Dictionary<int, int>();
        private Dictionary<long, int> pairRejectedUntilTick = new Dictionary<long, int>();
        private int lastPurgeTick;

        public TransformProposalComponent(Game game)
        {
        }

        /// <summary>The component for the current game, or null on the main menu.</summary>
        public static TransformProposalComponent Get()
        {
            return Current.Game?.GetComponent<TransformProposalComponent>();
        }

        private static long PairKey(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }

        /// <summary>True when the pawn's next candidate scan is due (throttles the expensive map scan).</summary>
        public bool CanScanNow(Pawn pawn, int now)
        {
            return pawn != null && (!nextScanTick.TryGetValue(pawn.thingIDNumber, out int tick) || now >= tick);
        }

        public void NoteScan(Pawn pawn, int nextTick)
        {
            if (pawn != null)
            {
                nextScanTick[pawn.thingIDNumber] = nextTick;
            }
        }

        /// <summary>True when the pawn is past the cooldown from their last proposal attempt.</summary>
        public bool CanAttemptNow(Pawn pawn, int now)
        {
            return pawn != null && (!nextAttemptTick.TryGetValue(pawn.thingIDNumber, out int tick) || now >= tick);
        }

        public void NoteAttempt(Pawn pawn, int nextTick)
        {
            if (pawn != null)
            {
                nextAttemptTick[pawn.thingIDNumber] = nextTick;
            }
        }

        /// <summary>True while another Momo is walking to (or transforming) this woman.</summary>
        public bool IsReserved(Pawn target, int now)
        {
            return target != null
                && targetReservedUntilTick.TryGetValue(target.thingIDNumber, out int tick)
                && now < tick;
        }

        public void ReserveTarget(Pawn target, int untilTick)
        {
            if (target != null)
            {
                targetReservedUntilTick[target.thingIDNumber] = untilTick;
            }
        }

        /// <summary>True while a rejection cooldown between these two pawns is still running (either direction).</summary>
        public bool PairCoolingDown(Pawn a, Pawn b, int now)
        {
            if (a == null || b == null)
            {
                return false;
            }
            return pairRejectedUntilTick.TryGetValue(PairKey(a.thingIDNumber, b.thingIDNumber), out int tick) && now < tick;
        }

        public void NoteRejection(Pawn a, Pawn b, int untilTick)
        {
            if (a != null && b != null)
            {
                pairRejectedUntilTick[PairKey(a.thingIDNumber, b.thingIDNumber)] = untilTick;
            }
        }

        /// <summary>Drops expired entries once a day so dead pawn ids don't accumulate.</summary>
        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now - lastPurgeTick < GenDate.TicksPerDay)
            {
                return;
            }
            lastPurgeTick = now;

            PurgeExpired(nextScanTick, now);
            PurgeExpired(nextAttemptTick, now);
            PurgeExpired(targetReservedUntilTick, now);
            PurgeExpiredPairs(now);
        }

        private static void PurgeExpired(Dictionary<int, int> dict, int now)
        {
            if (dict.Count == 0)
            {
                return;
            }
            purgeBuffer.Clear();
            foreach (KeyValuePair<int, int> kv in dict)
            {
                if (kv.Value < now)
                {
                    purgeBuffer.Add(kv.Key);
                }
            }
            for (int i = 0; i < purgeBuffer.Count; i++)
            {
                dict.Remove(purgeBuffer[i]);
            }
        }

        private void PurgeExpiredPairs(int now)
        {
            if (pairRejectedUntilTick.Count == 0)
            {
                return;
            }
            purgeBufferLong.Clear();
            foreach (KeyValuePair<long, int> kv in pairRejectedUntilTick)
            {
                if (kv.Value < now)
                {
                    purgeBufferLong.Add(kv.Key);
                }
            }
            for (int i = 0; i < purgeBufferLong.Count; i++)
            {
                pairRejectedUntilTick.Remove(purgeBufferLong[i]);
            }
        }

        private static readonly List<int> purgeBuffer = new List<int>();
        private static readonly List<long> purgeBufferLong = new List<long>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref nextScanTick, "nextScanTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref nextAttemptTick, "nextAttemptTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref targetReservedUntilTick, "targetReservedUntilTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pairRejectedUntilTick, "pairRejectedUntilTick", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref lastPurgeTick, "lastPurgeTick", 0);

            // Null-guard after load: the dictionaries must never be null.
            if (nextScanTick == null)
            {
                nextScanTick = new Dictionary<int, int>();
            }
            if (nextAttemptTick == null)
            {
                nextAttemptTick = new Dictionary<int, int>();
            }
            if (targetReservedUntilTick == null)
            {
                targetReservedUntilTick = new Dictionary<int, int>();
            }
            if (pairRejectedUntilTick == null)
            {
                pairRejectedUntilTick = new Dictionary<long, int>();
            }
        }
    }
}
