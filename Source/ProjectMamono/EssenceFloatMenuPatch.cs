using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// Adds the "Give essence" / "Drain essence" right-click orders when a drafted
    /// or selected colonist clicks another pawn:
    ///  - right-clicking a Mamono with a human selected offers "Give essence";
    ///  - right-clicking a human with a Mamono selected offers "Drain essence".
    /// Hooks Pawn.GetFloatMenuOptions (the per-pawn orders menu) and appends the
    /// appropriate job.
    ///
    /// A drain ordered on a captive of the colony - one of your prisoners or slaves -
    /// uses the dry job and the captive rules, exactly like an autonomous captive feed:
    /// any Mamono may do it, bonded or not, and another Mamono's incubation mark does not
    /// reserve him. Every other drain keeps the ordinary rules and the intimate variant.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class EssenceFloatMenuPatch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn __instance, Pawn selPawn)
        {
            foreach (FloatMenuOption opt in __result)
            {
                yield return opt;
            }

            Pawn target = __instance;
            if (selPawn == null || target == null || selPawn == target)
            {
                yield break;
            }
            if (!selPawn.Spawned || !target.Spawned || selPawn.Map != target.Map)
            {
                yield break;
            }

            // Selected is a Mamono, right-clicked a human -> Drain essence.
            if (EssenceTransfer.IsMamono(selPawn) && !EssenceTransfer.IsMamono(target))
            {
                // A captive of the colony is a meal the colony owns, not a partner, so the
                // order uses the dry job and the captive rules - the same exemption an
                // autonomous captive feed gets. Every other human keeps the ordinary drain.
                bool captiveMeal = EssenceTransfer.IsColonyCaptive(target);
                JobDef def = captiveMeal
                    ? ProjectMamono_DefOf.ProjectMamono_DrainEssenceDry
                    : ProjectMamono_DefOf.ProjectMamono_DrainEssence;
                FloatMenuOption opt = BuildOption(selPawn, target, def, "Drain essence", captiveMeal);
                if (opt != null)
                {
                    yield return opt;
                }
            }
            // Selected is a human, right-clicked a Mamono -> Give essence.
            else if (!EssenceTransfer.IsMamono(selPawn) && EssenceTransfer.IsMamono(target))
            {
                FloatMenuOption opt = BuildOption(selPawn, target, ProjectMamono_DefOf.ProjectMamono_GiveEssence, "Give essence");
                if (opt != null)
                {
                    yield return opt;
                }
            }
        }

        /// <summary>
        /// Builds the order, greyed out with a reason when the rules refuse it.
        /// <paramref name="captiveMeal"/> marks the drain of a captive of the colony: that
        /// order gates on <see cref="EssenceTransfer.IsCaptiveFeedTarget"/>, which exempts the
        /// meal from the bond rule and the incubation mark. Anything else gates on the
        /// ordinary <see cref="EssenceTransfer.CanTransfer"/>.
        /// </summary>
        private static FloatMenuOption BuildOption(Pawn actor, Pawn target, JobDef jobDef, string label, bool captiveMeal = false)
        {
            if (jobDef == null)
            {
                return null;
            }

            // The Mamono is the one whose mana is restored, whoever initiates.
            Pawn mamono = EssenceTransfer.IsMamono(actor) ? actor : target;
            Pawn human = EssenceTransfer.IsMamono(actor) ? target : actor;

            if (!actor.Drafted && !actor.IsColonistPlayerControlled)
            {
                return null; // only let the player order their own pawns
            }

            bool canFeed = captiveMeal
                ? EssenceTransfer.IsCaptiveFeedTarget(mamono, human)
                : EssenceTransfer.CanTransfer(mamono, human);
            if (!canFeed)
            {
                // Offer it greyed-out so the player understands why it's unavailable.
                // A captive meal never trips the bond rule, so only the ordinary path
                // reports it.
                string reason = !captiveMeal && !EssenceTransfer.BondAllowsFeeding(mamono, human)
                    ? " (bonded to another)"
                    : " (unavailable)";
                return new FloatMenuOption(label + reason, null);
            }

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, () =>
                {
                    Job job = JobMaker.MakeJob(jobDef, target);
                    actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                actor,
                target);
        }
    }
}
