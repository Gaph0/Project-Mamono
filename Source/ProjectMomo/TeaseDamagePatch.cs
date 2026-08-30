using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// After a Momo-carrier lands a melee hit on a pawn without the Momo gene,
    /// applies (or builds up) tease damage on the victim's brain.
    /// Targets the concrete Verb_MeleeAttackDamage implementation — the base
    /// Verb_MeleeAttack.ApplyMeleeDamageToTarget is abstract (no body) and
    /// cannot be patched.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class TeaseDamagePatch
    {
        private const float SeverityPerHit = 0.05f;

        /// <summary>
        /// Cancels the physical melee damage entirely when a Momo-carrier hits a
        /// pawn without the Momo gene — tease damage (applied in the postfix) is
        /// dealt instead. Runs before the original, so no injuries (bruises etc.)
        /// are created. The postfix still executes on a skipped original.
        /// </summary>
        public static bool Prefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, ref DamageWorker.DamageResult __result)
        {
            Pawn attacker = __instance.CasterPawn;
            Pawn victim = target.HasThing ? target.Thing as Pawn : null;

            if (!IsMomoCarrier(attacker) || victim == null)
            {
                return true; // not a Momo attack — vanilla damage
            }

            if (IsMomoCarrier(victim))
            {
                return true; // Momo-vs-Momo keeps vanilla damage
            }

            // Only humanlike victims are teased (and thus have their physical damage
            // cancelled). Animals, insects, mechanoids and everything else have no
            // tease physiology, so they keep taking real melee damage. Without this
            // gate the cancel below made a Momo deal ZERO damage to a wild animal:
            // the prefix cancelled the hit but the postfix's IsMomoTease (which does
            // require humanlike) dealt no tease either.
            if (victim.RaceProps == null || !victim.RaceProps.Humanlike)
            {
                return true; // non-human victim — vanilla damage
            }

            // Non-Momo humanlike victim: cancel the physical damage; tease is dealt instead (postfix).
            __result = new DamageWorker.DamageResult();
            return false;
        }

        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn attacker = __instance.CasterPawn;
            Pawn victim = target.HasThing ? target.Thing as Pawn : null;

            if (!IsMomoTease(attacker, victim))
            {
                return;
            }

            if (victim.health?.hediffSet == null)
            {
                return;
            }

            BodyPartRecord brain = victim.health.hediffSet.GetBrain();
            if (brain == null)
            {
                return;
            }

            // ISEKAI: CHA (attacker) boosts tease dealt, WIS (victim) resists it.
            // The attacker's beauty further amplifies the tease.
            float severity = SeverityPerHit
                * IsekaiCompat.TeaseDealtMultiplier(attacker)
                * IsekaiCompat.TeaseResistMultiplier(victim)
                * IsekaiCompat.TeaseBeautyMultiplier(attacker);
            if (float.IsNaN(severity) || float.IsInfinity(severity))
            {
                return;
            }

            float applied;
            Hediff tease = victim.health.hediffSet.GetFirstHediffOfDef(ProjectMomo_DefOf.ProjectMomo_TeaseDamage);
            if (tease != null)
            {
                float before = tease.Severity;
                tease.Severity = Mathf.Clamp(tease.Severity + severity, tease.def.minSeverity, tease.def.maxSeverity);
                applied = tease.Severity - before;
            }
            else
            {
                tease = HediffMaker.MakeHediff(ProjectMomo_DefOf.ProjectMomo_TeaseDamage, victim, brain);
                tease.Severity = Mathf.Clamp(severity, tease.def.minSeverity, tease.def.maxSeverity);
                victim.health.AddHediff(tease, brain);
                applied = tease.Severity;
            }

            // ISEKAI: XP proportional to the tease damage actually dealt this hit.
            IsekaiCompat.AwardTeaseDamageXP(attacker, applied);

            // Re-evaluate with the attacker in scope so a knockout here awards XP.
            TeaseKnockout.Evaluate(victim, attacker);
        }

        private static bool IsMomoTease(Pawn attacker, Pawn victim)
        {
            if (attacker == null || victim == null || attacker == victim)
            {
                return false;
            }

            if (victim.Dead || victim.Destroyed)
            {
                return false;
            }

            if (!IsMomoCarrier(attacker) || IsMomoCarrier(victim))
            {
                return false;
            }

            // Tease damage only affects humanlike pawns; animals, insects, and
            // mechanoids do not have the right physiology.
            if (victim.RaceProps == null || !victim.RaceProps.Humanlike)
            {
                return false;
            }

            return true;
        }

        private static bool IsMomoCarrier(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(ProjectMomo_DefOf.ProjectMomo_Momo);
        }
    }
}
