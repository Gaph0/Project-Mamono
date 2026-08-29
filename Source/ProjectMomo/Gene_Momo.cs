using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    public class Gene_Momo : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();

            if (pawn == null)
            {
                return;
            }

            bool graphicsChanged = false;

            if (pawn.gender != Gender.Female)
            {
                pawn.gender = Gender.Female;
                graphicsChanged = true;
            }

            if (pawn.story != null)
            {
                if (pawn.story.bodyType != BodyTypeDefOf.Female)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Female;
                    graphicsChanged = true;
                }

                if (!IsFemaleStyle(pawn.story.hairDef))
                {
                    HairDef femaleHair = GetRandomFemaleHair();
                    if (femaleHair != null)
                    {
                        pawn.story.hairDef = femaleHair;
                        graphicsChanged = true;
                    }
                }
            }

            if (graphicsChanged && pawn.Spawned && pawn.Drawer != null && pawn.Drawer.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        private static bool IsFemaleStyle(HairDef hair)
        {
            if (hair == null)
            {
                return false;
            }

            return hair.styleGender == StyleGender.Female
                || hair.styleGender == StyleGender.FemaleUsually
                || hair.styleGender == StyleGender.Any;
        }

        private static HairDef GetRandomFemaleHair()
        {
            List<HairDef> options = DefDatabase<HairDef>.AllDefs
                .Where(h => h.styleGender == StyleGender.Female || h.styleGender == StyleGender.FemaleUsually)
                .ToList();

            if (options.Count == 0)
            {
                return null;
            }

            return options.RandomElement();
        }
    }
}
