using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// Defensive patches for the social interaction log. Other mods (and occasionally
    /// edge cases in vanilla) can create PlayLogEntry_Interaction records with null
    /// initiators, recipients, missing rule packs, or missing interaction icons. When
    /// that happens the Social tab aborts, leaving GUI groups unbalanced and spamming
    /// the log. These finalizers catch those failures and return harmless placeholders
    /// so the tab still renders and the player can see the rest of the log.
    /// </summary>
    public static class SocialLogSanityPatch
    {
        [HarmonyPatch(typeof(PlayLogEntry_Interaction), "ToGameStringFromPOV_Worker")]
        public static class TextFinalizer
        {
            public static System.Exception Finalizer(System.Exception __exception, ref string __result, Thing pov)
            {
                if (__exception != null)
                {
                    Log.WarningOnce(
                        $"[Project Mamono] Suppressed social-log text error for {pov?.LabelShort ?? "null pov"}. " +
                        $"Another mod likely created a malformed interaction log entry. Exception: {__exception.GetType().Name}: {__exception.Message}",
                        78234651);
                    __result = "[broken interaction log entry]";
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(PlayLogEntry_Interaction), "IconFromPOV")]
        public static class IconFinalizer
        {
            public static System.Exception Finalizer(System.Exception __exception, ref Texture2D __result, Thing pov)
            {
                if (__exception != null)
                {
                    Log.WarningOnce(
                        $"[Project Mamono] Suppressed social-log icon error for {pov?.LabelShort ?? "null pov"}. " +
                        $"Another mod likely created a malformed interaction log entry. Exception: {__exception.GetType().Name}: {__exception.Message}",
                        78234652);
                    __result = BaseContent.BadTex;
                }
                return null;
            }
        }

        [HarmonyPatch(typeof(PlayLogEntry_Interaction), "IconColorFromPOV")]
        public static class IconColorFinalizer
        {
            public static System.Exception Finalizer(System.Exception __exception, ref Color? __result, Thing pov)
            {
                if (__exception != null)
                {
                    Log.WarningOnce(
                        $"[Project Mamono] Suppressed social-log icon-color error for {pov?.LabelShort ?? "null pov"}. " +
                        $"Another mod likely created a malformed interaction log entry. Exception: {__exception.GetType().Name}: {__exception.Message}",
                        78234653);
                    __result = Color.white;
                }
                return null;
            }
        }

    }
}
