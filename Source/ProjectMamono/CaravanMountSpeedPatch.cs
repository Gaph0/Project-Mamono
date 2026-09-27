using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Caravan mounts for mamonos. Vanilla 1.6 counts a mount only for NON-humanlike pawns:
    /// CaravanTicksPerMoveUtility.GetTicksPerMove scans the caravan with
    /// "if (pawn.RaceProps.Humanlike) humanCount++; else if (pawn.IsCaravanRideable())",
    /// so a humanlike pawn is a person and can never be a mount. Nothing about the pawn
    /// can change that - the loop never asks about a humanlike, and
    /// TransferableUIUtility.DoExtraIcons gates its rideable icon on RaceProps.Animal too,
    /// which is why the caravan's own "Ridable animals / people" line counts animals only.
    ///
    /// This postfix therefore adds the mod's own mounts on top: every non-downed flyer
    /// contributes her CaravanRidingSpeedFactor TWICE (herself and the passenger on her
    /// back) and every non-downed large-frame carrier ONCE (a passenger on her back).
    /// Vanilla's own mounts - the rideable animals - stay as they are, and the returned
    /// ticks-per-move is rescaled by ridingVanilla / ridingWithMounts, so the vanilla
    /// base/mass/bonus math and any other mod's patches are left untouched.
    ///
    /// The line appended below is what reports the mamono mounts; the vanilla rideable
    /// line next to it will keep saying "0 / N" for a caravan of mamonos.
    /// </summary>
    [HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove),
        new[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(bool), typeof(StringBuilder) })]
    public static class CaravanMountSpeedPatch
    {
        public static void Postfix(List<Pawn> pawns, bool isShuttle, StringBuilder explanation, ref int __result)
        {
            if (pawns == null || pawns.Count == 0 || isShuttle || __result <= 0)
            {
                return;
            }

            int humanCount = 0;
            bool hasMamonoMount = false;
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || !pawn.RaceProps.Humanlike)
                {
                    continue;
                }
                humanCount++;
                if (!hasMamonoMount && (IsFlyer(pawn) || IsLargeFrameCarrier(pawn)))
                {
                    hasMamonoMount = true;
                }
            }
            if (!hasMamonoMount || humanCount == 0)
            {
                return;
            }

            // Two lists: what vanilla computed (rideable animals only) and what the
            // caravan should count (those animals plus the mamono mounts).
            List<float> vanillaMounts = new List<float>();
            List<float> allMounts = new List<float>();
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null)
                {
                    continue;
                }
                if (!pawn.RaceProps.Humanlike)
                {
                    if (pawn.IsCaravanRideable())
                    {
                        float factor = pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor);
                        vanillaMounts.Add(factor);
                        allMounts.Add(factor);
                    }
                    continue;
                }
                if (IsFlyer(pawn))
                {
                    float factor = pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor);
                    allMounts.Add(factor); // she flies herself
                    allMounts.Add(factor); // and airlifts one passenger on her back
                }
                else if (IsLargeFrameCarrier(pawn))
                {
                    allMounts.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor)); // one passenger
                }
            }

            float ridingVanilla = RidingFactor(humanCount, vanillaMounts);
            float ridingWithMounts = RidingFactor(humanCount, allMounts);

            if (ridingWithMounts <= ridingVanilla)
            {
                return;
            }

            // Ticks-per-move is inversely proportional to the riding factor, so
            // rescale by the ratio instead of re-deriving the vanilla formula.
            __result = Mathf.Max(1, Mathf.RoundToInt(__result * ridingVanilla / ridingWithMounts));

            if (explanation != null)
            {
                explanation.AppendLine();
                explanation.Append("  " + "PMM_MultiplierFromMountedMamonos".Translate() + ": " + (ridingWithMounts / ridingVanilla).ToStringPercent());
            }
        }

        /// <summary>
        /// A flyer who can carry caravan weight: the strong flight gene is active, she is not
        /// downed and she is not a prisoner being hauled along. PMM_Gene_FlightWeak flyers are
        /// deliberately left out of the mount list - a small frame with thin wings can only
        /// lift herself, so she never airlifts a passenger.
        /// </summary>
        private static bool IsFlyer(Pawn pawn)
        {
            return !pawn.Downed && !pawn.IsPrisoner && pawn.genes != null
                && pawn.genes.HasActiveGene(ProjectMamono_DefOf.PMM_Gene_Flight);
        }

        /// <summary>
        /// A large-frame mamono who can carry caravan weight: the gene is active, she is not
        /// downed and she is not a prisoner being hauled along. Her CaravanRidingSpeedFactor
        /// (+0.3, a factor of 1.3) is what she contributes; at the base 1.0 she would count
        /// for nothing.
        /// </summary>
        private static bool IsLargeFrameCarrier(Pawn pawn)
        {
            return !pawn.Downed && !pawn.IsPrisoner && pawn.genes != null
                && pawn.genes.HasActiveGene(ProjectMamono_DefOf.PMM_Gene_LargeFrame);
        }

        /// <summary>Exact replica of the vanilla riding-factor computation in GetTicksPerMove.</summary>
        private static float RidingFactor(int humanCount, List<float> mountFactors)
        {
            if (humanCount <= 0 || mountFactors.Count == 0)
            {
                return 1f;
            }
            mountFactors.Sort();
            mountFactors.Reverse();
            if (mountFactors.Count > humanCount)
            {
                mountFactors.RemoveRange(humanCount, mountFactors.Count - humanCount);
            }
            while (mountFactors.Count < humanCount)
            {
                mountFactors.Add(1f);
            }
            return mountFactors.Average();
        }
    }
}
