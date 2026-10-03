using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Shared logic for moving essence from a human's Essence need into a Mamono's
    /// Mana need, used by both the human's "Give essence" and the Mamono's "Drain
    /// essence" actions. The amount moved is limited by how much essence the human
    /// has and how much mana the Mamono can take.
    /// </summary>
    public static class EssenceTransfer
    {
        /// <summary>True if the pawn carries the Mamono gene.</summary>
        public static bool IsMamono(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_Mamono);
        }

        /// <summary>
        /// True while the pawn is a visitor: a member of another, non-hostile faction
        /// on the colony map (not a colonist, not a wild man, not a raider). Visitors
        /// are guests of the colony - hostile pawns (raiders) are not "visiting" even
        /// though they enter the map, and are excluded here.
        /// </summary>
        public static bool IsVisitingGuest(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null || pawn.Faction.IsPlayer || pawn.IsWildMan())
            {
                return false;
            }
            Faction player = Faction.OfPlayer;
            return player == null || !pawn.Faction.HostileTo(player);
        }

        /// <summary>
        /// True when one of the pair is a visitor and the other is one of the player's own pawns.
        /// Every consensual proposal refuses this pair: a visitor who bonds a colonist can only
        /// leave (kidnapping him) or defect, and either way she is off the map in a few days while
        /// the colonist stays. The right-click orders use this for their refusal reason, and the
        /// autonomous givers use it in their target gates, which never call those orders.
        /// </summary>
        public static bool IsGuestColonistPair(Pawn a, Pawn b)
        {
            bool aIsOurs = a?.Faction?.IsPlayer == true;
            bool bIsOurs = b?.Faction?.IsPlayer == true;
            return (IsVisitingGuest(a) && bIsOurs) || (IsVisitingGuest(b) && aIsOurs);
        }

        /// <summary>The pawn's Essence need, or null.</summary>
        public static Need_Essence Essence(Pawn pawn)
        {
            return pawn?.needs?.TryGetNeed(ProjectMamono_DefOf.ProjectMamono_Essence) as Need_Essence;
        }

        /// <summary>The pawn's Mana need, or null.</summary>
        public static Need_Mana Mana(Pawn pawn)
        {
            return pawn?.needs?.TryGetNeed(ProjectMamono_DefOf.ProjectMamono_Mana) as Need_Mana;
        }

        /// <summary>
        /// True if <paramref name="mamono"/> can draw essence from <paramref name="human"/>:
        /// a living Mamono with a Mana need and a living, non-Mamono humanlike with an
        /// Essence need that has something left to give. A Mamono who carries a living
        /// tsugai bond may only feed from a bonded partner - she will not draw from
        /// a human she is not bonded to. A captive of the colony is the one exception:
        /// see <see cref="IsCaptiveFeedTarget"/>.
        /// </summary>
        public static bool CanTransfer(Pawn mamono, Pawn human)
        {
            return HasEssenceToDraw(mamono, human) && BondAllowsFeeding(mamono, human);
        }

        /// <summary>
        /// The checks every transfer shares, with no partner rules at all: a living Mamono
        /// with a Mana need, and a living, non-Mamono humanlike with essence left to give.
        /// Split out so a captive meal can skip <see cref="BondAllowsFeeding"/>.
        /// </summary>
        private static bool HasEssenceToDraw(Pawn mamono, Pawn human)
        {
            if (mamono == null || human == null || mamono == human || mamono.Dead || human.Dead)
            {
                return false;
            }
            if (!IsMamono(mamono) || IsMamono(human))
            {
                return false;
            }
            if (human.RaceProps == null || !human.RaceProps.Humanlike)
            {
                return false;
            }
            if (Mana(mamono) == null)
            {
                return false;
            }
            Need_Essence essence = Essence(human);
            return essence != null && essence.CurLevel > 0.001f;
        }

        /// <summary>
        /// The bonded-feeding rule on its own, so callers (float menu) can explain
        /// why an otherwise-valid transfer is refused. A captive of the colony is the one
        /// exception: a captive meal bypasses this rule and the mark inside it - see
        /// <see cref="IsCaptiveFeedTarget"/>, which does not call this.
        /// </summary>
        public static bool BondAllowsFeeding(Pawn mamono, Pawn human)
        {
            if (mamono?.relations == null || human == null)
            {
                return true;
            }

            List<Pawn> bonds = new List<Pawn>();
            mamono.relations.GetDirectRelations(ProjectMamono_DefOf.ProjectMamono_Tsugai, ref bonds);
            for (int i = 0; i < bonds.Count; i++)
            {
                Pawn bond = bonds[i];
                if (bond == null || bond.Dead)
                {
                    continue;
                }
                // She has at least one living bond: only a bonded partner may feed her.
                return bond == human;
            }
            // No living bonds: a man marked by another Mamono is claimed -
            // unbonded Mamonos respect the claim and leave him to her.
            if (Incubisation.MarkProtects(mamono, human))
            {
                return false;
            }
            // Unmarked: unbonded Mamonos may feed from anyone valid.
            return true;
        }

        /// <summary>
        /// True if <paramref name="candidate"/> is a captive of the colony: one of the
        /// player's own prisoners, or one of the player's own slaves.
        /// </summary>
        public static bool IsColonyCaptive(Pawn candidate)
        {
            return candidate != null && (candidate.IsPrisonerOfColony || candidate.IsSlaveOfColony);
        }

        /// <summary>
        /// True if <paramref name="mamono"/> may feed from <paramref name="candidate"/> as a
        /// captive meal: one of the colony's own prisoners or slaves, on her map and
        /// reachable, with essence left to give.
        ///
        /// A captive is a larder the colony owns, not a partner, so a captive meal is exempt
        /// from BOTH rules that govern feeding from a free human: the bond rule (a Mamono with a
        /// living tsugai may still take it) and the incubation mark (another Mamono's claim does
        /// not reserve him). The second exemption is what keeps the stock usable: without it,
        /// the first Mamono to feed on a captive would own him for good, and a married Mamono whose
        /// mate is away or dry would go hungry beside a full cell.
        ///
        /// Two callers share this: a captive Mamono feeding on a fellow prisoner or a slave (she
        /// cannot leave, cannot hunt, and is shut out of the break system, so captives are her
        /// only meal), and any hungry Mamono falling back to a captive when no mate can feed her.
        /// </summary>
        public static bool IsCaptiveFeedTarget(Pawn mamono, Pawn candidate)
        {
            if (mamono == null || candidate == null || candidate == mamono || mamono.Map == null)
            {
                return false;
            }
            if (!IsColonyCaptive(candidate) || candidate.Dead || !candidate.Spawned || candidate.Map != mamono.Map)
            {
                return false;
            }
            if (!mamono.CanReach(candidate, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }
            return HasEssenceToDraw(mamono, candidate);
        }

        /// <summary>The nearest colony captive <paramref name="mamono"/> may drain, or null.</summary>
        public static Pawn FindCaptiveToDrain(Pawn mamono)
        {
            if (mamono?.Map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestDistSq = float.MaxValue;
            System.Collections.Generic.IReadOnlyList<Pawn> pawns = mamono.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                // Cheap filter first: the reachability test inside is the expensive part.
                if (!IsColonyCaptive(candidate))
                {
                    continue;
                }
                if (!IsCaptiveFeedTarget(mamono, candidate))
                {
                    continue;
                }

                float dSq = candidate.Position.DistanceToSquared(mamono.Position);
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>
        /// Moves up to <paramref name="amount"/> essence from the human to the Mamono's
        /// mana. Returns the amount actually transferred. When
        /// <paramref name="intimateSideEffects"/> is false, the lovin' memory and the
        /// follow-up lovin' job are skipped. Two callers use that: an external system
        /// (the Intimacy mod) that has already handled the act itself, and a captive
        /// Mamono draining a fellow prisoner, which is not an intimate act at all.
        /// </summary>
        public static float Transfer(Pawn mamono, Pawn human, float amount, bool intimateSideEffects = true)
        {
            Need_Essence essence = Essence(human);
            Need_Mana mana = Mana(mamono);
            if (essence == null || mana == null)
            {
                return 0f;
            }

            float moved = Mathf.Min(amount, essence.CurLevel);
            moved = Mathf.Min(moved, mana.MaxLevel - mana.CurLevel);
            if (moved <= 0f)
            {
                return 0f;
            }

            essence.CurLevel -= moved;
            mana.CurLevel += moved;

            // A Mamono who broke from the hunger and finally fed gets catharsis:
            // a big, multi-day relief buff. Checked BEFORE the level rises, because
            // the break belongs to the starvation episode this feeding ends -
            // granted only while she is still in (or recovering from) the mental
            // state, so routine top-ups never trigger it.
            if (IsBreaking(mamono))
            {
                TryGrantCatharsis(mamono);
            }

            // Both parties share a warm 1-day mood buff.
            ThoughtDef shared = ProjectMamono_DefOf.ProjectMamono_EssenceShared;
            if (shared != null)
            {
                mamono.needs?.mood?.thoughts?.memories?.TryGainMemory(shared);
                human.needs?.mood?.thoughts?.memories?.TryGainMemory(shared);
            }

            if (intimateSideEffects)
            {
                // The transfer is intimate: treat it as vanilla lovin' for both. GotSomeLovin
                // is a social memory - granted with the partner as otherPawn, it supplies the
                // lovin' mood buff AND the opinion boost that keeps their compatibility up.
                GrantLovin(mamono, human);
                GrantLovin(human, mamono);

                // Then have the pair actually perform the vanilla lovin' job (the full
                // animation) in a shared bed. Queued to the next tick: this runs inside
                // the Drain/Give job's finish action (toil Cleanup), and starting a new
                // job here would interrupt the job being cleaned up and throw a
                // NullReferenceException.
                LovinQueueComponent.Queue(mamono, human);
            }

            // The Mamono earns Isekai XP scaled by how much she consumed.
            IsekaiCompat.AwardEssenceXP(mamono, moved);

            // Her mana seeps into him with every feeding: the slow road to incubisation.
            Incubisation.ApplyDose(mamono, human, moved);

            // A full incubus no longer lives on food alone - his mate's mana,
            // exchanged in the act, nourishes him directly. He regains a little
            // Food scaled by the essence moved (works for both Give and Drain,
            // since both route through here).
            if (Incubisation.IsFullIncubus(human) && human.needs?.food != null)
            {
                float factor = ProjectMamonoModSettings.Settings?.IncubusFoodPerEssence ?? 0.5f;
                Need_Food food = human.needs.food;
                food.CurLevel = Mathf.Min(food.MaxLevel, food.CurLevel + moved * factor);
            }

            return moved;
        }

        /// <summary>True while the Mamono is in a mental state - the starvation
        /// feeding break, essence berserk, or any other break a low-mana episode
        /// could have thrown her into.</summary>
        private static bool IsBreaking(Pawn mamono)
        {
            return mamono?.mindState?.mentalStateHandler != null
                && mamono.mindState.mentalStateHandler.InMentalState;
        }

        /// <summary>Grants the feeding-catharsis memory to a Mamono who fed her way
        /// out of a break. No-op if she lacks a mood (prisoners without needs, etc.).</summary>
        private static void TryGrantCatharsis(Pawn mamono)
        {
            ThoughtDef catharsis = ProjectMamono_DefOf.ProjectMamono_ManaCatharsis;
            if (catharsis == null)
            {
                return;
            }
            mamono.needs?.mood?.thoughts?.memories?.TryGainMemory(catharsis);
        }

        /// <summary>Grants <paramref name="pawn"/> the vanilla "got some lovin'" memory about <paramref name="partner"/> (mood + opinion).</summary>
        private static void GrantLovin(Pawn pawn, Pawn partner)
        {
            var memories = pawn?.needs?.mood?.thoughts?.memories;
            if (memories == null || partner == null)
            {
                return;
            }

            Thought_Memory lovin = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
            memories.TryGainMemory(lovin, partner);
        }

        /// <summary>
        /// Actually starts the vanilla lovin' job between the pair so they perform the
        /// real lovin' animation. Runs a tick after the transfer (via LovinQueueComponent)
        /// so it's safe to change the Mamono's job. Requires a free, reservable bed with
        /// room for both; silently does nothing if none is available by then. The
        /// initiator's job drives the bounce for both.
        /// </summary>
        public static void StartLovinNow(Pawn mamono, Pawn human)
        {
            if (mamono?.Map == null || human == null || mamono.Map != human.Map)
            {
                return;
            }
            if (!mamono.Spawned || !human.Spawned || mamono.Downed || human.Downed)
            {
                return;
            }
            if (mamono.jobs == null || human.jobs == null)
            {
                return;
            }

            Building_Bed bed = FindSharedBed(mamono, human);
            if (bed == null)
            {
                return;
            }

            Job lovin = JobMaker.MakeJob(JobDefOf.Lovin, human, bed);
            mamono.jobs.StartJob(lovin, JobCondition.InterruptForced);
        }

        /// <summary>
        /// The bed the pair can both lie in, or null. Prefers what
        /// <see cref="RestUtility.FindBedFor"/> would give either of them - her own bed, her
        /// lover's, then the nearest free one - and then looks for a bed for two anywhere on
        /// the map, so a pair whose own beds are singles still gets the animation when the
        /// colony owns a double.
        /// </summary>
        private static Building_Bed FindSharedBed(Pawn mamono, Pawn human)
        {
            Building_Bed bed = UsableSharedBed(RestUtility.FindBedFor(mamono, human, true, false, null), mamono, human);
            if (bed != null)
            {
                return bed;
            }

            bed = UsableSharedBed(RestUtility.FindBedFor(human, mamono, true, false, null), mamono, human);
            if (bed != null)
            {
                return bed;
            }

            return (Building_Bed)GenClosest.ClosestThingReachable(
                mamono.Position,
                mamono.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Bed),
                PathEndMode.OnCell,
                TraverseParms.For(mamono),
                9999f,
                thing => UsableSharedBed(thing as Building_Bed, mamono, human) != null);
        }

        /// <summary>
        /// The bed when it has room for two and one of the pair may use it, else null. The
        /// vanilla lovin' job lays BOTH pawns in the bed, and each of them reserves it with
        /// <c>Bed.SleepingSlotsCount</c> as the reservation's maxPawns - so a one-slot bed
        /// allows exactly one of the two: the second reservation fails, RimWorld logs
        /// "Could not reserve ... for maxPawns 1 and stackCount 0" and the job ends with
        /// JobCondition.Errored. Vanilla keeps the same rule: its lovin' giver only fires between
        /// two pawns already lying in one bed, and LovePartnerRelationUtility.GetPartnerInMyBed
        /// returns null for a bed with fewer than two slots. Either pawn may be the bed's owner,
        /// so both directions are asked - <see cref="RestUtility.FindBedFor"/> is called for each
        /// of them in turn.
        /// </summary>
        private static Building_Bed UsableSharedBed(Building_Bed bed, Pawn mamono, Pawn human)
        {
            if (bed == null || bed.SleepingSlotsCount <= 1)
            {
                return null;
            }
            if (RestUtility.IsValidBedFor(bed, mamono, human, true, false, false, null)
                || RestUtility.IsValidBedFor(bed, human, mamono, true, false, false, null))
            {
                return bed;
            }
            return null;
        }
    }
}
