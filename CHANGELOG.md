# Changelog
- 2026-09-05: Revert voluntary-transform override hook; driver back to plain proposer-xenotype path The Malef colony offer that used VoluntaryTransformTargetOverride was scrapped in favour of an ingestible item, so the hook is removed and JobDriver_TransformProposal once again always applies the proposer's xenotype. ConvertXenotype STAYS: the new Dark Dragon's Blood item uses it to re-stamp a normal Dragon into a Malef.
- 2026-09-05: Voluntary transformation: target-xenotype override hook + momo-target conversion JobDriver_TransformProposal always applied the proposer's own xenotype and ApplyXenotype refused momo targets ('already a monster'), so a sub-mod could never make a voluntary offer remake a victim differently. Adds MomoTransformation.VoluntaryTransformTargetOverride (proposer, target) -> xenotype, consulted at the ceremony's end; a monster target now goes through ConvertXenotype instead of being silently refused. Default behaviour unchanged (null override = proposer's xenotype; baseline targets unchanged). Needed by PMM Reptiles' Malef Dragon colony offer (1a: Dragon -> Malef via momo-momo offer).
- 2026-09-05: Add MomoTransformation.ConvertXenotype: re-stamp an already-monster pawn CanEverTransform refuses momos ('already a monster'), so ApplyXenotype could never swap one monster xenotype for another. ConvertXenotype removes the old def-based xenotype's endogenes the new xenotype lacks, then applies the same gene stamp / pregnancy-snapshot refresh / notification as a first corruption. Faction and join outcomes are untouched. Needed by PMM Reptiles' Malef Dragon corruption (Dragon -> Malef Dragon, other momos -> Dragonewt).
- 2026-09-05: Cap venom-pinned victims' Moving with setMax so buffed vitals can't beat it
- 2026-09-05: Give the flight gene -2 metabolic efficiency
- 2026-09-05: Give the fiery momo gene -4 metabolic efficiency
- 2026-09-05: feat: monster ascension precept varieties (relaxed: admires the
  ascended, no judgment of non-momos; exalted: unchanged; strict: doubled
  opinions) - all ascension precepts and both awakening rites now require the
  Monster Extremists meme
- 2026-09-05: Stop colonists from autonomously infusing
- 2026-09-05: Let raider Momos autonomously infuse; keep wild/visiting Momos feral
- 2026-09-05: Limit autonomous infusion to colonist Momos; restore unrestricted player orders
- 2026-09-05: Restrict corruption infusions to targets the Momo is hostile to
- 2026-09-05: balance: momo claws tease multiplier 1.5 -> 1.15
- 2026-09-05: feat: momo venom, fiery momo and momo claws genes
- 2026-09-05: feat: three bloodline genes - momo venom (melee injects a
  non-lethal slowing toxin into any living victim, pins at full dose, wears off
  in 4 hours), fiery momo (immune to fire/heat/lava; tsugai partners gain a
  lesser ward: +40C comfy ceiling, 15% less flammable, 15% less flame damage),
  momo claws (+15% tease damage, -15% manipulation). New Genes settings tab
- 2026-09-03: feat: Monster Extremists ideology meme - ascended/baseliner opinions, rite of awakening (colonist -> corruptor's xenotype), rite of forced awakening (prisoner/slave -> random monster xenotype, will/resistance/certainty shaken), Ideology settings tab
- 2026-09-02: chore: drop bundled MGE wiki copies and incubisation plan doc
- 2026-09-02: feat: autonomous mana feeding - bonded momos seek their mate at low mana
- 2026-08-30: fix: tease prefix humanlike gate - momos deal damage to animals again
- 2026-08-30: feat: full incubus regains Food on essence transfer (subsists on mate's mana, IncubusFoodPerEssence)
- 2026-08-30: feat: incubisation hidden from Health tab until 75% severity (becomeVisible), then surfaces
- 2026-08-30: dev: incubisation debug actions (progress, mark, complete, clear, status, mark-protection test, reset daily cap)
- 2026-08-30: balance: incubisation accrual slowed 20x (full incubus: ~133 days bonded, ~267 unbonded)
- 2026-08-30: feat: incubisation — men gradually change into incubi through intimate essence transfer (staged hediff, marking, age/rest/hunger/essence perks, settings tab)
- 2026-08-29: feat: Momo gene silently cures and prevents age-related ailments
- 2026-08-29: ci: remove workshop upload integration
- 2026-08-29: ci: add steamcmd workshop upload script
- 2026-08-29: ci: skip fresh builds and auto-tag in release.sh
- 2026-08-29: ci: add release.sh for GitHub releases
- 2026-08-29: build: add build.sh (Roslyn csc), pre-push builds before pushing
- 2026-08-29: fix: CS1738 named args, add missing VPEPsycastsEnabled setting
- 2026-08-29: fix: changelog hook sed address (1a)
- 2026-08-30: fix: momos dealt zero melee damage to wild animals - the tease prefix
  cancelled physical damage for ANY non-momo victim but only dealt tease to humanlikes,
  so animals got their hits cancelled with no tease. Added the humanlike gate to the
  prefix: non-human victims (animals/insects/mechs) keep vanilla damage
