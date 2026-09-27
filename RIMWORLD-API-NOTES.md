# RimWorld API notes (Project Mamono)

Facts we checked in the game's own code: `Assembly-CSharp`, RimWorld 1.6.9676.
Each note says what the player sees, why it happens, and how to write it right.
Add a new note under its own heading. Keep the notes short.

Files we read live in
`~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/Assembly-CSharp.dll`.

---

## Letters show accented letters: use `Formatted()`, never `Translate()`

**What the player sees.** The whole letter comes out with accents, like
"ĥàṡ ẉàŀķèḍ". Any name you put into the text stays plain.
Seen on 2026-09-24 in the Reptiles medusa ruins ambush letter.

**Why it happens.** `"...".Translate()` looks up a translation key. A finished
sentence from a def is not a key, so the lookup fails. RimWorld then returns the
sentence as the "translation" - and when Dev Mode is on, it runs that result
through `Translator.PseudoTranslated`.

`PseudoTranslated` swaps every letter for an accented one: a→à, e→è, i→ì, o→ò,
u→ù, l→ſ, k→к, s→ș, t→ṭ, and so on. It copies `{0}` as it is, so the name you
pass in gets filled in afterwards and looks normal. That clean name inside
accented text is the tell.

**How to write it.** Fill the placeholder with `Formatted()`. It only swaps the
argument, and it never looks for a key.

- Our example: `IncidentWorker_MedusaRuinsAmbush` in
  `Source/Reptiles/RuinsAmbush.cs` (`Project Mamono Reptiles`).
- Vanilla example: `IncidentWorker_Ambush_ManhunterPack.GetLetterText` fills
  `def.letterText` with `Formatted(...)`.
- Pass the argument with no label. Then `{0}` fills by position, and a
  translator stays free to move it.

**Watch out.**

- This only shows with Dev Mode on. Players who use Dev Mode still hit it, so
  fix the mod. Do not just tell them to turn Dev Mode off.
- Look for the same bug wherever `Translate()` gets text that was never made as
  a key: a def's `label`, a sentence built in code, or a key with a typo.
- `"yourCaravan"` is a real key in
  `Core/Languages/English/Keyed/Misc_Gameplay.xml`, so it is safe to translate.

**How to check the player's settings.** The saved options are in
`Config/Prefs.xml`: `<devMode>True</devMode>` and
`<langFolderName>English</langFolderName>`.

**A different message, same words.** The log line
`Translation data for language English has 19 errors` is not this bug. It means
a mod ships a DefInjected file with keys that point at fields a def no longer
has. Ask the player to make a translation report from the options to see which
mod.
