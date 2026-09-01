using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Persistent bookkeeping for autonomous mana feeding: per-pawn scan
    /// throttling (keeps the think-tree giver's reachability check cheap),
    /// per-pawn attempt cooldowns (so a Momo whose mate is dry — or whose feed
    /// was interrupted — doesn't re-issue the job every think tick), and a
    /// per-day cap on natural feeds so a hungry Momo doesn't empty her mate
    /// over and over. Follows the same auto-added GameComponent pattern as
    /// TransformProposalComponent. Expired entries are purged once a day so
    /// long games don't accumulate dead pawn ids.
    /// </summary>
    public class ManaFeedingComponent : GameComponent
    {
        private Dictionary<int, int> nextScanTick = new Dictionary<int, int>();
        private Dictionary<int, int> nextAttemptTick = new Dictionary<int, int>();
        private Dictionary<int, int> feedCount = new Dictionary<int, int>();
        private Dictionary<int, int> feedDay = new Dictionary<int, int>();
        private int lastPurgeTick;

        public ManaFeedingComponent(Game game)
        {
        }

        /// <summary>The component for the current game, or null on the main menu.</summary>
        public static ManaFeedingComponent Get()
        {
            return Current.Game?.GetComponent<ManaFeedingComponent>();
        }

        /// <summary>True when the pawn's next partner scan is due (throttles the expensive reachability check).</summary>
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

        /// <summary>True when the pawn is past the cooldown from their last feed attempt.</summary>
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

        /// <summary>True while the pawn has feeds left before hitting the per-day cap (0 = uncapped).</summary>
        public bool UnderDailyCap(Pawn pawn, int cap, int now)
        {
            if (pawn == null || cap <= 0)
            {
                return true;
            }
            if (feedDay.TryGetValue(pawn.thingIDNumber, out int day) && day != now / GenDate.TicksPerDay)
            {
                // Stale count from a previous day.
                return true;
            }
            return !feedCount.TryGetValue(pawn.thingIDNumber, out int count) || count < cap;
        }

        /// <summary>Counts one completed natural feed toward the pawn's daily cap.</summary>
        public void NoteFeed(Pawn pawn, int now)
        {
            if (pawn == null)
            {
                return;
            }
            int today = now / GenDate.TicksPerDay;
            if (!feedDay.TryGetValue(pawn.thingIDNumber, out int day) || day != today)
            {
                // First feed of a new day: reset the counter.
                feedDay[pawn.thingIDNumber] = today;
                feedCount[pawn.thingIDNumber] = 1;
                return;
            }
            feedCount[pawn.thingIDNumber] = feedCount.TryGetValue(pawn.thingIDNumber, out int count) ? count + 1 : 1;
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

            // Feed counts from days gone by are dead weight: drop them.
            int today = now / GenDate.TicksPerDay;
            if (feedDay.Count > 0)
            {
                purgeBuffer.Clear();
                foreach (KeyValuePair<int, int> kv in feedDay)
                {
                    if (kv.Value < today)
                    {
                        purgeBuffer.Add(kv.Key);
                    }
                }
                for (int i = 0; i < purgeBuffer.Count; i++)
                {
                    feedDay.Remove(purgeBuffer[i]);
                    feedCount.Remove(purgeBuffer[i]);
                }
            }
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

        private static readonly List<int> purgeBuffer = new List<int>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref nextScanTick, "nextScanTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref nextAttemptTick, "nextAttemptTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref feedCount, "feedCount", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref feedDay, "feedDay", LookMode.Value, LookMode.Value);
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
            if (feedCount == null)
            {
                feedCount = new Dictionary<int, int>();
            }
            if (feedDay == null)
            {
                feedDay = new Dictionary<int, int>();
            }
        }
    }
}
