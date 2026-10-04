using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// The "Tease attacks" switch. The flag itself lives on Gene_Mamono (it saves
    /// with the pawn's genes, and is dropped with them), and this class is the one
    /// place both the gizmo and TeaseDamagePatch ask about it.
    /// </summary>
    public static class TeaseMeleeSwitch
    {
        /// <summary>The Mamono gene instance on this pawn, or null.</summary>
        public static Gene_Mamono GeneFor(Pawn pawn)
        {
            return pawn?.genes?.GetFirstGeneOfType<Gene_Mamono>();
        }

        /// <summary>
        /// True when the player has switched this attacker's melee tease off. Only
        /// the melee path reads this. TeaseApplication - the entry point the other
        /// tease sources use, such as the slime psycasts and the reptile tail
        /// grapple - is deliberately left alone, so "no tease damage from melee"
        /// means exactly that and nothing more.
        /// </summary>
        public static bool IsOff(Pawn attacker)
        {
            Gene_Mamono gene = GeneFor(attacker);
            return gene != null && !gene.TeaseMeleeEnabled;
        }
    }

    /// <summary>
    /// Puts the switch on the inspect pane of every player-controlled mamono.
    /// A pass-through postfix on Pawn.GetGizmos: vanilla's own list is yielded
    /// first and the switch last, so none of the pawn's normal gizmos are lost.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class TeaseMeleeSwitchGizmoPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            // Vanilla's own test for "this pawn is mine to command": spawned, a
            // colonist - or a slave, which the property counts as one - and not in
            // a mental state. A prisoner, a guest or another faction's pawn never
            // qualifies, and none of them can be ordered into melee anyway.
            if (!__instance.IsColonistPlayerControlled)
            {
                yield break;
            }

            Gene_Mamono gene = TeaseMeleeSwitch.GeneFor(__instance);
            if (gene == null)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "Tease attacks",
                defaultDesc = "On, her melee hits deal tease damage instead of wounds. "
                    + "Off, they wound like anyone else's. Her abilities and psycasts keep teasing either way.",
                icon = TexCommand.AttackMelee,
                isActive = () => gene.TeaseMeleeEnabled,
                toggleAction = () => gene.TeaseMeleeEnabled = !gene.TeaseMeleeEnabled
            };
        }
    }
}
