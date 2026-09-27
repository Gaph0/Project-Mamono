using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// A colonist Mamono who has no bondable prey on the map walks to the map edge,
    /// leaves the player faction, and exits the map - the same behavior as the
    /// vanilla "leave colony" mental break.
    /// </summary>
    public class JobDriver_LeaveColony : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            IntVec3 exitCell = pawn.Position;
            if (pawn.Map != null)
            {
                if (!RCellFinder.TryFindBestExitSpot(pawn, out exitCell, TraverseMode.ByPawn))
                {
                    // Fallback: project onto the nearest map edge.
                    Rot4 edge = CellRect.WholeMap(pawn.Map).GetClosestEdge(pawn.Position);
                    exitCell = pawn.Position;
                    if (edge == Rot4.North) exitCell.z = pawn.Map.Size.z - 2;
                    else if (edge == Rot4.South) exitCell.z = 1;
                    else if (edge == Rot4.East) exitCell.x = pawn.Map.Size.x - 2;
                    else exitCell.x = 1;
                    if (!exitCell.Standable(pawn.Map))
                    {
                        exitCell = CellFinder.RandomClosewalkCellNear(exitCell, pawn.Map, 10);
                    }
                }
            }

            // Remove from the player faction as soon as the job starts so the
            // colony no longer counts her as a colonist while she walks out.
            Toil leaveFaction = new Toil();
            leaveFaction.initAction = () =>
            {
                if (pawn.Faction != null && pawn.Faction.IsPlayer)
                {
                    pawn.SetFaction(null);
                }
            };
            yield return leaveFaction;

            // Walk to the exit and leave the map.
            yield return Toils_Goto.GotoCell(exitCell, PathEndMode.OnCell);
            Toil exit = new Toil();
            exit.initAction = () =>
            {
                Rot4 edge = CellRect.WholeMap(pawn.Map).GetClosestEdge(pawn.Position);
                pawn.ExitMap(true, edge);
            };
            yield return exit;
        }
    }
}
