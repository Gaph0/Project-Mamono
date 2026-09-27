using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectMamono
{
    /// <summary>
    /// Forms the tsugai (bonded-pair) bond between a Mamono and the man she knocks
    /// out, via a timed action (JobDriver_FormTsugai). The victim must be an adult,
    /// male, humanlike, non-Mamono, alive, and unattached. A Mamono bonds at most one
    /// living husband.
    /// </summary>
    public static class TsugaiFormation
    {
        private static ProjectMamonoSettings Settings => ProjectMamonoModSettings.Settings;

        /// <summary>Starts the timed bonding action; the bond forms on completion.</summary>
        public static void TryStartBondJob(Pawn mamono, Pawn man)
        {
            // Mamonos never target pawns below the bondable age.
            if (man?.ageTracker == null || man.ageTracker.AgeBiologicalYearsFloat < Settings.BondMinAge)
            {
                return;
            }

            // A visiting Mamono never takes a husband by force on her hosts' map - she is a
            // guest, and bonding a colonist (then kidnapping him) is exactly what the visit
            // should never produce. Wild Mamono (factionless) and hostile raiders still can.
            if (EssenceTransfer.IsVisitingGuest(mamono))
            {
                return;
            }

            if (!CanBond(mamono, man) || mamono.jobs == null || mamono.Downed || !mamono.Spawned)
            {
                return;
            }

            Job cur = mamono.CurJob;
            if (cur != null && cur.def == ProjectMamono_DefOf.ProjectMamono_FormTsugai && cur.targetA.Thing == man)
            {
                return;
            }

            mamono.jobs.StartJob(JobMaker.MakeJob(ProjectMamono_DefOf.ProjectMamono_FormTsugai, man), JobCondition.InterruptForced);
        }

        /// <summary>
        /// Forms the bond: Social relation, bond marker on both, mood thought.
        /// A forced bond (the default, combat-knockout path) drains the man dry;
        /// a voluntary bond charges him only the fixed essence cost from settings,
        /// grants both the "bound by choice" thought, and boosts the join chance.
        /// </summary>
        public static void TryBond(Pawn mamono, Pawn man, bool voluntary = false)
        {
            if (!CanBond(mamono, man))
            {
                return;
            }

            mamono.relations.AddDirectRelation(ProjectMamono_DefOf.ProjectMamono_Tsugai, man);

            // Forming the bond is paid for in the man's essence, moved into the
            // Mamono's mana (which also grants the shared mood buff and her Isekai
            // XP). A forced bond consumes everything he has left; a voluntary one
            // costs only the agreed share - essence is the budget that keeps one
            // man from collecting Mamonos faster than he can regenerate.
            Need_Essence essence = EssenceTransfer.Essence(man);
            if (essence != null && essence.CurLevel > 0.001f)
            {
                float amount = voluntary
                    ? Mathf.Min(Settings.VoluntaryBondEssenceCost, essence.CurLevel)
                    : essence.CurLevel;
                EssenceTransfer.Transfer(mamono, man, amount);
            }

            // Add the bond marker to both, recording each partner individually.
            // Re-bonding (after a previous husband has died) adds another record,
            // and the willpower bonus scales with the number of recorded bonds.
            Hediff_TsugaiBond.AddBondTo(mamono, man);
            Hediff_TsugaiBond.AddBondTo(man, mamono);

            GrantFoundMateThought(mamono);
            GrantFoundMateThought(man);

            if (voluntary)
            {
                GrantWillingThought(mamono, man);
                GrantWillingThought(man, mamono);
                NotifyVoluntaryBond(mamono, man);
            }

            DecideOutcome(mamono, man, voluntary);
        }

        /// <summary>The "bound by choice" memory each willing partner keeps of the other.</summary>
        private static void GrantWillingThought(Pawn pawn, Pawn partner)
        {
            var memories = pawn?.needs?.mood?.thoughts?.memories;
            ThoughtDef def = ProjectMamono_DefOf.ProjectMamono_TsugaiWilling;
            if (memories == null || def == null)
            {
                return;
            }
            memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(def), partner);
        }

        /// <summary>Lets the player know when a voluntary bond forms involving one of their colonists.</summary>
        private static void NotifyVoluntaryBond(Pawn mamono, Pawn man)
        {
            if (mamono.Faction?.IsPlayer != true && man.Faction?.IsPlayer != true)
            {
                return;
            }
            Messages.Message(
                $"{mamono.LabelShortCap} and {man.LabelShort} have formed a tsugai bond by choice.",
                new LookTargets(mamono, man),
                MessageTypeDefOf.PositiveEvent,
                historical: false);
        }

        /// <summary>Current number of living tsugai partners a pawn has.</summary>
        private static int LivingBondCount(Pawn pawn)
        {
            if (pawn?.relations == null)
            {
                return 0;
            }

            List<Pawn> partners = new List<Pawn>();
            pawn.relations.GetDirectRelations(ProjectMamono_DefOf.ProjectMamono_Tsugai, ref partners);
            int count = 0;
            for (int i = 0; i < partners.Count; i++)
            {
                if (partners[i] != null && !partners[i].Dead)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Grants the "found my mate" memory at a stage matching how many living mates
        /// the pawn now has: stage 0 (one mate), 1 (two), 2 (three or more). The thought
        /// stacks (stackLimitForSameOtherPawn = 1), so each new bond adds a fresh memory
        /// at the new tier.
        /// </summary>
        private static void GrantFoundMateThought(Pawn pawn)
        {
            var memories = pawn?.needs?.mood?.thoughts?.memories;
            ThoughtDef def = ProjectMamono_DefOf.ProjectMamono_TsugaiFormed;
            if (memories == null || def == null)
            {
                return;
            }

            int stage = Mathf.Clamp(LivingBondCount(pawn) - 1, 0, def.stages.Count - 1);
            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
            thought.SetForcedStage(stage);
            memories.TryGainMemory(thought);
        }

        /// <summary>
        /// Decides whether a Mamono who bonded to a colonist joins the colony or
        /// kidnaps him, then hands the outcome to the queue component. Neither the
        /// faction swap nor the Kidnap job may run here, inside the bonding job's
        /// toil cleanup - both change the pawn's job/faction and would corrupt the
        /// job driver mid-Cleanup (NullReferenceException). Deferred to the next tick.
        /// </summary>
        private static void DecideOutcome(Pawn mamono, Pawn man, bool voluntary = false)
        {
            // A WILD Mamono (a wild man) who forms a tsugai bond with a colonist is tamed outright
            // - no join-chance roll, no kidnapping. Bonding with her tamer is the moment she
            // chooses him, so she follows him home unconditionally. This runs BEFORE the faction
            // guard below because wild slimes/Mamono are FACTIONLESS (Faction == null) - a
            // factionless-pawn check would wrongly skip them. Deferred a tick via
            // BondOutcomeComponent so the faction/job change is safe outside the job driver's
            // cleanup.
            if (mamono.IsWildMan() && !mamono.IsColonist && man.Faction != null && man.Faction.IsPlayer)
            {
                BondOutcomeComponent.Queue(mamono, man, join: true, chance: 1f, manLevel: IsekaiCompat.GetLevel(man), mamonoLevel: IsekaiCompat.GetLevel(mamono));
                return;
            }

            // Only a bond with an actual colonist can pull her in, and only if she
            // still belongs to some other faction.
            if (mamono.Faction == null || mamono.Faction.IsPlayer || man.Faction == null || !man.Faction.IsPlayer)
            {
                return;
            }

            int manLevel = IsekaiCompat.GetLevel(man);
            int mamonoLevel = IsekaiCompat.GetLevel(mamono);

            // A Protagonist always wins her over - no roll, no kidnapping.
            bool isProtagonist = ProjectMamono_DefOf.Isekai_Protagonist != null
                && man.story?.traits?.HasTrait(ProjectMamono_DefOf.Isekai_Protagonist) == true;

            float chance = Settings.BondJoinBaseChance
                + (manLevel - mamonoLevel) * Settings.BondJoinChancePerLevel;
            // She already chose him once - a willing bond makes her far more
            // likely to follow him home.
            if (voluntary)
            {
                chance += Settings.VoluntaryBondJoinBonus;
            }
            chance = Mathf.Clamp(chance, 0f, Settings.BondJoinMaxChance);

            bool join = isProtagonist || Rand.Chance(chance);
            BondOutcomeComponent.Queue(mamono, man, join, chance, manLevel, mamonoLevel);
        }

        /// <summary>
        /// Runs a tick after the bond forms (via BondOutcomeComponent): moves the
        /// bonded Mamono into the player faction. Safe to change faction/jobs here.
        /// </summary>
        public static void ExecuteJoin(Pawn mamono, Pawn man, float chance, int manLevel, int mamonoLevel)
        {
            if (mamono == null || mamono.Dead || !mamono.Spawned)
            {
                return;
            }

            // Re-validate: skip only if she's already a colonist. A FACTIONLESS pawn
            // (Faction == null, e.g. a wild slime) is exactly who the wild-bond tame targets,
            // so she must NOT be rejected here for having no faction.
            if (mamono.Faction != null && mamono.Faction.IsPlayer)
            {
                return;
            }

            // Detach her from any raid/visitor lord and guest status before the swap.
            mamono.GetLord()?.RemovePawn(mamono);
            mamono.guest?.SetGuestStatus(null);

            // Use the vanilla recruit path so her guest status flips to colonist along with the
            // faction swap (a plain SetFaction alone can leave a wild/ex-guest pawn in a broken
            // guest state). Recruit handles wild men, prisoners and guests uniformly.
            RecruitUtility.Recruit(mamono, Faction.OfPlayer);

            Find.LetterStack.ReceiveLetter(
                $"{mamono.LabelShortCap} joins the colony",
                $"{mamono.LabelShortCap} bonded with {man.LabelShort} and chose to follow her husband into your colony.\n\nJoin chance: {chance.ToStringPercent()} (husband level {manLevel}, Mamono level {mamonoLevel}).",
                LetterDefOf.PositiveEvent,
                mamono);
        }

        /// <summary>
        /// When a bonded Mamono refuses to defect, she drags her new husband home:
        /// gives her a kidnap lord and starts the vanilla Kidnap job, which carries
        /// the (downed) husband to the map edge. On ExitMap the carried pawn is
        /// handed to her faction's KidnappedPawnsTracker, so he becomes her faction's
        /// captive (raisable via the usual ransom/rescue flows). Runs a tick after
        /// the bond forms, via BondOutcomeComponent, so it's safe to start a new job.
        /// </summary>
        public static void ExecuteKidnap(Pawn mamono, Pawn man)
        {
            if (mamono == null || man == null || mamono.Dead || man.Dead)
            {
                return;
            }

            Map map = mamono.Map;
            if (map == null || mamono.Faction == null || !mamono.Spawned || mamono.Downed)
            {
                return;
            }

            // Find a way off the map before committing to the abduction.
            if (!RCellFinder.TryFindBestExitSpot(mamono, out IntVec3 exitSpot, TraverseMode.ByPawn, true))
            {
                return;
            }

            // She kidnaps for her own people, so she needs to belong to a kidnap-capable faction.
            Faction taker = mamono.Faction;

            // Ensure she has a lord whose job allows the kidnapping.
            if (mamono.GetLord() == null)
            {
                LordMaker.MakeNewLord(taker, new LordJob_Kidnap(), map, new List<Pawn> { mamono });
            }

            Job kidnap = JobMaker.MakeJob(JobDefOf.Kidnap, man, exitSpot);
            kidnap.count = 1;
            mamono.jobs?.StartJob(kidnap, JobCondition.InterruptForced);

            Find.LetterStack.ReceiveLetter(
                $"{mamono.LabelShortCap} kidnapped {man.LabelShort}",
                $"{mamono.LabelShortCap} refused to leave her people after bonding with {man.LabelShort} - so she took her husband with her instead. She is dragging {man.LabelShort} off the map as a captive of {taker.Name}.\n\nIntercept her before she escapes, or rescue {man.LabelShort} later.",
                LetterDefOf.ThreatBig,
                new LookTargets(mamono, man));
        }

        /// <summary>
        /// All hard requirements for a tsugai bond between this Mamono and this man
        /// (gene, gender, age, one living husband per Mamono, Protagonist harem rule).
        /// Shared by the forced knockout path and the voluntary proposal system.
        /// </summary>
        public static bool CanBond(Pawn mamono, Pawn man)
        {
            if (mamono == null || man == null || mamono == man || mamono.Dead || man.Dead)
            {
                return false;
            }

            // The attacker must be a Mamono-carrier.
            if (mamono.genes == null || !mamono.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_Mamono))
            {
                return false;
            }

            // The victim must be a male humanlike of bondable age without the Mamono gene.
            if (man.RaceProps == null || !man.RaceProps.Humanlike
                || man.gender != Gender.Male
                || man.ageTracker == null || man.ageTracker.AgeBiologicalYearsFloat < Settings.BondMinAge)
            {
                return false;
            }

            // One living husband per Mamono at a time, and the man must be unattached -
            // unless he is a Protagonist, who may bond with multiple Mamonos. Once a
            // partner has died the survivor may bond again - the new bond stacks onto
            // the old hediff.
            return !HasBondedPartner(mamono) && (HasBondedPartner(man) ? IsProtagonist(man) : true);
        }

        /// <summary>
        /// Could this pawn, in principle, be someone's tsugai victim? A bondable pawn is
        /// a living male humanlike of bondable age without the Mamono gene who is either
        /// unattached or a bond-collecting Protagonist. The vanilla-impossible case - a
        /// lesbian-attracted pawn - is also excluded: vanilla SecondaryRomanceChanceFactor
        /// only makes same-gender attraction possible when the ROMANCED pawn is Gay, so a
        /// female pawn without the Gay trait can never reciprocate a Mamono and could never
        /// be bonded even if the gender rule were ignored. The berserk prey selection uses
        /// this to keep a starving Mamono from mauling women she could never actually bond.
        /// (The Mamono's own Gay trait does not change this - it is the target's trait that
        /// gates same-gender romance for a female initiator in vanilla.)
        /// </summary>
        public static bool IsBondable(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            if (pawn.gender != Gender.Male
                || pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYearsFloat < Settings.BondMinAge)
            {
                return false;
            }

            // Mamonos are always female, so a bond candidate must be attracted to women.
            // Per the vanilla romance math above, only Gay pawns can feel same-gender
            // attraction, so a non-Gay female is permanently unbondable.
            if (pawn.gender == Gender.Female && !(pawn.story?.traits?.HasTrait(TraitDefOf.Gay) ?? false))
            {
                return false;
            }

            if (pawn.genes != null && pawn.genes.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_Mamono))
            {
                return false;
            }

            return !HasBondedPartner(pawn) || IsProtagonist(pawn);
        }

        /// <summary>True while the pawn has at least one living tsugai partner.</summary>
        public static bool HasBondedPartner(Pawn pawn)
        {
            return pawn?.relations != null
                && pawn.relations.GetFirstDirectRelationPawn(ProjectMamono_DefOf.ProjectMamono_Tsugai, p => p != null && !p.Dead) != null;
        }

        /// <summary>
        /// Called when a pawn dies (from the Notify_PawnKilled postfix on their
        /// relations tracker). Every surviving tsugai partner loses their bond
        /// hediff, gains the bond-grief debuff (a willpower penalty), and takes a
        /// heavy mood hit - stronger than losing a spouse.
        /// </summary>
        public static void HandleBondPartnerDied(Pawn dead)
        {
            if (dead?.relations == null)
            {
                return;
            }

            // Snapshot first: we mutate each partner's relations inside the loop.
            // GetDirectRelations fills a caller-supplied list (there is no
            // single-argument overload).
            List<Pawn> partners = new List<Pawn>();
            dead.relations.GetDirectRelations(ProjectMamono_DefOf.ProjectMamono_Tsugai, ref partners);
            partners.RemoveAll(p => p == null || p == dead || p.Dead);

            foreach (Pawn partner in partners)
            {
                // Sever the relation both ways. TryRemoveDirectRelation is the silent
                // variant - RemoveDirectRelation logs a "Could not remove relation"
                // warning when the bond is already gone, which is expected here (the
                // relation is symmetric, so the first removal can clear both sides).
                partner.relations?.TryRemoveDirectRelation(ProjectMamono_DefOf.ProjectMamono_Tsugai, dead);
                dead.relations.TryRemoveDirectRelation(ProjectMamono_DefOf.ProjectMamono_Tsugai, partner);

                // Remove only the bond to the dead partner, keeping any other living
                // bonds (a harem Protagonist keeps his surviving Mamonos' bonds). The
                // bond hediff is removed entirely only when no bonds remain.
                Hediff_TsugaiBond.RemoveBondFrom(partner, dead);

                // Apply the grief debuff that lowers willpower, recording which
                // partner's death caused it so a later resurrection can be matched.
                if (ProjectMamono_DefOf.ProjectMamono_TsugaiLoss != null)
                {
                    if (!(partner.health?.hediffSet?.GetFirstHediffOfDef(ProjectMamono_DefOf.ProjectMamono_TsugaiLoss) is Hediff_TsugaiLoss grief))
                    {
                        grief = partner.health?.AddHediff(ProjectMamono_DefOf.ProjectMamono_TsugaiLoss) as Hediff_TsugaiLoss;
                    }
                    grief?.RecordLostPartner(dead.thingIDNumber);
                }

                // The heavy mood penalty.
                if (ProjectMamono_DefOf.ProjectMamono_TsugaiLossThought != null)
                {
                    partner.needs?.mood?.thoughts?.memories?.TryGainMemory(ProjectMamono_DefOf.ProjectMamono_TsugaiLossThought);
                }
            }
        }

        /// <summary>True if the pawn carries the Isekai Protagonist trait.</summary>
        private static bool IsProtagonist(Pawn pawn)
        {
            return ProjectMamono_DefOf.Isekai_Protagonist != null
                && pawn?.story?.traits?.HasTrait(ProjectMamono_DefOf.Isekai_Protagonist) == true;
        }

        /// <summary>
        /// Called when a pawn is resurrected (from the ResurrectionUtility.TryResurrect
        /// postfix). For every living pawn still carrying bond grief, if the revived
        /// pawn is the partner they recorded, swap the grief for a positive "mate
        /// returned" moodlet and remove the willpower debuff. The bond is NOT
        /// re-established - they part as former mates.
        /// </summary>
        public static void HandleBondPartnerResurrected(Pawn revived)
        {
            if (revived == null)
            {
                return;
            }

            foreach (Pawn pawn in PawnsFinder.AllMapsAndWorld_Alive)
            {
                if (pawn == null || pawn == revived || pawn.Dead)
                {
                    continue;
                }

                // Only pawns grieving a broken bond can have it restored, and only
                // if the revived pawn is the partner whose death caused the grief.
                if (!(pawn.health?.hediffSet?.GetFirstHediffOfDef(ProjectMamono_DefOf.ProjectMamono_TsugaiLoss) is Hediff_TsugaiLoss grief)
                    || !grief.Grieves(revived.thingIDNumber))
                {
                    continue;
                }

                // Remove the grief debuff (restores willpower).
                pawn.health.RemoveHediff(grief);

                // Swap the negative memory for a positive one.
                var memories = pawn.needs?.mood?.thoughts?.memories;
                if (memories != null)
                {
                    if (ProjectMamono_DefOf.ProjectMamono_TsugaiLossThought != null)
                    {
                        memories.RemoveMemoriesOfDef(ProjectMamono_DefOf.ProjectMamono_TsugaiLossThought);
                    }
                    if (ProjectMamono_DefOf.ProjectMamono_TsugaiRestored != null)
                    {
                        memories.TryGainMemory(ProjectMamono_DefOf.ProjectMamono_TsugaiRestored);
                    }
                }
            }
        }
    }
}
