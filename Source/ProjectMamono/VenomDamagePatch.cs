using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// After a venom-gene carrier lands a melee hit on any living pawn, injects
    /// venom into the victim. Unlike the tease system this works on EVERY pawn -
    /// humanlikes, animals and fellow mamonos alike (user ruling): the venom is a
    /// physical toxin, not a tease. It also never replaces the hit's own damage:
    /// the tease prefix decides whether a humanlike victim takes tease or wounds,
    /// and the venom simply stacks on top of either outcome.
    /// Targets the concrete Verb_MeleeAttackDamage implementation, same as
    /// TeaseDamagePatch.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class VenomDamagePatch
    {
        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            Pawn attacker = __instance.CasterPawn;
            Pawn victim = target.HasThing ? target.Thing as Pawn : null;

            if (attacker == null || victim == null || attacker == victim)
            {
                return;
            }
            if (victim.Dead || victim.Destroyed)
            {
                return;
            }
            if (!(attacker.genes?.HasActiveGene(ProjectMamono_DefOf.ProjectMamono_MamonoVenom) ?? false))
            {
                return;
            }

            MamonoVenomApplication.TryApplyVenom(victim, attacker, ProjectMamonoModSettings.Settings.VenomSeverityPerHit);
        }
    }
}
