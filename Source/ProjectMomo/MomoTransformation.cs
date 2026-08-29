using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectMomo
{
    /// <summary>
    /// The single choke point for turning a female human into a monster (a Momo,
    /// a slime, or any xenotype that carries the Momo gene). Every entry path
    /// funnels through <see cref="ApplyXenotype"/>: mana corruption
    /// (Hediff_MomoCorruption at full severity), xenogerm implantation, and dev
    /// mode. The re-transformation guard lives in <see cref="CanEverTransform(Pawn)"/>:
    /// a pawn who already carries the Momo gene can never be transformed again.
    ///
    /// The transformation copies the corruptor's own xenotype: every gene from
    /// her XenotypeDef is added to the victim as an ENDOGENE (never a xenogene),
    /// so her children inherit the full monster xenotype. Vanilla snapshots a
    /// baby's genes on the pregnancy hediff at CONCEPTION (HediffWithParents.geneSet),
    /// and the BabyXenotypeInheritancePatch re-reads the mother's live endogenes
    /// only if the snapshot still holds her old pre-monster self. So after a
    /// transformation we also refresh that snapshot — RefreshPregnancySnapshot —
    /// otherwise a woman conceived while human gives birth to a baby stamped
    /// with her new xenotype but missing its genes (exactly the bug reported:
    /// a "slime" baby with no slime genes).
    /// </summary>
    public static class MomoTransformation
    {
        private static ProjectMomoSettings Settings => ProjectMomoModSettings.Settings;

        /// <summary>
        /// True if the xenotype is a "monster" xenotype — one whose gene list
        /// includes the Momo gene. This is the registry that makes corruption
        /// mutually exclusive across mods: any such xenotype both imprints
        /// victims and counts as "already a monster".
        /// </summary>
        public static bool IsMonsterXenotype(XenotypeDef def)
        {
            return def?.genes != null && ProjectMomo_DefOf.ProjectMomo_Momo != null
                && def.genes.Contains(ProjectMomo_DefOf.ProjectMomo_Momo);
        }

        /// <summary>
        /// The xenotype a corruptor imprints on her victims: her own def-based
        /// monster xenotype (covers slime variants with no extra code), or the
        /// base Momo xenotype when hers is custom or absent.
        /// </summary>
        public static XenotypeDef XenotypeFor(Pawn corruptor)
        {
            if (corruptor?.genes != null && !corruptor.genes.UniqueXenotype)
            {
                XenotypeDef def = corruptor.genes.Xenotype;
                if (IsMonsterXenotype(def))
                {
                    return def;
                }
            }
            return ProjectMomo_DefOf.ProjectMomo_Xenotype_Momo;
        }

        /// <summary>
        /// Could this pawn ever be transformed? A living, female, non-monster
        /// humanlike of corruptible age. The Momo-gene check doubles as the
        /// re-transformation guard: a completed (or dev-applied) change can
        /// never re-trigger. Pregnancy does NOT block transformation — the
        /// mother's new endogenes pass to the baby at birth.
        /// </summary>
        public static bool CanEverTransform(Pawn pawn)
        {
            return CanEverTransform(pawn, out _);
        }

        /// <summary>As <see cref="CanEverTransform(Pawn)"/>, with a player-facing reason for refusals.</summary>
        public static bool CanEverTransform(Pawn pawn, out string reason)
        {
            reason = null;
            if (pawn == null || pawn.Dead)
            {
                return false;
            }
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike || pawn.genes == null)
            {
                reason = "not human";
                return false;
            }
            if (EssenceTransfer.IsMomo(pawn))
            {
                reason = "already a monster";
                return false;
            }
            if (pawn.gender != Gender.Female)
            {
                reason = "not female";
                return false;
            }
            if (pawn.ageTracker == null || pawn.ageTracker.AgeBiologicalYearsFloat < Settings.CorruptionMinAge)
            {
                reason = "too young";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Orders the Momo to walk to the victim and begin an infusion
        /// (JobDriver_InfuseMomo). Re-validates and no-ops if she is already
        /// infusing this target.
        /// </summary>
        public static void TryStartInfusionJob(Pawn momo, Pawn victim)
        {
            if (!ProjectMomoModSettings.Settings.CorruptionEnabled
                || !EssenceTransfer.IsMomo(momo) || !CanEverTransform(victim)
                || momo.jobs == null || momo.Downed || !momo.Spawned)
            {
                return;
            }

            Job cur = momo.CurJob;
            if (cur != null && cur.def == ProjectMomo_DefOf.ProjectMomo_InfuseMomo && cur.targetA.Thing == victim)
            {
                return;
            }

            momo.jobs.StartJob(JobMaker.MakeJob(ProjectMomo_DefOf.ProjectMomo_InfuseMomo, victim), JobCondition.InterruptForced);
        }

        /// <summary>
        /// One completed mana infusion: pays the Momo's mana, creates or advances
        /// the victim's corruption hediff by one dose (CorruptionSeverityPerInfusion),
        /// and imprints the corruptor's xenotype — last corruptor wins, so a
        /// different-xenotype Momo infusing a half-corrupted woman overwrites the
        /// imprint while keeping her accumulated progress. Fires the transformation
        /// at full severity.
        /// </summary>
        public static void ApplyInfusion(Pawn momo, Pawn victim)
        {
            var settings = Settings;
            if (!settings.CorruptionEnabled || !EssenceTransfer.IsMomo(momo) || !CanEverTransform(victim))
            {
                return;
            }

            // Pay the mana cost. The float menu gates on this, but re-check here:
            // a starving Momo may have drained since the order was given.
            Need_Mana mana = EssenceTransfer.Mana(momo);
            if (mana == null || mana.CurLevel < settings.CorruptionManaCost)
            {
                return;
            }
            mana.CurLevel -= settings.CorruptionManaCost;

            Hediff_MomoCorruption corruption = victim.health?.hediffSet?
                .GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_MomoCorruption) as Hediff_MomoCorruption;
            bool fresh = corruption == null;
            if (fresh)
            {
                corruption = HediffMaker.MakeHediff(ProjectMomo_DefOf.ProjectMomo_MomoCorruption, victim) as Hediff_MomoCorruption;
                if (corruption == null)
                {
                    return;
                }
                victim.health.AddHediff(corruption);
            }

            corruption.Imprint(momo);
            corruption.Severity = fresh
                ? settings.CorruptionSeverityPerInfusion
                : corruption.Severity + settings.CorruptionSeverityPerInfusion;

            if (corruption.Severity >= corruption.def.maxSeverity - 0.0001f)
            {
                corruption.CompleteTransformation();
            }
        }

        /// <summary>
        /// Applies the full transformation. Refuses (returns false, no-op) when
        /// the guards say no or she already IS the target xenotype — making every
        /// entry path idempotent. Adds the xenotype's missing genes as endogenes,
        /// stamps her with the xenotype identity, clears the corruption hediff,
        /// grants the awakening memory, and notifies the player when relevant.
        /// </summary>
        public static bool ApplyXenotype(Pawn pawn, XenotypeDef xenotype, Pawn source = null)
        {
            if (!CanEverTransform(pawn) || xenotype == null || pawn.genes.Xenotype == xenotype)
            {
                return false;
            }

            // Add every xenotype gene she lacks as an ENDOGENE (heritable — her
            // children get the full monster xenotype). Genes conflicting with the
            // incoming set (e.g. her old skin colour) are removed first.
            // Gene_Momo.PostAdd feminizes her, restyles body/hair and dirties her
            // graphics; the gene's enablesNeeds/disablesNeeds swap Essence for Mana.
            for (int i = 0; i < xenotype.genes.Count; i++)
            {
                GeneDef gene = xenotype.genes[i];
                RemoveConflictingGenes(pawn, gene);
                if (!pawn.genes.HasActiveGene(gene))
                {
                    pawn.genes.AddGene(gene, xenogene: false);
                }
            }

            // Stamp her with the corruptor's xenotype identity.
            pawn.genes.SetXenotypeDirect(xenotype);

            // The corruption that brought her here is spent.
            Hediff corruption = pawn.health?.hediffSet?.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_MomoCorruption);
            if (corruption != null)
            {
                pawn.health.RemoveHediff(corruption);
            }

            // If she is pregnant, rewrite the baby's conception snapshot from her
            // NEW genes so the baby inherits the monster xenotype too — the
            // snapshot was frozen at conception, before she transformed.
            RefreshPregnancySnapshot(pawn);

            // The awakening memory: reborn, and euphoric about it.
            ThoughtDef awakened = ProjectMomo_DefOf.ProjectMomo_MomoAwakened;
            if (awakened != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(awakened);
            }

            // She remembers the Momo whose mana remade her, warmly.
            ThoughtDef turned = ProjectMomo_DefOf.ProjectMomo_MomoTurned;
            if (turned != null && source != null && source != pawn)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                    (Thought_Memory)ThoughtMaker.MakeThought(turned), source);
            }

            if (pawn.Spawned && pawn.Drawer?.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            NotifyTransformed(pawn, xenotype, source);
            DecideJoinOutcome(pawn, source);
            return true;
        }

        /// <summary>
        /// Decides whether a non-colonist transformed by one of your colonists
        /// joins the colony, then hands the outcome to the queue component. The
        /// faction swap may not run here, inside the infusion job's toil cleanup —
        /// it would change the pawn's faction/job while the old JobDriver is still
        /// mid-Cleanup (the same bug BondOutcomeComponent works around). Deferred
        /// to the next tick. Mirroring the tsugai rule: a Protagonist corruptor
        /// always wins her over, no roll.
        /// </summary>
        private static void DecideJoinOutcome(Pawn pawn, Pawn source)
        {
            // Only corruption by an actual colonist can pull her in, and only if
            // she still belongs to some other faction.
            if (pawn.Faction == null || pawn.Faction.IsPlayer || source == null || source.Faction?.IsPlayer != true)
            {
                return;
            }

            int sourceLevel = IsekaiCompat.GetLevel(source);
            int pawnLevel = IsekaiCompat.GetLevel(pawn);

            bool isProtagonist = ProjectMomo_DefOf.Isekai_Protagonist != null
                && source.story?.traits?.HasTrait(ProjectMomo_DefOf.Isekai_Protagonist) == true;

            float chance = Settings.CorruptionJoinBaseChance
                + (sourceLevel - pawnLevel) * Settings.CorruptionJoinChancePerLevel;
            // A woman who chose the change is far more likely to stay with her
            // new sister than one taken by force.
            bool hadCorruption = false;
            if (pawn.health?.hediffSet != null)
            {
                hadCorruption = pawn.health.hediffSet.HasHediff(ProjectMomo_DefOf.ProjectMomo_MomoCorruption);
            }
            if (!hadCorruption)
            {
                chance += Settings.VoluntaryCorruptionJoinBonus;
            }
            chance = Mathf.Clamp(chance, 0f, Settings.CorruptionJoinMaxChance);

            bool join = isProtagonist || Rand.Chance(chance);
            TransformOutcomeComponent.Queue(pawn, source, join, chance, sourceLevel, pawnLevel);
        }

        /// <summary>
        /// Runs a tick after the transformation (via TransformOutcomeComponent):
        /// moves the new monster into the player faction. Safe to change faction
        /// and jobs here. Mirrors TsugaiFormation.ExecuteJoin.
        /// </summary>
        public static void ExecuteTransformJoin(Pawn pawn, Pawn source, float chance, int sourceLevel, int pawnLevel)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            // Re-validate: only pull her in if she's still factioned and not already ours.
            if (pawn.Faction == null || pawn.Faction.IsPlayer)
            {
                return;
            }

            // Detach her from any raid/visitor lord and guest status before the swap.
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.guest?.SetGuestStatus(null);

            pawn.SetFaction(Faction.OfPlayer);

            string sourceName = source != null ? source.LabelShort : "a colonist";
            Find.LetterStack.ReceiveLetter(
                $"{pawn.LabelShortCap} joins the colony",
                $"{pawn.LabelShortCap} was transformed by {sourceName} and chose to stay with your colony.\n\nJoin chance: {chance.ToStringPercent()} (corruptor level {sourceLevel}, her level {pawnLevel}).",
                LetterDefOf.PositiveEvent,
                pawn);
        }

        /// <summary>
        /// Rebuilds the conception gene snapshot on a transformed mother's
        /// pregnancy hediff from her live endogenes. Vanilla takes this snapshot
        /// at conception (HediffWithParents.geneSet) and feeds it to
        /// ApplyBirthOutcome at birth; the BabyXenotypeInheritancePatch only
        /// substitutes the mother's live genes when the snapshot still reflects
        /// her old pre-monster genome. Refreshing it here keeps a transformed
        /// mother's unborn baby in step with her new xenotype.
        /// </summary>
        private static void RefreshPregnancySnapshot(Pawn mother)
        {
            if (mother?.health?.hediffSet == null || mother.genes == null)
            {
                return;
            }

            // The pregnancy hediff (and its geneSet snapshot) lives on
            // HediffWithParents; Hediff_Pregnant is the human-pregnancy subclass.
            HediffWithParents pregnancy = null;
            var hediffs = mother.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HediffWithParents candidate && candidate.geneSet != null
                    && candidate.Mother == mother)
                {
                    pregnancy = candidate;
                    break;
                }
            }
            if (pregnancy == null)
            {
                return;
            }

            // Recompute the inherited set through the same patched vanilla entry
            // point the conception used, so the inheritance patch's logic (mother
            // is now a monster: pass her full endogenes, nothing from the father)
            // decides the new snapshot. GetInheritedGenes reads the mother's LIVE
            // genes, which is exactly what we want post-transformation.
            List<GeneDef> inherited = PregnancyUtility.GetInheritedGenes(pregnancy.Father, mother, out bool success);
            if (!success || inherited == null)
            {
                return;
            }

            // Rebuild the snapshot in place: GeneSet has no clear method, but it
            // is mutable through AddGene and exposes its list read-only, so the
            // reliable path is a fresh GeneSet carrying the new genes.
            GeneSet fresh = new GeneSet();
            for (int i = 0; i < inherited.Count; i++)
            {
                fresh.AddGene(inherited[i]);
            }
            fresh.SortGenes();
            pregnancy.geneSet = fresh;
        }

        /// <summary>
        /// Removes any of the pawn's existing genes whose exclusion tags clash
        /// with the incoming gene (e.g. a previous Skin_* colour before the
        /// xenotype's own is added).
        /// </summary>
        private static void RemoveConflictingGenes(Pawn pawn, GeneDef incoming)
        {
            if (incoming.exclusionTags.NullOrEmpty())
            {
                return;
            }

            for (int i = pawn.genes.GenesListForReading.Count - 1; i >= 0; i--)
            {
                Gene existing = pawn.genes.GenesListForReading[i];
                if (existing.def.exclusionTags.NullOrEmpty())
                {
                    continue;
                }
                for (int t = 0; t < incoming.exclusionTags.Count; t++)
                {
                    if (existing.def.exclusionTags.Contains(incoming.exclusionTags[t]))
                    {
                        pawn.genes.RemoveGene(existing);
                        break;
                    }
                }
            }
        }

        /// <summary>Lets the player know when one of their pawns is involved on either side of a transformation.</summary>
        private static void NotifyTransformed(Pawn pawn, XenotypeDef xenotype, Pawn source)
        {
            if (pawn.Faction?.IsPlayer != true && source?.Faction?.IsPlayer != true)
            {
                return;
            }

            string text = source != null
                ? $"{pawn.LabelShortCap} has been transformed into a {xenotype.label} by {source.LabelShort}!"
                : $"{pawn.LabelShortCap} has been transformed into a {xenotype.label}!";
            Messages.Message(text, new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}
