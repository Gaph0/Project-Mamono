# Project Mamono

Core mod of the Mamono Project: monster-girl (MGE-flavored) mechanics for
RimWorld 1.6. Mamonos - women carrying the Mamono Lord's "Inma" - feed on the
essence of men, erode wills with teasing strikes, claim mates through the
tsugai bond, remake women into monsters, and slowly turn the men they feed on
into incubi.

## Requirements

Hard dependencies (must load before this mod):

- Harmony
- Biotech
- Big and Small - Framework - race bodies, size scaling, wings and serpent tails
- ISEKAI RPG Leveling - stats, levels and XP drive most formulas below
- Modlist Configurator

Optional integrations (detected at runtime):

- Ideology (DLC) - the Monster Extremists meme: opinions + transformation rites
- yayo's Animation (continued) - bonding/feeding/sex animations
- Intimacy – Friends n' Lovers - sex feeds the Mamono
- Vanilla Psycasts Expanded - monster xenotypes can spawn as psycasters

## The Mamono gene and xenotype

The `Mamono` gene (`ProjectMamono_Mamono`, geneClass `ProjectMamono.Gene_Mamono`,
Reproduction category):

- Forces its holder female on add (gender, body type, feminine hairstyle).
- Silently cures age-related ailments and blocks new ones (hourly backstop
  sweep catches mod/quest-forced ones).
- Fertility never declines with age, and nothing makes her sterile - not
  sterilization, drained or removed ovaries, or sterile genes (Biotech).
- Immune to all nudity thoughts; lovin' age factor forced to 1.
- Enables the Mana need, disables the Essence need.
- Melee strikes inflict tease damage instead of physical damage (see below).

`ProjectMamono_Xenotype_Mamono` is the base Mamono xenotype: just the Mamono gene,
inheritable, never spawns naturally - the only ways to become one are birth,
corruption, or a xenogerm. It is the fallback corruption outcome when the
corrupting Mamono has no def-based monster xenotype of her own; submods (e.g.
the Slime Faction) define their own monster xenotypes that include the Mamono
gene, and those imprint instead.

Children of a Mamono mother inherit her full endogenes (nothing from the
father, never a hybrid) and are always female; her xenotype is re-applied
after birth. Implanting a xenogerm that contains any monster-xenotype gene
completes the full transformation into the smallest matching xenotype
(implanted genes stay xenogenes, the rest become endogenes; gated on the
Corruption master switch).

## Needs: mana and essence

### Mana (`ProjectMamono_Mana` - Mamono-carriers only)

- Drains a full bar over 2 days. Avg(WIS, INT) above 5 reduces the drain
  3%/point (clamped 0.3x–1.2x); wild Mamonos drain at 0.25x.
- A Mamono in a caravan keeps her mana. The drain and the starvation decline
  pause while she travels, and start again when she is back on a map.
- Eating restores 0.15 mana per nutrition - a supplement, not a substitute.
- Feeding moves essence into mana 1:1 (Give/Drain jobs, bonding, Intimacy
  sex).
- At empty, `ProjectMamono_ManaStarvation` escalates to full over 1.5 days:
  **drained** (Manipulation ≤ 90%) → **desperate** (Consciousness ≤ 70%,
  Moving ≤ 80%, Manipulation ≤ 60%) → **collapsed** (Consciousness ≤ 10%).
  Never lethal - she collapses, not dies.
- Below 10% mana she can randomly suffer a feeding break (MTB 0.5 days); at
  desperate severity a break is guaranteed once per starvation episode (see
  "Low-mana mental breaks").

### Essence (`ProjectMamono_Essence` - ordinary humans)

- Regenerates from empty to full in 1 day. Avg(STR, VIT) above 5 speeds this
  3%/point (clamped 0.5x–3x); incubisation multiplies it (up to 2x for a full
  incubus).
- A transfer moves `min(requested, his essence, her headroom)`; an empty man
  simply can't be drained.
- Each transfer awards Isekai XP (50 per full essence point) and gives both
  parties the +5 "shared essence" moodlet (1 day); intimate transfers queue
  vanilla lovin' next tick when a usable bed exists. Feeding while in a
  mental break grants the +30 "feeding catharsis" thought (3 days).

### Feeding rules

- A Mamono with at least one living tsugai bond may **only** feed from bonded
  partners.
- An unbonded Mamono refuses men **marked** by another living Mamono (see
  Incubisation).
- Right-click float-menu orders: "Give essence" (human → Mamono) and "Drain
  essence" (Mamono → human), greyed out with a reason when the rules block
  them. On one of your own prisoners or slaves the drain ignores both rules
  above - any Mamono may take that meal - and skips the lovin' afterwards.

## Willpower and tease damage

`ProjectMamono_Willpower` is a pawn capacity on the Health tab (hidden for
Mamono-carriers themselves, via `WillpowerVisibilityPatch`):

```
Willpower = 1
          × (1 + (VIT-5) × 0.02)                 (clamped 0.5–3)
          × (1 + 0.25 × living tsugai bonds)
          × 1.15                                 (incubisation ≥ 75%)
          × (1 - min(0.5 × active griefs, 0.75)) (bond loss)
          × (1 - tease severity)
```

**Tease damage:** when a Mamono-carrier lands a melee hit on a non-Mamono
humanlike, all physical damage is cancelled and the victim instead gains
`ProjectMamono_TeaseDamage` on the brain (max severity 1, fades 0.5/day;
stages rosy → flustered → aroused → overwhelmed):

```
severity/hit = 0.05 × CHA mult (+5%/point over 5, clamped 0.5–3)
                    × WIS resist (-3%/point over 5, clamped 0.3–1.2)
                    × (1 + beauty × 0.25)          (clamped 0.3–3)
```

Tease also multiplies the victim's Blood Pumping and Breathing by
`1 + severity × 0.5` - arousal makes the body overperform even as the mind
gives out.

**Knockout:** when raw willpower (`1 - tease severity`) falls to 10% (tease
severity 0.9), the victim gains `ProjectMamono_WillpowerBreak` - Moving set to
0: they collapse, awake but helpless ("broken will"), and are forced downed.
The break lifts automatically once tease fades below 0.88 (0.02 hysteresis).
The attacker gains 25 XP (plus 40 XP per full tease severity dealt), the
victim suffers the Isekai broken-will mood, and if the victim is a bondable
man the Mamono moves in to claim him (see Tsugai bonds).

## Tsugai bonds

The tsugai (`ProjectMamono_Tsugai` relation, `ProjectMamono_TsugaiBond` hediff)
is the mate-bond between a Mamono and a man, shown on the Social tab
("bonded").

Effects while the bond lives:

- +25% willpower per living bond (both partners; up to 10 bonds).
- Opinion floored at 100; compatibility and romance/lovin' chance factors
  floored at max between bonded partners.
- Feeding exclusivity (above); Intimacy sex with a bonded partner feeds her.
- "Found my mate" moodlet scaling with bond count (+10/+16/+22).
- Harem-friendly: the "cheated on me" thought is suppressed between pawns
  who share a living mate; body-purist / prosthetic-precept thoughts are
  suppressed for bonded pawns and Mamonos.

### Forced bonding

- Fires automatically when a Mamono tease-knocks-out a bondable man: male,
  16+, non-Mamono, unattached (an Isekai Protagonist can be harem-claimed while
  attached); one living husband per Mamono.
- A ~30s bonding ritual (pink progress bar, yayo animation) seals the bond
  and drains all his remaining essence into her mana - with a full
  incubisation dose.
- A wild Mamono bonded by a colonist is tamed outright. Otherwise a faction
  Mamono rolls to join the colony: 25% + 5% per level gap (husband − Mamono),
  capped at 90%; a Protagonist husband always succeeds. On failure she
  kidnaps the downed husband off-map as her faction's captive (ransom /
  rescue, `ThreatBig` letter).

### Voluntary bonding

- Mamonos and men autonomously propose to partners they desire. The desire
  score weighs opinion, romance chance, an existing lover/fiancé/spouse
  relation, his essence, her mana hunger, and grief; it must reach 0.6 to
  act (6h attempt cooldown per pawn, 24h rejection cooldown per pair). Your own colonists are
  exempt by default (`VoluntaryBondColonistProposals`, on the Tsugai bond tab), so only
  outsiders propose on their own - and only to each other: a visitor never bonds one
  of your colonists, in either direction. Your pawns bond only when you order it.
- A bond costs the man half his essence bar - he must hold at least 0.5 to
  offer or be offered one.
- Acceptance: desire floor 0.25, scaling to near-certain at 0.95; a man's
  proposal to a Mamono is always accepted. Willing bonds grant both partners
  the +8 "bound by choice" thought and a flat +25% join bonus; proposals
  and rejections (-5, 2 days) are recorded in the social log.
- Player-initiated: right-click a man with a Mamono selected (or vice versa)
  → "Propose tsugai bond".

### Bond loss and restoration

- When a bonded partner dies, the relation is severed and the survivor
  gains `ProjectMamono_TsugaiLoss` (5–10 days) plus a −20 mood thought for 30
  days ("lost my bonded mate" - heavier than a spouse's death). Each active
  grief stacks −50% willpower, total penalty capped at 75%.
- Resurrecting the dead mate lifts the grief and grants +12 "my bonded mate
  returned" (15 days) - but the bond itself is not re-established; they
  part as former mates.

## Mamono corruption (female → monster)

`ProjectMamono_MamonoCorruption` is a Mamono's mana rewriting a woman into a
monster (stages: mana-touched → infused → blooming → on the verge).

### Forced corruption

- A calm Mamono with ≥ 20% mana infuses a **downed** corruptible woman
  (female, 16+, not already a carrier; pregnancy does not protect).
  Only hostile Mamonos - raiders - infuse on their own; your colonists never do, and wild or
  visiting Mamonos stay feral. A player order (right-click) works on any eligible target.
- Each completed infusion (~20s) costs 20% mana and adds 25% severity -
  four infusions to transform. While the victim is upright and fighting,
  corruption decays 20%/day, so partial corruption is reversible.
- Last corruptor wins: each infusion re-imprints the infuser's xenotype
  (her own def-based monster xenotype, else base Mamono) while keeping
  accumulated severity.
- At 100% she awakens: the xenotype's genes are applied as endogenes (the
  Mamono gene feminizes her), the corruption hediff is removed, and she gains
  +12 "reborn as a monster" (10 days) plus +20 opinion of her maker.
  Non-colonist victims of a colonist corruptor roll to join (same
  25% + 5%/level-gap formula; a Protagonist corruptor always succeeds).

### Voluntary transformation

- Mamonos autonomously offer the change to women they want (desire from
  opinion, her mana, and family ties - mothers, daughters and sisters are
  weighted; 0.6 threshold, same cooldowns) - or right-click an **upright**
  woman with a Mamono selected → "Offer transformation".
- A visiting Mamono never offers the change to one of your colonists. Your own
  Mamono may still offer it to a visitor.
- Acceptance completes the transformation in one short ceremony - no mana
  cost, no gradual corruption. Refusals sting (−4, 2 days) and are logged;
  willing conversions get the +25% join bonus.

## Incubisation (male → incubus)

`ProjectMamono_Incubisation` is the male counterpart: a man regularly fed on
by a Mamono gradually becomes an incubus - an ideal mate for monsters. He
stays human; each stage simply grants more of the incubus' perks.

- Progress per feeding = essence moved × 0.0075, ×2 when the feeding Mamono
  is bonded to him, capped at 0.0075/day per man - a full incubus takes
  roughly 133 days bonded or 267 unbonded. Permanent by default (decay is
  opt-in; a completed incubus never regresses).
- Hidden from the Health tab until 75% severity ("near-incubus"); the
  mark's protection and the growing perks work while unseen.
- Stages: **mana-touched** → **marked** (0.25) → **suffused** (0.5: −10%
  rest fall, −15% hunger) → **near-incubus** (0.75: −15% rest, −20% hunger)
  → **incubus** (1.0: −25% rest fall, −40% hunger, +5% consciousness).
- Perks: essence regen ×1.25 (marked) / ×1.75 (near-incubus) / ×2 (full);
  +15% willpower from 75%; age-ailment immunity from 50%; ageless fertility
  at full. A full incubus also regains 0.5 food per essence transferred -
  he subsists on his mate's mana.
- **Marking:** the last Mamono to feed him leaves her mark (tooltip: "Marked
  by {name}"). At 25%+ the mark sets: other unbonded Mamonos refuse to feed
  from him. Bonded Mamonos and the marker herself are exempt; a dead marker's
  claim fades.
- Completion grants +12 "unshackled" (10 days) and a fond memory (+6, +20
  opinion) of the Mamono who marked him.

## Monster Extremists meme (Ideology)

"All men should be drained, and all women should be transformed!" The
**monster extremists** meme (`ProjectMamono_Meme_MonsterExtremists`, Misc
  group) carries one opinion precept in three varieties and two ritual precepts - all defs are
`MayRequire` Ideology, so without the DLC none of this loads.

- **Monster ascension:** the meme needs one of three varieties. **Relaxed** holds the ascended
  opinion at half strength and has no baseliner opinion at all. **Exalted** is described below.
  **Strict** doubles both opinions.
- **Monster ascension (exalted):** believers hold an "ascended" opinion
  (+10 default) of any Mamono and of any pawn with a living tsugai bond, and
  an "unascended baseliner" opinion (−15 default) of adult humanlikes who
  are neither transformed nor bonded. Children are ignored, and "drained"
  is flavour only - no essence/incubisation check. Offsets are tunable.
  Conflicts with FleshPurity and Transhumanist.
- **Rite of awakening (colonist):** a monster (the corruptor) escorts a
  willing, transformable colonist to the ritual focus and pours her mana
  in. On a positive outcome the convert becomes the corruptor's own monster
  xenotype - the same instant path as voluntary transformation, with the
  usual awakening memories. Room impressiveness and participant count set
  ritual quality; a poor rite simply fizzles.
- **Rite of forced awakening (captive):** the corruptor escorts a prisoner
  or slave to the ritual focus instead. On a positive outcome the captive
  becomes a RANDOM monster xenotype (any loaded def carrying the Mamono gene -
  submod xenotypes like the Slime Faction's join the pool automatically),
  and the shock halves (tunable) her will, resistance and certainty in her
  old ideoligion. She stays a prisoner - no join roll, no warm memory.

Both rites are anytime rituals (no cooldown), started from the ideo ritual
gizmo at a ritual spot, ideogram or altar. Transformation runs through the
mod's single choke point (`MamonoTransformation.ApplyXenotype`), so it is
idempotent and pregnancy-snapshot-safe like every other path.

## Low-mana mental breaks

When a starving Mamono breaks (`LowManaBreak` - random below 10% mana,
guaranteed at desperate starvation):

- **Reachable tsugai partner** → "feeding on essence": she seeks out her
  bonded mate and drains him to fill her bar (then lovin'), recovering once
  the feed is done.
- **No partner** → essence berserk: she hunts the nearest unbonded,
  bondable man across the whole map (bashing doors), tease-knocks him out
  and force-bonds him - feeding through the bonding drain.
- **No prey anywhere** → a colonist Mamono leaves the player faction and
  walks off the map; NPC Mamonos flee.

A captive Mamono never breaks at all. She cannot act on the hunger, and a
berserk prisoner would only maul her captors. Instead she feeds calmly on
another captive when her mana falls below the seek threshold - a fellow
prisoner, or a slave. If no other captive has essence to give, she simply
waits. That feed is not an intimate act: no lovin', but the mana, the mood
buff and the incubisation dose still happen.

Any other Mamono can do the same. When no mate can feed her - she is unbonded,
her mate is off the map, behind a wall, or drained dry - she falls back on one
of your prisoners or slaves. A captive is a meal the colony owns, not a
partner, so the rule that keeps a bonded Mamono faithful does not stop her, and
neither does the mark another Mamono left on him.

## Mod integrations

### ISEKAI RPG Leveling (required)

| Stat (per point above 5) | Effect                          | Clamp      |
|--------------------------|---------------------------------|------------|
| Avg(STR, VIT)            | +3% essence regen               | 0.5x – 3x  |
| Avg(WIS, INT)            | −3% mana drain                  | 0.3x–1.2x  |
| VIT                      | +2% willpower                   | 0.5x – 3x  |
| CHA                      | +5% tease damage dealt          | 0.5x – 3x  |
| WIS                      | −3% tease damage taken          | 0.3x–1.2x  |
| Beauty (−2..+2)          | +25% tease damage dealt         | 0.3x – 3x  |

XP awards: 25 per willpower knockout, 40 per full tease severity dealt, 50
per full essence point consumed. Level gaps shift bond/corruption join
chances ±5% per level. The `Isekai_Protagonist` trait auto-wins join rolls,
can be harem-bonded while attached, and gets free-love treatment: no affair
or cheater thoughts, lovers are never broken up, spouses never demoted to
ex-spouse, and romance success is floored.

### Vanilla Psycasts Expanded (optional)

Monster xenotypes opt in per-def via the `MamonoPsycastExtension`
(psycast paths, 1–2 initial abilities, 0–2 stat upgrade points, spawn
chance). Generated pawns of those xenotypes then spawn with a psylink and
random path psycasts, mirroring VPE's own caster pawnkinds. Core's base
Mamono never opts in - which Mamono races become psycasters is decided
per-xenotype by submods.

### yayo's Animation (optional)

Romancin'-style bounce animation on both partners during tsugai bonding,
essence give/drain jobs, and Intimacy sex acts.

### Intimacy – Friends n' Lovers (optional)

After each completed Intimacy sex act involving exactly one Mamono, she
drains exactly enough essence from her partner to fill her mana bar.

## The flight gene and wings

`PMM_Gene_Flight` is what gives dragons, wyverns and malef dragons their wings.

- It swaps the pawn's body for the Big & Small winged body, so she grows real wing parts
  and renders feathered wings. The wings are as tough as any other part: every part's
  health is her `hitPoints x HealthScale`, and no hediff can change that.
- It grants `PMM_Ability_FlightLeap`: a long leap, range 29.9, no line of sight needed,
  60-tick cooldown.
- It also raises caravan riding speed by 2.5x. `CaravanMountSpeedPatch` counts each flyer
  twice, so a caravan of flyers carries extra riders.
- On a map she ignores rough ground: mud, sand, snow and slush never slow her, and
  she crosses water at her own pace. `FlyingPawnTerrainCostPatch` prices her cell as her own
  ticks-per-move - the same approach VEF's floating creatures and the VRE Insector wings gene
  use. Walls, doors and buildings still stop her, because impassable things keep their cost.

`PMM_Gene_FlightWeak` is the weak-winged version of the same gene, carried by the vamp
mosquito and the soldier beetle. They get the same `PMM_Ability_FlightLeap`, but their
`CaravanRidingSpeedFactor` stays at its base 1.0, so `CaravanMountSpeedPatch` ignores
them: a weak flyer carries no rider and adds no mount of her own. She still ignores rough
ground on a map, though, just like a strong flyer.

The weak gene also draws its own wings, and smaller ones: six nodes at `drawSize 0.8`
where B&S uses 1.23, scaled by editing `renderNodeProperties` on the gene. It has no
Flagger on purpose, so the winged body or race tracker blanks its full-size wings and
these are the only ones drawn. That is what makes one gene fit both the tiny vamp
mosquito and the heavy soldier beetle.

### The swap only works on Human-race pawns

B&S runs a gene's `thingDefSwap` with `force: false` and `targetPriority: 0`, and it
enters the pawn's own race def at priority 200. When the two bodies cannot be fused the
higher-priority entry wins, so a pawn whose race def is not literally `Human` keeps her
body and the swap is refused. That is why dragons, wyverns, malef dragons and lamias -
all xenotype-only species on vanilla Human pawns - get their wings and tails, while a
species with its own race def does not.

Species with their own race def carry the wings themselves: the three flying insect
mamonos put `BS_HumanoidWithWings_Body` in `<race>` and `BS_HumanoidWithWings_Race` in the
race's `raceHediffList`. The gene still supplies `PMM_Ability_FlightLeap` and the
`ShowBaseWingsRight/Left` flags that keep the winged tracker from blanking its own art.

## The large frame gene

`PMM_Gene_LargeFrame` is for the mamonos who are already large. It changes no body and no
size stat: a large mamono already carries more, because her bigger body size raises both
what she can hold in her hands and how much cargo her caravan can take.

What the gene adds is pace, in two ways.

- **A caravan with a live carrier in it moves 20% faster.** Vanilla averages this bonus
  over the pawns that have it, so it is a flat 20% and not 20% per carrier. A downed
  carrier gives nothing.
- **She is ridden like a mount.** Vanilla can only count an animal as a mount, so
  `CaravanMountSpeedPatch` adds her riding speed to the caravan's mount list itself. Her
  1.3 factor counts once, as the passenger on her back.

One thing to know when you look at a caravan: the game's own "Ridable animals / people"
line counts animals only, so it still reads `0 / 2` for a caravan of mamonos. The
"Multiplier from mounted mamonos" line our patch adds below it is the one that reports her.

Her Stats tab gains a "caravan speed factor: 120%" line, which is how you can see the
first half working.

Flight and the large frame cannot sit in the same woman: both carry the
`PMM_CaravanCarrier` exclusion tag, so the gene editor will not offer both, and if both
ever end up on one pawn the flight gene wins and the large frame gene goes inactive.

## Bloodline genes

Three genes sit beside the Mamono gene. All of them are tunable on the Genes settings tab.

- **Mamono venom** (`ProjectMamono_MamonoVenom`) - melee strikes inject a non-lethal slowing
  venom. It wears off after 4 hours.
- **Fiery mamono** (`ProjectMamono_MamonoFiery`) - immunity to fire, heat and lava. Her tsugai
  partner gains a weaker ward: +40 °C comfort ceiling, 15% less flammable, 15% less flame
  damage.
- **Mamono claws** (`ProjectMamono_MamonoClaws`) - +15% tease damage, −15% manipulation.

## Autonomous mana feeding

A Mamono whose mana drops below 30% walks to a meal, with no low-mana mental break. Her
mate comes first; if she has none to reach, or he is dry, she falls back on one of your
prisoners or slaves. Retries are rate-limited, the daily amount is capped, the pace
follows the lovin' MTB, and the captive fallback can be turned off in settings.

## Factions and world creation

- The Empire and Traders Guild xenotype pools now include monster girls, and both factions
  spawn normally.
- `Patches/DisableVisibleFactions.xml` zeroes the starting counts of the vanilla selectable
  factions, so a new world is not flooded with them.
- `Settings/Mod_Sensible Factions_FactionFilter.xml` is an Auto Mod Config preset that
  weights the Mamono family factions for the Sensible Factions mod.

## Settings

Everything above is tunable under Options → Mod Settings → Project Mamono,
organized into tabs: **ISEKAI** (stat scaling, beauty, tease capacity
boost) · **Experience** · **Mana** (food conversion, starvation pacing,
wild drain, break threshold/MTB) · **Corruption** (forced + voluntary
transformation, join chances, autonomous infusion) · **Incubisation**
(accrual, cap, mark, perks, decay) · **Tsugai bond** (voluntary bonding,
costs, cooldowns, join chances) · **Bonded** (opinion/romance floors,
willpower bonus, stack cap, grief) · **Animation** · **Intimacy** ·
**Debug**. Only **Debug** is hidden, and the **Intimacy** tab appears only while the
Intimacy mod is loaded. A "Restore defaults" button
resets all values. The Monster Extremists meme and the three bloodline genes have no tabs:
their values are fixed, and the meme, its precepts and rites, the venom, the fiery ward and
the claws all work exactly as before.

Dev-mode debug actions (Project Mamono category) cover incubisation: add
progress, mark by a Mamono, complete, clear, log status, test mark
protection, reset daily cap.

## Rebuilding the assembly

Run `./build.sh` from anywhere - it compiles `Source/ProjectMamono/*.cs` with
Roslyn `csc` against the RimWorld and workshop-mod references into
`Assemblies/ProjectMamono.dll` (also wired into the pre-push hook chain).

Build output stays in this workspace. Deployment into the game's `Mods`
folder is a separate manual step - never overwrite the DLL while RimWorld
is running, or the loaded assembly's metadata gets corrupted.