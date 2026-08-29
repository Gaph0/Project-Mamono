using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// Completes the transformation when a monster gene arrives through a
    /// xenogerm. Vanilla ImplantXenogerm only adds the genes the xenogerm
    /// contains, so implanting the Momo gene alone (or a slime's gel gene)
    /// would leave a half-monster: feminized, but with none of the xenotype's
    /// identity or other genes. This postfix runs after the implant finishes and
    /// fills in the rest through MomoTransformation.ApplyXenotype, so the
    /// awaken as the full xenotype. Implanted genes stay xenogenes (vanilla);
    /// only the genes needed to complete the xenotype become endogenes.
    ///
    /// The xenotype is inferred from the genes she now carries that she did not
    /// have before the implant: a monster xenotype whose gene list contains any
    /// freshly-implanted gene is chosen (smallest match wins — the base Momo
    /// xenotype over a bulkier variant on a shared gene). Finding none leaves
    /// her untouched. ApplyXenotype's own guards (alive, female, humanlike, of
    /// age, not already a monster) still apply, and it refuses a xenotype she
    /// already is, so a xenogerm with unrelated genes never triggers anything.
    ///
    /// Patch target: GeneUtility.ImplantXenogermItem, the single choke point
    /// for xenogerm application (used by the implant surgery, the sanguophage
    /// reimplant ability, and quest reimplants).
    /// </summary>
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.ImplantXenogermItem))]
    public static class XenogermCompletionPatch
    {
        public static void Prefix(Pawn pawn, Xenogerm xenogerm, out List<GeneDef> __state)
        {
            __state = null;
            if (!ProjectMomoModSettings.Settings.CorruptionEnabled || pawn?.genes == null)
            {
                return;
            }

            // Remember what she already has so the postfix can tell which genes
            // the implant actually delivered.
            var genes = pawn.genes.GenesListForReading;
            __state = new List<GeneDef>(genes.Count);
            for (int i = 0; i < genes.Count; i++)
            {
                __state.Add(genes[i].def);
            }
        }

        public static void Postfix(Pawn pawn, List<GeneDef> __state)
        {
            if (!ProjectMomoModSettings.Settings.CorruptionEnabled || pawn?.genes == null || __state == null)
            {
                return;
            }

            // Already a monster (e.g. re-implanting onto a completed transformation): nothing to do.
            if (EssenceTransfer.IsMomo(pawn))
            {
                return;
            }

            List<GeneDef> before = __state;
            XenotypeDef best = null;
            var genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                GeneDef fresh = genes[i].def;
                if (before.Contains(fresh))
                {
                    continue;
                }

                foreach (XenotypeDef def in DefDatabase<XenotypeDef>.AllDefsListForReading)
                {
                    if (!MomoTransformation.IsMonsterXenotype(def) || !def.genes.Contains(fresh))
                    {
                        continue;
                    }
                    if (best == null || def.genes.Count < best.genes.Count)
                    {
                        best = def;
                    }
                }
            }

            if (best != null)
            {
                MomoTransformation.ApplyXenotype(pawn, best, source: null);
            }
        }
    }
}
