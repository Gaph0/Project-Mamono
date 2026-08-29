using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMomo
{
    /// <summary>
    /// Adds the "Give essence" / "Drain essence" right-click orders when a drafted
    /// or selected colonist clicks another pawn:
    ///  - right-clicking a Momo with a human selected offers "Give essence";
    ///  - right-clicking a human with a Momo selected offers "Drain essence".
    /// Hooks Pawn.GetFloatMenuOptions (the per-pawn orders menu) and appends the
    /// appropriate job.
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

            // Selected is a Momo, right-clicked a human -> Drain essence.
            if (EssenceTransfer.IsMomo(selPawn) && !EssenceTransfer.IsMomo(target))
            {
                FloatMenuOption opt = BuildOption(selPawn, target, ProjectMomo_DefOf.ProjectMomo_DrainEssence, "Drain essence");
                if (opt != null)
                {
                    yield return opt;
                }
            }
            // Selected is a human, right-clicked a Momo -> Give essence.
            else if (!EssenceTransfer.IsMomo(selPawn) && EssenceTransfer.IsMomo(target))
            {
                FloatMenuOption opt = BuildOption(selPawn, target, ProjectMomo_DefOf.ProjectMomo_GiveEssence, "Give essence");
                if (opt != null)
                {
                    yield return opt;
                }
            }
        }

        private static FloatMenuOption BuildOption(Pawn actor, Pawn target, JobDef jobDef, string label)
        {
            if (jobDef == null)
            {
                return null;
            }

            // The Momo is the one whose mana is restored, whoever initiates.
            Pawn momo = EssenceTransfer.IsMomo(actor) ? actor : target;
            Pawn human = EssenceTransfer.IsMomo(actor) ? target : actor;

            if (!actor.Drafted && !actor.IsColonistPlayerControlled)
            {
                return null; // only let the player order their own pawns
            }

            if (!EssenceTransfer.CanTransfer(momo, human))
            {
                // Offer it greyed-out so the player understands why it's unavailable.
                string reason = !EssenceTransfer.BondAllowsFeeding(momo, human)
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
