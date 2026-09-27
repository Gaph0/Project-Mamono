using Verse;

namespace ProjectMamono
{
    /// <summary>
    /// The mamono family's development log. Everything the mods print for their own benefit goes
    /// through here instead of Log.Message, so an ordinary game's log stays quiet - the wander-in
    /// workers alone would otherwise print a line for every species they refuse, every time an
    /// attempt is rolled.
    ///
    /// That is not only tidiness: RimWorld stops logging for the rest of the session once a session
    /// reaches 10,000 messages ("Reached max messages limit. Stopping logging to avoid spam."), so
    /// chatter like this can silence the very log a bug report needs.
    ///
    /// Turn on development mode (Options → Development mode) to see everything again. Warnings and
    /// errors are deliberately NOT routed through here: those are for real problems, and they should
    /// reach players.
    /// </summary>
    public static class PMMLog
    {
        /// <summary>Logs a development line. Visible only in development mode.</summary>
        public static void Message(string text)
        {
            if (Prefs.DevMode)
            {
                Log.Message(text);
            }
        }
    }
}
