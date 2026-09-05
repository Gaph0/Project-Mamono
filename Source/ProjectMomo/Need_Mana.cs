using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The Momo "mana" need. Only Momo-carriers have it (NeedDef.onlyIfCausedByGene
    /// + the gene's enablesNeeds). It drains over time. When empty, a Momo suffers a
    /// chemical-dependency-style escalating "mana starvation" decline (a visible
    /// hediff) and, once desperate, is guaranteed to break — independent of her mood.
    /// Feeding (future mechanic) will restore it.
    /// </summary>
    public class Need_Mana : Need
    {
        // Full drain over ~2 days (120000 ticks).
        private const float FallPerTick = 1f / 120000f;

        // Severity at which the Momo is "desperate" and immediately breaks.
        private const float DesperateSeverity = 0.6f;

        private int ticksStarved;

        // Whether the desperate break has already fired for the current starvation
        // episode — the break is guaranteed, but only once per episode (reset when fed).
        private bool desperateTriggered;

        public Need_Mana(Pawn pawn) : base(pawn)
        {
            threshPercents = new System.Collections.Generic.List<float> { 0.15f, 0.4f };
        }

        public bool IsEmpty => CurLevel <= 0.001f;

        public override void NeedInterval()
        {
            if (IsFrozen)
            {
                return;
            }

            // ISEKAI: the average of WIS and INT slows mana drain.
            float drainMultiplier = IsekaiCompat.ManaConservationMultiplier(pawn);

            // Wild Momo (wild men) drain far slower than tamed/colonist Momo, so a slime or
            // other Momo roaming the map doesn't starve at the same rate as a colony member.
            if (pawn.IsWildMan())
            {
                drainMultiplier *= ProjectMomoModSettings.Settings.WildManaDrainFactor;
            }
            // Visiting Momo (guests of a non-hostile faction) drain far slower too: a visit
            // is short, and a starving guest going berserk on her hosts' sleeping colonists
            // is the exact scenario this avoids. Raiders (hostile factions) drain normally.
            else if (EssenceTransfer.IsVisitingGuest(pawn))
            {
                drainMultiplier *= ProjectMomoModSettings.Settings.GuestManaDrainFactor;
            }

            CurLevel -= FallPerTick * 150f * drainMultiplier;

            UpdateStarvation();
            LowManaBreak.CheckBreak(pawn, CurLevel);
        }

        private void UpdateStarvation()
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            var settings = ProjectMomoModSettings.Settings;

            if (IsEmpty)
            {
                ticksStarved += 150;

                // Chemical-style escalating decline: severity grows toward 1 over
                // StarvationDaysToMax days of being empty.
                float daysStarved = ticksStarved / 60000f;
                float severity = UnityEngine.Mathf.Clamp01(daysStarved / UnityEngine.Mathf.Max(0.01f, settings.StarvationDaysToMax));

                Hediff starvation = pawn.health?.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_ManaStarvation);
                if (starvation == null)
                {
                    starvation = HediffMaker.MakeHediff(ProjectMomo_DefOf.ProjectMomo_ManaStarvation, pawn);
                    starvation.Severity = severity;
                    pawn.health.AddHediff(starvation);
                }
                else
                {
                    starvation.Severity = severity;
                }

                // Once desperate, immediately suffer the feeding/berserk break —
                // guaranteed, once per starvation episode, regardless of mood. This is
                // the same break the low-mana threshold triggers, so a bonded Momo feeds
                // from her mate rather than only ever going berserk.
                if (severity >= DesperateSeverity && !desperateTriggered && pawn.Spawned && !pawn.Downed && !pawn.InMentalState)
                {
                    desperateTriggered = true;
                    LowManaBreak.Trigger(pawn);
                    // If the state somehow failed to start (forced starts essentially
                    // never fail), clear the flag so the next interval retries.
                    if (!pawn.InMentalState)
                    {
                        desperateTriggered = false;
                    }
                }
            }
            else if (ticksStarved > 0)
            {
                // Fed again: clear the decline and reset the timer.
                ticksStarved = 0;
                desperateTriggered = false;
                Hediff starvation = pawn.health?.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_ManaStarvation);
                if (starvation != null)
                {
                    pawn.health.RemoveHediff(starvation);
                }
            }
        }

        /// <summary>
        /// Called when a bonded Momo loses her partner mid-feeding. Clears the
        /// one-shot desperate-break lock so the starvation code can re-evaluate
        /// and berserk/flee on the next need interval.
        /// </summary>
        public void ResetDesperateTriggered()
        {
            desperateTriggered = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksStarved, "ticksStarved", 0);
            Scribe_Values.Look(ref desperateTriggered, "desperateTriggered", false);
        }
    }
}
