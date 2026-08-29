using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The Social tab crashes when it tries to render a PlayLogEntry_Interaction
    /// whose initiator or recipient pawn has become null (despawned, dead+removed,
    /// or created malformed by another mod). This prefix sanitises the entry list
    /// before InteractionCardUtility.DrawInteractionsLog renders it, removing any
    /// interaction whose participants can no longer be resolved.
    /// </summary>
    [HarmonyPatch(typeof(InteractionCardUtility), "DrawInteractionsLog")]
    public static class SocialLogEntryValidator
    {
        // PlayLogEntry_Interaction stores its pawns in private fields. Cache the
        // fields so we don't pay reflection cost every frame.
        private static readonly FieldInfo InitiatorField;
        private static readonly FieldInfo RecipientField;

        static SocialLogEntryValidator()
        {
            System.Type t = typeof(PlayLogEntry_Interaction);
            InitiatorField = t.GetField("initiator", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField("initiatorPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            RecipientField = t.GetField("recipient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField("recipientPawn", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void Prefix(ref List<LogEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i] is PlayLogEntry_Interaction interaction && HasNullParticipant(interaction))
                {
                    entries.RemoveAt(i);
                }
            }
        }

        private static bool HasNullParticipant(PlayLogEntry_Interaction entry)
        {
            if (entry == null)
            {
                return true;
            }

            if (InitiatorField != null && InitiatorField.GetValue(entry) == null)
            {
                return true;
            }

            if (RecipientField != null && RecipientField.GetValue(entry) == null)
            {
                return true;
            }

            return false;
        }
    }
}
