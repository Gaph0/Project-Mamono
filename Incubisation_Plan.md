# Incubisation — Implementation Plan

> **Status: Phase 1 implemented (2026-08-30).** Male counterpart to mamono
> corruption: a human man gradually transformed into an **incubus** through
> sustained exposure to a Momo's mamono mana (repeated intimate essence
> transfer). Lore source: `Wiki/Incubus/` (MGE wiki pages: Incubus,
> Incubus_Extra, Mamono Lord's husband, 「YOU」).
>
> **Phase 1 locked decisions:**
> - Hunger perk uses the native hediff-stage `hungerRateFactor` field (no
>   `HungerRateFactor` StatDef exists in Core) — applies unconditionally per
>   stage; the "only while bonded" condition is deferred.
> - Willpower bonus lives in `PawnCapacityWorker_Willpower` (the custom worker
>   ignores hediff capMods); rest uses `RestFallRateFactor` statFactors, +5%
>   Consciousness capMod at full.
> - Completion notifies via `Messages.Message` (transformation convention), not
>   a letter.
> - Essence regen: 1.25× once marked, halfway-to-full once near-incubus,
>   `IncubisationEssenceRegenFull` (2×) at completion.
> - Mark refusal lives in `EssenceTransfer.BondAllowsFeeding`, so Give/Drain
>   float menus and job `FailOn(CanTransfer)` respect it automatically; the
>   berserk hunt is untouched (desperation outranks courtesy).
> - No `IncubisationMonsteriseOnLovin` setting yet — that's the Phase 2 vector.

---

## 0. Lore summary (what an incubus IS)

- **Gradual, one-way, open-ended** — unlike female monsterisation (rapid), a man
  incubises slowly through repeated sex / mana exchange with a monster. Monsters
  with abundant mana (succubi, dark matters) do it faster; *Succubus Nostrum* is
  a medicine that expedites it.
- **Still human** — no xenotype change, no drastic appearance change, stays male.
  Technically "humans altered by mamono mana", *treated as* monsters by the Order.
- **Effects:** superhuman stamina/lust; boundless energy (sex partly replaces
  sleep); extended lifespan matching his monster mate (no age frailties);
  increased essence volume/quality; can **subsist on his partner's mamono mana
  instead of food**; environmental adaptation matching his mate (heat resist,
  underwater breathing, night vision — *no* combat powers copied).
- **Marking:** his partner's mana marks him as claimed — other monsters won't
  randomly ravish a marked man (harems still possible if another monster falls
  for him; last claimant's mana dominates).
- **Vector:** an incubus's essence monsterises human women he sleeps with.
- **True nature:** incubi are humans whose "shackles" (mana limit) were broken
  by mamono mana — the *same kind of being* as heroes (broken by divine mana).
  Superhuman growth potential → Isekai-XP synergy.

---

## 1. Requirements → mechanics mapping

| # | Lore requirement | Mechanism |
|---|---|---|
| 1 | Gradual, permanent change; still human, male, same appearance | **Severity-staged hediff** `ProjectMomo_Incubisation` (whole-body, 0→1). NOT a xenotype/gene — Bio tab unchanged, no body/hair edits |
| 2 | Accrues through repeated intimate essence transfer | Dose applied inside `EssenceTransfer.Transfer` (covers Give/Drain jobs, voluntary bonding, and Intimacy-mod sex, which all funnel through it) |
| 3 | Wives incubise fastest | Bonded multiplier: a living tsugai bond with the Momo multiplies the dose |
| 4 | Mana-rich monsters transform faster | Dose scaled by essence actually moved (bigger feedings = more mana absorbed) |
| 5 | Marking: claimed men left alone by other monsters | Reverse check in `EssenceTransfer.BondAllowsFeeding` / `CanTransfer`: a man whose incubisation ≥ mark threshold is refused by non-bonded Momos (berserk forced-feeding excepted?) |
| 6 | Last claimant wins (harems) | Hediff stores `source` pawn ref + marker xenotype defName, overwritten per dose — mirrors `Hediff_MomoCorruption.Imprint` |
| 7 | Lifespan matched to mate; frailties of age shed | Extend `MomoAgeAilmentPatch` + `MomoFertilityPatch` to full-incubus stage (reuse `RemoveAgeAilments` on completion) |
| 8 | Subsists on mana instead of food | Stage stat offsets: `HungerRateFactor` ↓ (only while a living bonded Momo exists — evaluated in a custom stat part or hediff stage swap) |
| 9 | Sex partly replaces sleep; boundless stamina | Stage offsets: `RestFallRateFactor` ↓, `RestRateMultiplier` ↑, movement/consciousness small buffs at high severity |
| 10 | Increased essence volume/quality | `Need_Essence` regen multiplier × stage (same hook as `IsekaiCompat.EssenceRechargeMultiplier`) |
| 11 | "Shackles removed" = hero-like growth | Isekai: XP-gain multiplier at completion (shim, like existing `IsekaiCompat`) |
| 12 | Incubus monsterises human women via sex | Optional (setting, default ON?): lovin' completion with a corruptible woman adds one `Hediff_MomoCorruption` dose, imprinting the *marker's* xenotype — reuses `MomoTransformation` pipeline wholesale |
| 13 | Succubus Nostrum expedites | **Phase 2:** ingestible item adding a flat dose (0.25) |
| 14 | Environmental adaptation by mate's race | **Phase 2:** xenotype→stat-offset map (comfy temps, UV/darkness…) on the hediff, keyed by stored marker defName. Base mod ships Momo-generic values; Slime Faction can patch in slime values |

---

## 2. What we deliberately do NOT reuse

- **`MomoTransformation.ApplyXenotype`** — it swaps xenotype/endogenes (female
  path). Incubi keep race, xenotype, genes, backstories. Incubisation only ever
  *adds a hediff*.
- **Completion event / `CompleteTransformation()`** — unneeded: a staged hediff
  expresses "full incubus" as its final severity stage. No transform choke
  point, no pregnancy-snapshot refresh, no join-offer flow.
- **Corruption decay-while-upright** — lore says incubisation is permanent once
  mana accumulates. Default decay = 0 (setting exists for players who want
  reversible progress).

---

## 3. Severity stages (single hediff, `ProjectMomo_Incubisation`)

| Severity | Label | Effects (stacking, all tunable) |
|---|---|---|
| 0.00–0.25 | *Mana-touched* (hidden) | marker recorded; tooltip shows "Marked by {source}" |
| 0.25–0.50 | *Marked* (hidden) | non-bonded Momos refuse to drain him (lore #5); essence regen ×1.25 |
| 0.50–0.75 | *Suffused* (hidden) | rest fall −10%, hunger rate −15% (while bonded Momo lives), age ailments **prevented** |
| 0.75–1.00 | *Near-incubus* (**visible**) | essence regen ×1.5, willpower +15% (shackles loosening) |
| 1.00 | **Incubus** | existing age ailments cured; fertility ageless; hunger rate −40% while bonded; rest fall −25%; essence regen ×2; Isekai XP ×1.25; permanent |

Letter fires once at 1.0 ("{MAN} has become an incubus", marker named). Man and
marker (if colonists) get a mood thought (`ProjectMomo_IncubusAwakened` /
social `ProjectMomo_IncubusTurned`, mirroring MomoAwakened/MomoTurned).

---

## 4. Accrual

```
EssenceTransfer.Transfer(momo, human, amount, intimateSideEffects:true)
  → Incubisation.ApplyDose(momo, human, amount)
      guards: setting enabled · human male · adult (≥ BondMinAge) · non-Momo · humanlike
      dose = amount × PerEssenceFactor × (HasLivingBondWith(momo, human) ? BondedMultiplier : 1)
      clamp to DailyCap (per-man tracked, resets on game-day boundary)
      hediff.Severity += dose; hediff.Imprint(momo)
```

- Vanilla lovin' between bonded pairs (no transfer) → Phase 1.5 optional: postfix
  on lovin' completion granting a small flat dose (keeps pure-romance couples
  progressing, per lore "long period at her side as her husband").
- **No mana cost to the Momo** (unlike infusion): she's already paying the
  intimate act; the change is a side effect, not a working.

## 5. Files

**XML (Defs/, one theme per file):**
- `HediffDefs_Incubisation.xml` — the staged hediff (hediffClass
  `ProjectMomo.Hediff_Incubisation`, maxSeverity 1, `isBad false`,
  `initialSeverity 0.01`, stages with statOffsets/capMods, `stages[4].minSeverity 1`)
- `ThoughtDefs_Incubisation.xml` — awakened/turned memories + "marked" thought

**C# (Source/ProjectMomo/):**
- `Hediff_Incubisation.cs` — marker ref + xenotype defName (`Imprint(Pawn)`),
  `TipStringExtra` ("Marked by X · stage"), daily-cap bookkeeping,
  `ExposeData`; on crossing 1.0 → `MomoAgeAilmentPatch.RemoveAgeAilments`,
  letter + thoughts (hash-interval re-check catches dev-mode severity edits,
  same pattern as `Hediff_MomoCorruption`)
- `Incubisation.cs` (static) — `CanEverIncubise(pawn, out reason)`,
  `ApplyDose(momo, man, amount)`, `IsFullIncubus(pawn)`,
  `MarkProtects(momo, man)` helper
- `IncubisationCompat` edits folded into existing files:
  - `EssenceTransfer.Transfer` — one call to `Incubisation.ApplyDose`
  - `EssenceTransfer.BondAllowsFeeding` — marked-man refusal branch
  - `MomoAgeAilmentPatch.Prefix` — `IsMomo(pawn) || Incubisation.IsIncubised(pawn, 0.5f)`
  - `MomoFertilityPatch` — same guard at 1.0
  - `Need_Essence.NeedInterval` — × `Incubisation.EssenceRegenMultiplier(pawn)`
  - `IsekaiCompat` — XP multiplier shim
- `ProjectMomo_DefOf.cs` — `ProjectMomo_Incubisation` hediff, 2–3 thoughts
- `ProjectMomoSettings.cs` + `ProjectMomoModSettings.cs` — new **Incubisation**
  tab (see §6)

**Docs/meta:** CHANGELOG entry; this plan file.

## 6. Settings (defaults)

| Setting | Default |
|---|---|
| `IncubisationEnabled` | true |
| `IncubisationPerEssenceFactor` | 0.0075 (a full-bar feeding ≈ 0.0075 progress) |
| `IncubisationBondedMultiplier` | 2.0 |
| `IncubisationDailyCap` | 0.0075 (devoted husband: ~133 days to full) |
| `IncubisationMarkThreshold` | 0.25 |
| `IncubisationEssenceRegenFull` | 2.0 |
| `IncubisationMonsteriseOnLovin` | false (opt-in — it's potent) |
| `IncubisationDecayPerDay` | 0 (lore: permanent) |

## 7. Phases

1. **Core** — hediff + accrual + stages 1–5 perks (regen, rest, hunger,
   age-ailment/fertility guards, mark refusal) + settings tab + letter/thoughts.
   Fully playable standalone.
2. **Flavor** — Succubus Nostrum item; environmental adaptation offset map
   (hook exposed for Slime Faction); lovin'-without-transfer micro-dose;
   monsterise-on-lovin' vector.
3. **Future** — natural-born incubi (sons of Momos: latent marker at birth,
   matures early), harem "unsatisfied unless all wives" quirk, Order-faction
   hostility toward known incubi.

## 8. Testing checklist

- Dev: `ApplyDose` via give-essence float menu → progress visible on Health tab
- Bonded vs unbonded rate; daily cap reset; marker overwrite by second Momo
- At 0.25: wild/berserk non-bonded Momo refuses the marked man
- At 1.0: letter fires; pre-existing bad back/frail removed; birthday ailments
  never land afterward; fertility flat with age
- Save/load: marker ref + defName survive (Scribe_References/Values)
- Intimacy-mod sex accrues (Transfer path) — regression: corruption, tsugai,
  starvation flows untouched
