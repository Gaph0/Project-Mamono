# Changelog

## Player-facing

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
