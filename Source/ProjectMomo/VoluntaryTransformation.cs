using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Voluntary (consensual) transformation: two-sided desire scoring and the
    /// proposal flow shared by the think-tree giver (autonomous offers) and the
    /// right-click float menu (player-ordered offers). A Momo who wants a woman to
    /// join her kind offers the mana; if the woman accepts, the pair perform a
    /// short ceremony (JobDriver_TransformProposal) that completes the
    /// transformation in one step — a willing body does not resist, so there is no
    /// gradual corruption or mana cost.
    ///
    /// The forced path (downing a woman and infusing her) is untouched by all of
    /// this: a Momo with a willing convert has no need to take her by force.
    /// </summary>
    public static class VoluntaryTransformation
    {
        // Minimum desire in the ACCEPTOR before an offer is even considered —
        // below this the answer is always no, so no roll is wasted.
        private const float AcceptanceFloor = 0.25f;

        // Acceptor desire at or above this always accepts.
        private const float AcceptanceCertain = 0.95f;

        // The think-tree giver scans the map at most this often per pawn.
        private const int ScanIntervalTicks = 250;

        // Reservation window: walk time (worst case) + the ceremony.
        private const int ReservationTicks = 2000;

        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        // ------------------------------------------------------------------
        // Desire scoring
        // ------------------------------------------------------------------

        /// <summary>How much this Momo wants this woman to join her kind (0..1).</summary>
        public static float MomoDesire(Pawn momo, Pawn woman)
        {
            if (momo?.relations == null || woman == null)
            {
                return 0f;
            }

            float desire = 0.10f;
            desire += OpinionFactor(momo, woman);

            // A Momo overflowing with mana can afford to share it.
            Need_Mana mana = EssenceTransfer.Mana(momo);
            if (mana != null)
            {
                desire += mana.CurLevelPercentage * 0.20f;
            }

            // Family is the strongest pull: she wants her mother, daughter or
            // sister by her side as one of her own.
            if (woman.relations != null && woman.relations.DirectRelationExists(PawnRelationDefOf.Parent, momo))
            {
                desire += 0.25f; // the woman is her mother
            }
            if (momo.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, p => p == woman) != null)
            {
                desire += 0.25f; // the woman is her daughter
            }
            if (momo.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Sibling, p => p == woman) != null)
            {
                desire += 0.20f; // her sister
            }

            return Mathf.Clamp01(desire);
        }

        /// <summary>How much this woman wants to become a monster through this Momo (0..1).</summary>
        public static float WomanDesire(Pawn woman, Pawn momo)
        {
            if (woman?.relations == null || momo == null)
            {
                return 0f;
            }

            // Fear and hatred are hard refusals.
            int opinion = woman.relations.OpinionOf(momo);
            if (opinion <= -20)
            {
                return 0f;
            }

            float desire = 0.05f;
            desire += OpinionFactor(woman, momo);

            // Already succumbing: a partly-corrupted woman feels the pull.
            Hediff corruption = woman.health?.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_MomoCorruption);
            if (corruption != null)
            {
                desire += corruption.Severity * 0.20f;
            }

            // Blood calls to blood: a daughter or sister of a monster is far more
            // willing to follow her across.
            if (woman.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, p => p == momo) != null)
            {
                desire += 0.25f; // the Momo is her mother
            }
            if (woman.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Sibling, p => p == momo) != null)
            {
                desire += 0.20f; // her sister
            }

            // Nobody volunteers for a new body while on the edge of a mental break.
            if (woman.needs?.mood != null && woman.mindState?.mentalBreaker != null
                && woman.needs.mood.CurLevel < woman.mindState.mentalBreaker.BreakThresholdMinor)
            {
                desire *= 0.25f;
            }

            return Mathf.Clamp01(desire);
        }

        /// <summary>Opinion mapped from -100..100 to 0..0.35 of desire.</summary>
        private static float OpinionFactor(Pawn pawn, Pawn other)
        {
            return Mathf.Clamp01((pawn.relations.OpinionOf(other) + 100) / 200f) * 0.35f;
        }

        // ------------------------------------------------------------------
        // Eligibility
        // ------------------------------------------------------------------

        /// <summary>
        /// Could <paramref name="momo"/> offer a voluntary transformation to
        /// <paramref name="woman"/> right now? Checks the corruption rules, the
        /// voluntary-only gates (cooldowns, reservations), and supplies a short
        /// reason when refused, for the float menu.
        /// </summary>
        public static bool CanProposeTo(Pawn momo, Pawn woman, out string reason)
        {
            reason = null;
            if (momo == null || woman == null || momo == woman)
            {
                return false;
            }
            if (!EssenceTransfer.IsMomo(momo))
            {
                return false;
            }
            // Progression: Education - a mute pawn cannot offer a transformation, and
            // cannot understand one either. Either side being mute refuses the offer.
            if (EducationCompat.IsMute(momo))
            {
                reason = EducationCompat.MuteReason(momo);
                return false;
            }
            if (EducationCompat.IsMute(woman))
            {
                reason = EducationCompat.MuteReason(woman);
                return false;
            }
            if (!MomoTransformation.CanEverTransform(woman, out reason))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            TransformProposalComponent comp = TransformProposalComponent.Get();
            if (comp != null && (comp.PairCoolingDown(momo, woman, now) || comp.IsReserved(woman, now)))
            {
                reason = "recently refused";
                return false;
            }

            if (WomanDesire(woman, momo) < AcceptanceFloor)
            {
                reason = "she has no interest";
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Proposal flow
        // ------------------------------------------------------------------

        /// <summary>
        /// The think-tree entry point: if this Momo wants a woman on the map enough,
        /// returns a proposal job to her best candidate; otherwise null. A failed
        /// acceptance roll applies the rejection thought and cooldown and returns
        /// null, so the Momo simply goes on with her day.
        /// </summary>
        public static Job TryCreateProposalJob(Pawn pawn)
        {
            if (!Settings.CorruptionEnabled || !Settings.VoluntaryCorruptionEnabled
                || !Settings.VoluntaryCorruptionMomoProposals || !CanInitiate(pawn))
            {
                return null;
            }

            int now = Find.TickManager.TicksGame;
            TransformProposalComponent comp = TransformProposalComponent.Get();
            if (comp != null)
            {
                if (!comp.CanScanNow(pawn, now))
                {
                    return null;
                }
                comp.NoteScan(pawn, now + ScanIntervalTicks);
                if (!comp.CanAttemptNow(pawn, now))
                {
                    return null;
                }
            }

            Pawn best = FindBestCandidate(pawn, comp, now);
            if (best == null)
            {
                return null;
            }

            return RollAndBegin(pawn, best, comp, now, ordered: false);
        }

        /// <summary>
        /// The float-menu entry point: re-validates, rolls acceptance, and on
        /// success orders the job onto the Momo. Returns true when the proposal
        /// job was started.
        /// </summary>
        public static bool TryPlayerOrderedProposal(Pawn momo, Pawn woman)
        {
            if (!CanProposeTo(momo, woman, out _))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            TransformProposalComponent comp = TransformProposalComponent.Get();

            Job job = RollAndBegin(momo, woman, comp, now, ordered: true);
            return job != null;
        }

        /// <summary>
        /// Rolls acceptance from the woman's desire. On success: cooldowns,
        /// reservation, and the proposal job (returned for the giver, or ordered
        /// directly for the float menu). On failure: rejection thought, pair
        /// cooldown, and a message when the player cares about either pawn.
        /// </summary>
        private static Job RollAndBegin(Pawn momo, Pawn woman, TransformProposalComponent comp, int now, bool ordered)
        {
            float acceptChance = Mathf.InverseLerp(AcceptanceFloor, AcceptanceCertain, WomanDesire(woman, momo));
            if (!Rand.Chance(acceptChance))
            {
                int cooldownTicks = Mathf.RoundToInt(Settings.VoluntaryCorruptionRejectionCooldownHours * GenDate.TicksPerHour);
                comp?.NoteRejection(momo, woman, now + cooldownTicks);
                GrantRejectionThought(momo, woman);
                NotifyRejected(momo, woman);
                LogProposal(momo, woman, accepted: false);
                return null;
            }

            int attemptTicks = Mathf.RoundToInt(Settings.VoluntaryCorruptionAttemptCooldownHours * GenDate.TicksPerHour);
            comp?.NoteAttempt(momo, now + attemptTicks);
            comp?.ReserveTarget(woman, now + ReservationTicks);

            Job job = JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_TransformProposal, woman);
            if (ordered)
            {
                momo.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
            }
            return job;
        }

        /// <summary>Cheap gates every autonomous Momo must pass before any scan runs.</summary>
        private static bool CanInitiate(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (!pawn.Awake() || pawn.Drafted || pawn.InMentalState)
            {
                return false;
            }
            if (pawn.mindState?.enemyTarget != null)
            {
                return false;
            }
            if (!EssenceTransfer.IsMomo(pawn))
            {
                return false;
            }
            // Progression: Education - a mute Momo never offers on her own. The
            // right-click order is gated separately, in CanProposeTo.
            if (EducationCompat.IsMute(pawn))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Picks the woman the Momo most wants to convert among eligible pawns on
        /// the map. The Momo's desire must clear the configured threshold and the
        /// candidate's own desire the acceptance floor.
        /// </summary>
        private static Pawn FindBestCandidate(Pawn momo, TransformProposalComponent comp, int now)
        {
            Pawn best = null;
            float bestDesire = Settings.VoluntaryCorruptionDesireThreshold;

            var pawns = momo.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!EligibleTarget(momo, candidate, comp, now))
                {
                    continue;
                }

                // The hard corruption rules (female, humanlike, of age, not already a monster).
                if (!MomoTransformation.CanEverTransform(candidate))
                {
                    continue;
                }

                if (WomanDesire(candidate, momo) < AcceptanceFloor)
                {
                    continue;
                }

                float desire = MomoDesire(momo, candidate);
                if (desire < bestDesire)
                {
                    continue;
                }
                best = candidate;
                bestDesire = desire;
            }

            return best;
        }

        /// <summary>Target-side state gates: present, upright, calm, reachable, unreserved, not hostile.</summary>
        private static bool EligibleTarget(Pawn momo, Pawn target, TransformProposalComponent comp, int now)
        {
            if (target == null || target == momo || target.Dead || !target.Spawned || target.Map != momo.Map)
            {
                return false;
            }
            if (target.RaceProps == null || !target.RaceProps.Humanlike)
            {
                return false;
            }
            // Progression: Education - a mute pawn cannot consent, so nobody ever
            // walks up to one on their own.
            if (EducationCompat.IsMute(target))
            {
                return false;
            }
            if (target.Downed || !target.Awake() || target.Drafted || target.InMentalState)
            {
                return false;
            }
            if (target.mindState?.enemyTarget != null)
            {
                return false;
            }
            // No wartime offers — raiders are corrupted by force, not by asking nicely.
            if (momo.Faction != null && target.Faction != null && momo.Faction.HostileTo(target.Faction))
            {
                return false;
            }
            // A visitor never offers the change to one of your colonists. The woman she turns is
            // yours to keep, and the offerer is off the map in a few days either way. The reverse is
            // deliberate: your own Momo may still offer it to a visitor.
            if (EssenceTransfer.IsVisitingGuest(momo) && target.Faction?.IsPlayer == true)
            {
                return false;
            }
            if (target.CurJobDef == ProjectMomo_DefOf.ProjectMomo_TransformProposal
                || target.CurJobDef == ProjectMomo_DefOf.ProjectMomo_InfuseMomo)
            {
                return false;
            }
            if (comp != null && (comp.PairCoolingDown(momo, target, now) || comp.IsReserved(target, now)))
            {
                return false;
            }
            if (!momo.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Records the proposal in both pawns' character logs, the same way the
        /// tsugai proposals are logged. Rejections are logged when the roll fails;
        /// acceptances are logged by the ceremony driver once the pair is actually
        /// face-to-face.
        /// </summary>
        public static void LogProposal(Pawn momo, Pawn woman, bool accepted)
        {
            if (momo == null || woman == null || Current.Game == null)
            {
                return;
            }
            InteractionDef def = accepted
                ? ProjectMomo_DefOf.ProjectMomo_TransformProposalAccepted
                : ProjectMomo_DefOf.ProjectMomo_TransformProposalRejected;
            if (def == null)
            {
                return;
            }
            Find.PlayLog.Add(new PlayLogEntry_Interaction(def, momo, woman, null));
        }

        /// <summary>The rejected Momo's mood thought, remembered against the woman who turned her down.</summary>
        private static void GrantRejectionThought(Pawn momo, Pawn woman)
        {
            var memories = momo?.needs?.mood?.thoughts?.memories;
            ThoughtDef def = ProjectMomo_DefOf.ProjectMomo_TransformRejected;
            if (memories == null || def == null)
            {
                return;
            }
            memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(def), woman);
        }

        private static void NotifyRejected(Pawn momo, Pawn woman)
        {
            bool playerCares = momo.Faction?.IsPlayer == true || woman.Faction?.IsPlayer == true;
            if (!playerCares)
            {
                return;
            }
            Messages.Message(
                $"{momo.LabelShortCap} offered to remake {woman.LabelShort} as a monster, but was turned down.",
                new LookTargets(momo, woman),
                MessageTypeDefOf.NeutralEvent,
                historical: false);
        }
    }
}
