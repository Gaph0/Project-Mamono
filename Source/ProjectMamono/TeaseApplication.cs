using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Public entry point for applying tease damage from outside melee combat
    /// (psycasts, abilities, mod integrations). Reuses the exact severity
    /// arithmetic of the melee path in TeaseDamagePatch: create-or-add on the
    /// victim's brain, clamp to the hediff's severity range, then re-evaluate
    /// the willpower knockout with the attacker in scope.
    /// </summary>
    public static class TeaseApplication
    {
        /// <summary>
        /// Applies <paramref name="severity"/> tease damage to the victim and
        /// returns the amount actually applied after clamping (0 when the victim
        /// has no brain, is already at max tease, or the severity is not finite).
        /// A positive hit re-evaluates the knockout, so a pawn whose will breaks
        /// here goes down on this tick.
        /// </summary>
        public static float TryApplyTease(Pawn victim, Pawn attacker, float severity)
        {
            if (victim?.health?.hediffSet == null || victim.Dead)
            {
                return 0f;
            }

            // The mamono claws gene scales tease from every source, not just melee.
            // Null attacker simply yields the neutral 1x factor.
            severity *= MamonoClaws.TeaseDealtMultiplier(attacker);

            if (float.IsNaN(severity) || float.IsInfinity(severity) || severity <= 0f)
            {
                return 0f;
            }

            BodyPartRecord brain = victim.health.hediffSet.GetBrain();
            if (brain == null)
            {
                return 0f;
            }

            float applied;
            Hediff tease = victim.health.hediffSet.GetFirstHediffOfDef(ProjectMamono_DefOf.ProjectMamono_TeaseDamage);
            if (tease != null)
            {
                float before = tease.Severity;
                tease.Severity = Mathf.Clamp(tease.Severity + severity, tease.def.minSeverity, tease.def.maxSeverity);
                applied = tease.Severity - before;
            }
            else
            {
                tease = HediffMaker.MakeHediff(ProjectMamono_DefOf.ProjectMamono_TeaseDamage, victim, brain);
                tease.Severity = Mathf.Clamp(severity, tease.def.minSeverity, tease.def.maxSeverity);
                victim.health.AddHediff(tease, brain);
                applied = tease.Severity;
            }

            if (applied > 0f)
            {
                TeaseKnockout.Evaluate(victim, attacker);
            }

            return applied;
        }
    }
}
