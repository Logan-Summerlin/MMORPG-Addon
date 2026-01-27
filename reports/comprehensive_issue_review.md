# DailiesChecklist Comprehensive Issue Review

**Date:** 2026-01-27  
**Reviewer:** Internal Verification (Plugin Maintainer)  
**Scope:** Validation of all reported issues in `/reports` plus additional codebase findings.  
**Codebase Reviewed:** `/workspace/MMORPG-Addon/DailiesChecklist`

---

## Executive Summary

This report validates **all issues** raised in the analyst reports and adds **additional findings** from a fresh codebase review. Each item is classified as:

- **Valid**: Confirmed in code and likely to occur.
- **Plausible**: Possible but depends on runtime behavior or environment; needs verification.
- **Incorrect**: Not supported by code or overstated.

Severity labels below reflect **real-world impact** for a safe, ToS-compliant FFXIV plugin distributed to friends.

---

## Validation of Analyst Reports (All Severities)

### Functionality & Architecture Report

#### Critical Issues

1. **CRIT-01: Thread safety violation in debounced save**  
   **Status:** **Valid (High)**  
   **Why:** `Timer` callback serializes `ChecklistState` without cloning while UI can mutate it. This is a real data race risk, potentially corrupting saves.  
   **Evidence:** `PersistenceService.OnDebounceTimerElapsed` and `SaveInternal` serialize shared mutable state without locking beyond swapping the reference.【F:DailiesChecklist/Services/PersistenceService.cs†L140-L232】

2. **CRIT-02: Static Plugin reference in Configuration.Save()**  
   **Status:** **Plausible but Overstated (Low–Medium)**  
   **Why:** `Configuration.Save()` calls `Plugin.PluginInterface` statically. If called before plugin initialization, it could throw. In practice, `Save()` is called from UI after initialization, so this is mostly a design/testability concern.  
   **Evidence:** `Configuration.Save()` references `Plugin.PluginInterface` directly.【F:DailiesChecklist/Configuration.cs†L128-L135】

3. **CRIT-03: Initialization order race condition**  
   **Status:** **Incorrect**  
   **Why:** `DetectionService.AddDetector()` calls `detector.Initialize()` synchronously, so detectors are fully initialized before `ApplyResetsAndSyncDetectors()` runs.  
   **Evidence:** `AddDetector` initializes before returning; plugin calls `ApplyResetsAndSyncDetectors()` after registration.【F:DailiesChecklist/Detectors/DetectionService.cs†L91-L129】【F:DailiesChecklist/Plugin.cs†L183-L193】

#### High Severity Issues

1. **HIGH-01: Jumbo Cactpot reset uses wrong timestamp**  
   **Status:** **Valid (High)**  
   **Why:** Jumbo reset is keyed off `LastWeeklyReset` (Tuesday) rather than Saturday.  
   **Evidence:** Jumbo reset check uses `state.LastWeeklyReset` instead of a dedicated Jumbo timestamp.【F:DailiesChecklist/Services/ResetService.cs†L243-L265】

2. **HIGH-02: `ContentRouletteId` may be cleared before `DutyCompleted`**  
   **Status:** **Plausible (Medium–High)**  
   **Why:** The detector relies on `IDutyState.ContentRouletteId` during `DutyCompleted`. If Dalamud clears this value before the event fires, detection will fail. This is runtime-dependent.  
   **Evidence:** `DetectRouletteCompletion` reads `ContentRouletteId` inside `DutyCompleted` handler.【F:DailiesChecklist/Detectors/Detectors/RouletteDetector.cs†L168-L233】

3. **HIGH-03: Mini Cactpot detection double-counts**  
   **Status:** **Valid (High)**  
   **Why:** Each time `MiniCactpotResult` addon appears, `RecordMiniCactpotPlay()` increments the count. Reopening the result screen will double-count.  
   **Evidence:** No debounce or uniqueness check in addon setup handler.【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L285-L415】

4. **HIGH-04: Task tooltip attached to wrong element**  
   **Status:** **Valid but Low Impact (Low–Medium)**  
   **Why:** Tooltip uses `ImGui.IsItemHovered()` after drawing the last item in the row (count or asterisk). Hovering task text may not show tooltip.  
   **Evidence:** Tooltip logic is tied to the last rendered item in `DrawTaskRow`.【F:DailiesChecklist/Windows/MainWindow.cs†L318-L377】

5. **HIGH-05: Settings reset breaks state sync**  
   **Status:** **Incorrect**  
   **Why:** Settings uses `_externalState.Tasks = defaultTasks` which updates the shared `ChecklistState`. MainWindow reads from the same instance.  
   **Evidence:** Shared state is passed to both windows, and reset writes to that shared state.【F:DailiesChecklist/Windows/SettingsWindow.cs†L501-L519】【F:DailiesChecklist/Plugin.cs†L195-L205】

6. **HIGH-06: Null check race in `Plugin.Dispose()`**  
   **Status:** **Incorrect**  
   **Why:** `PersistenceService` and `ChecklistState` are never set to null post-initialization. There is no concurrent nulling here; null checks are redundant but harmless.  
   **Evidence:** Services are created once and never reassigned to null.【F:DailiesChecklist/Plugin.cs†L260-L307】

7. **HIGH-07: Detector event handler cleanup on exception**  
   **Status:** **Mostly Incorrect / Mitigated**  
   **Why:** On detector initialization failure, the code unsubscribes handlers and removes mappings. It is not a leak.  
   **Evidence:** Catch block removes event handlers and mappings when initialization fails.【F:DailiesChecklist/Detectors/DetectionService.cs†L112-L149】

#### Medium Severity Issues

1. **MED-01: Static mutable state in UIHelpers**  
   **Status:** **Valid (Low–Medium)**  
   **Why:** `_popupOpen` is static; multiple popups could interfere with each other.  
   **Evidence:** `_popupOpen` is a static field used for all modals.【F:DailiesChecklist/Utils/UIHelpers.cs†L365-L407】

2. **MED-02: Unused constructor parameters in detectors**  
   **Status:** **Valid (Low)**  
   **Why:** `_framework` in `BeastTribeDetector` and `_framework`/`_gameGui` in `CactpotDetector` are currently unused.  
   **Evidence:** Fields are assigned but not referenced elsewhere.【F:DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs†L35-L77】【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L34-L45】

3. **MED-03: Inefficient `TaskRegistry.GetTaskById()`**  
   **Status:** **Valid (Low)**  
   **Why:** Each call creates a full default list.  
   **Evidence:** `GetTaskById` calls `GetDefaultTasks()` every time.【F:DailiesChecklist/Services/TaskRegistry.cs†L455-L478】

4. **MED-04: `IDalamudConfigProvider` never used in plugin**  
   **Status:** **Valid (Low)**  
   **Why:** The plugin uses file-path persistence and does not instantiate `PersistenceService` with `IDalamudConfigProvider`.  
   **Evidence:** Plugin passes file path to `PersistenceService` rather than provider.【F:DailiesChecklist/Plugin.cs†L164-L167】

5. **MED-05: Feature flags not versioned**  
   **Status:** **Plausible (Low)**  
   **Why:** New flags may default to `false` or default initialization values without migrations.  
   **Evidence:** No explicit migration logic in `Configuration` beyond `Version` property.【F:DailiesChecklist/Configuration.cs†L22-L124】

6. **MED-06: Global using alias confusion**  
   **Status:** **Valid (Low)**  
   **Why:** `global using Log = IPluginLog` is unused, and the code uses `Plugin.Log` directly.  
   **Evidence:** `Log` alias is declared but unused.【F:DailiesChecklist/Core/Global.cs†L41-L46】

7. **MED-07: No cancellation token support in services**  
   **Status:** **Valid (Low)**  
   **Why:** Services are synchronous except `LoadAsync`, which accepts a token but does not use it internally beyond `Task.Run`.  
   **Evidence:** `LoadAsync` passes token but internal operations are not cancellable.【F:DailiesChecklist/Services/PersistenceService.cs†L300-L308】

8. **MED-08: Empty catch block**  
   **Status:** **Valid (Low–Medium)**  
   **Why:** Silent failures hide errors when computing reset text.  
   **Evidence:** Empty catch in `MainWindow.GetLastResetText()`.【F:DailiesChecklist/Windows/MainWindow.cs†L429-L440】

9. **MED-09: `MaximumSize` uses `float.MaxValue`**  
   **Status:** **Valid (Low)**  
   **Why:** Large max sizes can produce poor UI behavior, but it’s not dangerous.  
   **Evidence:** Maximum size uses `float.MaxValue` for both dimensions.【F:DailiesChecklist/Windows/MainWindow.cs†L68-L73】

10. **MED-10: ImRaii.Child failure not logged**  
    **Status:** **Valid (Low)**  
    **Why:** Child window failure is silent; no logging.  
    **Evidence:** Child is used without a failure path log.【F:DailiesChecklist/Windows/SettingsWindow.cs†L245-L285】

#### Low Severity Issues

1. **LOW-01: Dead code `EnsureUtc`**  
   **Status:** **Valid (Low)**  
   **Evidence:** `EnsureUtc` is unused anywhere.【F:DailiesChecklist/Services/ResetService.cs†L357-L378】

2. **LOW-02: TODOs for detector initial state**  
   **Status:** **Valid (Medium usability impact)**  
   **Why:** The plugin only tracks within-session completions. Users will see incomplete state after relog.  
   **Evidence:** Explicit TODO and log messages across detectors.【F:DailiesChecklist/Detectors/Detectors/RouletteDetector.cs†L292-L313】【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L111-L189】【F:DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs†L145-L208】

---

### Security & Compliance Report

1. **M-001: File path not validated for traversal**  
   **Status:** **Valid but Low Risk**  
   **Why:** `PersistenceService` accepts any path string. In production the path comes from `ConfigDirectory`, so risk is low.  
   **Evidence:** Constructor accepts arbitrary path without canonicalization or sandbox checks.【F:DailiesChecklist/Services/PersistenceService.cs†L83-L99】

2. **M-002: Character data stored without disclosure**  
   **Status:** **Valid (Low)**  
   **Why:** Character ID and name are stored persistently. This is not unsafe but should be disclosed for transparency.  
   **Evidence:** Character fields in `ChecklistState` are serialized as part of persistence.【F:DailiesChecklist/Models/ChecklistState.cs†L50-L59】

3. **L-001: Timer callback during disposal**  
   **Status:** **Plausible**  
   **Why:** The timer could fire while `Dispose()` is running since it is not synchronized.  
   **Evidence:** `Dispose()` does not lock around timer disposal; callback uses shared state.【F:DailiesChecklist/Services/PersistenceService.cs†L486-L518】

4. **L-002: No bounds on task list size**  
   **Status:** **Valid (Low)**  
   **Why:** Loaded config could contain arbitrarily large task lists. There is no list size cap or sanity limit.  
   **Evidence:** `ValidateAndRepair` checks tasks but does not enforce max count or size limits.【F:DailiesChecklist/Services/PersistenceService.cs†L345-L420】

5. **L-003: Detector TODOs for incomplete implementations**  
   **Status:** **Valid (Medium usability)**  
   **Evidence:** Multiple TODOs across detectors for missing initial state detection.【F:DailiesChecklist/Detectors/Detectors/RouletteDetector.cs†L292-L313】【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L111-L189】【F:DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs†L145-L208】

6. **L-004: Static mutable state in UIHelpers**  
   **Status:** **Valid (Low)**  
   **Evidence:** `_popupOpen` is static and shared across all popups.【F:DailiesChecklist/Utils/UIHelpers.cs†L365-L407】

7. **L-005: Empty catch block**  
   **Status:** **Valid (Low–Medium)**  
   **Evidence:** Silent exception handling in `MainWindow.GetLastResetText()`.【F:DailiesChecklist/Windows/MainWindow.cs†L429-L440】

---

### Style & Syntax Report

The style report includes many valid maintainability concerns but overstates criticality. Confirmed highlights:

- **Silent catch block** (valid; low–medium).【F:DailiesChecklist/Windows/MainWindow.cs†L429-L440】
- **Unused `EnsureUtc`** (valid; low).【F:DailiesChecklist/Services/ResetService.cs†L357-L378】
- **Unused injected fields** (valid; low).【F:DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs†L35-L77】【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L34-L45】
- **Inefficient `TaskRegistry` caching** (valid; low).【F:DailiesChecklist/Services/TaskRegistry.cs†L440-L478】
- **Static popup state** (valid; low–medium).【F:DailiesChecklist/Utils/UIHelpers.cs†L365-L407】
- **Global alias unused** (valid; low).【F:DailiesChecklist/Core/Global.cs†L41-L46】
- **Inconsistent namespace syntax in `Enums.cs`** (valid; cosmetic).【F:DailiesChecklist/Models/Enums.cs†L1-L41】

Incorrect or overstated items:

- **`_isInGoldSaucer` unused** — **Incorrect**; the field is used to track enter/leave transitions.【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L168-L205】

---

## Additional Findings (New Issues Not in Reports)

1. **ChecklistState shared mutable state is saved without cloning**  
   **Status:** **Valid (High)**  
   **Why:** Save uses the same mutable object without copying. This is the core of the thread safety issue, and can also lead to inconsistent snapshot saves.  
   **Evidence:** `SaveInternal` serializes the passed object directly; UI continues mutating it.【F:DailiesChecklist/Services/PersistenceService.cs†L190-L215】

2. **No validation that task IDs in detectors exist in registry**  
   **Status:** **Plausible (Low)**  
   **Why:** The detector list is hard-coded. There is no assertion that these IDs exist in task registry. If registry changes, detection could silently break.  
   **Evidence:** IDs are maintained separately in `TaskRegistry` and `RouletteDetector`/`CactpotDetector`/`BeastTribeDetector`.【F:DailiesChecklist/Services/TaskRegistry.cs†L48-L432】【F:DailiesChecklist/Detectors/Detectors/RouletteDetector.cs†L40-L74】

3. **`ChecklistState.Tasks` default list can be replaced without reconciling completed/metadata**  
   **Status:** **Valid (Low–Medium)**  
   **Why:** Resetting to defaults completely replaces the list, dropping any per-task user metadata (manual overrides, etc.). This is a UX issue rather than a crash bug.  
   **Evidence:** `ResetTasksToDefaults()` overwrites `Tasks` with a new list.【F:DailiesChecklist/Windows/SettingsWindow.cs†L501-L519】

4. **Feature flags lack migration or schema compatibility logic**  
   **Status:** **Plausible (Low)**  
   **Why:** `Configuration.Version` exists but no migration or default handling for new flags. This can lead to newly added flags defaulting to `false` unexpectedly.  
   **Evidence:** `Configuration` version is not used elsewhere; no migration function exists.【F:DailiesChecklist/Configuration.cs†L22-L124】

5. **`GetFormattedTimeUntilReset` fallback hides errors without any log context**  
   **Status:** **Valid (Low)**  
   **Why:** If the reset service throws, there’s no logging and the code falls back silently, hiding the root cause.  
   **Evidence:** Empty catch in `GetLastResetText()`.【F:DailiesChecklist/Windows/MainWindow.cs†L429-L440】

---

## Summary of Confirmed Real Issues (Actionable)

**High Priority (Fix Before Distribution):**

- Debounced save thread-safety and inconsistent snapshotting.【F:DailiesChecklist/Services/PersistenceService.cs†L140-L232】
- Jumbo Cactpot reset uses weekly reset timestamp (wrong day).【F:DailiesChecklist/Services/ResetService.cs†L243-L265】
- Mini Cactpot detection can double-count tickets.【F:DailiesChecklist/Detectors/Detectors/CactpotDetector.cs†L285-L415】
- Detector initial state detection is unimplemented (session-only detection).【F:DailiesChecklist/Detectors/Detectors/RouletteDetector.cs†L292-L313】

**Medium/Low Priority:**

- Silent exception handling in UI (no logging).【F:DailiesChecklist/Windows/MainWindow.cs†L429-L440】
- Static popup state in `UIHelpers` can cause modal interference.【F:DailiesChecklist/Utils/UIHelpers.cs†L365-L407】
- Inefficient TaskRegistry list creation per lookup.【F:DailiesChecklist/Services/TaskRegistry.cs†L440-L478】
- Unused injected dependencies in detectors (cleanup or implement).【F:DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs†L35-L77】
- Lack of path validation in `PersistenceService` (low risk in production).【F:DailiesChecklist/Services/PersistenceService.cs†L83-L99】

---

## Closing Notes

The plugin is **ToS-compliant** and **non-automating**, but several functional correctness issues exist. The most impactful for your friends are the Cactpot reset mismatch, Mini Cactpot double-counting, and the save-thread race. Addressing these first will significantly improve reliability and user trust.
