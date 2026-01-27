# DailiesChecklist Plugin - Functionality & Architecture Review

**Review Date:** 2026-01-27
**Reviewer:** Technical Analyst
**Plugin Version:** 1.0.0.0
**Overall Assessment:** CONDITIONAL PASS - Requires fixes before distribution

---

## Executive Summary

The DailiesChecklist plugin is **mostly functional** but contains several **architecture issues that will cause incorrect behavior** in production. The codebase demonstrates reasonable structure and proper Dalamud patterns, but ChatGPT's implementation has left behind:

1. **3 Critical issues** - Will cause crashes or data corruption
2. **7 High severity issues** - Will cause incorrect behavior users will notice
3. **10 Medium severity issues** - May cause subtle bugs or maintenance problems
4. **6 Low severity issues** - Code quality concerns

**Recommendation:** Fix all Critical and High issues before distributing to friends.

---

## Critical Issues (Must Fix Before Distribution)

### CRIT-01: Thread Safety Violation in Debounced Save
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/PersistenceService.cs`
**Lines:** 171-185

**Problem:** The `OnDebounceTimerElapsed` callback executes on a ThreadPool thread, but `SaveInternal` accesses the `ChecklistState` object which may be simultaneously modified by the UI thread.

**Impact:** Corrupted JSON save files, crashes during serialization, lost user data.

**Fix:** Clone the state before saving.

---

### CRIT-02: Static Plugin Reference in Configuration.Save()
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Configuration.cs`
**Line:** 134

**Problem:** `Configuration.Save()` directly accesses `Plugin.PluginInterface` which is a static property. If called before the plugin is fully initialized, this will throw a `NullReferenceException`.

**Impact:** Plugin crash during early initialization.

**Fix:** Pass `IDalamudPluginInterface` to Configuration constructor.

---

### CRIT-03: Initialization Order Race Condition
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
**Lines:** 185-193

**Problem:** `RegisterDetectors()` is called, then `ApplyResetsAndSyncDetectors()` immediately calls methods on those detectors. Detector initialization may not be complete.

**Impact:** Detectors receive reset calls before they're ready, causing null reference exceptions.

**Fix:** Delay detector synchronization or add "IsReady" checks.

---

## High Severity Issues

### HIGH-01: JumboCactpot Reset Uses Wrong Timestamp
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/ResetService.cs`
**Lines:** 257-265

**Problem:** Checks JumboCactpot reset against `state.LastWeeklyReset`, but JumboCactpot draws on **Saturday 08:00 UTC** while weekly reset is **Tuesday 08:00 UTC**.

**Impact:** JumboCactpot tickets appear reset on Tuesday instead of Saturday.

**Fix:** Add `LastJumboCactpotReset` to track separately.

---

### HIGH-02: ContentRouletteId May Be Cleared Before Event Handler
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs`
**Lines:** 173-233

**Problem:** The `DutyCompleted` event fires after the duty ends. By this time, `_dutyState.ContentRouletteId` may already be reset to 0, causing all roulette completions to be missed.

**Impact:** Roulette completions are never auto-detected.

**Fix:** Subscribe to `DutyStarted` to capture ContentRouletteId, store it, then use in `DutyCompleted`.

---

### HIGH-03: MiniCactpot Detection Double-Counts
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/CactpotDetector.cs`
**Lines:** 291-304

**Problem:** `OnMiniCactpotResultAddonSetup` fires every time the addon appears. If player reopens result screen, it counts as another ticket.

**Impact:** Mini Cactpot shows 3/3 complete after only 1 or 2 actual tickets.

**Fix:** Add timestamp debounce or unique identifier tracking.

---

### HIGH-04: Task Tooltip Attached to Wrong Element
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs`
**Lines:** 362-378

**Problem:** Tooltip logic uses `ImGui.IsItemHovered()` after rendering the count text, so tooltip only shows when hovering count, not task name.

**Impact:** Users can't see task descriptions by hovering task name.

**Fix:** Track row position and use custom hover check.

---

### HIGH-05: SettingsWindow.ResetTasksToDefaults Breaks State Sync
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs`
**Lines:** 501-519

**Problem:** When resetting to defaults, a new `List<ChecklistTask>` is assigned to `_externalState.Tasks`. MainWindow still holds reference to OLD ChecklistState.

**Impact:** After reset, MainWindow shows old tasks while SettingsWindow shows new tasks.

**Fix:** Call `mainWindow.SetChecklistState()` after modifying.

---

### HIGH-06: Null Check Race Condition in Plugin.Dispose()
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
**Lines:** 282-293

**Problem:** Code checks `PersistenceService != null && ChecklistState != null` then uses them in separate statements. Could become null between check and use.

**Impact:** Crash during plugin shutdown.

**Fix:** Capture references locally before null check.

---

### HIGH-07: Detector Event Handler Cleanup on Exception
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/DetectionService.cs`
**Lines:** 113-149

**Problem:** If `detector.Initialize()` throws, event handler may have been subscribed but cleanup state is inconsistent.

**Impact:** Memory leaks from orphaned event handlers.

**Fix:** Move event subscription AFTER successful initialization.

---

## Medium Severity Issues

### MED-01: Static Mutable State in UIHelpers
**File:** `UIHelpers.cs:397`
`_popupOpen` is static, causing interference between popups.

### MED-02: Unused Constructor Parameters in Detectors
**Files:** `CactpotDetector.cs` (`_gameGui`), `BeastTribeDetector.cs` (`_framework`)
Constructor requires services that are never used.

### MED-03: Inefficient TaskRegistry.GetTaskById()
**File:** `TaskRegistry.cs:459-463`
Creates entire default task list just to find one task.

### MED-04: PersistenceService IDalamudConfigProvider Never Used
**File:** `PersistenceService.cs:77-81`
Dead code path.

### MED-05: Configuration.FeatureFlags Not Versioned
**File:** `Configuration.cs:11-21, 124`
Adding new flags will cause them to default to `false`.

### MED-06: Global Using Alias Confusion
**File:** `Global.cs:46`
`Log` alias creates confusion with `Plugin.Log`.

### MED-07: No Cancellation Token Support
**Files:** Multiple service classes
Services don't support cancellation during shutdown.

### MED-08: Empty Catch Block Swallows Errors
**File:** `MainWindow.cs:432-439`
No logging in catch block.

### MED-09: MaximumSize Uses float.MaxValue
**File:** `MainWindow.cs:69-73`
Could cause rendering issues.

### MED-10: ImRaii.Child Success Check Missing Log
**File:** `SettingsWindow.cs:262`
Silent failure when child region fails.

---

## Low Severity Issues

### LOW-01: Dead Code - EnsureUtc Method
**File:** `ResetService.cs:356-378`
Method defined but never called.

### LOW-02: Missing Initial State Query Implementation
**Files:** All detector files
TODO comments indicate incomplete implementation.

### LOW-03: Clone() Uses Object Initializer
**File:** `ChecklistTask.cs:85-103`
Less performant than MemberwiseClone for frequent cloning.

### LOW-04: LoadAsync Uses Task.Run
**File:** `PersistenceService.cs:312-315`
Synchronous work wrapped in Task.Run wastes thread pool thread.

### LOW-05: Missing Null Validation in Window Constructors
**Files:** MainWindow.cs, SettingsWindow.cs
Some nullable parameters not validated.

### LOW-06: Inconsistent Property Accessors
**File:** `Plugin.cs:129`
`ChecklistState` has `{ get; set; }` while others use `{ get; init; }`.

---

## Suggested Test Scenarios

1. **Save/Load Cycle Test** - Rapidly modify tasks, verify persistence
2. **Daily Reset Test** - Test at 15:00 UTC boundary
3. **Roulette Detection Test** - Complete duty, verify detection (expected FAIL until HIGH-02 fixed)
4. **Mini Cactpot Test** - Play ticket, verify count (expected FAIL until HIGH-03 fixed)
5. **Settings Sync Test** - Reset to defaults, verify main window updates (expected FAIL until HIGH-05 fixed)
6. **Shutdown Save Test** - Kill process, verify state saved

---

## Files Reviewed

| File | Lines | Status |
|------|-------|--------|
| Plugin.cs | 569 | 3 issues found |
| Configuration.cs | 136 | 2 issues found |
| Core/Global.cs | 47 | 1 issue found |
| Models/ChecklistTask.cs | 105 | 1 issue found |
| Models/ChecklistState.cs | 155 | Clean |
| Models/Enums.cs | 47 | Clean |
| Services/ResetService.cs | 429 | 2 issues found |
| Services/PersistenceService.cs | 538 | 4 issues found |
| Services/TaskRegistry.cs | 480 | 1 issue found |
| Detectors/DetectionService.cs | 443 | 1 issue found |
| Detectors/ITaskDetector.cs | 74 | Clean |
| Detectors/RouletteDetector.cs | 394 | 2 issues found |
| Detectors/CactpotDetector.cs | 537 | 2 issues found |
| Detectors/BeastTribeDetector.cs | 392 | 1 issue found |
| Windows/MainWindow.cs | 543 | 3 issues found |
| Windows/SettingsWindow.cs | 580 | 2 issues found |
| Utils/UIHelpers.cs | 410 | 1 issue found |

---

**End of Report**
