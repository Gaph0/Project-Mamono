using System.Collections.Generic;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Data-driven opt-in for the optional Vanilla Psycasts Expanded integration:
    /// a monster xenotype carrying this extension on its XenotypeDef can spawn
    /// (while VPE is active and the integration is enabled in the mod settings)
    /// as a VPE psycaster. Everything is stored as plain strings and numbers -
    /// no VPE types - so the XML loads cleanly with VPE absent, and VPECompat
    /// simply never reads the data then.
    /// </summary>
    public class MamonoPsycastExtension : DefModExtension
    {
        // defNames of VPE PsycasterPathDefs this xenotype can unlock; one is
        // picked at random per pawn. Paths the pawn may not unlock (VPE locks
        // some behind backstories, memes, genes or meditation foci) are skipped
        // automatically, so prefer unrestricted VPE paths here.
        public List<string> pathDefNames;

        // Psycasts granted at spawn. Each grant also raises the psylink level by
        // one, mirroring how VPE equips its own caster pawn kinds (Empire_Caster_*).
        public IntRange initialAbilities = new IntRange(1, 2);

        // Extra psylink levels beyond the ability grants, spent on VPE's
        // psycaster stat upgrades (also mirroring VPE's caster pawn kinds).
        public IntRange statUpgradePoints = new IntRange(0, 2);

        // Chance a spawned pawn of this xenotype becomes a psycaster at all.
        public float spawnChance = 1f;
    }
}
