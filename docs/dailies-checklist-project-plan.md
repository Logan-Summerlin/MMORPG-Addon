# Dailies Checklist Plugin - Project Plan

## 1. Executive Summary

The **Dailies Checklist** plugin is a Dalamud-based quality-of-life overlay for Final Fantasy XIV that helps players track their daily and weekly reset activities. The plugin displays an in-game ImGui window showing a categorized checklist of tasks with location information and completion status.

**Core Value Proposition**: Players often forget which daily activities they have completed, especially across multiple play sessions. This plugin provides a persistent, at-a-glance view of daily/weekly task progress with automatic detection where technically feasible, and manual tracking for activities that cannot be auto-detected.

**Compliance Statement**: This plugin is strictly informational and does not automate gameplay. It reads game state to display information and allows manual checkbox interaction. No actions are performed on behalf of the player.

---

## 2. User Stories

### Primary User: Casual to Mid-Core FFXIV Player

| ID | Story | Acceptance Criteria |
|----|-------|---------------------|
| US-01 | As a player, I want to see a checklist of daily activities so I know what I haven't done yet | Overlay displays categorized list of daily tasks with completion status |
| US-02 | As a player, I want each task to show its location so I know where to go | Each task displays "Location: X. Task: Y" format |
| US-03 | As a player, I want the checklist to auto-detect completed activities when possible | Supported activities automatically mark as complete when game state indicates completion |
| US-04 | As a player, I want to manually check/uncheck tasks that can't be auto-detected | Manual toggle available for all tasks |
| US-05 | As a player, I want the checklist to reset appropriately at daily/weekly reset times | Tasks reset based on their category (daily vs weekly) at correct server times |
| US-06 | As a player, I want to customize which tasks appear on my checklist | Settings allow enabling/disabling individual tasks |
| US-07 | As a player, I want the overlay to be toggleable and moveable | Keybind/command to show/hide; window is draggable and resizable |
| US-08 | As a player, I want my checklist state to persist between game sessions | Data saved locally and restored on login |

### Non-Goals (Explicit Exclusions)
- No automation of any gameplay actions
- No teleportation or navigation assistance
- No integration with external websites or APIs
- No tracking of other players' activities
- No retainer or Free Company management (existing plugins cover this)

---

## 3. Feature Specification

### 3.1 Core Features (Phase 1)

#### 3.1.1 Checklist UI Window
- ImGui-based overlay window
- Title bar with collapse/close buttons
- Categorized sections with collapsible headers:
  - **Daily Activities**
  - **Weekly Activities**
- Each task entry displays:
  - Checkbox (checked/unchecked state)
  - Location text (e.g., "Gold Saucer")
  - Task description (e.g., "Mini Cactpot x3")
  - Visual indicator for auto-detected vs manual tasks
- Window features:
  - Draggable positioning
  - Resizable
  - Configurable opacity/transparency
  - Lock position option

#### 3.1.2 Task Categories and Items

**Daily Activities** (reset at 10:00 AM EST / 3:00 PM UTC):

| Task | Location | Auto-Detectable | Detection Method |
|------|----------|-----------------|------------------|
| Mini Cactpot (x3) | Gold Saucer | Yes | Game state tracking via addon data |
| Beast Tribe Quests (12 allowances) | Various | Yes | Quest allowance tracking |
| Daily Hunts (ARR) | Ul'dah/Limsa/Gridania | Partial | Hunt bill pickup detection |
| Daily Hunts (HW) | Foundation | Partial | Hunt bill pickup detection |
| Daily Hunts (SB) | Kugane/Rhalgr's Reach | Partial | Hunt bill pickup detection |
| Daily Hunts (ShB) | Crystarium/Eulmore | Partial | Hunt bill pickup detection |
| Daily Hunts (EW) | Old Sharlayan/Radz-at-Han | Partial | Hunt bill pickup detection |
| Daily Hunts (DT) | Tuliyollal/Solution Nine | Partial | Hunt bill pickup detection |
| Duty Roulette: Leveling | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Expert | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Level 50/60/70/80/90 | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Trials | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Main Scenario | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Alliance Raids | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Normal Raids | Duty Finder | Yes | Duty completion tracking |
| Duty Roulette: Frontline | Duty Finder | Yes | Duty completion tracking |
| GC Daily Supply/Provisioning | Grand Company | Yes | GC delivery tracking |
| Tribal Quest Allowances | Various tribal areas | Yes | Allowance counter |

**Weekly Activities** (reset Tuesday 1:00 AM PST / 9:00 AM UTC):

| Task | Location | Auto-Detectable | Detection Method |
|------|----------|-----------------|------------------|
| Jumbo Cactpot (x3 tickets) | Gold Saucer | Yes | Ticket purchase tracking |
| Wondrous Tails | Idyllshire (Khloe) | Yes | Journal state tracking |
| Challenge Log Progress | Character menu | Partial | Log completion percentage |
| Custom Deliveries (12 allowances) | Various NPCs | Yes | Delivery allowance tracking |
| Weekly Elite Marks (B-Rank) | Hunt boards | Partial | Mark completion tracking |
| Fashion Report | Gold Saucer | Yes | Participation tracking |
| Masked Carnivale (Weekly) | Ul'dah | Partial | Completion tracking |
| Doman Enclave Donations | Doman Enclave | Yes | Weekly budget tracking |

#### 3.1.3 Manual Check/Uncheck
- All tasks support manual toggle regardless of auto-detection
- Manual override persists until next reset
- Visual distinction between auto-detected and manually marked tasks
- "Reset All" button per category
- "Mark All Complete" option for testing/catch-up

#### 3.1.4 Settings Panel
- Enable/disable individual tasks
- Enable/disable entire categories
- Show/hide location text
- Show/hide auto-detection indicators
- Window opacity slider (25% - 100%)
- Lock window position toggle
- Reset window position button
- Keybind configuration for toggle visibility

### 3.2 Auto-Detection Features (Phase 2)

#### 3.2.1 Technically Feasible Auto-Detection

Based on Dalamud API capabilities, the following can be reliably auto-detected:

**High Confidence (Direct Game State Access)**:
- **Duty Roulette completion**: Via `IClientState` duty completion events and roulette bonus tracking
- **Beast Tribe allowances**: Via quest allowance game state
- **Mini/Jumbo Cactpot**: Via Gold Saucer game state and currency tracking
- **Wondrous Tails**: Via journal key item state
- **Custom Deliveries**: Via delivery allowance counters
- **GC Supply/Provisioning**: Via Grand Company delivery state
- **Doman Enclave**: Via weekly budget tracking

**Medium Confidence (Requires UI/Addon Reading)**:
- **Daily Hunts**: Detecting hunt bill acceptance (not kill completion)
- **Challenge Log**: Reading completion percentages from character UI
- **Fashion Report**: Participation flag detection

**Low Confidence / Manual Only**:
- **Specific hunt mark kills**: Would require combat log parsing (too invasive)
- **Treasure map completion**: Inconsistent state tracking
- **Deep Dungeon progress**: Complex instance state

#### 3.2.2 Detection Architecture

```
Game State Change
       |
       v
+------------------+     +-------------------+
| Dalamud Services | --> | State Listeners   |
| - IClientState   |     | - TerritoryChange |
| - DataManager    |     | - DutyComplete    |
| - IGameGui       |     | - QuestUpdate     |
+------------------+     +-------------------+
                                  |
                                  v
                         +------------------+
                         | Detection Engine |
                         | - Validate state |
                         | - Update model   |
                         +------------------+
                                  |
                                  v
                         +------------------+
                         | Checklist Model  |
                         | - Task states    |
                         | - Timestamps     |
                         +------------------+
                                  |
                                  v
                         +------------------+
                         |    ImGui UI      |
                         +------------------+
```

### 3.3 Reset Handling

#### 3.3.1 Reset Timer Logic
- Plugin tracks server time (not local time)
- Daily reset: 10:00 AM EST (adjusted for DST)
- Weekly reset: Tuesday 1:00 AM PST (adjusted for DST)
- Grand Company reset: 1:00 PM PST daily
- On login: Check if reset occurred since last session
- Graceful handling of time zone edge cases

#### 3.3.2 Reset Behavior
- Auto-detected tasks: Reset to unchecked, await new detection
- Manually checked tasks: Reset to unchecked
- Preserve task enable/disable preferences
- Log reset events for debugging

### 3.4 Configuration Options

```csharp
public class PluginConfiguration
{
    // Window Settings
    public bool WindowVisible { get; set; } = true;
    public bool WindowLocked { get; set; } = false;
    public float WindowOpacity { get; set; } = 1.0f;
    public Vector2 WindowPosition { get; set; }
    public Vector2 WindowSize { get; set; }

    // Display Settings
    public bool ShowLocations { get; set; } = true;
    public bool ShowAutoDetectIndicators { get; set; } = true;
    public bool CollapseDailyByDefault { get; set; } = false;
    public bool CollapseWeeklyByDefault { get; set; } = false;

    // Task Settings
    public Dictionary<string, bool> EnabledTasks { get; set; }
    public Dictionary<string, bool> TaskStates { get; set; }
    public Dictionary<string, DateTime> LastCompletionTimes { get; set; }

    // Notification Settings
    public bool EnableChatNotifications { get; set; } = false;
    public bool NotifyOnLogin { get; set; } = true;

    // Reset Tracking
    public DateTime LastDailyReset { get; set; }
    public DateTime LastWeeklyReset { get; set; }
}
```

---

## 4. Technical Architecture

### 4.1 Required Dalamud Services

| Service | Purpose | Injection |
|---------|---------|-----------|
| `IClientState` | Player login state, territory changes, duty events | Constructor |
| `IDataManager` | Game data sheets (territories, quests, duties) | Constructor |
| `IGameGui` | UI addon access for state reading | Constructor |
| `IPluginLog` | Logging for debugging | Constructor |
| `IFramework` | Frame update hooks for polling | Constructor |
| `ICommandManager` | Chat command registration | Constructor |
| `ICondition` | Player condition flags (in duty, in combat, etc.) | Constructor |
| `IDalamudPluginInterface` | Plugin lifecycle, config save/load | Constructor |

### 4.2 Data Model

```csharp
public enum TaskCategory
{
    Daily,
    Weekly
}

public enum DetectionType
{
    Manual,           // User must check manually
    AutoDetected,     // Plugin can detect completion
    Hybrid           // Auto-detect with manual override
}

public class ChecklistTask
{
    public string Id { get; set; }              // Unique identifier
    public string Name { get; set; }            // Display name
    public string Location { get; set; }        // Where to do this task
    public string Description { get; set; }     // What to do
    public TaskCategory Category { get; set; }
    public DetectionType Detection { get; set; }
    public bool IsEnabled { get; set; }         // User preference
    public bool IsCompleted { get; set; }       // Current state
    public bool IsManuallySet { get; set; }     // Was this manually toggled?
    public DateTime? CompletedAt { get; set; }  // When completed
    public int SortOrder { get; set; }          // Display order
}

public class ChecklistState
{
    public List<ChecklistTask> Tasks { get; set; }
    public DateTime LastDailyReset { get; set; }
    public DateTime LastWeeklyReset { get; set; }
    public DateTime LastSaveTime { get; set; }
}
```

### 4.3 State Persistence

- **Storage**: Dalamud plugin configuration directory
- **Format**: JSON serialization via `Newtonsoft.Json` or `System.Text.Json`
- **Save Triggers**:
  - On task state change (debounced, 2-second delay)
  - On window close
  - On plugin unload
  - On logout
- **Load Triggers**:
  - On plugin enable
  - On character login
- **Migration**: Version field in config for future schema changes

### 4.4 UI Component Design

```
+------------------------------------------+
| Dailies Checklist                   [_][X]|
+------------------------------------------+
| v Daily Activities          [Reset All]  |
|   [ ] Gold Saucer - Mini Cactpot (0/3)   |
|   [x] Duty Finder - Leveling Roulette *  |
|   [x] Duty Finder - Expert Roulette *    |
|   [ ] Foundation - Daily Hunts (HW)      |
|   ...                                     |
+------------------------------------------+
| v Weekly Activities         [Reset All]  |
|   [ ] Gold Saucer - Jumbo Cactpot (0/3)  |
|   [ ] Idyllshire - Wondrous Tails        |
|   [x] Various - Custom Deliveries (12) * |
|   ...                                     |
+------------------------------------------+
| [Settings]              Last reset: 2h ago|
+------------------------------------------+

* = Auto-detected
```

### 4.5 Module Structure

```
DailiesChecklist/
├── DailiesChecklist.csproj
├── Plugin.cs                 // Entry point, service injection
├── Configuration.cs          // Settings persistence
├── Windows/
│   ├── MainWindow.cs         // Primary checklist UI
│   └── SettingsWindow.cs     // Configuration UI
├── Models/
│   ├── ChecklistTask.cs      // Task data model
│   └── ChecklistState.cs     // State container
├── Services/
│   ├── TaskRegistry.cs       // Defines all available tasks
│   ├── DetectionService.cs   // Auto-detection logic
│   ├── ResetService.cs       // Reset timer management
│   └── PersistenceService.cs // Save/load operations
└── Detectors/
    ├── ITaskDetector.cs      // Detector interface
    ├── RouletteDetector.cs   // Duty roulette detection
    ├── CactpotDetector.cs    // Gold Saucer detection
    ├── BeastTribeDetector.cs // Tribal quest detection
    └── ...                   // Additional detectors
```

---

## 5. Implementation Plan

### Phase 1: Core UI and Manual Checklist (Week 1-2)

**Objective**: Functional checklist with manual tracking, proper reset handling, and persistence.

| Task | Deliverable | Definition of Done |
|------|-------------|-------------------|
| 1.1 | Project scaffolding | Solution compiles, plugin loads in Dalamud |
| 1.2 | Configuration system | Settings save/load correctly, survive restarts |
| 1.3 | Task registry | All daily/weekly tasks defined with metadata |
| 1.4 | Main window UI | Categorized checklist displays, checkboxes work |
| 1.5 | Manual check/uncheck | All tasks can be toggled manually |
| 1.6 | Reset timer logic | Daily/weekly resets trigger correctly |
| 1.7 | State persistence | Checklist state survives logout/login |
| 1.8 | Settings window | Enable/disable tasks, UI preferences |
| 1.9 | Chat command | `/dailies` toggles window visibility |

**Exit Criteria**:
- Plugin installs and loads without errors
- User can see checklist, manually check items
- State persists across sessions
- Resets work at correct times

### Phase 2: Auto-Detection (Week 3-4)

**Objective**: Implement auto-detection for high-confidence tasks.

| Task | Deliverable | Definition of Done |
|------|-------------|-------------------|
| 2.1 | Detection framework | ITaskDetector interface, registration system |
| 2.2 | Roulette detector | All duty roulettes auto-detect completion |
| 2.3 | Cactpot detector | Mini/Jumbo Cactpot ticket usage detected |
| 2.4 | Beast tribe detector | Allowance usage tracked |
| 2.5 | Custom delivery detector | Delivery allowances tracked |
| 2.6 | Wondrous tails detector | Journal state tracked |
| 2.7 | GC delivery detector | Supply/provisioning completion tracked |
| 2.8 | Doman enclave detector | Weekly budget tracked |
| 2.9 | Visual indicators | Auto-detected tasks show indicator in UI |

**Exit Criteria**:
- High-confidence tasks auto-detect reliably
- Manual override still works for all tasks
- No false positives/negatives in normal gameplay
- Performance impact < 1ms per frame

### Phase 3: Polish and Additional Features (Week 5-6)

**Objective**: Quality-of-life improvements and edge case handling.

| Task | Deliverable | Definition of Done |
|------|-------------|-------------------|
| 3.1 | Hunt detection | Daily hunt bill pickup detection |
| 3.2 | Challenge log detection | Completion percentage tracking |
| 3.3 | Fashion report detection | Participation flag detection |
| 3.4 | Login notifications | Optional chat message on login showing incomplete tasks |
| 3.5 | Window improvements | Better styling, icons, progress bars |
| 3.6 | Keybind support | Configurable keybind for window toggle |
| 3.7 | Tooltip help | Hover tooltips explaining each task |
| 3.8 | Error handling | Graceful degradation if detection fails |
| 3.9 | Documentation | User guide, configuration help |
| 3.10 | Testing | Full test pass across all scenarios |

**Exit Criteria**:
- All planned detectors implemented
- No crashes or hangs in testing
- Documentation complete
- Ready for distribution

---

## 6. Risk Assessment

### 6.1 Technical Risks

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Game patch breaks detection | High | High | Use feature flags to disable broken detectors; abstract detection behind interfaces for easy updates |
| False positive detection | Medium | Medium | Conservative detection logic; always allow manual override |
| Performance impact | Medium | Low | Polling intervals configurable; minimize per-frame work |
| State corruption | Medium | Low | Validation on load; backup configs; versioned schema |
| Dalamud API changes | Medium | Medium | Pin to stable Dalamud version; monitor API changes |

### 6.2 What Cannot Be Auto-Detected

The following activities cannot be reliably auto-detected and will remain manual-only:

1. **Specific hunt mark kills**: Would require combat log parsing or damage tracking, which is invasive and potentially unstable
2. **Treasure map completion**: Instance state is not reliably exposed
3. **Crafting/gathering leve completion**: Complex state tracking not worth the effort
4. **Specific FATE completion**: Too many FATEs, state tracking unreliable
5. **PvP match specifics**: Beyond roulette completion, detailed tracking is complex

### 6.3 Patch Resilience Strategy

1. **Signature-free design**: Rely on official Dalamud services, not memory signatures
2. **Feature flags**: Each detector has an enable flag; can disable broken modules
3. **Graceful degradation**: If detection fails, task falls back to manual mode
4. **Version checks**: Compare game version, warn if untested
5. **Quick update path**: Modular detector design allows targeted fixes

### 6.4 Compliance Considerations

| Concern | Assessment | Mitigation |
|---------|------------|------------|
| Automation | Not applicable | Plugin only reads state; no actions performed |
| Unfair advantage | None | Information is already in-game; this is organization only |
| Privacy | None | No other player data accessed or stored |
| Network | None | No external API calls |
| ToS | Low risk | Informational overlay only; similar to existing approved plugins |

---

## 7. Definition of Done

### 7.1 Feature Acceptance Criteria

- [ ] Plugin loads without errors on current Dalamud stable
- [ ] Main window displays categorized checklist
- [ ] All defined tasks appear with location and description
- [ ] Manual check/uncheck works for all tasks
- [ ] Auto-detection works for high-confidence tasks (roulettes, cactpot, beast tribes)
- [ ] State persists across logout/login
- [ ] State persists across game restart
- [ ] Daily reset clears daily tasks at correct time
- [ ] Weekly reset clears weekly tasks at correct time
- [ ] Settings window allows task enable/disable
- [ ] Settings window allows UI customization
- [ ] `/dailies` command toggles window visibility
- [ ] Window is draggable and resizable
- [ ] Window position/size persists

### 7.2 Quality Criteria

- [ ] No crashes during normal gameplay (8+ hour session)
- [ ] No memory leaks (stable memory usage over time)
- [ ] Frame time impact < 1ms average
- [ ] Graceful handling of all edge cases:
  - Login/logout during detection
  - Zone change during detection
  - Game minimized/unfocused
  - Rapid task completion
- [ ] All error paths logged appropriately
- [ ] No sensitive data in logs

### 7.3 Testing Checklist

| Scenario | Test Steps | Expected Result |
|----------|------------|-----------------|
| Fresh install | Install plugin, open window | Default tasks visible, all unchecked |
| Manual check | Click checkbox | Task shows checked, state saves |
| Persistence | Check task, logout, login | Task still checked |
| Daily reset | Wait for reset time | Daily tasks unchecked, weekly unchanged |
| Weekly reset | Wait for Tuesday reset | Weekly tasks unchecked |
| Auto-detect roulette | Complete leveling roulette | Task auto-checks |
| Auto-detect cactpot | Play Mini Cactpot | Counter increments |
| Manual override | Auto-detected task, manually uncheck | Task shows unchecked despite detection |
| Settings | Disable a task | Task hidden from list |
| Zone change | Change zones | Detection continues working |
| Relog | Logout, login different character | Separate state per character |

### 7.4 Documentation Deliverables

- [ ] User guide: How to use the plugin
- [ ] Configuration reference: All settings explained
- [ ] FAQ: Common questions and troubleshooting
- [ ] Changelog: Version history

---

## 8. References

### Research Sources
- [Dalamud API Documentation](https://dalamud.dev/api/)
- [IClientState Interface](https://dalamud.dev/api/Dalamud.Plugin.Services/Interfaces/IClientState/)
- [DailyDuty Plugin (Reference Implementation)](https://github.com/MidoriKami/DailyDuty)
- [Dalamud GitHub Repository](https://github.com/goatcorp/Dalamud)

### FFXIV Daily/Weekly Reset Information
- Daily Reset: 10:00 AM EST / 3:00 PM UTC
- Weekly Reset: Tuesday 1:00 AM PST / 9:00 AM UTC
- Grand Company Reset: 1:00 PM PST daily

---

## Appendix A: Full Task Registry

### Daily Tasks

```csharp
new ChecklistTask
{
    Id = "mini_cactpot",
    Name = "Mini Cactpot",
    Location = "Gold Saucer",
    Description = "Mini Cactpot x3",
    Category = TaskCategory.Daily,
    Detection = DetectionType.AutoDetected
},
new ChecklistTask
{
    Id = "beast_tribe_daily",
    Name = "Beast Tribe Quests",
    Location = "Various",
    Description = "Beast Tribe allowances (0/12)",
    Category = TaskCategory.Daily,
    Detection = DetectionType.AutoDetected
},
// ... (all other daily tasks)
```

### Weekly Tasks

```csharp
new ChecklistTask
{
    Id = "jumbo_cactpot",
    Name = "Jumbo Cactpot",
    Location = "Gold Saucer",
    Description = "Jumbo Cactpot tickets (0/3)",
    Category = TaskCategory.Weekly,
    Detection = DetectionType.AutoDetected
},
new ChecklistTask
{
    Id = "wondrous_tails",
    Name = "Wondrous Tails",
    Location = "Idyllshire",
    Description = "Khloe's Wondrous Tails journal",
    Category = TaskCategory.Weekly,
    Detection = DetectionType.AutoDetected
},
// ... (all other weekly tasks)
```

---

*Document Version: 1.0*
*Created: 2026-01-27*
*Status: Proposal - Pending Review*
