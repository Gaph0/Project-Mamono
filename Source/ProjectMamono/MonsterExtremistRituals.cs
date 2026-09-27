using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Ritual roles for the Monster Extremists rites of awakening. All three are
    /// only referenced by defs gated behind MayRequire Ideology, so they load
    /// only when the DLC is active.
    /// </summary>

    /// <summary>The rite's organiser: a colonist who is herself a monster.</summary>
    public class RitualRoleMamono : RitualRole
    {
        /// <summary>Not an ideo-role-bound role (any Mamono may lead); always false.</summary>
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual precept = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }

        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget,
            LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (!AppliesIfChild(p, out reason))
            {
                return false;
            }
            if (p.Dead || p.RaceProps == null || !p.RaceProps.Humanlike)
            {
                reason = Label + " must be humanlike";
                return false;
            }
            if (p.Faction == null || !p.Faction.IsPlayer)
            {
                reason = Label + " must be a colonist";
                return false;
            }
            if (!EssenceTransfer.IsMamono(p))
            {
                reason = Label + " must be a monster";
                return false;
            }
            return true;
        }
    }

    /// <summary>The rite's target: a free colonist woman who can still be transformed.</summary>
    public class RitualRoleTransformableColonist : RitualRole
    {
        /// <summary>Not an ideo-role-bound role; always false.</summary>
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual precept = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }

        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget,
            LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (!AppliesIfChild(p, out reason))
            {
                return false;
            }
            if (!p.IsColonist || p.IsPrisoner || p.IsSlave)
            {
                reason = Label + " must be a free colonist";
                return false;
            }
            if (!MamonoTransformation.CanEverTransform(p, out string inner))
            {
                reason = Label + " cannot be transformed" + (inner != null ? $" ({inner})" : "");
                return false;
            }
            return true;
        }
    }

    /// <summary>The rite's target: a prisoner or slave of the colony who can still be transformed.</summary>
    public class RitualRoleTransformableCaptive : RitualRole
    {
        /// <summary>Not an ideo-role-bound role; always false.</summary>
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual precept = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }

        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget,
            LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null,
            Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (!AppliesIfChild(p, out reason))
            {
                return false;
            }
            if (!p.IsPrisonerOfColony && !p.IsSlaveOfColony)
            {
                reason = Label + " must be a prisoner or slave of the colony";
                return false;
            }
            if (!MamonoTransformation.CanEverTransform(p, out string inner))
            {
                reason = Label + " cannot be transformed" + (inner != null ? $" ({inner})" : "");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Shared outcome logic for the two rites of awakening, modelled on the
    /// vanilla conversion ritual's outcome worker: quality decides the outcome;
    /// only a positive outcome triggers the transformation - anything less
    /// fizzles harmlessly. The transformation itself runs through the mod's
    /// single choke point (MamonoTransformation.ApplyXenotype), so it is
    /// idempotent and grants the usual awakening memories.
    /// </summary>
    public abstract class RitualOutcomeEffectWorker_TransformBase : RitualOutcomeEffectWorker_FromQuality
    {
        protected RitualOutcomeEffectWorker_TransformBase() { }

        protected RitualOutcomeEffectWorker_TransformBase(RitualOutcomeEffectDef def) : base(def) { }

        /// <summary>Role id of the pawn being transformed ("convert" / "captive").</summary>
        protected abstract string ConvertRoleId { get; }

        /// <summary>The xenotype the target becomes on a successful rite.</summary>
        protected abstract XenotypeDef XenotypeFor(Pawn corruptor);

        /// <summary>
        /// Who is recorded as the transformation's source. The source drives the
        /// warm "turned" memory and the non-colonist join roll inside
        /// ApplyXenotype. The captive rite passes null: a prisoner transformed by
        /// force stays a prisoner and keeps no warm memory of it.
        /// </summary>
        protected abstract Pawn TransformationSource(Pawn corruptor);

        /// <summary>Extra effects after a successful transformation (captive rite only).</summary>
        protected virtual void OnTransformed(Pawn target, StringBuilder letterText) { }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn corruptor = jobRitual.PawnWithRole("corruptor");
            Pawn target = jobRitual.PawnWithRole(ConvertRoleId);
            float quality = GetQuality(jobRitual, progress);
            RitualOutcomePossibility outcome = GetOutcome(quality, jobRitual);

            LookTargets letterLookTargets = new LookTargets(corruptor);
            ApplyAttachableOutcome(totalPresence, jobRitual, outcome, out string extraLetterText, ref letterLookTargets);

            bool transformed = false;
            XenotypeDef applied = null;
            if (outcome.Positive && corruptor != null && target != null)
            {
                applied = XenotypeFor(corruptor);
                transformed = applied != null
                    && MamonoTransformation.ApplyXenotype(target, applied, TransformationSource(corruptor));
            }

            string label = (outcome.label + ": " + jobRitual.Ritual.Label).CapitalizeFirst();
            StringBuilder text = new StringBuilder(string.Format(outcome.description, jobRitual.Ritual.Label));
            if (transformed)
            {
                text.Append("\n\n").Append(target.LabelShortCap).Append(" has awakened as a ")
                    .Append(applied.label).Append("!");
                OnTransformed(target, text);
            }
            else if (outcome.Positive)
            {
                // Positive roll, but the guards refused (e.g. she was transformed
                // by other means mid-rite). Same fizzle as a failed roll.
                text.Append("\n\nThe mana failed to take hold - the rite must be performed again.");
            }
            else
            {
                text.Append("\n\nThe rite fizzled; nothing happened.");
            }
            if (!extraLetterText.NullOrEmpty())
            {
                text.Append("\n\n").Append(extraLetterText);
            }

            Find.LetterStack.ReceiveLetter(label, text.ToString(),
                outcome.Positive ? LetterDefOf.PositiveEvent : LetterDefOf.NeutralEvent,
                letterLookTargets);
        }
    }

    /// <summary>
    /// Rite of awakening (colonist): on a positive outcome the convert is
    /// transformed into the corruptor's own monster xenotype, with the usual
    /// awakening memories and the warm bond toward her corruptor.
    /// </summary>
    public class RitualOutcomeEffectWorker_TransformColonist : RitualOutcomeEffectWorker_TransformBase
    {
        public RitualOutcomeEffectWorker_TransformColonist() { }

        public RitualOutcomeEffectWorker_TransformColonist(RitualOutcomeEffectDef def) : base(def) { }

        protected override string ConvertRoleId => "convert";

        protected override XenotypeDef XenotypeFor(Pawn corruptor)
        {
            return MamonoTransformation.XenotypeFor(corruptor);
        }

        protected override Pawn TransformationSource(Pawn corruptor)
        {
            return corruptor;
        }
    }

    /// <summary>
    /// Rite of awakening (captive): on a positive outcome the prisoner or slave
    /// is transformed into a RANDOM monster xenotype (any def that carries the
    /// Mamono gene, so submods like the Slime Faction join the pool automatically).
    /// The shock of the change leaves her will, resistance and certainty in her
    /// old beliefs shaken (all three tunable in mod settings).
    /// </summary>
    public class RitualOutcomeEffectWorker_TransformCaptive : RitualOutcomeEffectWorker_TransformBase
    {
        private static ProjectMamonoSettings Settings => ProjectMamonoModSettings.Settings;

        public RitualOutcomeEffectWorker_TransformCaptive() { }

        public RitualOutcomeEffectWorker_TransformCaptive(RitualOutcomeEffectDef def) : base(def) { }

        protected override string ConvertRoleId => "captive";

        protected override XenotypeDef XenotypeFor(Pawn corruptor)
        {
            return DefDatabase<XenotypeDef>.AllDefsListForReading
                .Where(MamonoTransformation.IsMonsterXenotype)
                .RandomElementWithFallback(ProjectMamono_DefOf.ProjectMamono_Xenotype_Mamono);
        }

        protected override Pawn TransformationSource(Pawn corruptor)
        {
            // Null: no warm "turned" memory, and ApplyXenotype's join roll is
            // skipped - a transformed prisoner stays a prisoner.
            return null;
        }

        protected override void OnTransformed(Pawn target, StringBuilder letterText)
        {
            if (target.guest != null)
            {
                target.guest.will = Mathf.Max(0f, target.guest.will * Settings.MonsterExtremistCaptiveWillFactor);
                target.guest.resistance = Mathf.Max(0f, target.guest.resistance * Settings.MonsterExtremistCaptiveResistanceFactor);
                letterText.Append(" Her will to resist has been shaken.");
            }
            if (target.ideo != null)
            {
                // The Certainty setter is private; OffsetCertainty is the public
                // path the vanilla conversion ritual uses. Offset by the amount
                // that leaves (certainty * factor) remaining.
                float certainty = target.ideo.Certainty;
                target.ideo.OffsetCertainty(certainty * (Settings.MonsterExtremistCaptiveCertaintyFactor - 1f));
                letterText.Append(" Her certainty in her old beliefs is crumbling.");
            }
        }
    }
}
