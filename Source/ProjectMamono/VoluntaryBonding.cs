using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Voluntary (consensual) tsugai bonding: two-sided desire scoring and the
    /// proposal flow shared by the think-tree giver (autonomous proposals) and the
    /// right-click float menu (player-ordered proposals).
    ///
    /// Desire is scored 0..1 per direction - <see cref="MamonoDesire"/> and
    /// <see cref="ManDesire"/> - from opinion, vanilla romance attraction, any
    /// existing love relation, and the man's essence fullness. Essence is the
    /// bonding budget: a voluntary bond costs the man a fixed share of essence
    /// (settings), so a man can only collect as many Mamonos as his regeneration
    /// supports, and a well-stocked man is a more attractive husband to a Mamono.
    ///
    /// A proposal is only attempted when the initiator's desire clears the
    /// configured threshold and the target's desire clears a low acceptance floor;
    /// the target's desire then sets the acceptance chance. Rejection applies a
    /// mood thought and a per-pair cooldown. A forced (combat-knockout) bond is
    /// untouched by all of this - a Mamono with a willing partner has no need to
    /// take him by force.
    /// </summary>
    public static class VoluntaryBonding
    {
        // Minimum desire in the ACCEPTOR before a proposal is even considered -
        // below this the answer is always no, so no roll is wasted.
        private const float AcceptanceFloor = 0.25f;

        // Target desire at or above this always accepts.
        private const float AcceptanceCertain = 0.95f;

        // The think-tree giver scans the map at most this often per pawn.
        private const int ScanIntervalTicks = 250;

        // Reservation window: walk time (worst case) + the bonding ceremony.
        private const int ReservationTicks = 3000;

        private static ProjectMamonoSettings Settings => ProjectMamonoModSettings.Settings;

        // ------------------------------------------------------------------
        // Desire scoring
        // ------------------------------------------------------------------

        /// <summary>How much this Mamono wants to bond with this man (0..1).</summary>
        public static float MamonoDesire(Pawn mamono, Pawn man)
        {
            if (mamono?.relations == null || man == null)
            {
                return 0f;
            }

            float desire = 0.10f;
            desire += OpinionFactor(mamono, man);
            desire += Mathf.Clamp01(mamono.relations.SecondaryRomanceChanceFactor(man)) * 0.25f;
            if (LoveRelationExists(mamono, man))
            {
                desire += 0.15f;
            }

            // A full essence bar is the appeal of a renewable resource: she wants
            // a husband who keeps producing, and bonding empties the well for a while.
            Need_Essence essence = EssenceTransfer.Essence(man);
            if (essence != null)
            {
                desire += essence.CurLevelPercentage * 0.20f;
            }

            // A hungry Mamono is more eager to settle down with a provider.
            Need_Mana mana = EssenceTransfer.Mana(mamono);
            if (mana != null && mana.CurLevelPercentage < 0.3f)
            {
                desire += 0.10f;
            }

            if (GrievingBondLoss(mamono))
            {
                desire -= 0.5f;
            }

            return Mathf.Clamp01(desire);
        }

        /// <summary>How much this man wants to bond with this Mamono (0..1).</summary>
        public static float ManDesire(Pawn man, Pawn mamono)
        {
            if (man?.relations == null || mamono == null)
            {
                return 0f;
            }

            // The bond is paid for in essence: a man who can't cover the cost
            // has nothing to offer, however much he admires her.
            Need_Essence essence = EssenceTransfer.Essence(man);
            if (essence == null || essence.CurLevel < Settings.VoluntaryBondEssenceCost)
            {
                return 0f;
            }

            float desire = 0.05f;
            desire += OpinionFactor(man, mamono);
            desire += Mathf.Clamp01(man.relations.SecondaryRomanceChanceFactor(mamono)) * 0.25f;
            if (LoveRelationExists(man, mamono))
            {
                desire += 0.15f;
            }

            // The fuller his essence, the more he can afford to share.
            desire += essence.CurLevelPercentage * 0.15f;

            // Nobody pledges themselves while on the edge of a mental break.
            if (man.needs?.mood != null && man.mindState?.mentalBreaker != null
                && man.needs.mood.CurLevel < man.mindState.mentalBreaker.BreakThresholdMinor)
            {
                desire *= 0.25f;
            }

            if (GrievingBondLoss(man))
            {
                desire -= 0.5f;
            }

            return Mathf.Clamp01(desire);
        }

        /// <summary>Opinion mapped from -100..100 to 0..0.35 of desire.</summary>
        private static float OpinionFactor(Pawn pawn, Pawn other)
        {
            return Mathf.Clamp01((pawn.relations.OpinionOf(other) + 100) / 200f) * 0.35f;
        }

        private static bool LoveRelationExists(Pawn pawn, Pawn other)
        {
            return pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, p => p == other) != null
                || pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, p => p == other) != null
                || pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse, p => p == other) != null;
        }

        private static bool GrievingBondLoss(Pawn pawn)
        {
            HediffDef loss = ProjectMamono_DefOf.ProjectMamono_TsugaiLoss;
            return loss != null && pawn.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(loss);
        }

        // ------------------------------------------------------------------
        // Eligibility
        // ------------------------------------------------------------------

        /// <summary>
        /// Could <paramref name="initiator"/> (Mamono or man) make a voluntary bond
        /// proposal to <paramref name="target"/> right now? Checks the bond rules
        /// (via TsugaiFormation.CanBond), the voluntary-only gates (essence budget,
        /// rejection cooldowns, reservations), and supplies a short reason when
        /// refused, for the float menu.
        /// </summary>
        public static bool CanProposeTo(Pawn initiator, Pawn target, out string reason)
        {
            reason = null;
            if (initiator == null || target == null || initiator == target)
            {
                return false;
            }

            Pawn mamono = EssenceTransfer.IsMamono(initiator) ? initiator : (EssenceTransfer.IsMamono(target) ? target : null);
            Pawn man = mamono == initiator ? target : initiator;
            if (mamono == null)
            {
                return false;
            }

            // Progression: Education - a mute pawn cannot ask for a bond, and cannot
            // understand one either. Either side being mute refuses the proposal, so
            // the reason names whichever of the two is mute.
            Pawn acceptor = initiator == mamono ? man : mamono;
            if (EducationCompat.IsMute(initiator))
            {
                reason = EducationCompat.MuteReason(initiator);
                return false;
            }
            if (EducationCompat.IsMute(acceptor))
            {
                reason = EducationCompat.MuteReason(acceptor);
                return false;
            }

            if (TsugaiFormation.HasBondedPartner(mamono))
            {
                reason = "she already has a husband";
                return false;
            }
            if (TsugaiFormation.HasBondedPartner(man))
            {
                reason = "already bonded";
                return false;
            }
            if (!TsugaiFormation.CanBond(mamono, man))
            {
                reason = "cannot bond";
                return false;
            }

            // No visitor/colonist bonds: a visiting Mamono who bonded a colonist could only
            // leave (kidnapping him) or defect, neither of which a visit should produce, so
            // the consensual path is closed to guests of the colony in both directions.
            if (EssenceTransfer.IsGuestColonistPair(mamono, man))
            {
                reason = "a visitor cannot bond a colonist";
                return false;
            }

            Need_Essence essence = EssenceTransfer.Essence(man);
            if (essence == null || essence.CurLevel < Settings.VoluntaryBondEssenceCost)
            {
                reason = "not enough essence";
                return false;
            }

            int now = Find.TickManager.TicksGame;
            VoluntaryBondComponent comp = VoluntaryBondComponent.Get();
            if (comp != null && (comp.PairCoolingDown(initiator, target, now) || comp.IsReserved(target, now)))
            {
                reason = "recently rejected";
                return false;
            }

            float acceptorDesire = initiator == mamono ? ManDesire(man, mamono) : MamonoDesire(mamono, man);
            if (acceptorDesire < AcceptanceFloor)
            {
                reason = initiator == mamono ? "he has no interest" : "she has no interest";
                return false;
            }

            // There has to be a path. The proposal job's first toil waits for arrival
            // (ToilCompleteMode.PatherArrival) and a pawn who cannot path simply stands
            // where he is, so ordering a proposal to someone he cannot walk to would
            // freeze him until the player noticed. The autonomous scan already refuses
            // these candidates in EligibleTarget.
            if (!initiator.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                reason = "cannot reach them";
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Proposal flow
        // ------------------------------------------------------------------

        /// <summary>
        /// The think-tree entry point: if this pawn (a Mamono or a bondable man) wants
        /// someone enough, returns a proposal job to their best partner; otherwise
        /// null. A failed acceptance roll applies the rejection thought and cooldown
        /// and returns null, so the pawn simply goes on with their day.
        /// </summary>
        public static Job TryCreateProposalJob(Pawn pawn)
        {
            if (!Settings.VoluntaryBondingEnabled || !CanInitiate(pawn))
            {
                return null;
            }

            int now = Find.TickManager.TicksGame;
            VoluntaryBondComponent comp = VoluntaryBondComponent.Get();
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

            bool mamonoInitiator = EssenceTransfer.IsMamono(pawn);
            if (mamonoInitiator ? !Settings.VoluntaryBondMamonoProposals : !Settings.VoluntaryBondManProposals)
            {
                return null;
            }

            Pawn best = FindBestPartner(pawn, mamonoInitiator, comp, now);
            if (best == null)
            {
                return null;
            }

            return BeginProposal(pawn, best, comp, now, ordered: false);
        }

        /// <summary>
        /// The float-menu entry point: re-validates, rolls acceptance, and on
        /// success orders the job onto the initiator. Returns true when the
        /// proposal job was started.
        /// </summary>
        public static bool TryPlayerOrderedProposal(Pawn initiator, Pawn target)
        {
            if (!CanProposeTo(initiator, target, out _))
            {
                return false;
            }

            int now = Find.TickManager.TicksGame;
            VoluntaryBondComponent comp = VoluntaryBondComponent.Get();

            Job job = BeginProposal(initiator, target, comp, now, ordered: true);
            return job != null;
        }

        /// <summary>
        /// Starts the proposal: notes the attempt and the reservation, then either hands the
        /// job to an ordered pawn or returns it to the think tree. The answer is deliberately
        /// not rolled here, and not carried on the job - see <see cref="AcceptsProposal"/>.
        /// </summary>
        private static Job BeginProposal(Pawn initiator, Pawn target, VoluntaryBondComponent comp, int now, bool ordered)
        {
            int attemptTicks = Mathf.RoundToInt(Settings.VoluntaryBondAttemptCooldownHours * GenDate.TicksPerHour);
            comp?.NoteAttempt(initiator, now + attemptTicks);
            comp?.ReserveTarget(target, now + ReservationTicks);

            Job job = JobMaker.MakeJob(ProjectMamono_DefOf.ProjectMamono_ProposeTsugaiBond, target);
            if (ordered)
            {
                initiator.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
            }
            return job;
        }

        /// <summary>
        /// Rolls the target's answer, called by the proposal driver once the initiator is
        /// standing before the target - never earlier, because a rejection's effects (mood
        /// thought, pair cooldown, message) belong face to face. A man who offers his bond
        /// always gets it; a Mamono's proposal is rolled against the man's desire on the same
        /// curve the pre-walk gate uses, so below AcceptanceFloor the answer is always no and
        /// at AcceptanceCertain always yes. An autonomous proposal has already cleared
        /// AcceptanceFloor to be attempted at all, so this is the roll that decides it.
        ///
        /// The verdict is not carried on the job: vanilla overwrites Job.playerForced when a
        /// job is player-ordered (Pawn_JobTracker.TryTakeOrderedJob sets it to true), which
        /// made every ordered proposal succeed no matter how the roll went.
        /// </summary>
        public static bool AcceptsProposal(Pawn initiator, Pawn target)
        {
            if (initiator == null || target == null)
            {
                return false;
            }
            // Progression: Education - a mute pawn cannot consent, so the roll always
            // fails and the driver applies the ordinary face-to-face rejection. This
            // has to sit above the man-initiates shortcut below, which accepts without
            // a roll, and it is the last line of defence: the gates above normally stop
            // the walk from happening at all.
            if (EducationCompat.IsMute(target))
            {
                return false;
            }
            if (!EssenceTransfer.IsMamono(initiator))
            {
                return true;
            }

            float acceptorDesire = ManDesire(target, initiator);
            return Rand.Chance(Mathf.InverseLerp(AcceptanceFloor, AcceptanceCertain, acceptorDesire));
        }

        /// <summary>
        /// Applies a face-to-face rejection: pair cooldown, the rejected mood thought,
        /// a message when the player cares, and the rejected log entry. Called by the
        /// proposal driver once the initiator has reached the target.
        /// </summary>
        public static void ApplyFaceToFaceRejection(Pawn initiator, Pawn target)
        {
            int now = Find.TickManager.TicksGame;
            int cooldownTicks = Mathf.RoundToInt(Settings.VoluntaryBondRejectionCooldownHours * GenDate.TicksPerHour);
            VoluntaryBondComponent.Get()?.NoteRejection(initiator, target, now + cooldownTicks);
            GrantRejectionThought(initiator, target);
            NotifyRejected(initiator, target);
            LogProposal(initiator, target, accepted: false);
        }

        /// <summary>Cheap gates every autonomous initiator must pass before any scan runs.</summary>
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
            // Colonists only bond autonomously when the player allows it; the
            // right-click order and autonomous proposals by visitors/raiders are
            // unaffected. (This gate is the autonomous path only - TryPlayerOrderedProposal
            // never reaches CanInitiate.)
            if (!Settings.VoluntaryBondColonistProposals && pawn.IsColonistPlayerControlled)
            {
                return false;
            }
            // A would-be initiator is either a Mamono or a bondable man; anyone else
            // (women, children, animals) never proposes.
            if (!EssenceTransfer.IsMamono(pawn) && !TsugaiFormation.IsBondable(pawn))
            {
                return false;
            }
            // Progression: Education - a mute pawn never proposes on her own. The
            // right-click order is gated separately, in CanProposeTo.
            if (EducationCompat.IsMute(pawn))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Picks the partner the initiator wants most among eligible pawns on the
        /// map. The initiator's desire must clear the configured threshold and the
        /// candidate's own desire the acceptance floor (checked per pair, since
        /// desire is directional).
        /// </summary>
        private static Pawn FindBestPartner(Pawn initiator, bool mamonoInitiator, VoluntaryBondComponent comp, int now)
        {
            Pawn best = null;
            float bestDesire = Settings.VoluntaryBondDesireThreshold;

            var pawns = initiator.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!EligibleTarget(initiator, candidate, comp, now))
                {
                    continue;
                }

                // The hard bond rules (one living husband per Mamono, Protagonist
                // harem rule, age, gender) - checked up front so nobody walks
                // across the map for a bond that can never form.
                Pawn mamono = mamonoInitiator ? initiator : candidate;
                Pawn man = mamonoInitiator ? candidate : initiator;
                if (!TsugaiFormation.CanBond(mamono, man))
                {
                    continue;
                }

                if (mamonoInitiator)
                {
                    if (!TsugaiFormation.IsBondable(candidate) || ManDesire(candidate, initiator) < AcceptanceFloor)
                    {
                        continue;
                    }
                    float desire = MamonoDesire(initiator, candidate);
                    if (desire < bestDesire)
                    {
                        continue;
                    }
                    best = candidate;
                    bestDesire = desire;
                }
                else
                {
                    if (!EssenceTransfer.IsMamono(candidate) || MamonoDesire(candidate, initiator) < AcceptanceFloor)
                    {
                        continue;
                    }
                    float desire = ManDesire(initiator, candidate);
                    if (desire < bestDesire)
                    {
                        continue;
                    }
                    best = candidate;
                    bestDesire = desire;
                }
            }

            return best;
        }

        /// <summary>Target-side state gates: present, upright, calm, reachable, unreserved, not hostile.</summary>
        private static bool EligibleTarget(Pawn initiator, Pawn target, VoluntaryBondComponent comp, int now)
        {
            if (target == null || target == initiator || target.Dead || !target.Spawned || target.Map != initiator.Map)
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
            // No wartime proposals - raiders bond by force, not by asking nicely.
            if (initiator.Faction != null && target.Faction != null && initiator.Faction.HostileTo(target.Faction))
            {
                return false;
            }
            // The visitor rule belongs on this path too, not only in CanProposeTo: the autonomous
            // scan never calls that, so a visitor could pick a colonist and walk up to her. Visitors
            // may still propose to each other - they are the only ones who do it on their own.
            if (EssenceTransfer.IsGuestColonistPair(initiator, target))
            {
                return false;
            }
            if (target.CurJobDef == ProjectMamono_DefOf.ProjectMamono_ProposeTsugaiBond
                || target.CurJobDef == ProjectMamono_DefOf.ProjectMamono_FormTsugai)
            {
                return false;
            }
            if (comp != null && (comp.PairCoolingDown(initiator, target, now) || comp.IsReserved(target, now)))
            {
                return false;
            }
            if (!initiator.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Records the proposal in both pawns' character logs, the same way vanilla
        /// marriage proposals are logged (PlayLogEntry_Interaction). Rejections are
        /// logged when the roll fails; acceptances are logged by the ceremony driver
        /// once the pair is actually face-to-face.
        /// </summary>
        public static void LogProposal(Pawn initiator, Pawn target, bool accepted)
        {
            if (initiator == null || target == null || Current.Game == null)
            {
                return;
            }
            InteractionDef def = accepted
                ? ProjectMamono_DefOf.ProjectMamono_TsugaiProposalAccepted
                : ProjectMamono_DefOf.ProjectMamono_TsugaiProposalRejected;
            if (def == null)
            {
                return;
            }
            Find.PlayLog.Add(new PlayLogEntry_Interaction(def, initiator, target, null));
        }

        /// <summary>The rejected initiator's mood thought, remembered against the pawn who turned them down.</summary>
        private static void GrantRejectionThought(Pawn initiator, Pawn target)
        {
            var memories = initiator?.needs?.mood?.thoughts?.memories;
            ThoughtDef def = ProjectMamono_DefOf.ProjectMamono_TsugaiRejected;
            if (memories == null || def == null)
            {
                return;
            }
            memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(def), target);
        }

        private static void NotifyRejected(Pawn initiator, Pawn target)
        {
            bool playerCares = initiator.Faction?.IsPlayer == true || target.Faction?.IsPlayer == true;
            if (!playerCares)
            {
                return;
            }
            string pronoun = initiator.gender == Gender.Male ? "he" : "she";
            Messages.Message(
                $"{initiator.LabelShortCap} tried to seduce {target.LabelShort}, {pronoun} was turned down.",
                new LookTargets(initiator, target),
                MessageTypeDefOf.NeutralEvent,
                historical: false);
        }
    }
}
