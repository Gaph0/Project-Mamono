# Changelog

## Player-facing

- 2026-09-22: Fixed a caravan with a flying or large-frame momo moving far faster than it should. The speed is applied once now, not twice.

- 2026-09-22: Changed the Medieval Overhaul factions to stay out of a new world. They no longer fill it with settlements, and you can still add them by hand at world creation.

- 2026-09-22: Added a mute pawn being unable to propose or accept a tsugai bond or a transformation.

- 2026-09-21: Changed both flight genes to ignore rough ground. Mud, sand, snow and slush no longer slow a flying momo on a map, and she crosses water at her own pace.
- 2026-09-21: Added the large frame gene. She counts as a riding animal in caravans, and any caravan she joins moves 20% faster than with a normal pawn.
- 2026-09-20: Changed the mod family's own notes to stay out of the log unless development mode is on, so an ordinary log stays quiet.
- 2026-09-20: Added captive momos feeding on their fellow prisoners when their mana runs low.
- 2026-09-20: Changed captive momos to never break from low mana. A hungry captive just waits.
- 2026-09-20: Fixed a pawn standing still forever after starting a voluntary bond proposal he could not walk to. The order is refused now, and a walk that stalls gives up.
- 2026-09-20: Fixed a right-click bond proposal always being accepted. The answer is rolled when she reaches him, as the menu option promises.
- 2026-09-20: Fixed a bond partner being left standing in place for good after the ceremony. He is released when it ends, and the hold can never outlast it.
- 2026-09-20: Changed a visiting momo's mana to stop draining at a fifth of a bar, so no visit can leave her starving.
- 2026-09-20: Changed visiting momos to arrive with a full mana bar, instead of the half bar they were generated with.
- 2026-09-20: Added one "momo corpses" line, so every momo mod groups its corpses under a single heading.
- 2026-09-20: Fixed the Health tab so blood pumping and breathing show the rise from tease build-up.
- 2026-09-20: Changed the weak flight gene to draw its own smaller wings, so every carrier gets them.
- 2026-09-20: Changed the weak flight gene's text so it fits heavy momos with weak wings, not just small ones.
- 2026-09-20: Added the weak flight gene. She gets the fly ability and wings, but no faster caravan travel and no passenger.
- 2026-09-19: Added wings to dragons, wyverns and malef dragons. They fly on the Big & Small winged body.
- 2026-09-19: Changed Big & Small body parts, such as wings, to be tougher than the rest of the body.
- 2026-09-19: Added Big and Small - Framework as a required mod.
- 2026-09-06: Changed autonomous bonding so your own colonists no longer propose a tsugai bond on their own by default.
- 2026-09-06: Changed the Empire and Traders Guild xenotypes to include monster girls, and let both factions spawn normally.
- 2026-09-05: Added a setting that stops your own colonists from proposing a tsugai bond on their own. The right-click proposal still works.
- 2026-09-05: Fixed venom-pinned victims escaping when their movement was buffed.
- 2026-09-05: Rebalanced the flight gene: it now costs 2 metabolic efficiency.
- 2026-09-05: Rebalanced the fiery momo gene: it now costs 4 metabolic efficiency.
- 2026-09-05: Added three monster ascension precept varieties: relaxed, exalted and strict. All ascension precepts and awakening rites now need the Monster Extremists meme.
- 2026-09-05: Changed autonomous infusion: your own colonists stop doing it on their own.
- 2026-09-05: Changed raider momos to infuse on their own. Wild and visiting momos stay feral.
- 2026-09-05: Changed infusion orders you give yourself to work without restriction.
- 2026-09-05: Changed corruption infusion to work only on targets the momo is hostile to.
- 2026-09-05: Rebalanced momo claws: the tease multiplier is lowered from 1.5 to 1.15.
- 2026-09-05: Added three bloodline genes: momo venom, fiery momo and momo claws.
- 2026-09-05: Added a Genes tab in mod settings.
- 2026-09-03: Added the Monster Extremists ideology meme, with opinions that admire ascended momos, and two awakening rites. A prisoner or slave can be forcibly awakened as a random monster.
- 2026-09-03: Added an Ideology tab in mod settings.
- 2026-09-02: Added autonomous mana feeding. A bonded momo seeks her mate when her mana runs low.
- 2026-08-30: Fixed momos dealing no melee damage to wild animals.
- 2026-08-30: Changed a full incubus to regain food from essence transfer, so he lives off his mate's mana.
- 2026-08-30: Changed incubisation to stay hidden in the Health tab until it passes 75%, then appear.
- 2026-08-30: Rebalanced incubisation to build up 20 times slower. A full incubus takes about 133 days bonded and 267 days unbonded.
- 2026-08-30: Added incubisation. Men slowly become incubi through intimate essence transfer, and gain age, rest, hunger and essence perks.
- 2026-08-29: Changed the momo gene to cure and prevent old-age ailments without a message.

## Internal

- 2026-09-22: Removed the superseded flight caravan patch, which patched the same method as CaravanMountSpeedPatch and applied the riding factor twice.

- 2026-09-22: Added a patch that zeroes Medieval Overhaul's faction start counts, the same pair of fields the vanilla faction patch clears.

- 2026-09-22: Added a soft hook that reads the mute trait from Progression: Education by name.

- 2026-09-21: Added FlyingPawnTerrainCostPatch, which prices a flying pawn's cell as her own ticks-per-move (the VEF floating-creature approach; impassable terrain and things still cost 10000).
- 2026-09-21: Changed FlightCaravanSpeedPatch into CaravanMountSpeedPatch, which now counts a large-frame momo as a caravan mount too.
- 2026-09-20: Added `PMMLog`, a development-mode-gated wrapper for the family's own log lines.
- 2026-09-20: Added ProjectMomo_DrainEssenceDry, a dry-drain variant of the drain job that skips the lovin' memory and the lovin' job.
- 2026-09-20: Added EssenceTransfer.FindFellowPrisonerToDrain and the captive branch of JobGiver_SeekManaFeeding, taken when no bonded mate is reachable.
- 2026-09-20: Changed LowManaBreak to skip prisoners in CheckBreak and Trigger, so neither the random break nor the guaranteed desperate one fires for a captive.
- 2026-09-20: Added a reachability gate to `CanProposeTo` and a give-up condition to the proposal driver's walk toil, so a job can never hang waiting on an arrival that cannot happen.
- 2026-09-20: Changed the proposal driver to roll acceptance face to face instead of carrying the verdict on `Job.playerForced`, which vanilla overwrites when a job is player-ordered.
- 2026-09-20: Added `HoldPartner`/`ReleasePartner` to the ceremony drivers, so a held partner is released when the ceremony ends and cannot be held past it.
- 2026-09-20: Added the `GuestManaFloor` setting and the drain floor in `Need_Mana`, so a visiting momo's mana cannot empty.
- 2026-09-20: Added `Need_Mana.DrainMultiplier`, so the drain rate can be reasoned about outside the need.
- 2026-09-20: Added a spawn hook that fills a visiting momo's mana need, so every mod's visitors arrive fed.
- 2026-09-20: Added the shared momo corpse category and the code that files momo corpses under it.
- 2026-09-20: Fixed the faction xenotype patch to use the new species names.
- 2026-09-20: Added a Big and Small call to the transformation, so the new xenotype's declared race is applied when a woman transforms and not only at pawn generation.
- 2026-09-20: Changed the flight gene's comment to explain why its body swap only reaches Human-race pawns.
- 2026-09-20: Removed the post-commit changelog hook, which wrote entries in the old format.
- 2026-09-20: Added `changelog-check.sh`, and the build now runs it before compiling.
- 2026-09-20: Added the missing Big and Small - Framework dependency to `About.xml`.
- 2026-09-20: Changed the README, and the stale comments in the defs and code, to match what the code does.
- 2026-09-19: Changed the build to one solution, one shared props file and one command.
- 2026-09-19: Added XSD schema links to the def roots, so def edits validate in the editor.
- 2026-09-19: Changed the Auto Mod Config preset to document the version-bump convention.
- 2026-09-19: Added an Auto Mod Config preset for Sensible Factions that weights Momo family factions.
- 2026-09-19: Added the Big & Small assembly reference and XSD types for its defs.
- 2026-09-19: Added colour tags for the Big & Small custom race UI.
- 2026-09-19: Added a GlobalSettings def that turns every Big & Small feature off.
- 2026-09-05: Removed the voluntary-transform override hook. The transform driver applies the proposer's xenotype again.
- 2026-09-05: Added MomoTransformation.ConvertXenotype, so one monster xenotype can be swapped for another.
- 2026-09-02: Removed the bundled MGE wiki copies and the incubisation plan doc.
- 2026-08-30: Added incubisation debug actions.
- 2026-08-29: Removed the Workshop upload step from the release script.
- 2026-08-29: Added a SteamCMD Workshop upload script.
- 2026-08-29: Changed release.sh to skip a fresh build and tag the release.
- 2026-08-29: Added release.sh for GitHub releases.
- 2026-08-29: Added build.sh and a pre-push build.
- 2026-08-29: Fixed a compile error and added the missing VPEPsycastsEnabled setting.
- 2026-08-29: Fixed the changelog hook's sed address.
