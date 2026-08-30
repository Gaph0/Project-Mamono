using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The incubisation progress hediff: a Momo's mana slowly rewriting a man
    /// into an incubus. Severity grows only through intimate essence transfers
    /// (Incubisation.ApplyDose), is daily-capped, and is permanent by default —
    /// incubisation has no completion event and never removes itself; each
    /// severity stage simply grants more of the incubus' perks. At full severity
    /// the one-time completion effects fire (age ailments stripped, awakening
    /// memories, player notification).
    ///
    /// Last-claimant-wins: the hediff remembers the most recent Momo whose mana
    /// infused him and her xenotype. Her mark protects him from other unbonded
    /// Momos' feeding (Incubisation.MarkProtects).
    /// </summary>
    public class Hediff_Incubisation : HediffWithComps
    {
        // Epsilon for "full severity" comparisons (same tolerance as the corruption path).
        private const float FullEpsilon = 0.0001f;

        // The Momo whose mana most recently infused him (her mark protects him).
        private Pawn source;

        // defName of her monster xenotype at imprint time (reserved for the
        // environmental-adaptation hook — a slime's mate will breathe differently
        // than a lava-dweller's).
        private string sourceXenotypeDefName;

        // Progress applied today, and the day that counter belongs to (daily cap).
        private float doseToday;
        private int lastDoseDay = -1;

        // Completion effects (letter, memories, age-ailment sweep) fire exactly once.
        private bool completed;

        /// <summary>The Momo whose mana most recently infused him — his current claim.</summary>
        public Pawn Source => source;

        /// <summary>The monster xenotype his progress is attuned to (base Momo when unknown).</summary>
        public XenotypeDef SourceXenotype
        {
            get
            {
                XenotypeDef def = sourceXenotypeDefName != null
                    ? DefDatabase<XenotypeDef>.GetNamedSilentFail(sourceXenotypeDefName)
                    : null;
                if (MomoTransformation.IsMonsterXenotype(def))
                {
                    return def;
                }
                return ProjectMomo_DefOf.ProjectMomo_Xenotype_Momo;
            }
        }

        /// <summary>Imprints the feeding Momo as his most recent claim (overwriting any previous one).</summary>
        public void Imprint(Pawn momo)
        {
            source = momo;
            sourceXenotypeDefName = MomoTransformation.XenotypeFor(momo)?.defName;
        }

        /// <summary>
        /// Applies up to <paramref name="dose"/> progress, limited by the daily
        /// cap and the remaining severity. Returns true if any progress landed.
        /// Fires the completion effects when the change runs its full course.
        /// </summary>
        public bool TryApplyDose(float dose)
        {
            if (dose <= 0f || completed)
            {
                return false;
            }

            int today = GenDate.DaysPassed;
            if (today != lastDoseDay)
            {
                lastDoseDay = today;
                doseToday = 0f;
            }

            float applied = Mathf.Min(dose, ProjectMomoModSettings.Settings.IncubisationDailyCap - doseToday);
            applied = Mathf.Min(applied, def.maxSeverity - Severity);
            if (applied <= 0f)
            {
                return false;
            }

            doseToday += applied;
            Severity += applied;

            if (Severity >= def.maxSeverity - FullEpsilon)
            {
                CompleteIncubisation();
            }
            return true;
        }

        /// <summary>Dev tool: clears today's dose counter so the daily cap no longer throttles this pawn.</summary>
        public void DevResetDailyCap()
        {
            doseToday = 0f;
            lastDoseDay = -1;
        }

        /// <summary>
        /// The change runs its full course: strips the age ailments he already
        /// carries (prevention is handled ongoing by MomoAgeAilmentPatch), grants
        /// the awakening memories, and lets the player know when one of their
        /// pawns is involved on either side. Idempotent — fires exactly once.
        /// </summary>
        public void CompleteIncubisation()
        {
            if (completed || pawn == null || pawn.Dead)
            {
                return;
            }
            completed = true;
            Severity = def.maxSeverity;

            // The frailties of old age fall away — same silent sweep a new Momo gets.
            MomoAgeAilmentPatch.RemoveAgeAilments(pawn);

            ThoughtDef awakened = ProjectMomo_DefOf.ProjectMomo_IncubusAwakened;
            if (awakened != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(awakened);
            }

            // He remembers the Momo whose mana remade him, warmly.
            ThoughtDef turned = ProjectMomo_DefOf.ProjectMomo_IncubusTurned;
            if (turned != null && source != null && source != pawn)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                    (Thought_Memory)ThoughtMaker.MakeThought(turned), source);
            }

            NotifyCompleted();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // Rare-interval re-check is plenty (incubisation moves over days, not ticks).
            if (pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250, delta))
            {
                return;
            }

            // Completion normally fires from the dose that lands it; this re-check
            // catches dev-mode severity edits — the same self-resolving pattern as
            // Hediff_MomoCorruption.
            if (!completed && Severity >= def.maxSeverity - FullEpsilon)
            {
                CompleteIncubisation();
                return;
            }

            // Optional decay for players who want partial progress reversible
            // (default 0 — lore says incubisation is permanent). A completed
            // incubus never regresses.
            float decayPerDay = ProjectMomoModSettings.Settings.IncubisationDecayPerDay;
            if (!completed && decayPerDay > 0f)
            {
                float decayed = Severity - decayPerDay * (250f / 60000f);
                if (decayed <= 0.001f)
                {
                    // The last of her mana leaves his body; the claim fades with it.
                    pawn.health.RemoveHediff(this);
                }
                else
                {
                    Severity = decayed;
                }
            }
        }

        /// <summary>Lets the player know when one of their pawns is involved on either side of the change.</summary>
        private void NotifyCompleted()
        {
            if (pawn.Faction?.IsPlayer != true && source?.Faction?.IsPlayer != true)
            {
                return;
            }

            string text = source != null
                ? $"{pawn.LabelShortCap} has become an incubus under {source.LabelShort}'s mark!"
                : $"{pawn.LabelShortCap} has become an incubus!";
            Messages.Message(text, new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, historical: false);
        }

        public override string TipStringExtra
        {
            get
            {
                string extra = base.TipStringExtra;
                if (source != null)
                {
                    string claim = $"Marked by {source.LabelShort} ({SourceXenotype?.label ?? "monster"})";
                    extra = string.IsNullOrEmpty(extra) ? claim : extra + "\n" + claim;
                }
                return extra;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref source, "source");
            Scribe_Values.Look(ref sourceXenotypeDefName, "sourceXenotypeDefName");
            Scribe_Values.Look(ref doseToday, "doseToday");
            Scribe_Values.Look(ref lastDoseDay, "lastDoseDay", -1);
            Scribe_Values.Look(ref completed, "completed");
        }
    }
}
