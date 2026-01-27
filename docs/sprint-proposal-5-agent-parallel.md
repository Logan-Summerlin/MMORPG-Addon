# Dailies Checklist Plugin - 5 Agent Parallel Sprint Proposal

**Sprint Goal:** Build the foundational Dailies Checklist plugin with manual task tracking, UI, and detection framework ready for Phase 2 auto-detection integration.

**Status:** Draft Proposal
**Date:** 2026-01-27
**Branch:** `claude/draft-plugin-sprint-tasks-ganIm`

---

## Documentation Summary

### Reference Documents

| Document | Location | Key Content |
|----------|----------|-------------|
| **Project Plan** | `docs/dailies-checklist-project-plan.md` | Complete PRD including user stories, technical architecture, data models, and implementation phases |
| **FF14 Dailies Research** | `docs/ff14-dailies-research.md` | Comprehensive list of all daily/weekly activities, reset times (Daily: 15:00 UTC, Weekly: Tuesday 08:00 UTC, GC: 20:00 UTC), and detection feasibility for each activity |
| **Dalamud API Reference** | `docs/dalamud-api-reference-guide.md` | Service injection patterns, IClientState, IFramework, IDataManager, ImGui UI development, event handling, and configuration persistence |
| **Plugin Development Guide** | `docs/dalamud-plugin-development-guide.md` | Project setup, manifest structure, plugin lifecycle, IDalamudPlugin interface, and distribution requirements |
| **Code Patterns Guide** | `docs/sampleplugin-code-patterns-guide.md` | Window patterns, service injection, configuration patterns, command registration, ImGui controls, and ImRaii usage |
| **Directory Guide** | `docs/sampleplugin-directory-guide.md` | Standard project structure, file naming conventions, and dependency relationships |

### Target Module Structure

```
DailiesChecklist/
├── DailiesChecklist.csproj     # Agent 1
├── DailiesChecklist.json       # Agent 1
├── Plugin.cs                   # Agent 1
├── Configuration.cs            # Agent 1
├── Windows/
│   ├── MainWindow.cs           # Agent 3
│   └── SettingsWindow.cs       # Agent 3
├── Models/
│   ├── ChecklistTask.cs        # Agent 2
│   ├── ChecklistState.cs       # Agent 2
│   └── Enums.cs                # Agent 2
├── Services/
│   ├── TaskRegistry.cs         # Agent 2
│   ├── ResetService.cs         # Agent 4
│   └── PersistenceService.cs   # Agent 4
└── Detectors/
    ├── ITaskDetector.cs        # Agent 5
    ├── DetectionService.cs     # Agent 5
    └── Detectors/              # Agent 5 (stubs)
```

---

## Agent Assignments

### Agent 1: Core Plugin Infrastructure

**Focus:** Plugin scaffold, project setup, and entry point

**Reference Documentation:**
- `docs/dalamud-plugin-development-guide.md` - Sections 2-6 (Prerequisites, Project Setup, Manifest, Lifecycle, Service Injection)
- `docs/sampleplugin-code-patterns-guide.md` - Sections 2-6 (Project File, Manifest, Plugin Class, DI, Configuration)
- `docs/sampleplugin-directory-guide.md` - Full document for structure reference

**Tasks:**

| ID | Task | Deliverable | Acceptance Criteria |
|----|------|-------------|---------------------|
| A1.1 | Create project structure | `DailiesChecklist/` directory with proper layout | Follows SamplePlugin directory structure |
| A1.2 | Configure project file | `DailiesChecklist.csproj` | Uses `Dalamud.NET.Sdk/14.0.1`, version 1.0.0.0 |
| A1.3 | Create plugin manifest | `DailiesChecklist.json` | Contains Author, Name, Punchline, Description, Tags |
| A1.4 | Implement Plugin.cs | Main entry point with service injection | Implements IDalamudPlugin, injects core services, proper Dispose() |
| A1.5 | Implement Configuration.cs | Settings class | Implements IPluginConfiguration, includes window and display settings |
| A1.6 | Register `/dailies` command | Command handler | Toggles main window visibility |
| A1.7 | Setup WindowSystem | Window management | WindowSystem initialized, Draw event wired, OpenConfigUi/OpenMainUi handlers |

**Dependencies:** None (foundational work)

**Output:** Compiling plugin that loads in Dalamud with command registration and empty window stubs

---

### Agent 2: Data Models & Task Registry

**Focus:** Domain models and task definitions

**Reference Documentation:**
- `docs/dailies-checklist-project-plan.md` - Section 4.2 (Data Model), Appendix A (Full Task Registry)
- `docs/ff14-dailies-research.md` - Full document for activity details, reset times, and detection potential ratings

**Tasks:**

| ID | Task | Deliverable | Acceptance Criteria |
|----|------|-------------|---------------------|
| A2.1 | Create Enums.cs | TaskCategory, DetectionType enums | Matches project plan definitions |
| A2.2 | Create ChecklistTask.cs | Task data model | Properties: Id, Name, Location, Description, Category, Detection, IsEnabled, IsCompleted, IsManuallySet, CompletedAt, SortOrder |
| A2.3 | Create ChecklistState.cs | State container | Contains List<ChecklistTask>, LastDailyReset, LastWeeklyReset, LastSaveTime |
| A2.4 | Create TaskRegistry.cs | Static task definitions | All daily tasks from project plan (Mini Cactpot, Beast Tribes, all Roulettes, Daily Hunts, GC Supply) |
| A2.5 | Add weekly tasks | Complete weekly definitions | Jumbo Cactpot, Wondrous Tails, Custom Deliveries, Fashion Report, Weekly Hunts, Doman Enclave, Challenge Log |
| A2.6 | Add task metadata | Location strings and descriptions | Each task has accurate Location and Description matching FF14 research doc |
| A2.7 | Implement GetDefaultTasks() | Factory method | Returns fresh list of all tasks with default states |

**Dependencies:** None (data layer)

**Output:** Complete data model and registry with all 25+ tasks defined per project plan

**Key Reference Data (from ff14-dailies-research.md):**
- Daily Reset: 15:00 UTC
- Weekly Reset: Tuesday 08:00 UTC
- GC Reset: 20:00 UTC (different from standard daily!)
- Jumbo Cactpot Drawing: Saturday 08:00 UTC

---

### Agent 3: UI Windows

**Focus:** ImGui windows for checklist and settings

**Reference Documentation:**
- `docs/dalamud-api-reference-guide.md` - Section 3 (ImGui UI Development)
- `docs/sampleplugin-code-patterns-guide.md` - Sections 7, 11 (Window Patterns, ImGui Integration)
- `docs/dailies-checklist-project-plan.md` - Section 4.4 (UI Component Design wireframe)

**Tasks:**

| ID | Task | Deliverable | Acceptance Criteria |
|----|------|-------------|---------------------|
| A3.1 | Create MainWindow.cs | Primary checklist window | Extends Window class, implements IDisposable |
| A3.2 | Implement collapsible headers | Daily/Weekly sections | ImGui.CollapsingHeader for each category |
| A3.3 | Implement task list rendering | Checkbox list with location/description | Each task shows: checkbox, location, task name, auto-detect indicator |
| A3.4 | Add "Reset All" buttons | Per-category reset | Button in each section header |
| A3.5 | Implement window constraints | Size and position | MinimumSize: 300x200, draggable, resizable |
| A3.6 | Create SettingsWindow.cs | Configuration UI | Enable/disable tasks, show/hide options |
| A3.7 | Add display options | Opacity, lock position | Slider for opacity (25-100%), checkbox for lock |
| A3.8 | Add task toggles | Enable/disable individual tasks | Checkbox list of all tasks in settings |

**Dependencies:** Agent 2 (needs ChecklistTask model for rendering)

**Output:** Functional UI windows that can display task lists (using mock data if needed initially)

**UI Wireframe Reference (from project plan):**
```
+------------------------------------------+
| Dailies Checklist                   [_][X]|
+------------------------------------------+
| v Daily Activities          [Reset All]  |
|   [ ] Gold Saucer - Mini Cactpot (0/3)   |
|   [x] Duty Finder - Leveling Roulette *  |
+------------------------------------------+
| v Weekly Activities         [Reset All]  |
|   [ ] Gold Saucer - Jumbo Cactpot (0/3)  |
+------------------------------------------+
| [Settings]              Last reset: 2h ago|
+------------------------------------------+
* = Auto-detected
```

---

### Agent 4: Core Services (Reset & Persistence)

**Focus:** Reset timer logic and state persistence

**Reference Documentation:**
- `docs/dailies-checklist-project-plan.md` - Sections 3.3 (Reset Handling), 4.3 (State Persistence)
- `docs/ff14-dailies-research.md` - Reset Times Overview table
- `docs/dalamud-api-reference-guide.md` - Section 8 (Configuration Persistence)

**Tasks:**

| ID | Task | Deliverable | Acceptance Criteria |
|----|------|-------------|---------------------|
| A4.1 | Create ResetService.cs | Reset timer management | Tracks daily, weekly, and GC reset times |
| A4.2 | Implement GetNextDailyReset() | DateTime calculation | Returns next 15:00 UTC |
| A4.3 | Implement GetNextWeeklyReset() | DateTime calculation | Returns next Tuesday 08:00 UTC |
| A4.4 | Implement GetNextGCReset() | DateTime calculation | Returns next 20:00 UTC |
| A4.5 | Implement CheckAndApplyResets() | Reset detection | Compares last reset times, resets appropriate tasks |
| A4.6 | Create PersistenceService.cs | Save/load state | JSON serialization via Dalamud config system |
| A4.7 | Implement debounced save | Save on change with delay | 2-second debounce to prevent excessive writes |
| A4.8 | Implement load with migration | Version-aware loading | Handle missing/corrupt config gracefully |

**Dependencies:** Agent 2 (needs ChecklistState model)

**Output:** Services that can track resets and persist state between sessions

**Reset Time Reference (from ff14-dailies-research.md):**
| Reset Type | UTC Time | Notes |
|------------|----------|-------|
| Daily | 15:00 | Roulettes, Beast Tribes, Mini Cactpot |
| Grand Company | 20:00 | Supply/Provisioning (different from daily!) |
| Weekly | Tuesday 08:00 | Raids, Challenge Log, Custom Deliveries |
| Jumbo Cactpot | Saturday 08:00 | Drawing time |

---

### Agent 5: Detection Framework

**Focus:** Auto-detection architecture and initial detectors

**Reference Documentation:**
- `docs/dailies-checklist-project-plan.md` - Section 3.2 (Auto-Detection Features), Section 4.5 (Module Structure)
- `docs/dalamud-api-reference-guide.md` - Sections 2 (Core Services), 6 (Event System)
- `docs/ff14-dailies-research.md` - "Detection Potential" ratings for each activity, Section 20 (Programmatic Detection Notes)

**Tasks:**

| ID | Task | Deliverable | Acceptance Criteria |
|----|------|-------------|---------------------|
| A5.1 | Create ITaskDetector.cs | Detector interface | Methods: CanDetect(taskId), GetCompletionState(), Initialize(), Dispose() |
| A5.2 | Create DetectionService.cs | Detector orchestration | Manages detector registration, polls/subscribes for updates |
| A5.3 | Implement detector registration | Plugin-style pattern | AddDetector<T>(), RemoveDetector(), GetDetector(taskId) |
| A5.4 | Create RouletteDetector.cs stub | Duty roulette detection | Injects IDutyState, subscribes to DutyCompleted event |
| A5.5 | Create CactpotDetector.cs stub | Gold Saucer detection | Stub with TODO for Mini/Jumbo Cactpot tracking |
| A5.6 | Create BeastTribeDetector.cs stub | Tribal quest detection | Stub with TODO for allowance tracking |
| A5.7 | Implement feature flags | Enable/disable detectors | Per-detector enable flag, graceful degradation |
| A5.8 | Add detection event | State change notification | Event fired when detector updates task completion |

**Dependencies:** Agent 2 (needs task model for completion state)

**Output:** Detection framework ready for Phase 2 implementation with working stubs

**Detection Feasibility Reference (from ff14-dailies-research.md):**
| Activity | Detection Potential | Notes |
|----------|---------------------|-------|
| Duty Roulettes | HIGH | Via IDutyState events |
| Mini/Jumbo Cactpot | HIGH | Gold Saucer game state |
| Beast Tribes | HIGH | Quest allowance tracking |
| Custom Deliveries | HIGH | Delivery allowance counters |
| Wondrous Tails | HIGH | Journal state tracking |
| Daily Hunts | MEDIUM | Bill pickup detection (not kills) |
| Challenge Log | MEDIUM | Reading completion percentages |

---

## Integration Points

After parallel development, integration requires:

1. **Plugin.cs integration** (Agent 1 receives from all):
   - Instantiate TaskRegistry (Agent 2)
   - Create MainWindow/SettingsWindow (Agent 3)
   - Initialize ResetService, PersistenceService (Agent 4)
   - Initialize DetectionService (Agent 5)

2. **MainWindow integration** (Agent 3 consumes):
   - TaskRegistry for task list (Agent 2)
   - ChecklistState for current state (Agent 2)
   - ResetService for "time since reset" display (Agent 4)

3. **Configuration binding** (Agent 1 provides to):
   - SettingsWindow for display (Agent 3)
   - PersistenceService for storage (Agent 4)

---

## Sprint Timeline

| Day | Agent 1 | Agent 2 | Agent 3 | Agent 4 | Agent 5 |
|-----|---------|---------|---------|---------|---------|
| 1 | A1.1-A1.3 | A2.1-A2.3 | A3.1-A3.2 | A4.1-A4.2 | A5.1-A5.2 |
| 2 | A1.4-A1.5 | A2.4-A2.5 | A3.3-A3.5 | A4.3-A4.5 | A5.3-A5.5 |
| 3 | A1.6-A1.7 | A2.6-A2.7 | A3.6-A3.8 | A4.6-A4.8 | A5.6-A5.8 |
| 4 | Integration | Integration | Integration | Integration | Integration |

---

## Success Criteria

### Sprint Complete When:

1. [ ] Plugin loads in Dalamud without errors
2. [ ] `/dailies` command toggles main window
3. [ ] Main window displays categorized task list
4. [ ] All 25+ tasks from project plan are defined
5. [ ] Manual check/uncheck works for all tasks
6. [ ] Settings window allows task enable/disable
7. [ ] State persists across game restarts
8. [ ] Reset logic correctly identifies daily/weekly boundaries
9. [ ] Detection framework is ready for Phase 2 detectors
10. [ ] Code follows Dalamud patterns from documentation

### Quality Gates:

- No compiler errors or warnings
- Proper disposal pattern in all classes
- Event handlers unregistered in Dispose()
- Null checks for game state access
- ImRaii used for ImGui state management

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Model mismatches during integration | Agents use exact property names from project plan Section 4.2 |
| Reset time calculation errors | Use UTC throughout, test with edge cases |
| ImGui rendering issues | Follow SamplePlugin patterns exactly |
| Service injection failures | Use static property injection pattern from code patterns guide |

---

## Approval

- [ ] Project Manager Review
- [ ] Technical Analyst Review (code patterns compliance)
- [ ] User Acceptance (feature scope alignment)

---

*Document Version: 1.0*
*Created: 2026-01-27*
*Status: Draft Proposal - Awaiting Approval*
