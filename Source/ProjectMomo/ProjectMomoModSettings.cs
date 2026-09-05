using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectMomo
{
    /// <summary>
    /// The mod entry point that owns the settings and draws the options UI
    /// (Options &gt; Mod Settings &gt; Project Momo). Named ProjectMomoModSettings
    /// to avoid clashing with the static Harmony-init class ProjectMomoMod.
    /// Settings are grouped into one tab per category.
    /// </summary>
    public class ProjectMomoModSettings : Mod
    {
        private enum SettingsTab
        {
            Isekai,
            Experience,
            ManaStarvation,
            Corruption,
            Incubisation,
            TsugaiBond,
            BondedCouple,
            Animation,
            Intimacy,
            Ideology,
            Genes,
            Debug
        }

        public static ProjectMomoSettings Settings { get; private set; }

        private SettingsTab currentTab = SettingsTab.Isekai;
        private Vector2 scrollPosition = Vector2.zero;
        private readonly Dictionary<string, string> textBuffers = new Dictionary<string, string>();
        // Measured content height per tab, fed back after each draw so the scroll view fits exactly.
        private readonly Dictionary<SettingsTab, float> tabContentHeights = new Dictionary<SettingsTab, float>();

        public ProjectMomoModSettings(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ProjectMomoSettings>();
        }

        public override string SettingsCategory()
        {
            return "Project Momo";
        }

        /// <summary>
        /// Checks the hidden flag file inside the mod's About folder. This lets the
        /// debug tab be enabled without touching persisted mod settings.
        /// </summary>
        private bool HiddenTabFileEnabled()
        {
            if (Content?.RootDir == null)
            {
                return false;
            }
            try
            {
                string folder = System.IO.Path.Combine(Content.RootDir, "About");
                string path = System.IO.Path.Combine(folder, "PackageId.txt");
                if (!System.IO.File.Exists(path))
                {
                    return false;
                }
                string text = System.IO.File.ReadAllText(path).Trim();
                return text.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool DebugTabEnabled
        {
            get { return Settings.DebugTabEnabled; }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // TabDrawer.DrawTabs draws the tab row in the 32px band directly above the rect
            // it is given, so reserve that space at the top of the window.
            // Reserve a strip at the bottom for the restore-defaults button.
            const float buttonHeight = 38f;
            const float buttonMargin = 8f;
            Rect tabContentRect = new Rect(inRect.x, inRect.y + TabDrawer.TabHeight, inRect.width, inRect.height - TabDrawer.TabHeight - buttonHeight - buttonMargin);
            Rect buttonRect = new Rect(inRect.x, tabContentRect.yMax + buttonMargin, inRect.width, buttonHeight);

            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("ISEKAI", () => SwitchTab(SettingsTab.Isekai), currentTab == SettingsTab.Isekai),
                new TabRecord("Experience", () => SwitchTab(SettingsTab.Experience), currentTab == SettingsTab.Experience),
                new TabRecord("Mana", () => SwitchTab(SettingsTab.ManaStarvation), currentTab == SettingsTab.ManaStarvation),
                new TabRecord("Corruption", () => SwitchTab(SettingsTab.Corruption), currentTab == SettingsTab.Corruption),
                new TabRecord("Incubisation", () => SwitchTab(SettingsTab.Incubisation), currentTab == SettingsTab.Incubisation),
                new TabRecord("Tsugai bond", () => SwitchTab(SettingsTab.TsugaiBond), currentTab == SettingsTab.TsugaiBond),
                new TabRecord("Bonded", () => SwitchTab(SettingsTab.BondedCouple), currentTab == SettingsTab.BondedCouple),
                new TabRecord("Animation", () => SwitchTab(SettingsTab.Animation), currentTab == SettingsTab.Animation),
                new TabRecord("Intimacy", () => SwitchTab(SettingsTab.Intimacy), currentTab == SettingsTab.Intimacy),
                new TabRecord("Ideology", () => SwitchTab(SettingsTab.Ideology), currentTab == SettingsTab.Ideology),
                new TabRecord("Genes", () => SwitchTab(SettingsTab.Genes), currentTab == SettingsTab.Genes)
            };

            // Hidden debug tab, enabled through HiddenTab.txt or the DebugTabEnabled setting.
            if (HiddenTabFileEnabled() || DebugTabEnabled)
            {
                tabs.Add(new TabRecord("Debug", () => SwitchTab(SettingsTab.Debug), currentTab == SettingsTab.Debug));
            }

            Widgets.DrawMenuSection(tabContentRect);
            TabDrawer.DrawTabs(tabContentRect, tabs);

            Rect contentRect = tabContentRect.ContractedBy(8f);
            float contentHeight = tabContentHeights.TryGetValue(currentTab, out float height) ? height : 400f;
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, contentHeight);

            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            // Without this, Listing silently breaks into a new (off-screen, clipped) column
            // whenever content exceeds the listing rect height.
            listing.maxOneColumn = true;
            listing.Begin(new Rect(0f, 0f, viewRect.width, contentHeight));

            switch (currentTab)
            {
                case SettingsTab.Isekai:
                    DrawIsekaiTab(listing);
                    break;
                case SettingsTab.Experience:
                    DrawExperienceTab(listing);
                    break;
                case SettingsTab.ManaStarvation:
                    DrawManaStarvationTab(listing);
                    break;
                case SettingsTab.Corruption:
                    DrawCorruptionTab(listing);
                    break;
                case SettingsTab.Incubisation:
                    DrawIncubisationTab(listing);
                    break;
                case SettingsTab.TsugaiBond:
                    DrawTsugaiBondTab(listing);
                    break;
                case SettingsTab.BondedCouple:
                    DrawBondedCoupleTab(listing);
                    break;
                case SettingsTab.Animation:
                    DrawAnimationTab(listing);
                    break;
                case SettingsTab.Intimacy:
                    DrawIntimacyTab(listing);
                    break;
                case SettingsTab.Ideology:
                    DrawIdeologyTab(listing);
                    break;
                case SettingsTab.Genes:
                    DrawGenesTab(listing);
                    break;
                case SettingsTab.Debug:
                    DrawDebugTab(listing);
                    break;
            }

            listing.End();
            Widgets.EndScrollView();

            // Feed the real content height back so the scrollbar matches this tab's content.
            tabContentHeights[currentTab] = listing.CurHeight;

            // Restore defaults button with a confirmation prompt to avoid accidental resets.
            if (Widgets.ButtonText(buttonRect, "Restore defaults"))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Restore all Project Momo settings to their default values?",
                    "Yes",
                    () =>
                    {
                        Settings.ResetToDefaults();
                        textBuffers.Clear();
                    },
                    "No",
                    null,
                    null,
                    true));
            }
        }

        private void SwitchTab(SettingsTab tab)
        {
            currentTab = tab;
            scrollPosition = Vector2.zero;
        }

        private void DrawIsekaiTab(Listing_Standard listing)
        {
            listing.Label("<b>ISEKAI compatibility</b>");
            listing.Label("<color=#888888>How ISEKAI RPG stats (VIT / CHA / WIS) feed into Willpower and tease damage, and the XP awarded.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "VIT to Willpower", ref Settings.VitWillpowerPerPoint, 0f, 0.10f, 0.02f,
                "Willpower multiplier per VIT point above 5.");
            DrawPerPointSlider(listing, "CHA to tease dealt", ref Settings.ChaTeasePerPoint, 0f, 0.20f, 0.05f,
                "Tease damage the attacker deals per CHA point above 5.");
            DrawPerPointSlider(listing, "WIS tease resist", ref Settings.WisTeaseResistPerPoint, 0f, 0.20f, 0.03f,
                "Tease damage the victim resists per WIS point above 5.");
            DrawPerPointSlider(listing, "STR+VIT essence regen", ref Settings.StrVitEssenceRegenPerPoint, 0f, 0.20f, 0.03f,
                "Essence recharge rate per point of avg(STR, VIT) above 5.");
            DrawPerPointSlider(listing, "WIS+INT mana conservation", ref Settings.WisIntManaDrainPerPoint, 0f, 0.20f, 0.03f,
                "Mana drain reduction per point of avg(WIS, INT) above 5.");
            DrawPerPointSlider(listing, "Tease capacity boost", ref Settings.TeaseCapacityBoostPerSeverity, 0f, 2f, 0.5f,
                "Blood pumping & breathing boost per point of tease severity (0.5 = +50% at full tease).", true);
            DrawPerPointSlider(listing, "Beauty tease bonus", ref Settings.BeautyTeasePerPoint, 0f, 1f, 0.25f,
                "Tease damage bonus per point of the attacker's beauty (vanilla beauty is -2 to +2).", true);
        }

        private void DrawExperienceTab(Listing_Standard listing)
        {
            listing.Label("<b>Experience</b>");
            listing.GapLine();

            DrawIntField(listing, "Knockout XP", ref Settings.KnockoutXP, 0, 1000,
                "Lump XP awarded to the Momo-carrier when a victim's will breaks.");
            DrawPerPointSlider(listing, "XP per tease severity", ref Settings.XPPerTeaseSeverity, 0f, 200f, 40f,
                "XP per full point of tease severity dealt (scaled by actual damage applied).", true);
            DrawPerPointSlider(listing, "XP per essence", ref Settings.XPPerEssence, 0f, 200f, 50f,
                "XP per full point of essence a Momo consumes via Give/Drain essence (scaled by actual amount).", true);
        }

        private void DrawManaStarvationTab(Listing_Standard listing)
        {
            listing.Label("<b>Mana from food</b>");
            listing.Label("<color=#888888>When a Momo eats, part of her Mana is restored, scaled by the food's nutrition. Ordinary food is only a supplement — essence feeding remains the real source of mana.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Mana from food (per nutrition)", ref Settings.ManaFromFoodPerNutrition, 0f, 0.5f, 0.15f,
                "Mana restored per point of food nutrition eaten. A 0.9-nutrition meal at 15% restores ~13% of the mana bar. 0 = food gives no mana.");
            listing.GapLine();

            listing.Label("<b>Mana starvation</b>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Days to full starvation", ref Settings.StarvationDaysToMax, 0.25f, 10f, 1.5f,
                "Days of empty Mana for the starvation decline to fully escalate.", true);
            listing.Label("<color=#888888>Once a Momo is desperate (starvation at 60% severity), the feeding/berserk break is guaranteed and triggers immediately — once per starvation episode.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Wild Momo mana drain", ref Settings.WildManaDrainFactor, 0.05f, 1f, 0.25f,
                "Mana drain multiplier for wild (untamed) Momo. Lower = wild Momo drain mana more slowly than tamed/colonist Momo. 1 = same as tamed.");
            DrawPerPointSlider(listing, "Visiting Momo mana drain", ref Settings.GuestManaDrainFactor, 0.05f, 1f, 0.25f,
                "Mana drain multiplier for visiting Momo (guests of a non-hostile faction). Lower = visitors drain mana more slowly, so a guest won't starve toward a berserk break during an ordinary visit. 1 = same as colonists.");

            listing.Label("<b>Low-mana break</b>");
            listing.Label("<color=#888888>While Mana is below the threshold, a Momo can randomly suffer a feeding/berserk break.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Low mana break threshold", ref Settings.LowManaBreakThreshold, 0f, 0.5f, 0.1f,
                "Mana level below which a Momo can randomly suffer a feeding/berserk break.");
            DrawPerPointSlider(listing, "Break MTB (days)", ref Settings.LowManaBreakMtbDays, 0.1f, 5f, 0.5f,
                "Mean time between random feeding/berserk breaks while Mana is below the threshold (lower = more frequent).", true);

            listing.GapLine();
            listing.Label("<b>Autonomous feeding</b>");
            listing.Label("<color=#888888>A bonded Momo whose Mana runs low seeks out her mate and feeds on her own — like pawns seeking food, no mental break required. If she cannot reach him or he is dry, the breaks above remain the fallback.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Bonded Momos feed autonomously", ref Settings.AutonomousFeedingEnabled,
                "Let a bonded Momo autonomously walk to her tsugai partner and drain essence when her Mana runs low.");

            DrawPerPointSlider(listing, "Seek mate threshold", ref Settings.AutonomousFeedThreshold, 0f, 0.6f, 0.3f,
                "Mana level below which a bonded Momo seeks out her mate to feed. Keep above the low mana break threshold so seeking preempts breaking.");
            DrawPerPointSlider(listing, "Feed retry cooldown (hours)", ref Settings.AutonomousFeedRetryCooldownHours, 0.25f, 12f, 1f,
                "Hours a Momo waits before retrying after a feed attempt failed (mate dry, or the walk/drain interrupted).", true);
            DrawIntField(listing, "Max natural feeds per day", ref Settings.AutonomousFeedMaxPerDay, 0, 10,
                "Completed autonomous feeds a Momo will seek per day (0 = no limit). Past the cap, low Mana falls back to the feeding/berserk break. Break-driven feeds never count toward the cap.");

            listing.CheckboxLabeled("Feeding frequency follows lovin' MTB", ref Settings.AutonomousFeedLovinDriven,
                "Tie autonomous feeding to the pair's lovin' MTB: the shorter their mean time between lovin' (the higher her drive), the more eagerly she seeks her mate — she starts looking at a higher Mana level, re-checks and retries sooner, and may feed more times per day. A low-drive Momo seeks less than the configured rates.");
            DrawPerPointSlider(listing, "Max lovin' drive effect", ref Settings.AutonomousFeedLovinDriveMaxEffect, 1f, 5f, 3f,
                "Cap on how far lovin' drive may speed up (or slow down) autonomous feeding. 3 = up to three times the configured frequency (or a third of it) at the extremes.", true);
        }

        private void DrawCorruptionTab(Listing_Standard listing)
        {
            listing.Label("<b>Mamono corruption</b>");
            listing.Label("<color=#888888>A Momo can pour mana into any downed woman, slowly corrupting her until she transforms into the corruptor's own xenotype. While she is on her feet, she fights the corruption off and it fades. The last Momo to infuse her decides what she becomes.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Enable corruption", ref Settings.CorruptionEnabled,
                "Master switch for the mana-corruption transformation system (female human to monster).");

            DrawPerPointSlider(listing, "Corruption per infusion", ref Settings.CorruptionSeverityPerInfusion, 0.05f, 1f, 0.25f,
                "Corruption progress from one completed infusion (25% = four infusions to transform).");
            DrawPerPointSlider(listing, "Decay per day", ref Settings.CorruptionDecayPerDay, 0f, 1f, 0.2f,
                "Corruption lost per day while the victim's willpower is above the knockout threshold.");
            DrawPerPointSlider(listing, "Mana cost per infusion", ref Settings.CorruptionManaCost, 0.05f, 1f, 0.2f,
                "Mana one infusion costs the Momo (fraction of her mana bar).");
            DrawPerPointSlider(listing, "Minimum age", ref Settings.CorruptionMinAge, 0f, 20f, 16f,
                "Minimum biological age a woman must be to be corrupted.", true);

            listing.GapLine();
            listing.Label("<b>Join offer</b>");
            listing.Label("<color=#888888>When one of your colonists corrupts a non-colonist woman, the chance the new monster chooses to join the colony, scaled by the Isekai level gap between corruptor and victim. A Protagonist corruptor always wins her over.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Base join chance", ref Settings.CorruptionJoinBaseChance, 0f, 1f, 0.25f,
                "Chance to join when corruptor and victim are the same level.");
            DrawPerPointSlider(listing, "Join chance per level gap", ref Settings.CorruptionJoinChancePerLevel, 0f, 0.25f, 0.05f,
                "Chance added per level the corruptor outlevels the victim (subtracted when she outlevels the corruptor).");
            DrawPerPointSlider(listing, "Max join chance", ref Settings.CorruptionJoinMaxChance, 0f, 1f, 0.9f,
                "Upper cap on the join chance.");

            listing.GapLine();
            listing.Label("<b>Voluntary transformation</b>");
            listing.Label("<color=#888888>A Momo can offer the mana to a woman who wants it. Acceptance is rolled from the woman's desire; a willing body does not resist, so the change completes in one short ceremony — no gradual corruption, no mana cost. A refused Momo respects the refusal for a while.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Enable voluntary transformation", ref Settings.VoluntaryCorruptionEnabled,
                "Master switch for consensual transformation offers. The forced (infuse a downed woman) path is unaffected.");
            listing.CheckboxLabeled("Momos offer autonomously", ref Settings.VoluntaryCorruptionMomoProposals,
                "Let Momos autonomously offer a transformation to women they want.");

            DrawPerPointSlider(listing, "Desire threshold", ref Settings.VoluntaryCorruptionDesireThreshold, 0.1f, 1f, 0.6f,
                "Minimum desire for a Momo to offer a transformation (higher = only when truly eager).", true);
            DrawPerPointSlider(listing, "Voluntary join bonus", ref Settings.VoluntaryCorruptionJoinBonus, 0f, 0.5f, 0.25f,
                "Flat join-chance bonus when a woman willingly accepts the change.");
            DrawPerPointSlider(listing, "Attempt cooldown (hours)", ref Settings.VoluntaryCorruptionAttemptCooldownHours, 1f, 48f, 6f,
                "Hours a Momo must wait between transformation offers.", true);
            DrawPerPointSlider(listing, "Rejection cooldown (hours)", ref Settings.VoluntaryCorruptionRejectionCooldownHours, 1f, 72f, 24f,
                "Hours a refused Momo must wait before offering to the same woman again.", true);

            listing.GapLine();
            listing.Label("<b>Autonomous corruption</b>");
            listing.Label("<color=#888888>The forced path, driven by the think tree: a Momo with mana to spare who finds a helpless woman will pour mana into her on her own.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Momos infuse downed women", ref Settings.AutonomousCorruptionEnabled,
                "Let Momos autonomously infuse downed, corruptible women (the forced path), without a right-click order.");
        }

        private void DrawIncubisationTab(Listing_Standard listing)
        {
            listing.Label("<b>Incubisation</b>");
            listing.Label("<color=#888888>A human man who keeps giving his essence to a Momo — bonded or not — absorbs her mana and slowly changes into an incubus: richer essence, inhuman stamina, freedom from old age, and a lifespan matched to his mate. The change is gradual and permanent. Her mana also marks him: once the mark sets, other unbonded Momos will not feed from him.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Enable incubisation", ref Settings.IncubisationEnabled,
                "Master switch for the male human-to-incubus gradual transformation system.");

            DrawPerPointSlider(listing, "Progress per essence", ref Settings.IncubisationPerEssenceFactor, 0.001f, 0.15f, 0.0075f,
                "Incubisation progress per full point of essence transferred (0.75% = a full-bar feeding moves him 0.75% of the way).");
            DrawPerPointSlider(listing, "Bonded dose multiplier", ref Settings.IncubisationBondedMultiplier, 1f, 5f, 2f,
                "Progress multiplier when the feeding Momo is bonded (tsugai) to the man — wives incubise their husbands fastest.", true);
            DrawPerPointSlider(listing, "Daily progress cap", ref Settings.IncubisationDailyCap, 0.001f, 0.15f, 0.0075f,
                "Max incubisation progress one man can gain per day (0.75% = a devoted husband completes the change in about two seasons).");
            DrawPerPointSlider(listing, "Mark threshold", ref Settings.IncubisationMarkThreshold, 0.1f, 0.75f, 0.25f,
                "Severity at which the marker Momo's claim sets: other unbonded Momos will no longer feed from him.");
            DrawPerPointSlider(listing, "Essence regen at full incubus", ref Settings.IncubisationEssenceRegenFull, 1f, 4f, 2f,
                "Essence regeneration multiplier for a full incubus (marked men get 1.25x, near-incubi halfway to this).", true);
            DrawPerPointSlider(listing, "Willpower bonus near-complete", ref Settings.IncubisationWillpowerBonus, 0f, 0.5f, 0.15f,
                "Willpower bonus once incubisation passes the near-incubus stage (75% severity).");
            DrawPerPointSlider(listing, "Decay per day", ref Settings.IncubisationDecayPerDay, 0f, 0.2f, 0f,
                "Incubisation progress lost per day (0% = permanent, lore-accurate; a completed incubus never regresses).");

            listing.GapLine();
            listing.Label("<b>Full incubus</b>");
            listing.Label("<color=#888888>A completed incubus subsists on his mate's mana in place of ordinary food — the act of feeding her (or being drained) nourishes him.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Food per essence", ref Settings.IncubusFoodPerEssence, 0f, 2f, 0.5f,
                "Food a full incubus regains per full point of essence transferred (50% = a full-bar feeding restores half his food bar). 0 = disabled.");
        }

        private void DrawTsugaiBondTab(Listing_Standard listing)
        {
            listing.Label("<b>Tsugai bond</b>");
            listing.Label("<color=#888888>When a Momo bonds to one of your colonists, the chance she follows her husband and joins the colony, scaled by their Isekai level gap.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Base join chance", ref Settings.BondJoinBaseChance, 0f, 1f, 0.25f,
                "Chance to join when husband and Momo are the same level.");
            DrawPerPointSlider(listing, "Join chance per level gap", ref Settings.BondJoinChancePerLevel, 0f, 0.25f, 0.05f,
                "Chance added per level the husband outlevels the Momo (subtracted when she outlevels him).");
            DrawPerPointSlider(listing, "Max join chance", ref Settings.BondJoinMaxChance, 0f, 1f, 0.9f,
                "Upper cap on the join chance.");

            listing.GapLine();
            listing.Label("<b>Voluntary bonding</b>");
            listing.Label("<color=#888888>Momos and men who want each other can form the bond by consent — autonomously, or via a right-click \"Propose tsugai bond\" order. A willing bond costs the man a fixed share of essence instead of draining him dry: essence is the bonding budget, so one man can only collect as many Momos as his regeneration supports.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Enable voluntary bonding", ref Settings.VoluntaryBondingEnabled,
                "Master switch for consensual tsugai bonds. The forced (combat knockout) path is unaffected.");
            listing.CheckboxLabeled("Momos propose", ref Settings.VoluntaryBondMomoProposals,
                "Let Momos autonomously propose a bond to men they want.");
            listing.CheckboxLabeled("Men propose", ref Settings.VoluntaryBondManProposals,
                "Let men autonomously propose a bond to Momos they want.");

            DrawPerPointSlider(listing, "Voluntary bond essence cost", ref Settings.VoluntaryBondEssenceCost, 0.1f, 1f, 0.5f,
                "Essence a voluntary bond costs the man, and the minimum he must have to offer one (1.0 = full bar).", true);
            DrawPerPointSlider(listing, "Desire threshold", ref Settings.VoluntaryBondDesireThreshold, 0.1f, 1f, 0.6f,
                "Minimum desire for a pawn to act on a voluntary bond (higher = proposals only when truly smitten).", true);
            DrawPerPointSlider(listing, "Voluntary join bonus", ref Settings.VoluntaryBondJoinBonus, 0f, 0.5f, 0.25f,
                "Flat join-chance bonus when a Momo willingly bonds with one of your colonists.");
            DrawPerPointSlider(listing, "Attempt cooldown (hours)", ref Settings.VoluntaryBondAttemptCooldownHours, 1f, 48f, 6f,
                "Hours a pawn must wait between proposal attempts.", true);
            DrawPerPointSlider(listing, "Rejection cooldown (hours)", ref Settings.VoluntaryBondRejectionCooldownHours, 1f, 72f, 24f,
                "Hours a rejected pair must wait before either may propose to the other again.", true);
        }

        private void DrawBondedCoupleTab(Listing_Standard listing)
        {
            listing.Label("<b>Bonded couple</b>");
            listing.Label("<color=#888888>Bonded (tsugai) pawns are perfectly matched — these set the floor for their compatibility and romance/lovin' chance factors.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Bonded compatibility", ref Settings.BondCompatibility, 0f, 1f, 1.0f,
                "Compatibility floor between bonded pawns (1.0 = maximum).", true);
            DrawPerPointSlider(listing, "Bonded romance factor", ref Settings.BondRomanceFactor, 0f, 1f, 1.0f,
                "Romance/lovin' chance-factor floor between bonded pawns (1.0 = maximum).", true);
            DrawIntField(listing, "Bonded opinion", ref Settings.BondOpinion, -100, 100,
                "Opinion floor between bonded pawns (100 = vanilla maximum).");
            DrawPerPointSlider(listing, "Bonded willpower bonus", ref Settings.BondWillpowerBonus, 0f, 1f, 0.25f,
                "Willpower bonus per tsugai bond, stacking with each bond (0.25 = +25% willpower per bond).", true);

            DrawIntField(listing, "Max bond stacks", ref Settings.BondMaxStacks, 1, 10,
                "Hard cap on how many tsugai bonds one pawn can stack (the hediff's max severity is 10).");
            DrawPerPointSlider(listing, "Bond loss willpower penalty", ref Settings.BondLossWillpowerPenalty, 0f, 1f, 0.5f,
                "Willpower penalty per broken bond being grieved (0.5 = -50% willpower each).", true);
            DrawPerPointSlider(listing, "Bond loss penalty cap", ref Settings.BondLossWillpowerPenaltyCap, 0f, 1f, 0.75f,
                "Floor on the total bond-loss willpower penalty. Stacked griefs can never reduce willpower past this fraction (0.75 = always keeps at least 25%).", true);
            // Remember where min started so the autocorrect below can tell which
            // end of the range the user actually moved this frame.
            float prevMinDays = Settings.BondLossMinDays;

            DrawPerPointSlider(listing, "Bond grief min days", ref Settings.BondLossMinDays, 0.5f, 30f, 5f,
                "Shortest a bond-grief debuff lasts before it fades.", true);
            DrawPerPointSlider(listing, "Bond grief max days", ref Settings.BondLossMaxDays, 0.5f, 30f, 10f,
                "Longest a bond-grief debuff lasts before it fades.", true);

            // Autocorrect an inverted range: min must never exceed max. The end the
            // user just moved wins and drags the other along; an already-inverted
            // (e.g. hand-edited) range clamps min down to max. Clearing the adjusted
            // field's text buffer keeps its text box from restoring the stale value.
            if (Settings.BondLossMinDays > Settings.BondLossMaxDays)
            {
                if (Settings.BondLossMinDays != prevMinDays)
                {
                    Settings.BondLossMaxDays = Settings.BondLossMinDays;
                    textBuffers.Remove("Bond grief max days");
                }
                else
                {
                    Settings.BondLossMinDays = Settings.BondLossMaxDays;
                    textBuffers.Remove("Bond grief min days");
                }
            }
        }

        private void DrawAnimationTab(Listing_Standard listing)
        {
            listing.Label("<b>Yayo's Animation</b>");
            listing.GapLine();

            listing.CheckboxLabeled("Bonding animation (requires Yayo's Animation)", ref Settings.YayoBondingAnimation,
                "Play Yayo's romancin' (lovin') animation on a Momo while she forms the tsugai bond. Also respects Yayo's own lovin' animation setting.");
        }

        private void DrawIntimacyTab(Listing_Standard listing)
        {
            listing.Label("<b>Intimacy - Friends n' Lovers</b>");
            listing.Label("<color=#888888>Only applies while the Intimacy mod is active. With it off, Momos gain no Mana from Intimacy sex acts.</color>");
            listing.GapLine();

            listing.CheckboxLabeled("Ageless fertility", ref Settings.MomoFertilityAgeless,
                "Momos never lose fertility to age (Biotech's fertility age curve is cancelled for Momo-carriers). Hediff-based fertility changes still apply.");

            listing.CheckboxLabeled("Always fertile (requires Biotech)", ref Settings.MomoAlwaysFertile,
                "Momos can always conceive: an adult Momo's fertility never drops below 100%, and sterility from sterilization, fertility-drained or removed reproductive organs, and sterile genes is ignored. An active pregnancy still prevents another conception, and children are unaffected. Subsumes ageless fertility.");

            listing.CheckboxLabeled("Feed through Intimacy sex acts", ref Settings.IntimacyFeeding,
                "When a Momo has sex through the Intimacy mod, she drains enough of her partner's essence to fill her Mana bar (limited by how much essence the partner has).");

            listing.CheckboxLabeled("Animate sex acts (requires Yayo's Animation)", ref Settings.IntimacyAnimation,
                "Play Yayo's romancin' (lovin') bounce on both partners while an Intimacy sex act runs. Intimacy ships no pawn animation of its own, and Yayo only recognizes the vanilla Lovin job — this bridges the two.");
        }

        private void DrawIdeologyTab(Listing_Standard listing)
        {
            listing.Label("<b>Monster Extremists meme (Ideology DLC)</b>");
            listing.Label("<color=#888888>The Monster Extremists meme (\"All men should be drained, and all women should be transformed!\") grants believers a social opinion of ascended pawns — transformed women (any Momo) and tsugai-bonded men — and, depending on the monster ascension precept, a low opinion of untransformed, unbonded baseliner adults. The precept comes in three varieties, exclusive to the meme: relaxed (admires the ascended, no judgment of baseliners), exalted (the values below) and strict (doubled values). The meme also unlocks two rites of awakening: one transforms a willing colonist into the organiser's own monster xenotype, the other transforms a prisoner or slave into a random monster. A failed rite simply fizzles.</color>");
            listing.GapLine();

            DrawIntField(listing, "Ascended opinion", ref Settings.MonsterExtremistAscendedOpinion, -100, 100,
                "Believers' social opinion of ascended pawns (any Momo, or any pawn with a living tsugai bond). The relaxed precept grants half this, the strict precept double.");
            DrawIntField(listing, "Baseliner opinion", ref Settings.MonsterExtremistBaselinerOpinion, -100, 100,
                "Believers' social opinion of baseliners: adult humanlikes who are neither transformed nor bonded. Children are ignored. Only the exalted and strict precepts judge baseliners — the strict precept doubles this, the relaxed precept has no baseliner opinion.");

            listing.GapLine();
            listing.Label("<b>Captive rite aftermath</b>");
            listing.Label("<color=#888888>A prisoner or slave transformed by the rite of awakening keeps these fractions of her will, resistance and certainty in her old beliefs (50% = halved). 100% = no effect.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Will remaining", ref Settings.MonsterExtremistCaptiveWillFactor, 0f, 1f, 0.5f,
                "Fraction of a transformed prisoner's will remaining after the captive rite (50% = halved).", true);
            DrawPerPointSlider(listing, "Resistance remaining", ref Settings.MonsterExtremistCaptiveResistanceFactor, 0f, 1f, 0.5f,
                "Fraction of a transformed prisoner's resistance remaining after the captive rite (50% = halved).", true);
            DrawPerPointSlider(listing, "Ideo certainty remaining", ref Settings.MonsterExtremistCaptiveCertaintyFactor, 0f, 1f, 0.5f,
                "Fraction of a transformed captive's certainty in her old ideoligion remaining after the rite (50% = halved).", true);
        }

        private void DrawGenesTab(Listing_Standard listing)
        {
            listing.Label("<b>Momo venom</b>");
            listing.Label("<color=#888888>A venom-gene carrier's melee strikes inject a slowing toxin into any living victim — humans, animals and other momos alike. The venom only suppresses the Moving capacity: it pins victims helpless but alive and aware, and a full dose wears off in 4 hours.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Venom per hit", ref Settings.VenomSeverityPerHit, 0.01f, 0.25f, 0.05f,
                "Venom build-up per melee hit (5% = 20 hits to fully pin a victim; 100% wears off in 4 hours).");

            listing.GapLine();
            listing.Label("<b>Fiery momo</b>");
            listing.Label("<color=#888888>A fiery momo is fully immune to fire, heat and lava: she cannot ignite, takes no flame damage, and no heat can discomfort her. A man bonded to her shares a lesser ward: +40C max comfortable temperature, 15% less flammable, and flame damage reduced to this fraction.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Warded flame damage taken", ref Settings.FieryWardFlameFactor, 0f, 1f, 0.85f,
                "Flame-damage multiplier for a pawn bonded to a fiery momo (85% = 15% less flame damage). The carrier herself is always fully immune.");

            listing.GapLine();
            listing.Label("<b>Momo claws</b>");
            listing.Label("<color=#888888>A clawed momo's strikes inflame her victims: more tease damage with every hit, paid for with clumsier hands.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Claws tease damage", ref Settings.ClawsTeaseMultiplier, 1f, 3f, 1.15f,
                "Tease-damage multiplier for a clawed momo (115% = a little more tease per strike).");
            DrawPerPointSlider(listing, "Claws manipulation penalty", ref Settings.ClawsManipulationPenalty, 0f, 0.5f, 0.15f,
                "Manipulation capacity penalty for a clawed momo (15% = clumsier hands).");
        }

        private void DrawDebugTab(Listing_Standard listing)
        {
            listing.Label("<b>Debug</b>");
            listing.Label("<color=#888888>This tab is hidden unless HiddenTab.txt in the mod folder is set to true.</color>");
            listing.GapLine();

            DrawPerPointSlider(listing, "Minimum bond age", ref Settings.BondMinAge, 0f, 20f, 16f,
                "Minimum biological age a male must be to be bonded to.", true);

            listing.CheckboxLabeled("Disable incest prevention", ref Settings.DisableIncestPrevention,
                "When enabled, blood relations will not reduce romance chance for pawns affected by Project Momo systems.");
        }

        private void DrawPerPointSlider(Listing_Standard listing, string label, ref float value, float min, float max, float defaultValue, string tooltip, bool absolute = false)
        {
            string unit = absolute ? "" : "%";
            float shown = absolute ? value : value * 100f;
            string defaultStr = $"<color=#888888>(default {(absolute ? defaultValue : defaultValue * 100f):0.##}{unit})</color>";
            TaggedString caption = $"{label}: {shown:0.##}{unit}  {defaultStr}";
            listing.Label(caption, -1f, tooltip);

            Rect fullRect = listing.GetRect(22f);
            float textBoxWidth = 60f;
            float gap = 8f;
            Rect sliderRect = new Rect(fullRect.x, fullRect.y, fullRect.width - textBoxWidth - gap, fullRect.height);
            Rect textRect = new Rect(sliderRect.xMax + gap, fullRect.y, textBoxWidth, fullRect.height);

            // Slider operates on the raw stored value.
            float rawValue = value;
            rawValue = Widgets.HorizontalSlider(sliderRect, rawValue, min, max);

            // Text box operates on the user-facing value (percentage unless absolute).
            string bufferKey = label;
            if (!textBuffers.TryGetValue(bufferKey, out string buffer) || buffer == null)
            {
                buffer = shown.ToString("0.##");
            }

            float shownValue = shown;
            float shownMin = absolute ? min : min * 100f;
            float shownMax = absolute ? max : max * 100f;
            Widgets.TextFieldNumeric(textRect, ref shownValue, ref buffer, shownMin, shownMax);

            // Apply whichever control changed this frame and keep the text buffer in sync.
            if (rawValue != value)
            {
                value = rawValue;
                buffer = (absolute ? value : value * 100f).ToString("0.##");
            }
            else if (shownValue != shown)
            {
                value = absolute ? shownValue : shownValue / 100f;
            }

            textBuffers[bufferKey] = buffer;

            listing.Label($"<color=#888888>{tooltip}</color>");
            listing.Gap(6f);
        }

        private void DrawIntField(Listing_Standard listing, string label, ref int value, int min, int max, string tooltip)
        {
            Rect fullRect = listing.GetRect(24f);
            float labelWidth = fullRect.width - 70f;
            Rect labelRect = new Rect(fullRect.x, fullRect.y, labelWidth, fullRect.height);
            Rect fieldRect = new Rect(fullRect.x + labelWidth, fullRect.y, 60f, fullRect.height);

            Widgets.Label(labelRect, label);
            TooltipHandler.TipRegion(labelRect, tooltip);

            string bufferKey = label;
            if (!textBuffers.TryGetValue(bufferKey, out string buffer) || buffer == null)
            {
                buffer = value.ToString();
            }
            string current = buffer;
            Widgets.TextFieldNumeric(fieldRect, ref value, ref current, min, max);
            textBuffers[bufferKey] = current;

            listing.Label($"<color=#888888>{tooltip}</color>");
            listing.Gap(6f);
        }
    }
}
