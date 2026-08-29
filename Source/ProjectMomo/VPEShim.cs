using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VanillaPsycastsExpanded;
using VEF.Abilities;
using Verse;
using AbilityDef = VEF.Abilities.AbilityDef;

namespace ProjectMomo
{
    /// <summary>
    /// The VPE-dependent half of the psycast integration. Grants a pawn VPE
    /// psycasts exactly the way VPE's own pawn-generation patch equips its
    /// caster pawn kinds (see VanillaPsycastsExpanded.PawnGen_Patch):
    /// psylink on the brain -> VPE_PsycastAbilityImplant -> InitializeFromPsylink
    /// -> unlock one path -> grant random psycasts from it (+1 psylink level
    /// each, spending the point it awards) -> spend the stat-upgrade points on
    /// VPE's psycaster stats. Net result: psylink level = abilities + stat
    /// upgrades, no unspent points, same as VPE's Empire_Caster_* pawn kinds.
    ///
    /// SOFT DEPENDENCY: no other type may touch this one unless VPE is active
    /// (VPE hard-requires the Vanilla Expanded Framework, so both assemblies are
    /// guaranteed present whenever this runs).
    /// </summary>
    public static class VPEShim
    {
        // VPE's psycast-abilities implant hediff (the Hediff_PsycastAbilities class).
        public const string ImplantDefName = "VPE_PsycastAbilityImplant";

        public static void Grant(Pawn pawn, MomoPsycastExtension extension)
        {
            HediffDef implantDef = DefDatabase<HediffDef>.GetNamedSilentFail(ImplantDefName);
            if (implantDef == null)
            {
                Log.Warning("[Project Momo] VPE compat: VPE_PsycastAbilityImplant hediff not found, skipping psycast grant.");
                return;
            }

            // Pick a path the pawn may actually unlock: VPE locks some paths
            // behind backstories, memes, genes or meditation foci.
            PsycasterPathDef path = extension.pathDefNames
                .Select(defName => DefDatabase<PsycasterPathDef>.GetNamedSilentFail(defName))
                .Where(def => def != null && def.CanPawnUnlock(pawn))
                .RandomElementWithFallback();
            if (path == null)
            {
                return;
            }

            // Already a psycaster (e.g. VPE's Basilicus storyteller roll fired
            // first) — leave the pawn as VPE made it.
            if (pawn.health.hediffSet.GetFirstHediffOfDef(implantDef) is Hediff_PsycastAbilities)
            {
                return;
            }

            // Psylink on the brain (whole body for a race with no brain part).
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (!(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) is Hediff_Psylink psylink))
            {
                psylink = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn, brain);
                pawn.health.AddHediff(psylink, brain);
            }

            // VPE's implant. (VPE's own pawn kinds receive theirs through VEF's
            // ability extension; we add it directly.) Created with the brain as
            // its part, exactly like VEF's generator does — MakeHediff with a
            // part also stamps hediff.Part, which matters because vanilla's
            // Hediff_Level.PostAdd logs an error for any levelled hediff added
            // with a null Part. (VEF's own AddHediff(implant, null) triggers
            // that same error for VPE's built-in casters; it is harmless, but
            // slimes spawn in packs and the log spam is not.)
            Hediff implant = HediffMaker.MakeHediff(implantDef, pawn, brain);
            pawn.health.AddHediff(implant);
            var psycasts = (Hediff_PsycastAbilities)implant;
            if (psycasts.psylink == null)
            {
                psycasts.InitializeFromPsylink(psylink);
            }

            psycasts.UnlockPath(path);

            // Grant random psycasts from the path. Each grant costs the point the
            // level-up awarded, so no unspent points remain — the same
            // bookkeeping as VPE's own caster generation.
            CompAbilities comp = pawn.GetComp<CompAbilities>();
            int abilityCount = extension.initialAbilities.RandomInRange;
            if (comp != null && abilityCount > 0)
            {
                List<AbilityDef> candidates = path.abilityLevelsInOrder
                    .Take(path.MaxLevel)
                    .SelectMany(level => level)
                    .Where(ability => ability != null && ability != PsycasterPathDef.Blank)
                    .ToList();

                while (abilityCount-- > 0)
                {
                    List<AbilityDef> learnable = candidates.Where(ability => PrereqsCompleted(ability, comp)).ToList();
                    if (learnable.Count == 0)
                    {
                        break;
                    }

                    AbilityDef chosen = learnable.RandomElement();
                    comp.GiveAbility(chosen);
                    psycasts.ChangeLevel(1, false);
                    psycasts.points--;
                    candidates.Remove(chosen);
                }
            }

            // Remaining levels go into VPE's psycaster stat upgrades.
            int statPoints = extension.statUpgradePoints.RandomInRange;
            if (statPoints > 0)
            {
                psycasts.ChangeLevel(statPoints);
                psycasts.points -= statPoints;
                psycasts.ImproveStats(statPoints);
            }
        }

        private static bool PrereqsCompleted(AbilityDef ability, CompAbilities comp)
        {
            var psycastExtension = ability.GetModExtension<AbilityExtension_Psycast>();
            return psycastExtension == null || psycastExtension.PrereqsCompleted(comp);
        }
    }
}
