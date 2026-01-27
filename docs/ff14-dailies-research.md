# Final Fantasy XIV Daily & Weekly Activities Research Document

**Purpose:** Comprehensive research for a "Dailies Checklist" Dalamud plugin
**Research Date:** January 2026
**Status:** Technical Analyst Research Complete

---

## Table of Contents

1. [Reset Times Overview](#reset-times-overview)
2. [Duty Finder Roulettes](#duty-finder-roulettes)
3. [Gold Saucer Activities](#gold-saucer-activities)
4. [Beast Tribe / Allied Society Quests](#beast-tribe--allied-society-quests)
5. [Hunt Marks](#hunt-marks)
6. [Treasure Maps](#treasure-maps)
7. [Grand Company Activities](#grand-company-activities)
8. [Custom Deliveries](#custom-deliveries)
9. [Wondrous Tails](#wondrous-tails)
10. [Challenge Log](#challenge-log)
11. [Island Sanctuary](#island-sanctuary)
12. [Faux Hollows / Unreal Trials](#faux-hollows--unreal-trials)
13. [Masked Carnivale (Blue Mage)](#masked-carnivale-blue-mage)
14. [Doman Enclave Reconstruction](#doman-enclave-reconstruction)
15. [Tomestone Weekly Caps](#tomestone-weekly-caps)
16. [Leve Allowances](#leve-allowances)
17. [Retainer Ventures](#retainer-ventures)
18. [Raid Lockouts](#raid-lockouts)
19. [PVP Content](#pvp-content)
20. [Programmatic Detection Notes](#programmatic-detection-notes)

---

## Reset Times Overview

FFXIV has multiple reset times that affect different content:

| Reset Type | Time (UTC/GMT) | Time (PST) | Time (EST) | Affected Content |
|------------|----------------|------------|------------|------------------|
| **Daily Reset** | 15:00 UTC | 7:00 AM PST | 10:00 AM EST | Duty Roulettes, Beast Tribes, Daily Hunts, Mini Cactpot |
| **Grand Company Reset** | 20:00 UTC | 12:00 PM PST | 3:00 PM EST | Supply/Provisioning, Squadron Training |
| **Weekly Reset** | Tuesday 08:00 UTC | Tuesday 12:00 AM PST | Tuesday 3:00 AM EST | Raids, Challenge Log, Custom Deliveries, Wondrous Tails, Weekly Hunts, Tomestone Caps |
| **Jumbo Cactpot Drawing** | Saturday 08:00 UTC | Saturday 12:00 AM PST | Saturday 3:00 AM EST | Jumbo Cactpot results |
| **Fashion Report Judging** | Friday 08:00 UTC | Friday 12:00 AM PST | Friday 3:00 AM EST | Fashion Report begins |

**Note:** Reset times shift by 1 hour during Daylight Saving Time transitions.

---

## Duty Finder Roulettes

### Overview
Duty Roulettes provide bonus rewards (EXP, Gil, Tomestones, Grand Company Seals) once per day for queuing into random duties.

### All Roulette Types

| Roulette | Unlock Requirement | Level Req | Rewards | Priority |
|----------|-------------------|-----------|---------|----------|
| **Expert** | Complete all current max-level dungeons | 100 | Tomestones of Aesthetics & Heliometry, Gil, EXP | HIGH |
| **Level Cap Dungeons** | Unlock 2+ current expansion level-cap dungeons | 100 | Tomestones of Aesthetics & Heliometry | MEDIUM |
| **High-Level Dungeons** | Unlock 2+ level 50/60/70/80/90 dungeons | 50+ | Tomestones of Poetics, Aesthetics | MEDIUM |
| **Leveling** | Complete Sastasha & Tam-Tara Deepcroft | 16+ | Large EXP bonus, Gil | HIGH |
| **Main Scenario** | Complete all three ARR finale duties | 50 (iLvl 42) | Tomestones of Poetics (large amount) | MEDIUM |
| **Trials** | Unlock 2+ trials | Varies | Tomestones, EXP | MEDIUM |
| **Alliance Raids** | Unlock 1+ alliance raid | 50+ | High EXP, Tomestones | HIGH (for leveling) |
| **Normal Raids** | Unlock 2+ normal raids | 60+ | Tomestones, EXP | MEDIUM |
| **Guildhests** | Complete 2+ guildhests | 10+ | Small EXP bonus | LOW |
| **Frontline (PVP)** | Unlock PVP | 30+ | EXP, Wolf Marks, Tomestones | MEDIUM |
| **Mentor** | Battle Mentor certification | 100 | Mentor rewards | LOW (mentors only) |

### Access Method
- **Menu:** Duty > Duty Finder > Duty Roulette tab
- **Keyboard Shortcut:** U (default)

### Detection Potential
**HIGH** - Roulette completion status likely tracked via Dalamud APIs. The in-game Timers window shows roulette availability.

---

## Gold Saucer Activities

### Mini Cactpot (Daily)

| Attribute | Details |
|-----------|---------|
| **Activity** | Daily lottery scratch card |
| **Location** | Gold Saucer (X:5.1, Y:6.5) - Mini Cactpot Broker |
| **Reset** | Daily at 15:00 UTC |
| **Attempts** | 3 per day |
| **Cost** | 10 MGP per ticket |
| **Rewards** | 36 - 10,000 MGP per ticket |
| **Unlock** | "It Could Happen to You" quest |
| **Priority** | HIGH - Quick, guaranteed MGP |

### Jumbo Cactpot (Weekly)

| Attribute | Details |
|-----------|---------|
| **Activity** | Weekly lottery |
| **Location** | Gold Saucer (X:8, Y:5) - Jumbo Cactpot Broker |
| **Reset** | Saturday 08:00 UTC (drawing) |
| **Tickets** | 3 per week (100/150/200 MGP) |
| **Rewards** | Up to millions of MGP |
| **Unlock** | "Hitting the Cactpot" quest (Level 15) |
| **Priority** | HIGH - Takes seconds, high potential reward |

### Fashion Report (Weekly)

| Attribute | Details |
|-----------|---------|
| **Activity** | Glamour judging challenge |
| **Location** | Gold Saucer (X:7.2, Y:7.4) - Masked Rose |
| **Theme Release** | Tuesday 08:00 UTC |
| **Judging Begins** | Friday 08:00 UTC |
| **Attempts** | Up to 4 per week |
| **Rewards** | 10,000 MGP (participation) + 50,000 MGP (80+ score) |
| **Unlock** | "Passion for Fashion" quest |
| **Priority** | HIGH - Easy 60,000 MGP with guides |

### GATEs (Gates)

| Attribute | Details |
|-----------|---------|
| **Activity** | Timed mini-games |
| **Location** | Various Gold Saucer locations |
| **Frequency** | Every 20 minutes (Eorzean time) |
| **Rewards** | MGP varying by event |
| **Priority** | MEDIUM - Good MGP if in Gold Saucer |

### Triple Triad

| Attribute | Details |
|-----------|---------|
| **NPC Battles** | Various NPCs have daily regional rules |
| **Tournaments** | Biweekly, limited matches (20) |
| **Location** | Gold Saucer Battlehall, NPCs throughout Eorzea |
| **Priority** | LOW - Card collection focused |

### Detection Potential
**HIGH** - Mini Cactpot completion count trackable. Fashion Report submission status trackable.

---

## Beast Tribe / Allied Society Quests

### Overview
Daily quests for various faction reputations. Renamed from "Beast Tribe Quests" to "Allied Society Quests" in patch 7.0.

### Allowance System

| Attribute | Details |
|-----------|---------|
| **Daily Allowance** | 12 quests total across all tribes |
| **Per-Tribe Limit** | 3 quests per tribe per day (HW onwards) |
| **Reset** | Daily at 15:00 UTC |
| **Unlock** | Level 41 MSQ "In Pursuit of the Past" |

### All Tribes by Expansion

#### A Realm Reborn (Combat & Crafting)
| Tribe | Focus | Location | Unlock Quest |
|-------|-------|----------|--------------|
| Amalj'aa | Combat | Southern Thanalan | "Peace for Thanalan" |
| Sylphs | Combat | East Shroud | "Seeking Solace" |
| Kobolds | Combat | Outer La Noscea | "Highway Robbery" |
| Sahagin | Combat | Western La Noscea | "They Came from the Deep" |
| Ixal | Crafting | North Shroud | "A Bad Bladder" |

#### Heavensward
| Tribe | Focus | Location | Unlock Quest |
|-------|-------|----------|--------------|
| Vath | Combat | The Dravanian Forelands | "Adventurers Don't Get Cold Feet" |
| Vanu Vanu | Combat | The Sea of Clouds | "Three Hearts as One" |
| Moogles | Crafting | The Churning Mists | "Tricks and Stones" |

#### Stormblood
| Tribe | Focus | Location | Unlock Quest |
|-------|-------|----------|--------------|
| Kojin | Combat | The Ruby Sea | "Heaven-sent" |
| Ananta | Combat | The Fringes | "Brooding Broodmother" |
| Namazu | Crafting/Gathering | The Azim Steppe | "One Size Fits All" |

#### Shadowbringers
| Tribe | Focus | Location | Unlock Quest |
|-------|-------|----------|--------------|
| Pixies | Combat | Il Mheg | "Manic Pixie Dream Realm" |
| Qitari | Gathering | The Rak'tika Greatwood | "The Stewards of Note" |
| Dwarves | Crafting | Lakeland | "A Pact with the Automaton" |

#### Endwalker
| Tribe | Focus | Location | Unlock Quest |
|-------|-------|----------|--------------|
| Arkasodara | Combat | Thavnair | "A Budding Friendship" |
| Loporrits | Crafting | Mare Lamentorum | "Loporrit Lore and Order" |
| Omicrons | Gathering | Ultima Thule | "What's in a Parent" |

#### Dawntrail
| Tribe | Focus | Location | Notes |
|-------|-------|----------|-------|
| Pelupelu | TBD | Tuliyollal area | Introduced in 7.x |
| Moblins | TBD | TBD | Introduced in 7.x |

### Rewards
- EXP (for combat/crafting/gathering classes)
- Gil
- Unique Mounts, Minions, Furnishings (at max reputation)
- Tomestones (some tribes)
- Ventures

### Priority
**HIGH** - Excellent EXP source, unique rewards

### Detection Potential
**HIGH** - Quest completion and allowance remaining likely trackable via Dalamud APIs.

---

## Hunt Marks

### Daily Hunt Bills

| Expansion | Board Location | Currency | Reset |
|-----------|---------------|----------|-------|
| ARR | Grand Company HQ | Allied Seals | Daily 15:00 UTC |
| Heavensward | Foundation (outside Forgotten Knight) | Centurio Seals | Daily 15:00 UTC |
| Stormblood | Kugane / Rhalgr's Reach | Centurio Seals | Daily 15:00 UTC |
| Shadowbringers | Crystarium / Eulmore | Sacks of Nuts | Daily 15:00 UTC |
| Endwalker | Old Sharlayan / Radz-at-Han | Sacks of Nuts | Daily 15:00 UTC |
| Dawntrail | Tuliyollal (X:13.8, Y:13.6) | TBD | Daily 15:00 UTC |

### Weekly Elite Hunt Bills (B-Rank)

| Attribute | Details |
|-----------|---------|
| **Reset** | Tuesday 08:00 UTC |
| **Reward** | 5,000 Gil + 100 Allied Seals (ARR) |
| **Spawn** | Always present in zones |

### Elite Marks (A/S Rank)
- No daily/weekly limit
- Spawn based on conditions/timers
- Kill on sight for rewards
- Hunt trains organized by community

### Unlock Requirement
- Second Lieutenant rank in Grand Company
- Quest: "Let the Hunt Begin" at GC HQ

### Rewards
- Gil, EXP, Tomestones
- Allied Seals, Centurio Seals, Sacks of Nuts
- High-level Materia (exchangeable)
- Unique minions, gear

### Priority
**HIGH** - Quick daily rewards, excellent currency source

### Detection Potential
**MEDIUM** - Bill completion trackable; current bills may require reading game state.

---

## Treasure Maps

### Daily Gathering Limit

| Attribute | Details |
|-----------|---------|
| **Cooldown** | 18 hours (real time) |
| **Gathering Method** | Level 40+ gathering nodes (BTN/MIN/FSH) |
| **Inventory Limit** | 1 undeciphered map of each type |
| **Reset Tracking** | Visible in Timers menu |

### Map Types by Level
- Timeworn Leather Map (Lv 40)
- Timeworn Goatskin Map (Lv 45)
- Timeworn Peisteskin Map (Lv 50)
- ... (continues for each expansion)
- Timeworn Gargantuaskin Map (Lv 100 - Dawntrail)

### Access Method
- Gather from level 40+ standard nodes
- Purchase from Market Board (no cooldown)
- Decipher via Key Items menu

### Unlock
- Quest: "Treasures and Tribulations" from H'loonh in Eastern La Noscea (X:21.1, Y:21.1)

### Rewards
- Gil (direct and materials)
- Crafting materials
- Minions, mounts, glamour
- Portal to special dungeon (party maps)

### Priority
**MEDIUM** - 18-hour cooldown is forgiving; group maps yield high value

### Detection Potential
**MEDIUM** - Timer visible in game; Dalamud likely can read this timer state.

---

## Grand Company Activities

### Supply and Provisioning Missions (Daily)

| Attribute | Details |
|-----------|---------|
| **Reset** | Daily at 20:00 UTC (different from other dailies!) |
| **Activity** | Turn in crafted/gathered items |
| **Quests Per Day** | 1 per DoH/DoL class |
| **Rewards** | Large EXP + GC Seals (doubled for HQ, doubled again if starred) |
| **Location** | Grand Company Personnel Officer |
| **View Items** | Timers menu > Individual > Next Mission Allowance |

### Expert Delivery (No limit)

| Attribute | Details |
|-----------|---------|
| **Unlock** | Sergeant Second Class rank |
| **Activity** | Turn in dungeon/raid gear for seals |
| **Limit** | None |
| **Best For** | Converting unwanted gear to seals |

### Squadron Missions

| Attribute | Details |
|-----------|---------|
| **Unlock** | Second Lieutenant rank |
| **Training** | Reset daily at 20:00 UTC |
| **Priority Missions** | Reset weekly on Tuesday |
| **Command Missions** | No reset (dungeon runs with NPC squadron) |

### Priority
**HIGH** (Supply/Provisioning for leveling crafters/gatherers)

### Detection Potential
**HIGH** - Timers menu data likely accessible.

---

## Custom Deliveries

### Overview
Weekly collectable turn-ins to specific NPCs for gil, EXP, and Scrips.

### Allowance System

| Attribute | Details |
|-----------|---------|
| **Weekly Cap** | 12 deliveries total |
| **Per-NPC Cap** | 6 deliveries per NPC |
| **Reset** | Tuesday 08:00 UTC |
| **Satisfaction Ranks** | 5 levels per NPC |

### All Custom Delivery NPCs

| NPC | Location | Expansion | Level Req | Unlock Quest |
|-----|----------|-----------|-----------|--------------|
| **Zhloe Aliapoh** | Idyllshire (X:5.9, Y:7.2) | HW | 60 DoH/DoL | "Arms Wide Open" |
| **M'naago** | Rhalgr's Reach (X:9.7, Y:11.8) | SB | 60 DoH/DoL | "None Forgotten, None Forsaken" |
| **Kurenai** | Tamamizu (X:27.9, Y:16.9) | SB | 60 DoH/DoL | "The Seaweed Is Always Greener" |
| **Kai-Shirr** | Eulmore (X:11.7, Y:11.3) | ShB | 70 DoH/DoL | "Oh, Beehive Yourself" |
| **Ehll Tou** | The Firmament (X:14.2, Y:12.6) | ShB | 70 DoH/DoL | "The Beasts Know Best" |
| **Charlemend** | The Firmament (X:9.7, Y:8.4) | ShB | 70 DoH/DoL | "A Surprising Skaenura" |
| **Ameliance** | Old Sharlayan (X:12.7, Y:10.3) | EW | 80 DoH/DoL | "A Request of One's Own" |
| **Anden** | TBD | EW | 80 DoH/DoL | TBD |
| **Margrat** | TBD | DT | 90 DoH/DoL | TBD |
| **Nitowikwe** | TBD | DT | 90 DoH/DoL | TBD |

### Rewards
- Gil
- EXP for DoH/DoL classes
- Purple/Orange Crafters' Scrips
- Purple/Orange Gatherers' Scrips
- Satisfaction stories (at max rank)
- Glamour option for NPC (at max rank)

### Priority
**HIGH** - Best weekly source of scrips for crafter/gatherer gear

### Detection Potential
**HIGH** - Delivery count and satisfaction levels likely trackable.

---

## Wondrous Tails

### Overview
Weekly sticker book activity with random duties to complete.

| Attribute | Details |
|-----------|---------|
| **Location** | Idyllshire (X:5.7, Y:6.1) - Khloe Aliapoh |
| **Reset** | Tuesday 08:00 UTC |
| **Journal Duration** | 2 weeks before expiring |
| **Stickers Required** | 9 to submit |
| **Unlock** | "Keeping Up with the Aliapohs" quest (Level 60) |

### How It Works
1. Collect journal from Khloe Aliapoh
2. Complete 9 of 16 random duty objectives
3. Stickers placed randomly on 4x4 grid
4. Rewards based on lines completed (0-3)
5. Second Chance points for retries/shuffles

### Second Chance Points
- Earned when completing duties with first-timers
- Max 9 points stored
- Use to retry sticker placement or shuffle objectives

### Reward Tiers

| Lines | Possible Rewards |
|-------|-----------------|
| 0 | Gil, EXP, Tomestones |
| 1 | MGP cards, Tomestones, Maps |
| 2 | Silver Certificates, better rewards |
| 3 | Gold Certificates (rare mounts, ornate gear) |

### Priority
**HIGH** - Combines with regular duty completion; high-value potential rewards

### Detection Potential
**HIGH** - Journal state and completion status likely trackable via Dalamud.

---

## Challenge Log

### Overview
Weekly objectives across all content types providing bonus EXP and rewards.

| Attribute | Details |
|-----------|---------|
| **Reset** | Tuesday 08:00 UTC |
| **Access** | Logs > Challenge Log (Ctrl+U variant) |
| **Unlock** | "Rising to the Challenge" quest from I'tolwann in Limsa Upper Decks (X:11.4, Y:11.0) |
| **Requirement** | Level 15, completed "Call of the Sea" MSQ |

### Categories

| Category | Notable Entries | Rewards |
|----------|----------------|---------|
| **Battle** | Defeat enemies, complete dungeons | EXP |
| **FATE** | Complete FATEs | EXP |
| **Levequests** | Complete leves | EXP |
| **Crafting** | Craft items, HQ items | EXP |
| **Gathering** | Gather items, HQ items | EXP |
| **Beast Tribes** | Complete tribe quests | GC Seals |
| **Gold Saucer** | Play games, win matches | MGP |
| **Treasure Hunt** | Complete treasure maps | Gil |
| **Player Commendation** | Receive commendations | Gil, EXP |
| **Companions** | Battle with chocobo | Companion EXP |
| **Eureka** | Eureka-specific objectives | Elemental EXP |
| **Island Sanctuary** | Island activities | Island EXP |
| **Complete** | Finish X other challenges | Gil |

### Potential Weekly Earnings
- Up to 95,000 bonus EXP
- Up to 108,000 Gil
- Significant MGP from Gold Saucer entries

### Priority
**HIGH** - Passive completion through normal play; good to track for optimization

### Detection Potential
**HIGH** - Challenge Log progress visible in-game and likely accessible via Dalamud.

---

## Island Sanctuary

### Daily Reset Activities

| Activity | Reset | Details |
|----------|-------|---------|
| **Pasture Animals** | Daily 08:00 UTC | Animals produce materials |
| **Granary Expeditions** | Daily 08:00 UTC | Collect expedition materials |
| **Crop Harvest** | Every 24+ hours | Based on crop growth time |
| **Feeding/Tending** | ~6 hours | Can be automated |

### Weekly Activities

| Activity | Details |
|----------|---------|
| **Workshop Cycles** | Plan production weekly around supply/demand |
| **Felicitous Favors** | Up to 70 tokens per week |
| **Cowrie Spending** | Plan expedition days (50/day) |

### Access
- Unlocked via MSQ progression in Endwalker
- Travel via Duty Finder or ferry

### Priority
**MEDIUM** - Set-and-forget content; mainly for cosmetic rewards

### Detection Potential
**MEDIUM** - Island state may be accessible but complex.

---

## Faux Hollows / Unreal Trials

### Overview
Weekly unreal trial with mini-game reward.

| Attribute | Details |
|-----------|---------|
| **Location** | Idyllshire (X:5.7, Y:6.1) - Faux Commander |
| **Reset** | Tuesday 08:00 UTC |
| **Attempts** | 1 per week (2 if "Retelling" earned) |
| **Unlock** | "Fantastic Mr. Faux" quest (Level 80, Shadowbringers complete) |

### How It Works
1. Complete current Unreal Trial (old Extreme scaled to max level)
2. Speak to Faux Commander
3. Play 6x6 grid minigame
4. Uncover illustrations for Faux Leaves currency

### Rewards
- Faux Leaves currency
- Exchange for mounts, minions, glamour
- Sellable items for Gil

### Priority
**MEDIUM** - Requires Extreme-level skill; good rewards

### Detection Potential
**MEDIUM** - Completion trackable but minigame state complex.

---

## Masked Carnivale (Blue Mage)

### Weekly Targets

| Attribute | Details |
|-----------|---------|
| **Activity** | Solo Blue Mage challenge stages |
| **Location** | Ul'dah - Steps of Thal (X:11.5, Y:13.2) |
| **Reset** | Tuesday 08:00 UTC |
| **Requirements** | Blue Mage job |

### Target Tiers

| Tier | Indicator | Requirements | Allied Seals |
|------|-----------|--------------|--------------|
| Novice | Blue star | Clear stage | ~100 |
| Moderate | White star | Clear + 1 objective | ~150 |
| Advanced | Gold star | Clear + 2-3 objectives | 300 |

**Total Weekly:** ~550 Allied Seals

### Priority
**LOW** - Blue Mage specific; good Allied Seals if playing BLU

### Detection Potential
**HIGH** - Target completion visible in Timers menu.

---

## Doman Enclave Reconstruction

### Weekly Donation

| Attribute | Details |
|-----------|---------|
| **Location** | Doman Enclave |
| **Reset** | Tuesday 08:00 UTC |
| **Initial Budget** | 20,000 Gil/week |
| **Max Budget** | 40,000 Gil/week (after milestones) |
| **Gratuity** | 120% to 200% of vendor price |
| **Unlock** | Stormblood MSQ + "Precious Reclamation" quest |

### How It Works
1. Donate unwanted items to Enclave NPCs
2. Receive bonus Gil above vendor price
3. Progress reconstruction storyline

### Priority
**MEDIUM** - Easy extra Gil; limited by budget

### Detection Potential
**MEDIUM** - Budget remaining may be trackable.

---

## Tomestone Weekly Caps

### Current Tomestones (Dawntrail)

| Tomestone | Cap | Reset | Use |
|-----------|-----|-------|-----|
| **Poetics** | 2,000 (no weekly cap) | N/A | Old expansion gear |
| **Aesthetics** | 2,000 (no weekly cap) | N/A | Neo Kingdom gear (iLvl 710) |
| **Heliometry** | 450/week (2,000 max) | Tuesday 08:00 UTC | Quetzalli gear (iLvl 720) |

### Sources
- Expert Roulette: ~90 Aesthetics, ~60 Heliometry
- Hunt trains: Significant tomestones
- Raids: Varying amounts
- Wondrous Tails: Bonus tomestones

### Priority
**HIGH** - Track weekly Heliometry cap for gearing

### Detection Potential
**HIGH** - Tomestone counts clearly trackable.

---

## Leve Allowances

### Allowance System

| Attribute | Details |
|-----------|---------|
| **Regeneration** | 3 allowances every 12 real hours |
| **Maximum** | 100 allowances |
| **Reset** | Rolling (not fixed time) |
| **View** | Journal > Levequests |

### Best Uses
- Crafting levequests (fastest crafter leveling)
- Gathering levequests (decent gatherer EXP)
- NOT recommended for battle classes

### Priority
**LOW** - Allowances accumulate; mainly for crafting sprints

### Detection Potential
**HIGH** - Allowance count visible in-game.

---

## Retainer Ventures

### Venture Types

| Type | Duration | Cost | Notes |
|------|----------|------|-------|
| **Quick Exploration** | 1 hour | 2 Ventures | Best for leveling |
| **Targeted/Field** | 40-60 min | 1 Venture | Specific item gathering |
| **Exploration** | 18 hours | 2 Ventures | Higher level required |

### Reset
- No fixed reset; timer-based per venture
- Best practice: Send on 18-hour before logging off

### Priority
**MEDIUM** - Passive income/materials; no strict daily requirement

### Detection Potential
**HIGH** - Retainer status accessible.

---

## Raid Lockouts

### Weekly Lockout Content

| Content | Reset | Notes |
|---------|-------|-------|
| **Normal Raids** | Tuesday 08:00 UTC | 1 token per boss per week |
| **Alliance Raids** | Tuesday 08:00 UTC | 1 item per week (older raids uncapped) |
| **Savage Raids** | Tuesday 08:00 UTC | 1 clear reward per floor per week |

### Priority
**HIGH** - Critical for raiders to track

### Detection Potential
**HIGH** - Lockout status trackable.

---

## PVP Content

### Frontline Daily

| Attribute | Details |
|-----------|---------|
| **Roulette** | Duty Roulette: Frontline |
| **Reset** | Daily 15:00 UTC |
| **Rewards** | EXP, Wolf Marks, Tomestones |
| **First Place Bonus** | 1,000 Wolf Marks + 600 base |

### Series Malmstones
- Seasonal PVP track
- Not reset-based; cumulative

### Priority
**MEDIUM** - Good daily EXP and unique rewards

### Detection Potential
**HIGH** - Roulette completion trackable.

---

## Programmatic Detection Notes

### Dalamud API Considerations

**Highly Accessible (via known APIs):**
- Duty Roulette completion status
- Tomestone counts and caps
- Challenge Log progress
- Quest/duty completion flags
- Retainer status
- Leve allowance count
- Beast Tribe reputation and daily quest counts

**Moderately Accessible:**
- Hunt bill completion (may need custom tracking)
- Treasure map timer
- Custom Delivery allowances
- Wondrous Tails journal state

**May Require Custom Implementation:**
- Gold Saucer Mini Cactpot count
- Fashion Report submission status
- Island Sanctuary state
- Faux Hollows availability

### Recommended Plugin Features

1. **Timer Display** - Show time until each reset type
2. **Completion Tracking** - Check/uncheck system with auto-detection where possible
3. **Character-Specific** - Support multiple characters
4. **Customizable** - Let users enable/disable tracked activities
5. **Notifications** - Optional alerts for upcoming resets
6. **Quick Access** - Buttons to open relevant in-game windows

### External Resources
- XIVToDo (xivtodo.com) - Existing web-based tracker for reference
- FFXIV Timers (ffxivtimers.com) - Reset time reference

---

## Priority Summary

### Daily (High Priority)
1. Duty Roulettes (Expert, Leveling, Alliance)
2. Mini Cactpot (3x)
3. Beast Tribe Quests (12 allowances)
4. Daily Hunt Bills
5. Grand Company Supply/Provisioning

### Weekly (High Priority)
1. Tomestone cap (Heliometry)
2. Custom Deliveries (12 allowances)
3. Wondrous Tails
4. Fashion Report
5. Jumbo Cactpot (3 tickets)
6. Weekly Elite Hunts
7. Challenge Log completion

### Weekly (Medium Priority)
1. Raid lockouts
2. Faux Hollows
3. Doman Enclave donations
4. Island Sanctuary workshop/granary

### As Needed
1. Leve allowances (cap at 100)
2. Retainer ventures
3. Treasure map gathering (18hr cooldown)

---

## Sources

- [Console Games Wiki - Duty Roulette](https://ffxiv.consolegameswiki.com/wiki/Duty_Roulette)
- [Console Games Wiki - The Gold Saucer](https://ffxiv.consolegameswiki.com/wiki/The_Gold_Saucer)
- [Console Games Wiki - Allied Society Quests](https://ffxiv.consolegameswiki.com/wiki/Allied_Society_Quests)
- [Console Games Wiki - The Hunt](https://ffxiv.consolegameswiki.com/wiki/The_Hunt)
- [Console Games Wiki - Treasure Hunt](https://ffxiv.consolegameswiki.com/wiki/Treasure_Hunt)
- [Console Games Wiki - Custom Deliveries](https://ffxiv.consolegameswiki.com/wiki/Custom_Deliveries)
- [Console Games Wiki - Wondrous Tails](https://ffxiv.consolegameswiki.com/wiki/Wondrous_Tails)
- [Console Games Wiki - Challenge Log](https://ffxiv.consolegameswiki.com/wiki/Challenge_Log)
- [Console Games Wiki - Reset](https://ffxiv.consolegameswiki.com/wiki/Reset)
- [Icy Veins - FFXIV Guides](https://www.icy-veins.com/ffxiv/)
- [Fanbyte - FFXIV Weekly and Daily Reset Checklist](https://www.fanbyte.com/ffxiv/guides/weekly-and-daily-reset-checklist)
- [XIV ToDo](https://xivtodo.com/)
- [FFXIV Lodestone - Official Guides](https://na.finalfantasyxiv.com/lodestone/playguide/)
