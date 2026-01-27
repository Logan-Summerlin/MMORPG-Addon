# DailiesChecklist Plugin - Style, Syntax & Code Quality Report

**Reviewer:** Technical Analyst
**Date:** 2026-01-27
**Files Reviewed:** 17 C# source files + project files

---

## Executive Summary

**VERDICT: CONDITIONAL PASS WITH MANDATORY FIXES**

The DailiesChecklist plugin demonstrates reasonable overall code quality with good documentation practices and consistent structure. However, several issues require attention before distribution:

- **2 Critical Issues** - Must be fixed (silent exception handling, dead code affecting maintainability)
- **7 High Issues** - Should be fixed (inefficiency, unused code, improper patterns)
- **15 Medium Issues** - Recommended fixes (style inconsistencies, redundant code)
- **12 Low Issues** - Minor improvements (naming, documentation gaps)

The code will likely compile and run, but contains dead code, inefficient patterns, and style inconsistencies that will confuse future maintainers and could cause subtle bugs.

---

## Critical Issues (Must Fix)

### CRIT-001: Silent Exception Swallowing
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs`
**Line:** 437-439
**Severity:** CRITICAL

```csharp
catch
{
    // Fall through to legacy calculation on error
}
```

**Problem:** Empty catch block silently swallows all exceptions with no logging. This hides bugs and makes debugging nearly impossible.

**Fix:** At minimum, log the exception:
```csharp
catch (Exception ex)
{
    Plugin.Log.Warning(ex, "Failed to get reset time from ResetService, falling back to legacy calculation");
}
```

---

### CRIT-002: Dead Code - Unused Method
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/ResetService.cs`
**Lines:** 363-378
**Severity:** CRITICAL

**Problem:** The `EnsureUtc` method is **never called anywhere in the codebase**. Dead code increases maintenance burden and suggests incomplete refactoring.

**Fix:** Delete the method entirely, or use it where UTC validation is needed.

---

## High Issues (Should Fix)

### HIGH-001: Unused Field - Injected Dependency Never Used
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs`
**Line:** 37
**Severity:** HIGH

```csharp
private readonly IFramework _framework;
```

**Problem:** `_framework` is injected but never used. Wastes memory and confuses readers.

**Fix:** Remove the field and constructor parameter, or implement the planned usage.

---

### HIGH-002: Dead Code - Unused Type Alias
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Core/Global.cs`
**Line:** 46
**Severity:** HIGH

```csharp
global using Log = Dalamud.Plugin.Services.IPluginLog;
```

**Problem:** This type alias `Log` is **never used anywhere**. All files use `IPluginLog` directly.

**Fix:** Remove the unused alias.

---

### HIGH-003: Severe Inefficiency - Repeated Task List Generation
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/TaskRegistry.cs`
**Lines:** 440-478
**Severity:** HIGH

```csharp
public static int TotalTaskCount => GetDefaultTasks().Count;
public static int DailyTaskCount => GetDailyTasks().Count;
public static int WeeklyTaskCount => GetWeeklyTasks().Count;
```

**Problem:** These properties call `GetDefaultTasks()` which creates **27+ new ChecklistTask objects** every single access.

**Fix:** Cache the default tasks with lazy initialization.

---

### HIGH-004: Static Mutable State in Utility Class
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs`
**Line:** 397
**Severity:** HIGH

```csharp
private static bool _popupOpen = true;
```

**Problem:** Static mutable state causes interference between multiple popups. Not thread-safe.

**Fix:** Pass state as parameter or use instance-based approach.

---

### HIGH-005: Public Readonly Field Instead of Property
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
**Line:** 99
**Severity:** HIGH

```csharp
public readonly WindowSystem WindowSystem = new("DailiesChecklist");
```

**Problem:** Public field instead of property. Fields cannot have logic added and break binary compatibility.

**Fix:** Convert to auto-property: `public WindowSystem WindowSystem { get; } = new("DailiesChecklist");`

---

### HIGH-006: Inconsistent Namespace Syntax
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Models/Enums.cs`
**Line:** 1
**Severity:** HIGH

```csharp
namespace DailiesChecklist.Models
{
```

**Problem:** Uses block-scoped namespace while ALL other files use file-scoped namespace syntax.

**Fix:** Convert to file-scoped: `namespace DailiesChecklist.Models;`

---

### HIGH-007: Non-Nullable Field Assigned from Nullable Expression
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs`
**Line:** 35, 58
**Severity:** HIGH

**Problem:** `_taskList` declared non-nullable but checked for null on line 483. Contradictory.

**Fix:** Be consistent - either field can be null (make nullable) or cannot (remove null checks).

---

## Medium Issues (Recommended Fixes)

### MED-001: Redundant Using Directives
**Files:** Multiple files
**Severity:** MEDIUM

Files have redundant `using` directives already provided by `Global.cs`:
- ChecklistTask.cs:1 - `using System;`
- ChecklistState.cs:1-4 - `using System; using System.Collections.Generic; using System.Linq;`
- DetectionService.cs:1-4 - All standard usings
- RouletteDetector.cs:1-3
- CactpotDetector.cs:1-5
- BeastTribeDetector.cs:1-3
- ResetService.cs:1-2
- TaskRegistry.cs:1-2

---

### MED-002: Unused Event Handler Parameters
**Files:** Multiple detector files
**Severity:** MEDIUM

| File:Line | Method | Unused Parameters |
|-----------|--------|-------------------|
| RouletteDetector.cs:173 | `OnDutyCompleted` | `sender` |
| RouletteDetector.cs:324 | `OnLogout` | `type`, `code` |
| CactpotDetector.cs:291 | `OnMiniCactpotResultAddonSetup` | `type`, `args` |
| CactpotDetector.cs:320 | `OnJumboCactpotAddonSetup` | `type`, `args` |
| CactpotDetector.cs:454 | `OnLogout` | `type`, `code` |
| PersistenceService.cs:171 | `OnDebounceTimerElapsed` | `state` |

**Fix:** Use discard pattern: `private void OnDutyCompleted(object? _, ushort territoryType)`

---

### MED-003: Unnecessary Full Namespace Qualification
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
**Line:** 322
**Severity:** MEDIUM

```csharp
var argList = args.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
```

**Fix:** Use unqualified `StringSplitOptions`.

---

### MED-004: Unnecessary Full Namespace Qualification
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/PersistenceService.cs`
**Line:** 332
**Severity:** MEDIUM

```csharp
Tasks = new System.Collections.Generic.List<ChecklistTask>()
```

**Fix:** Use unqualified `List<ChecklistTask>()`.

---

### MED-005: Inconsistent Object Initialization Syntax
**Files:** Multiple
**Severity:** MEDIUM

Some use `new()`, others use `new object()`. Pick one style.

---

### MED-006: Redundant Boolean Initialization
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Models/ChecklistTask.cs`
**Lines:** 49, 55
**Severity:** MEDIUM

```csharp
public bool IsCompleted { get; set; } = false;
public bool IsManuallySet { get; set; } = false;
```

**Problem:** `bool` defaults to `false`. Explicit initialization is redundant.

---

### MED-007: Redundant Integer Initialization
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Configuration.cs`
**Line:** 34
**Severity:** MEDIUM

```csharp
private int _version = 0;
```

---

### MED-008: Unnecessary else After Return
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/ResetService.cs`
**Lines:** 283-291
**Severity:** MEDIUM

```csharp
if (now < todayTarget)
{
    return todayTarget;
}
else
{
    return todayTarget.AddDays(1);
}
```

**Fix:** Remove unnecessary `else`.

---

### MED-009: Disposed Flag Set But Never Checked Before Disposal
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/ResetService.cs`
**Line:** 69, 380-390
**Severity:** MEDIUM

The `_disposed` flag is set but never checked before any operation.

---

### MED-010: Missing Null Validation in Clone Methods
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Models/ChecklistTask.cs`
**Lines:** 85-103
**Severity:** MEDIUM

---

### MED-011: Potential NullReferenceException
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs`
**Line:** 42, 66
**Severity:** MEDIUM

---

### MED-012: Unused Field in CactpotDetector
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/CactpotDetector.cs`
**Line:** 44
**Severity:** MEDIUM

```csharp
private bool _isInGoldSaucer;
```

Field is redundantly checked via territory ID comparison.

---

### MED-013: Inconsistent Null Check Patterns
**Files:** Multiple
**Severity:** MEDIUM

Some use `IsNullOrWhiteSpace`, others use `IsNullOrEmpty`.

---

### MED-014: Magic Numbers in UI Code
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs`
**Lines:** 53, 84, 137, 379, 387
**Severity:** MEDIUM

Values like `350f`, `18f`, `100` used without named constants.

---

### MED-015: TextUnformatted vs Text
**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs`
**Lines:** 330, 334
**Severity:** MEDIUM

`ImGui.Text()` parses format specifiers. Use `ImGui.TextUnformatted()` for dynamic text.

---

## Low Issues (Minor Improvements)

| ID | File | Line | Issue |
|----|------|------|-------|
| LOW-001 | ChecklistTask.cs | 33 | `Category` property has no explicit default |
| LOW-002 | ChecklistState.cs | 30,36,43 | Comments mention reset times but use `DateTime.MinValue` |
| LOW-003 | Plugin.cs | 260-269 | Null checks on `{ get; init; }` properties |
| LOW-004 | ResetService.cs | 256-265 | JumboCactpot uses `LastWeeklyReset` timestamp |
| LOW-005 | UIHelpers.cs | 74 | `HelpMarker` has `bool sameLine = true` default |
| LOW-006 | MainWindow.cs | 36 | `ResetService?` nullable but always passed non-null |
| LOW-007 | SettingsWindow.cs | 574 | Static method `IsTaskInCategory` |
| LOW-008 | MainWindow.cs | 519 | `IsTaskInCategory` duplicated from SettingsWindow |
| LOW-009 | Plugin.cs | 94 | `Configuration` uses `{ get; init; }` |
| LOW-010 | Multiple | - | Some XML docs missing on public methods |
| LOW-011 | TaskRegistry.cs | 22 | Static class with all static members |
| LOW-012 | DailiesChecklist.csproj | 7 | `IsPackable` set to false |

---

## Summary Statistics

| Severity | Count | Action Required |
|----------|-------|-----------------|
| Critical | 2 | MUST FIX before distribution |
| High | 7 | SHOULD FIX before distribution |
| Medium | 15 | RECOMMENDED to fix |
| Low | 12 | OPTIONAL improvements |
| **Total** | **36** | |

---

## Recommendations

### Immediate Actions (Pre-Distribution)
1. Fix CRIT-001: Add exception logging to MainWindow.cs catch block
2. Fix CRIT-002: Remove or use `EnsureUtc` method in ResetService.cs
3. Fix HIGH-001: Remove unused `_framework` field from BeastTribeDetector.cs
4. Fix HIGH-002: Remove unused `Log` type alias from Global.cs
5. Fix HIGH-003: Cache task list in TaskRegistry
6. Fix HIGH-006: Convert Enums.cs to file-scoped namespace

### Before Next Release
1. Remove all redundant using directives (MED-001)
2. Standardize initialization syntax (MED-005)
3. Address unused parameters with discard pattern (MED-002)
4. Consolidate duplicate `IsTaskInCategory` methods (LOW-008)

---

## Conclusion

The codebase is functional but shows signs of rapid development without consistent code review. The dead code suggests incomplete refactoring. The silent exception handling is a serious concern for production use.

With the critical and high-severity issues addressed, this plugin would be acceptable for distribution.

**Estimated remediation effort:** 2-4 hours for critical/high issues, 4-6 hours for all issues.
