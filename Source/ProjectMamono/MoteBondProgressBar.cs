using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// A <see cref="MoteProgressBar"/> that draws its fill in pink instead of the
    /// vanilla gold. Vanilla bakes its fill/unfill colours into shared static
    /// materials (<c>MoteProgressBar.FilledMat</c> / <c>UnfilledMat</c>), so we
    /// can't just tint one instance - overriding DrawAt lets us swap in our own
    /// materials without touching the globals every other progress bar uses.
    /// Height and width match the vanilla bar exactly
    /// (<c>linearScale</c> 0.68 × 0.12, margin 0.12).
    ///
    /// The pink materials are created eagerly in a static constructor that runs on
    /// the main thread at startup - Unity requires Materials to be created there,
    /// whereas DrawAt runs on the render thread. This mirrors how vanilla builds
    /// MoteProgressBar's own FilledMat/UnfilledMat.
    /// </summary>
    [StaticConstructorOnStartup]
    public class MoteBondProgressBar : MoteProgressBar
    {
        // Vanilla bar dimensions (SubEffecter_ProgressBar sets these on the mote).
        private const float BarWidth = 0.68f;
        private const float BarHeight = 0.12f;
        private const float BarMargin = 0.12f;

        // Soft Mamono pink for the fill; the unfilled track keeps the vanilla translucent dark.
        private static readonly Color FillColor = new Color(0.99f, 0.55f, 0.78f, 0.85f);
        private static readonly Color UnfillColor = new Color(0.3f, 0.3f, 0.3f, 0.65f);

        private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(FillColor, ShaderDatabase.MetaOverlay);
        private static readonly Material UnfilledMat = SolidColorMaterials.NewSolidColorMaterial(UnfillColor, ShaderDatabase.MetaOverlay);

        /// <summary>Sets the bar to the exact vanilla size (called once at spawn).</summary>
        public void SetVanillaBarSize()
        {
            linearScale = new Vector3(BarWidth, 1f, BarHeight);
        }

        /// <summary>Same as the base DrawAt, but with pink materials.</summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            UpdatePositionAndRotation();
            if (OnlyShowForClosestZoom && Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest)
            {
                return;
            }

            if (Find.ScreenshotModeHandler.Active)
            {
                return;
            }

            GenDraw.FillableBarRequest request = default(GenDraw.FillableBarRequest);
            request.center = exactPosition;
            request.center.z += offsetZ;
            request.size = new Vector2(linearScale.x, linearScale.z);
            request.fillPercent = progress;
            request.filledMat = FilledMat;
            request.unfilledMat = UnfilledMat;
            request.margin = BarMargin;
            GenDraw.DrawFillableBar(request);
        }
    }
}
