using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Marker for a gene whose carriers still suffer the ailments of age.
    ///
    /// The Mamono gene is a cure for old age: no age-driven giver fires on a
    /// carrier, and the ailments she already had are stripped (see
    /// MamonoAgeAilmentPatch). This extension opts a species out of that cure, so
    /// a short-lived drone ages and dies like the insect she is, and a gene that
    /// promises age-related illness - VRE_RapidLifeCycle is one - finally bites
    /// (user ruling 2026-09-27, for the insect mamono).
    ///
    /// The contract is deliberately empty and generic: core looks for this marker
    /// and never names a gene, so any mod can put it on a gene of its own and
    /// species added later get the behaviour for free.
    ///
    /// Fertility is NOT part of the opt-out. An opted-out pawn still keeps the
    /// Mamono fertility floor (MomoFertilityPatch), and is still released from it
    /// only by a sterile gene - that half was ruled to stay as it was.
    /// </summary>
    public class AllowsAgeAilmentsExtension : DefModExtension
    {
    }
}
