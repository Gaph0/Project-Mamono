# Changelog
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
