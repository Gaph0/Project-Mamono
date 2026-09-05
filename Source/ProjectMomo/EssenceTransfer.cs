using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Shared logic for moving essence from a human's Essence need into a Momo's
    /// Mana need, used by both the human's "Give essence" and the Momo's "Drain
    /// essence" actions. The amount moved is limited by how much essence the human
    /// has and how much mana the Momo can take.
    /// </summary>
    public static class EssenceTransfer
    {
        /// <summary>True if the pawn carries the Momo gene.</summary>
        public static bool IsMomo(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo);
        }

        /// <summary>
        /// True while the pawn is a visitor: a member of another, non-hostile faction
        /// on the colony map (not a colonist, not a wild man, not a raider). Visitors
        /// are guests of the colony — hostile pawns (raiders) are not "visiting" even
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

        /// <summary>The pawn's Essence need, or null.</summary>
        public static Need_Essence Essence(Pawn pawn)
        {
            return pawn?.needs?.TryGetNeed(ProjectMomo_DefOf.ProjectMomo_Essence) as Need_Essence;
        }

        /// <summary>The pawn's Mana need, or null.</summary>
        public static Need_Mana Mana(Pawn pawn)
        {
            return pawn?.needs?.TryGetNeed(ProjectMomo_DefOf.ProjectMomo_Mana) as Need_Mana;
        }

        /// <summary>
        /// True if <paramref name="momo"/> can draw essence from <paramref name="human"/>:
        /// a living Momo with a Mana need and a living, non-Momo humanlike with an
        /// Essence need that has something left to give. A Momo who carries a living
        /// tsugai bond may only feed from a bonded partner — she will not draw from
        /// a human she is not bonded to.
        /// </summary>
        public static bool CanTransfer(Pawn momo, Pawn human)
        {
            if (momo == null || human == null || momo == human || momo.Dead || human.Dead)
            {
                return false;
            }
            if (!IsMomo(momo) || IsMomo(human))
            {
                return false;
            }
            if (human.RaceProps == null || !human.RaceProps.Humanlike)
            {
                return false;
            }
            if (Mana(momo) == null)
            {
                return false;
            }
            Need_Essence essence = Essence(human);
            if (essence == null || essence.CurLevel <= 0.001f)
            {
                return false;
            }
            return BondAllowsFeeding(momo, human);
        }

        /// <summary>
        /// The bonded-feeding rule on its own, so callers (float menu) can explain
        /// why an otherwise-valid transfer is refused.
        /// </summary>
        public static bool BondAllowsFeeding(Pawn momo, Pawn human)
        {
            if (momo?.relations == null || human == null)
            {
                return true;
            }

            List<Pawn> bonds = new List<Pawn>();
            momo.relations.GetDirectRelations(ProjectMomo_DefOf.ProjectMomo_Tsugai, ref bonds);
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
            // No living bonds: a man marked by another Momo is claimed —
            // unbonded Momos respect the claim and leave him to her.
            if (Incubisation.MarkProtects(momo, human))
            {
                return false;
            }
            // Unmarked: unbonded Momos may feed from anyone valid.
            return true;
        }

        /// <summary>
        /// Moves up to <paramref name="amount"/> essence from the human to the Momo's
        /// mana. Returns the amount actually transferred. When
        /// <paramref name="intimateSideEffects"/> is false, the lovin' memory and the
        /// follow-up lovin' job are skipped — used when an external system (the
        /// Intimacy mod) has already handled the act itself.
        /// </summary>
        public static float Transfer(Pawn momo, Pawn human, float amount, bool intimateSideEffects = true)
        {
            Need_Essence essence = Essence(human);
            Need_Mana mana = Mana(momo);
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

            // A Momo who broke from the hunger and finally fed gets catharsis:
            // a big, multi-day relief buff. Checked BEFORE the level rises, because
            // the break belongs to the starvation episode this feeding ends —
            // granted only while she is still in (or recovering from) the mental
            // state, so routine top-ups never trigger it.
            if (IsBreaking(momo))
            {
                TryGrantCatharsis(momo);
            }

            // Both parties share a warm 1-day mood buff.
            ThoughtDef shared = ProjectMomo_DefOf.ProjectMomo_EssenceShared;
            if (shared != null)
            {
                momo.needs?.mood?.thoughts?.memories?.TryGainMemory(shared);
                human.needs?.mood?.thoughts?.memories?.TryGainMemory(shared);
            }

            if (intimateSideEffects)
            {
                // The transfer is intimate: treat it as vanilla lovin' for both. GotSomeLovin
                // is a social memory — granted with the partner as otherPawn, it supplies the
                // lovin' mood buff AND the opinion boost that keeps their compatibility up.
                GrantLovin(momo, human);
                GrantLovin(human, momo);

                // Then have the pair actually perform the vanilla lovin' job (the full
                // animation) in a shared bed. Queued to the next tick: this runs inside
                // the Drain/Give job's finish action (toil Cleanup), and starting a new
                // job here would interrupt the job being cleaned up and throw a
                // NullReferenceException.
                LovinQueueComponent.Queue(momo, human);
            }

            // The Momo earns Isekai XP scaled by how much she consumed.
            IsekaiCompat.AwardEssenceXP(momo, moved);

            // Her mana seeps into him with every feeding: the slow road to incubisation.
            Incubisation.ApplyDose(momo, human, moved);

            // A full incubus no longer lives on food alone — his mate's mana,
            // exchanged in the act, nourishes him directly. He regains a little
            // Food scaled by the essence moved (works for both Give and Drain,
            // since both route through here).
            if (Incubisation.IsFullIncubus(human) && human.needs?.food != null)
            {
                float factor = ProjectMomoModSettings.Settings?.IncubusFoodPerEssence ?? 0.5f;
                Need_Food food = human.needs.food;
                food.CurLevel = Mathf.Min(food.MaxLevel, food.CurLevel + moved * factor);
            }

            return moved;
        }

        /// <summary>True while the Momo is in a mental state — the starvation
        /// feeding break, essence berserk, or any other break a low-mana episode
        /// could have thrown her into.</summary>
        private static bool IsBreaking(Pawn momo)
        {
            return momo?.mindState?.mentalStateHandler != null
                && momo.mindState.mentalStateHandler.InMentalState;
        }

        /// <summary>Grants the feeding-catharsis memory to a Momo who fed her way
        /// out of a break. No-op if she lacks a mood (prisoners without needs, etc.).</summary>
        private static void TryGrantCatharsis(Pawn momo)
        {
            ThoughtDef catharsis = ProjectMomo_DefOf.ProjectMomo_ManaCatharsis;
            if (catharsis == null)
            {
                return;
            }
            momo.needs?.mood?.thoughts?.memories?.TryGainMemory(catharsis);
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
        /// so it's safe to change the Momo's job. Requires a free, reservable bed both
        /// can use; silently does nothing if none is available by then. The initiator's
        /// job drives the bounce for both.
        /// </summary>
        public static void StartLovinNow(Pawn momo, Pawn human)
        {
            if (momo?.Map == null || human == null || momo.Map != human.Map)
            {
                return;
            }
            if (!momo.Spawned || !human.Spawned || momo.Downed || human.Downed)
            {
                return;
            }
            if (momo.jobs == null || human.jobs == null)
            {
                return;
            }

            // The vanilla lovin' job needs a bed both partners can reserve.
            Building_Bed bed = RestUtility.FindBedFor(momo, human, true, false, null);
            if (bed == null)
            {
                bed = RestUtility.FindBedFor(human, momo, true, false, null);
            }
            if (bed == null)
            {
                return;
            }

            Job lovin = JobMaker.MakeJob(JobDefOf.Lovin, human, bed);
            momo.jobs.StartJob(lovin, JobCondition.InterruptForced);
        }
    }
}
