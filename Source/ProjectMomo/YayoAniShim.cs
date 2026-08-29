using UnityEngine;
using Verse;
using YayoAnimation;
using YayoAnimation.Data;

namespace ProjectMomo
{
    /// <summary>
    /// The Yayo-dependent half of the tsugai bonding animation. JobDriver_FormTsugai
    /// references this type through its <c>[MayRequire]</c> field, so the field is
    /// null (and the animation silently skipped) when Yayo's Animation isn't
    /// installed. YayoAniCompat forwards its CheckAni postfix here, and the driver
    /// pings the partner each tick so the downed pawn's body plays the same
    /// romancin' bounce through Yayo's normal rendering path.
    ///
    /// SOFT DEPENDENCY: no other type may touch this one unless Yayo is active.
    /// </summary>
    public static class YayoAniShim
    {
        // Same thrust offsets Yayo's own lovin' animation uses.
        private static readonly Vector3 ZOffset003 = new Vector3(0f, 0f, 0.03f);
        private static readonly Vector3 ZOffsetM003 = new Vector3(0f, 0f, -0.03f);
        private static readonly Vector3 ZOffset006 = new Vector3(0f, 0f, 0.06f);

        /// <summary>
        /// Applies the bonding bounce to a pawn's Yayo draw data, using Yayo's own
        /// Core.Ani tween segments so it integrates exactly like one of Yayo's
        /// built-in job animations (a lean-in buildup, then a rhythmic thrust along
        /// the axis facing the partner). Faces <paramref name="pawn"/> toward
        /// <paramref name="other"/> and applies the same upright motion to both — so a
        /// downed partner animates as if he weren't downed (fixedRot overrides the
        /// lying rotation Yayo would otherwise use).
        /// </summary>
        public static void ApplyBondAnimation(Pawn pawn, Pawn other, Rot4 rot, PawnDrawData pdd)
        {
            Rot4 facing = other != null ? FacingToward(other.DrawPos - pawn.DrawPos, rot) : (rot.IsValid ? rot : Rot4.South);

            float oa = 0f;
            Vector3 op = Vector3.zero;

            // 120-tick (2s) cycle, desynced per pawn: 40-tick lean-in, then a
            // 40-tick steady thrust, then settle back. Both pawns share the same
            // tick clock and cycle, so they stay in phase with each other.
            int idTick = pawn.thingIDNumber * 20;
            int t = (Find.TickManager.TicksGame + idTick % 30) % 120;

            // Lean in toward the partner (0 -> 40 ticks).
            if (!Core.Ani(ref t, 20, ref oa, 0f, 4f, -1f, ref op, Vector3.zero, ZOffset003, facing, Core.tweenType.sin, facing) &&
                !Core.Ani(ref t, 20, ref oa, 4f, 6f, -1f, ref op, ZOffset003, ZOffset006, facing, Core.tweenType.sin, facing))
            {
                // Thrust rhythmically (40 -> 120 ticks, 20-tick sub-cycles).
                t = (Find.TickManager.TicksGame + idTick) % 20;
                if (!Core.Ani(ref t, 10, ref oa, 6f, 6f, -1f, ref op, ZOffset003, ZOffsetM003, facing, Core.tweenType.sin, facing))
                {
                    Core.Ani(ref t, 10, ref oa, 6f, 6f, -1f, ref op, ZOffsetM003, ZOffset003, facing, Core.tweenType.sin, facing);
                }
            }

            pdd.angleOffset = oa;
            pdd.posOffset = new Vector3(op.x, 0f, op.z);
            pdd.fixedRot = facing;
        }

        /// <summary>
        /// Called each bond toil tick so the downed partner also bounces. No-op when
        /// the partner isn't spawned, down, or humanlike.
        /// </summary>
        public static void PingPartner(Pawn partner, Pawn momo)
        {
            if (partner == null || !partner.Spawned || !partner.RaceProps.Humanlike)
            {
                return;
            }

            // Face them toward their partner (upright) rather than using any downed
            // rotation. Works for standing partners too (essence feeding).
            ApplyBondAnimation(partner, momo, partner.Rotation, partner.GetData());
        }

        /// <summary>Cardinal direction from a pawn toward their bonding partner.</summary>
        private static Rot4 FacingToward(Vector3 delta, Rot4 fallback)
        {
            if (delta.x * delta.x + delta.z * delta.z < 0.001f)
            {
                return fallback.IsValid ? fallback : Rot4.South;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                return delta.x >= 0f ? Rot4.East : Rot4.West;
            }

            return delta.z >= 0f ? Rot4.North : Rot4.South;
        }
    }
}
