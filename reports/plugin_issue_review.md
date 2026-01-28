# Dailies Checklist Plugin - Issue Review (Valid/Plausible/Possible)

This document consolidates **valid**, **plausible**, and **possible** issues found in the current Dailies Checklist plugin codebase. It is intentionally conservative and focuses on anything that could realistically impact correctness, user experience, or maintainability.

> Scope: This review is based on the current code in the repository and is **not** a response to prior reports. It stands alone as a checklist of issues to address or confirm.

## ✅ Confirmed Issues (High Confidence)

### 1) Tooltip hover target is misleading (UX bug)
**File:** `DailiesChecklist/Windows/MainWindow.cs`

The tooltip for a task description is tied to the *last drawn ImGui item* (often the count text or auto-detect indicator), not the task label itself. Users hovering the task name may not see the tooltip. This is visible when tasks have count text or detection indicators.

**Impact:** Confusing UI; users think tooltips are broken or missing.

**Suggested fix:** Capture hover state immediately after rendering the task text or use `ImGui.IsItemHovered()` on an invisible selectable overlay covering the row.

---

### 2) Feature-completeness gaps for auto-detection (TODOs that affect behavior)
**Files:**
- `DailiesChecklist/Detectors/Detectors/RouletteDetector.cs`
- `DailiesChecklist/Detectors/Detectors/CactpotDetector.cs`
- `DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs`

Several detectors explicitly state that initial state detection is not implemented yet (e.g., pulling current completion state on login), and Jumbo Cactpot purchase detection is intentionally skipped. This means the plugin can **miss already-completed activities** and may require manual toggles until the user does a new action while the plugin is running.

**Impact:** Users can see incomplete tasks even after completing them earlier in the day/week.

**Suggested fix:** Implement the TODO logic or add clear UI messaging indicating that detection only works *after* activity happens during the current session.

---

### 3) Character data persisted without explicit user disclosure
**File:** `DailiesChecklist/Models/ChecklistState.cs`

The plugin stores `CharacterId` and `CharacterName` persistently. The manifest description does not explicitly mention that per‑character identifiers are stored in local config data.

**Impact:** Potential trust/compliance concern for users who expect minimal data collection.

**Suggested fix:** Update the plugin description to mention stored character identifiers and add a “clear local data” action in settings.

---

## ⚠️ Plausible Issues (Moderate Confidence)

### 4) Reset-sync timing depends on detector initialization behavior
**File:** `DailiesChecklist/Plugin.cs`

Reset application is triggered immediately after detector registration. If a detector’s `Initialize()` subscribes to game events asynchronously or depends on game state that is not yet ready, the reset may be applied before it can respond to reset calls.

**Impact:** Possible missed reset signals in edge cases (e.g., immediately after plugin load).

**Suggested fix:** Add explicit detector readiness flags or run reset sync after first framework tick.

---

### 5) Settings reset may desync the main view in some flows
**File:** `DailiesChecklist/Windows/SettingsWindow.cs`

Resetting tasks in the settings window replaces the task list object (`_externalState.Tasks = defaultTasks`), which is safe if all views reference the external state directly. However, if any UI component cached the old list reference before reset, it could display stale data.

**Impact:** Inconsistent UI state in edge cases.

**Suggested fix:** Ensure all views rebind to the external state list after reset or use a mutation‑based reset that keeps the list instance.

---

### 6) Dispose-time save sequence is fragile if services are nulled
**File:** `DailiesChecklist/Plugin.cs`

Dispose uses instance members without first caching them locally. If future changes allow partial initialization or disposal during failure recovery, access patterns could become unsafe.

**Impact:** Potential shutdown crash in edge cases.

**Suggested fix:** Capture references locally before using them in `Dispose()`.

---

## ❓ Possible Issues (Lower Confidence / Defensive Hardening)

### 7) Path validation for PersistenceService (defensive hardening)
**File:** `DailiesChecklist/Services/PersistenceService.cs`

The file-based constructor accepts a raw path with no validation. The current usage passes a safe path from `PluginInterface.ConfigDirectory`, but if reused elsewhere it could be abused or misused.

**Impact:** Risk in future re-use or tests; not currently exploitable in normal plugin use.

**Suggested fix:** Normalize and restrict paths to the plugin’s config directory.

---

### 8) No bounds on task list size in persisted state
**File:** `DailiesChecklist/Models/ChecklistState.cs`

If the config file is corrupted or manually edited, the task list could contain an extremely large number of entries, which could impact performance or memory usage.

**Impact:** Performance issues or high memory usage on load.

**Suggested fix:** Clamp task count during validation, or fallback to defaults if list size exceeds a reasonable limit.

---

### 9) Task registry list copying may be unnecessarily expensive
**File:** `DailiesChecklist/Services/TaskRegistry.cs`

Multiple accessors return freshly cloned lists every call. This is fine for small lists, but repeated calls during UI rendering could create unnecessary allocations.

**Impact:** Minor performance overhead.

**Suggested fix:** Cache immutable default lists and only clone when the caller intends to mutate.

---

### 10) Static popup state can retain stale IDs
**File:** `DailiesChecklist/Utils/UIHelpers.cs`

Popup state is stored in a static dictionary keyed by IDs. If IDs are generated dynamically or reused inconsistently, popup state can persist longer than expected.

**Impact:** Rare UI oddities (e.g., popup not opening because stale closed state persists).

**Suggested fix:** Provide a cleanup path or use instance-scoped popup state per window.

---

## Notes & Next Steps

- The current code already addresses several common issues (debounced save snapshots, roulette ID caching, mini cactpot debounce). These should not be re‑flagged unless regressions appear.
- Focus on the high‑confidence UX issues and TODO detection gaps first for maximum player-facing improvement.
- Consider adding explicit UI messaging for detection limitations to set player expectations.

