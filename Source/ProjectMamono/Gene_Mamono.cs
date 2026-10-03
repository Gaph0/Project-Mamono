using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProjectMamono
{
    public class Gene_Mamono : Gene
    {
        /// <summary>
        /// The "Tease attacks" switch on the inspect pane of a player-controlled
        /// mamono. On - the default, and what every mamono carries until her player
        /// says otherwise - her melee strikes deal tease damage; off, they wound
        /// normally.
        /// It lives on the gene rather than in a game component so that it saves
        /// with the pawn, follows her through save and load, and is dropped with the
        /// gene if she ever loses it.
        /// </summary>
        public bool TeaseMeleeEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref TeaseMeleeEnabled, "teaseMeleeEnabled", true);
        }

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

            // The Inma's gift rejuvenates: any ailment of old age she already
            // had silently vanishes the moment the gene takes hold.
            MamonoAgeAilmentPatch.RemoveAgeAilments(pawn);
        }

        public override void Tick()
        {
            base.Tick();

            // Backstop for the prevention patch: sweep once per in-game hour in
            // case an age ailment was forced on by dev tools, a quest script,
            // or another mod adding hediffs directly instead of through a giver.
            if (pawn != null && pawn.IsHashIntervalTick(2500))
            {
                MamonoAgeAilmentPatch.RemoveAgeAilments(pawn);
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
