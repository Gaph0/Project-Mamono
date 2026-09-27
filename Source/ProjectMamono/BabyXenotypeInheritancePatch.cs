using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Makes a Mamono's baby inherit the mother's xenotype instead of the normal
    /// hybrid result.
    ///
    /// In vanilla a baby whose parents have different xenotypes gets roughly half
    /// its endogenes from each parent (a "hybrid"). A Mamono mother instead passes
    /// on her full xenotype: the baby gets every one of the mother's endogenes -
    /// including her melanin and hair colour - is not flagged as a hybrid, and
    /// keeps the mother's xenotype name/icon. The father contributes nothing.
    ///
    /// The patch hooks <see cref="PregnancyUtility.GetInheritedGenes(Pawn, Pawn, out bool)"/>,
    /// which is evaluated once at conception (and again for embryos/vats), and
    /// only alters the result when the mother is a Mamono.
    /// </summary>
    // The vanilla method is GetInheritedGenes(Pawn, Pawn, out bool): the third
    // argument must be the by-ref bool type itself (bool&), produced by
    // typeof(bool).MakeByRefType(). That can't appear inside an attribute (not a
    // constant), so the patch resolves its target explicitly in TargetMethod.
    // The previous declaration passed a delegate *class* as the parameter type,
    // which never matches - Harmony found no target, threw inside PatchAll, and
    // took down every other patch in the mod on startup.
    [HarmonyPatch]
    public static class BabyXenotypeInheritancePatch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(PregnancyUtility),
                nameof(PregnancyUtility.GetInheritedGenes),
                new[] { typeof(Pawn), typeof(Pawn), typeof(bool).MakeByRefType() });
        }

        public static void Postfix(Pawn father, Pawn mother, ref bool success, ref List<GeneDef> __result)
        {
            if (!ModsConfig.BiotechActive || !EssenceTransfer.IsMamono(mother))
            {
                return;
            }
            if (mother?.genes == null || __result == null)
            {
                return;
            }

            List<GeneDef> motherEndogenes = mother.genes.Endogenes?.ConvertAll(g => g.def);
            if (motherEndogenes == null || motherEndogenes.Count == 0)
            {
                return;
            }

            // The baby is a genetic copy of the mother: her full endogenes, nothing
            // from the father (no melanin/hair-colour blending).
            success = true;
            __result = new List<GeneDef>(motherEndogenes);
        }
    }

    /// <summary>
    /// Ensures a Mamono's baby keeps the mother's xenotype identity instead of being
    /// stamped "hybrid". Vanilla's ApplyBirthOutcome only calls SetXenotypeDirect
    /// when TryGetInheritedXenotype succeeds - which requires mother and father to
    /// share a heritable xenotype. A Mamono mother × human father fails that check,
    /// so vanilla falls through to ShouldByHybrid and stamps the baby "Hybrid".
    /// This postfix runs after vanilla and restores the mother's full xenotype:
    /// her XenotypeDef (for def-based xenotypes) or her custom name/icon (for
    /// custom ones), and clears the hybrid flag either way.
    /// </summary>
    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class BabyCustomXenotypeBirthPatch
    {
        public static void Postfix(Pawn geneticMother, Thing __result)
        {
            if (!EssenceTransfer.IsMamono(geneticMother))
            {
                return;
            }
            Pawn baby = __result as Pawn;
            if (baby?.genes == null || geneticMother.genes == null)
            {
                return;
            }

            // Undo vanilla's hybrid stamping and give the baby the mother's xenotype.
            baby.genes.hybrid = false;
            if (geneticMother.genes.UniqueXenotype)
            {
                // Custom (non-def) xenotype: carry over the name and icon.
                baby.genes.xenotypeName = geneticMother.genes.xenotypeName;
                baby.genes.iconDef = geneticMother.genes.iconDef;
            }
            else if (geneticMother.genes.Xenotype != null)
            {
                // Def-based xenotype: adopt the mother's def outright.
                baby.genes.SetXenotypeDirect(geneticMother.genes.Xenotype);
            }

            // Mamonos are always female (see Gene_Mamono); their children are too.
            ForceFemale(baby);
        }

        private static void ForceFemale(Pawn baby)
        {
            bool graphicsChanged = false;

            if (baby.gender != Gender.Female)
            {
                baby.gender = Gender.Female;
                graphicsChanged = true;
            }

            if (baby.story != null && baby.story.bodyType != BodyTypeDefOf.Female)
            {
                baby.story.bodyType = BodyTypeDefOf.Female;
                graphicsChanged = true;
            }

            if (graphicsChanged && baby.Spawned && baby.Drawer?.renderer != null)
            {
                baby.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }
}
