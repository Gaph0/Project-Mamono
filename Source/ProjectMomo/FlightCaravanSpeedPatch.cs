using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Flight gene caravan integration. Vanilla 1.6 caravan speed only counts
    /// non-humanlike pawns as mounts: each rideable animal contributes its
    /// CaravanRidingSpeedFactor, the top 'humanCount' factors are kept, the list
    /// is padded with 1.0 up to humanCount, and the average becomes the caravan's
    /// riding multiplier (CaravanTicksPerMoveUtility.GetTicksPerMove).
    ///
    /// A pawn with the flight gene is humanlike, so vanilla never treats her as a
    /// mount. This postfix recomputes the riding factor with every non-downed
    /// flyer contributing her CaravanRidingSpeedFactor TWICE: once for herself
    /// and once for the passenger riding on her back. The returned ticks-per-move
    /// is then rescaled by ridingVanilla / ridingWithFlight, leaving the vanilla
    /// base/mass/bonus math (and any other mod's patches) untouched.
    /// </summary>
    [HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove),
        new[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(bool), typeof(StringBuilder) })]
    public static class FlightCaravanSpeedPatch
    {
        public static void Postfix(List<Pawn> pawns, bool isShuttle, StringBuilder explanation, ref int __result)
        {
            if (pawns == null || pawns.Count == 0 || isShuttle || __result <= 0)
            {
                return;
            }

            int humanCount = 0;
            bool hasFlyer = false;
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || !pawn.RaceProps.Humanlike)
                {
                    continue;
                }
                humanCount++;
                if (!hasFlyer && IsFlyer(pawn))
                {
                    hasFlyer = true;
                }
            }
            if (!hasFlyer || humanCount == 0)
            {
                return;
            }

            // Vanilla mount factors (animals only) and the flight-augmented ones.
            List<float> animalFactors = new List<float>();
            List<float> flightFactors = new List<float>();
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null)
                {
                    continue;
                }
                if (pawn.RaceProps.Humanlike)
                {
                    if (IsFlyer(pawn))
                    {
                        float factor = pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor);
                        flightFactors.Add(factor); // she flies herself
                        flightFactors.Add(factor); // and airlifts one passenger on her back
                    }
                }
                else if (pawn.IsCaravanRideable())
                {
                    animalFactors.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor));
                }
            }

            float ridingVanilla = RidingFactor(humanCount, animalFactors);
            List<float> combined = new List<float>(animalFactors);
            combined.AddRange(flightFactors);
            float ridingWithFlight = RidingFactor(humanCount, combined);

            if (ridingWithFlight <= ridingVanilla)
            {
                return;
            }

            // Ticks-per-move is inversely proportional to the riding factor, so
            // rescale by the ratio instead of re-deriving the vanilla formula.
            __result = Mathf.Max(1, Mathf.RoundToInt(__result * ridingVanilla / ridingWithFlight));

            if (explanation != null)
            {
                explanation.AppendLine();
                explanation.Append("  " + "PMM_MultiplierFromFlyingPawns".Translate() + ": " + (ridingWithFlight / ridingVanilla).ToStringPercent());
            }
        }

        /// <summary>A humanlike pawn able to fly: flight gene present, active and not downed.</summary>
        private static bool IsFlyer(Pawn pawn)
        {
            return !pawn.Downed && pawn.genes != null && pawn.genes.HasActiveGene(ProjectMomo_DefOf.PMM_Gene_Flight);
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
