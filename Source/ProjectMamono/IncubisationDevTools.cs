using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Development-mode tools for the incubisation system, surfaced in the game's
    /// "Debug actions" menu under "Project Mamono" (visible only when Dev mode is
    /// on). Covers the whole lifecycle: granting/clearing progress, firing the
    /// completion effects, inspecting a man's mark and daily cap, and exercising
    /// the marking rule against a Mamono.
    /// </summary>
    public static class IncubisationDevTools
    {
        private const string Category = "Project Mamono";

        /// <summary>Every male pawn on the current map that could be incubised.</summary>
        private static List<DebugMenuOption> IncubisableOptions()
        {
            List<DebugMenuOption> options = new List<DebugMenuOption>();
            foreach (Pawn pawn in PawnsOnMap())
            {
                Pawn local = pawn;
                if (local.gender == Gender.Male && Incubisation.CanEverIncubise(local))
                {
                    options.Add(new DebugMenuOption(Label(local), DebugMenuOptionMode.Action, () => Act(local)));
                }
            }
            if (options.Count == 0)
            {
                options.Add(new DebugMenuOption("(no incubisable men on map)", DebugMenuOptionMode.Action, () => { }));
            }
            return options;
        }

        // The action the current option list operates on - set before building the list.
        private static System.Action<Pawn> act;
        private static void Act(Pawn pawn) => act(pawn);

        private static IEnumerable<Pawn> PawnsOnMap()
        {
            Map map = Find.CurrentMap;
            return map?.mapPawns?.AllPawnsSpawned ?? new List<Pawn>();
        }

        private static string Label(Pawn pawn)
        {
            Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
            string marker = progress?.Source != null ? $" [mark: {progress.Source.LabelShort}]" : "";
            string pct = progress != null ? $" ({progress.Severity:P0})" : "";
            return $"{pawn.LabelShort}{pct}{marker}";
        }

        private static void OpenPawnPicker(string title, System.Action<Pawn> action)
        {
            act = action;
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(IncubisableOptions()));
        }

        [DebugAction(Category, "Incubisation: add 10% progress", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddProgress()
        {
            OpenPawnPicker("Add 10% incubisation to...", pawn =>
            {
                if (Incubisation.ProgressOf(pawn) == null)
                {
                    // Fresh progress needs a mark too: use the first Mamono on the map.
                    Pawn mamono = FirstMamono();
                    Incubisation.ApplyDose(mamono, pawn, 1f);
                    Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                    if (progress != null && progress.Severity < 0.10f)
                    {
                        progress.Severity = 0.10f;
                        if (progress.Severity >= progress.def.maxSeverity - 0.0001f)
                        {
                            progress.CompleteIncubisation();
                        }
                    }
                }
                else
                {
                    Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                    progress.Severity += 0.10f;
                    if (progress.Severity >= progress.def.maxSeverity - 0.0001f)
                    {
                        progress.CompleteIncubisation();
                    }
                }
                Messages.Message($"[Dev] Incubisation on {pawn.LabelShort}: {Incubisation.ProgressOf(pawn)?.Severity:P0}", MessageTypeDefOf.NeutralEvent, false);
            });
        }

        [DebugAction(Category, "Incubisation: add 10% progress (mark by Mamono)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddProgressMarked()
        {
            OpenPawnPicker("Add 10% (marked by Mamono) to...", pawn =>
            {
                Pawn mamono = FirstMamono();
                if (mamono == null)
                {
                    Messages.Message("[Dev] No Mamono on map to mark him.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                // Bypass the daily cap so repeat clicks stack 10% each.
                for (int i = 0; i < 20; i++)
                {
                    Incubisation.ApplyDose(mamono, pawn, 0.05f);
                    Hediff_Incubisation p = Incubisation.ProgressOf(pawn);
                    if (p != null && p.Severity >= 0.0999f)
                    {
                        break;
                    }
                }
                Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                if (progress != null && progress.Severity < 0.10f)
                {
                    progress.Severity = 0.10f;
                }
                Messages.Message($"[Dev] {pawn.LabelShort} marked by {mamono.LabelShort}: {progress?.Severity:P0}", MessageTypeDefOf.NeutralEvent, false);
            });
        }

        [DebugAction(Category, "Incubisation: set to 100% (complete)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Complete()
        {
            OpenPawnPicker("Complete incubisation on...", pawn =>
            {
                Pawn mamono = FirstMamono();
                if (Incubisation.ProgressOf(pawn) == null)
                {
                    Incubisation.ApplyDose(mamono, pawn, 1f);
                }
                Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                if (progress == null)
                {
                    Messages.Message($"[Dev] Could not create progress on {pawn.LabelShort}.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                progress.Severity = progress.def.maxSeverity;
                progress.CompleteIncubisation();
                Messages.Message($"[Dev] {pawn.LabelShort} is now a full incubus.", MessageTypeDefOf.NeutralEvent, false);
            });
        }

        [DebugAction(Category, "Incubisation: clear progress", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearProgress()
        {
            OpenPawnPicker("Clear incubisation from...", pawn =>
            {
                Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                if (progress == null)
                {
                    Messages.Message($"[Dev] {pawn.LabelShort} has no incubisation.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                pawn.health.RemoveHediff(progress);
                Messages.Message($"[Dev] Cleared incubisation from {pawn.LabelShort}.", MessageTypeDefOf.NeutralEvent, false);
            });
        }

        [DebugAction(Category, "Incubisation: log status", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogStatus()
        {
            var sb = new StringBuilder("[Dev] Incubisation status:\n");
            int count = 0;
            foreach (Pawn pawn in PawnsOnMap())
            {
                Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                if (progress != null)
                {
                    count++;
                    string mark = progress.Source != null ? $"{progress.Source.LabelShort} ({progress.SourceXenotype?.label ?? "?"})" : "(none)";
                    sb.AppendLine($"  {pawn.LabelShort}: {progress.Severity:P1} stage={progress.CurStage?.label ?? "?"} mark={mark} full={Incubisation.IsFullIncubus(pawn)} ageGuard={Incubisation.PreventsAgeAilments(pawn)} regen=x{Incubisation.EssenceRegenMultiplier(pawn):0.##}");
                }
            }
            if (count == 0)
            {
                sb.AppendLine("  (no pawns with incubisation on this map)");
            }
            Log.Message(sb.ToString());
        }

        [DebugAction(Category, "Incubisation: test mark protection", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestMarkProtection()
        {
            OpenPawnPicker("Test mark protection for...", man =>
            {
                var sb = new StringBuilder($"[Dev] Mark protection on {man.LabelShort}:\n");
                bool any = false;
                foreach (Pawn mamono in PawnsOnMap())
                {
                    if (EssenceTransfer.IsMamono(mamono) && mamono != man)
                    {
                        any = true;
                        bool protects = Incubisation.MarkProtects(mamono, man);
                        bool bonded = mamono.relations != null && mamono.relations.DirectRelationExists(ProjectMamono_DefOf.ProjectMamono_Tsugai, man);
                        sb.AppendLine($"  {mamono.LabelShort}: protects={protects} bondedToHim={bonded} canFeed={EssenceTransfer.CanTransfer(mamono, man)}");
                    }
                }
                if (!any)
                {
                    sb.AppendLine("  (no Mamonos on this map)");
                }
                Log.Message(sb.ToString());
            });
        }

        [DebugAction(Category, "Incubisation: reset daily cap", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetDailyCap()
        {
            OpenPawnPicker("Reset daily cap for...", pawn =>
            {
                Hediff_Incubisation progress = Incubisation.ProgressOf(pawn);
                if (progress == null)
                {
                    Messages.Message($"[Dev] {pawn.LabelShort} has no incubisation.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                progress.DevResetDailyCap();
                Messages.Message($"[Dev] Daily cap reset for {pawn.LabelShort}.", MessageTypeDefOf.NeutralEvent, false);
            });
        }

        /// <summary>The first Mamono on the map (dev helper for marking/imprinting).</summary>
        private static Pawn FirstMamono()
        {
            foreach (Pawn pawn in PawnsOnMap())
            {
                if (EssenceTransfer.IsMamono(pawn))
                {
                    return pawn;
                }
            }
            return null;
        }
    }
}
