using RimWorld;
using Verse;
using Verse.AI;

namespace ProjectMamono
{
    /// <summary>
    /// The essence-starvation berserk state. Behaves like vanilla berserk (hostile to
    /// everything, wanders when no victim) but hunts the nearest unbonded humanlike -
    /// the pawn the break picked as its initial victim - and keeps chasing them across
    /// the map until they go down, then moves on to the next unbonded pawn. Bonded
    /// (tsugai) pawns are ignored as prey.
    /// </summary>
    public class MentalState_EssenceBerserk : MentalState_Berserk
    {
        /// <summary>The currently hunted victim. Seeded from causedByPawn on start and
        /// re-picked by the job giver whenever the prey is downed or lost.</summary>
        public Pawn currentPrey;

        /// <summary>Set once the job giver decides there is no prey left: the Mamono flees
        /// toward the map edge and leaves the map instead of wandering.</summary>
        public bool fleeing;

        // Vanilla MentalState_Berserk forces hostility toward EVERYTHING (both overloads
        // return true), so any animal the Mamono walks past during a hunt/wander/flee gets
        // auto-attacked by her melee verbs - no job giver involved. Narrow it: only valid
        // unbonded humanlike prey is forced hostile. Animals and bonded pawns are left alone.
        public override bool ForceHostileTo(Thing t)
        {
            return t is Pawn prey && JobGiver_EssenceBerserk.IsValidPrey(pawn, prey);
        }

        // Keep faction-level hostility off too, so the Mamono doesn't turn hostile to whole
        // factions (and their animals) the way vanilla berserk does.
        public override bool ForceHostileTo(Faction f)
        {
            return false;
        }

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            // The hunt exists to win her a mate. The moment a living tsugai bond
            // exists the state is fulfilled - recover instead of hunting further
            // prey or, finding no unbonded pawn left on the map, fleeing the colony.
            // This is what saves a freshly bonded Mamono whose drained partner is
            // immediately carried away: without it the job giver sees no viable
            // bonding target and sends her off the map.
            if (TsugaiFormation.HasBondedPartner(pawn))
            {
                RecoverFromState();
            }
        }

        public override void PreStart()
        {
            base.PreStart();
            currentPrey = causedByPawn;

            // An NPC in a Lord (raider/visitor, e.g. a slime-faction pawn in a
            // LordJob_DefendPoint) has her job driven by the lord's duty tree, which
            // re-issues its wander/defend job every tick and overrides the hunt. Take
            // her out of the lord so the essence-berserk think-tree job owns her.
            Verse.AI.Group.LordUtility.GetLord(pawn)?.RemovePawn(pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref currentPrey, "currentPrey");
            Scribe_Values.Look(ref fleeing, "fleeing", false);
        }
    }
}
