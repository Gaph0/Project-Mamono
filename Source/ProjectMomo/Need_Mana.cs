using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The Momo "mana" need. Only Momo-carriers have it (NeedDef.onlyIfCausedByGene
    /// + the gene's enablesNeeds). It drains over time. When empty, a Momo suffers a
    /// chemical-dependency-style escalating "mana starvation" decline (a visible
    /// hediff) and, once desperate, is guaranteed to break — independent of her mood.
    /// Feeding (future mechanic) will restore it. A Momo in a caravan does not drain
    /// at all — see the caravan guard in <see cref="NeedInterval"/>.
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

            // A caravan member is ticked like any other alive world pawn (WorldPawns.WorldPawnsTick
            // calls Thing.DoTick on her), so without this guard her mana drains in transit exactly as
            // it does on a map — about half a bar a day of travelling. Nothing can feed her out
            // there, so the whole interval is frozen instead: no drain, no starvation timer, no break
            // roll, resuming the moment she spawns again. Vanilla's Need.IsFrozen counts caravan
            // members as live on purpose, so this guard has to be ours.
            if (pawn != null && CaravanUtility.IsCaravanMember(pawn))
            {
                return;
            }

            float fall = FallPerTick * 150f * DrainMultiplier(pawn);

            // A visiting guest never starves: her passive drain stops at the guest floor (a fifth
            // of a bar by default) instead of running to empty. Her stay is the visiting mod's
            // decision and she cannot leave it early, so a visit longer than one bar lasts - eight
            // days at the default drain - would otherwise leave her starving. Only the drain is
            // floored: nothing is added back, so a guest who spends mana still spends it, and a
            // guest already below the floor simply stops draining.
            if (EssenceTransfer.IsVisitingGuest(pawn))
            {
                fall = UnityEngine.Mathf.Min(fall, UnityEngine.Mathf.Max(0f, CurLevel - ProjectMomoModSettings.Settings.GuestManaFloor));
            }

            CurLevel -= fall;

            UpdateStarvation();
            LowManaBreak.CheckBreak(pawn, CurLevel);
        }

        /// <summary>
        /// This pawn's mana drain as a fraction of the normal rate: wild Momo and visiting guests
        /// both drain slower than a colonist, and the Isekai mod's WIS/INT conservation applies on
        /// top. Split out of NeedInterval so anything that reasons about a pawn's drain uses the
        /// same number the need does.
        /// </summary>
        public static float DrainMultiplier(Pawn pawn)
        {
            if (pawn == null)
            {
                return 1f;
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

            // Gear: a metal that wards mana (dragonium) slows the drain while it is
            // worn. PMM_ManaDrain is a FACTOR stat fed from worn apparel, so a full
            // set drains slower than any one piece on its own.
            drainMultiplier *= pawn.GetStatValue(ProjectMomo_DefOf.PMM_ManaDrain);

            return drainMultiplier;
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
