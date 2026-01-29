# Phase 3: Runtime Logic Review (API 14 Debugging Checklist)

**Reviewer**: Analyst Subagent 2
**Date**: 2026-01-29
**Plugin**: DailiesChecklist
**Branch**: claude/dalamud-api-14-review-aFVDN

---

## Executive Summary

This review evaluates the DailiesChecklist plugin against Phase 3 (Runtime Logic) of the Dalamud API 14 debugging checklist. The plugin demonstrates **generally good practices** with proper exception handling and safe patterns in most areas. However, there are **2 warnings** that should be addressed for improved stability.

| Status | Count |
|--------|-------|
| PASS | 3 |
| WARNING | 2 |
| FAIL | 0 |
| N/A (Not Used) | 5 |

---

## Detailed Checklist Review

### 1. Pointer Safety
**Status**: WARNING

**Requirement**: API 14 uses more raw pointers. Wrap unsafe blocks specifically around FFXIVClientStructs access.

**Finding**: One `unsafe` block exists in the codebase:

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs`
**Lines**: 492-510

```csharp
private unsafe byte GetContentRouletteId()
{
    try
    {
        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return 0;
        }

        // Access the queued content roulette ID from the ContentsFinder struct
        return contentsFinder->QueueInfo.QueuedContentRouletteId;
    }
    catch (Exception ex)
    {
        _log.Error(ex, "Failed to get ContentRouletteId from FFXIVClientStructs.");
        return 0;
    }
}
```

**Analysis**:
- The method is marked `unsafe` and accesses FFXIVClientStructs (`ContentsFinder.Instance()`)
- Null check is performed before dereferencing the pointer
- Exception handling is present with a try-catch block
- The unsafe scope is appropriately limited to just this method

**Concern**: While the current implementation is reasonable, the entire method is marked `unsafe` rather than using a minimal `unsafe` block around just the pointer access. This is minor but could be tightened.

---

### 2. Target Checks
**Status**: N/A (Not Applicable)

**Requirement**: ITargetManager.Target can be null. ALWAYS check if (Target != null) before accessing .Name.

**Finding**: The codebase does NOT use `ITargetManager` or access any `.Target` property.

- No `ITargetManager` service is injected
- No target-related code exists in any detector or service

**Conclusion**: This checklist item does not apply to this plugin.

---

### 3. String Handling
**Status**: N/A (Not Applicable)

**Requirement**: Convert standard C# string to SeString when sending chat via IChatGui.

**Finding**: The codebase does NOT use `IChatGui` or `SeString`.

- No chat messages are sent by the plugin
- No `IChatGui` service is injected
- No imports of `Dalamud.Game.Text.SeStringHandling`

**Conclusion**: This checklist item does not apply to this plugin.

---

### 4. Async/Await
**Status**: PASS

**Requirement**: Do not use `async void`; use `async Task` for everything except event handlers to prevent silent failures.

**Finding**: Only one async method exists in the codebase:

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Services/PersistenceService.cs`
**Lines**: 433-440

```csharp
public async Task<ChecklistState> LoadAsync(CancellationToken cancellationToken = default)
{
    return await Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Load();
    }, cancellationToken);
}
```

**Analysis**:
- Correctly uses `async Task<T>` return type
- Properly supports cancellation via `CancellationToken`
- No `async void` methods found in any source files
- Event handlers (like `OnLogin`, `OnLogout`, `OnDutyCompleted`) are correctly implemented as synchronous methods

**Conclusion**: The codebase follows async/await best practices.

---

### 5. Main Thread Enforcement
**Status**: PASS

**Requirement**: If calling IClientState from a Task, wrap it in IFramework.RunOnFrameworkThread().

**Finding**: No cross-thread IClientState access detected.

**Analysis**:
- `IClientState` is only accessed from:
  - Event handlers (`OnLogin`, `OnLogout`, `OnTerritoryChanged`) which run on the framework thread
  - Synchronous checks like `_clientState.IsLoggedIn` during initialization
- The `LoadAsync` method in PersistenceService does NOT access IClientState
- Timer callbacks in PersistenceService only perform file I/O, not game state access
- `OnFrameworkUpdate` is already on the main thread

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
**Lines**: 508-529 (OnFrameworkUpdate - already on framework thread)

```csharp
private void OnFrameworkUpdate(IFramework framework)
{
    // Already on framework thread - safe to access game state
    if (_pendingInitialResetSync)
    {
        _pendingInitialResetSync = false;
        Service.Log.Debug("Performing deferred initial reset sync on first framework tick.");
        ApplyResetsAndSyncDetectors();
        return;
    }
    // ...
}
```

**Conclusion**: No RunOnFrameworkThread() calls needed; all IClientState access occurs on appropriate threads.

---

### 6. Hook Signatures
**Status**: N/A (Not Applicable)

**Requirement**: Check IGameInteropProvider hooks. A nint vs long mismatch will crash the game instantly on 64-bit .NET 10.

**Finding**: The codebase does NOT use `IGameInteropProvider` or any game hooks.

- No hook registrations found
- No signature scanning or memory hooking
- Plugin relies entirely on Dalamud service events (IDutyState, IClientState, IAddonLifecycle)

**Conclusion**: This checklist item does not apply to this plugin.

---

### 7. Conditionals (Loading Screen Checks)
**Status**: WARNING

**Requirement**: Use `ICondition[ConditionFlag.BetweenAreas]` to pause processing during loading screens.

**Finding**: `ICondition` is available but NOT used for loading screen checks.

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Service.cs`
**Lines**: 49, 90

```csharp
public static ICondition Condition { get; private set; }
// ...
ICondition condition,
```

**Analysis**:
- `ICondition` is properly injected and stored in the Service container
- However, **no detector or service checks `ConditionFlag.BetweenAreas`** before accessing game state
- The following operations could be problematic during loading screens:
  - `RouletteDetector.GetContentRouletteId()` - accessing ContentsFinder during zone transition
  - `CactpotDetector.OnTerritoryChanged()` - checking territory ID during load
  - `Plugin.ApplyResetsAndSyncDetectors()` - accessing detector states during load

**Risk**: Accessing game memory/state during loading screens (when `ConditionFlag.BetweenAreas` is true) can cause crashes or undefined behavior as game structures may be in an inconsistent state.

**Affected Files**:
- `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs`
- `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/CactpotDetector.cs`
- `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`

---

### 8. Object Table
**Status**: N/A (Not Applicable)

**Requirement**: IObjectTable is now an IEnumerable. Do not use indexer [i] inside a tight loop if iterating; use foreach.

**Finding**: The codebase does NOT use `IObjectTable`.

- No object table iteration
- No NPC/entity scanning
- Plugin does not interact with game objects directly

**Conclusion**: This checklist item does not apply to this plugin.

---

### 9. Party List
**Status**: N/A (Not Applicable)

**Requirement**: IPartyList members might be valid but have GameObject as null (if they are out of render range). Check both.

**Finding**: The codebase does NOT use `IPartyList`.

- No party member tracking
- No party-related functionality

**Conclusion**: This checklist item does not apply to this plugin.

---

### 10. Toast Safety
**Status**: N/A (Not Applicable)

**Requirement**: Don't fire INotificationManager.AddNotification inside a Draw loop (spam risk).

**Finding**: The codebase does NOT use `INotificationManager`.

- No toast notifications
- No notification manager service injected
- UI feedback is provided via ImGui windows only

**Conclusion**: This checklist item does not apply to this plugin.

---

## Summary Table

| # | Check | Status | Notes |
|---|-------|--------|-------|
| 1 | Pointer Safety | WARNING | Unsafe block exists but is properly guarded; scope could be tighter |
| 2 | Target Checks | N/A | ITargetManager not used |
| 3 | String Handling | N/A | IChatGui/SeString not used |
| 4 | Async/Await | PASS | Correctly uses async Task, no async void |
| 5 | Main Thread Enforcement | PASS | All IClientState access on correct threads |
| 6 | Hook Signatures | N/A | IGameInteropProvider not used |
| 7 | Conditionals | WARNING | ICondition available but BetweenAreas not checked |
| 8 | Object Table | N/A | IObjectTable not used |
| 9 | Party List | N/A | IPartyList not used |
| 10 | Toast Safety | N/A | INotificationManager not used |

---

## PROPOSAL FOR FIXES

### Fix 1: Add Loading Screen Guard (Priority: Medium)

**Issue**: Detectors and plugin do not check `ConditionFlag.BetweenAreas` before accessing game state.

**Proposed Changes**:

#### Option A: Guard at Framework Update Level (Recommended)

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`

Add a loading screen check at the start of `OnFrameworkUpdate`:

```csharp
private void OnFrameworkUpdate(IFramework framework)
{
    // Skip processing during loading screens to prevent accessing invalid game state
    if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] ||
        Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51])
    {
        return;
    }

    // Existing code continues...
    if (_pendingInitialResetSync)
    {
        // ...
    }
}
```

#### Option B: Guard at Detector Level

**File**: `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs`

Add check before unsafe memory access:

```csharp
private unsafe byte GetContentRouletteId()
{
    try
    {
        // Guard against accessing game memory during loading screens
        if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
        {
            return 0;
        }

        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return 0;
        }

        return contentsFinder->QueueInfo.QueuedContentRouletteId;
    }
    catch (Exception ex)
    {
        _log.Error(ex, "Failed to get ContentRouletteId from FFXIVClientStructs.");
        return 0;
    }
}
```

**Required Import**:
```csharp
using Dalamud.Game.ClientState.Conditions;
```

---

### Fix 2: Tighten Unsafe Block Scope (Priority: Low)

**Issue**: The entire `GetContentRouletteId` method is marked unsafe when only the pointer access needs it.

**Current Code** (`RouletteDetector.cs:492-510`):
```csharp
private unsafe byte GetContentRouletteId()
{
    try
    {
        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return 0;
        }
        return contentsFinder->QueueInfo.QueuedContentRouletteId;
    }
    catch (Exception ex)
    {
        _log.Error(ex, "Failed to get ContentRouletteId from FFXIVClientStructs.");
        return 0;
    }
}
```

**Proposed Code**:
```csharp
private byte GetContentRouletteId()
{
    try
    {
        unsafe
        {
            var contentsFinder = ContentsFinder.Instance();
            if (contentsFinder == null)
            {
                return 0;
            }
            return contentsFinder->QueueInfo.QueuedContentRouletteId;
        }
    }
    catch (Exception ex)
    {
        _log.Error(ex, "Failed to get ContentRouletteId from FFXIVClientStructs.");
        return 0;
    }
}
```

**Rationale**: Moving the `unsafe` block inside the method body makes it clearer exactly which code requires unsafe context and keeps the method signature cleaner.

---

## Recommendations Summary

1. **HIGH PRIORITY**: Implement loading screen guard (`ConditionFlag.BetweenAreas`) in `OnFrameworkUpdate` to prevent game state access during zone transitions.

2. **LOW PRIORITY**: Consider tightening the scope of the unsafe block in `RouletteDetector.GetContentRouletteId()` for clarity.

3. **POSITIVE NOTE**: The plugin demonstrates good practices:
   - Proper async/await usage (no async void)
   - All IClientState access on correct threads
   - Comprehensive exception handling around unsafe code
   - No use of problematic patterns (tight loops with indexers, toast spam in Draw, etc.)

---

## Files Reviewed

| File | Path |
|------|------|
| Plugin.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs` |
| Service.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Service.cs` |
| DetectionService.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/DetectionService.cs` |
| ITaskDetector.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/ITaskDetector.cs` |
| RouletteDetector.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs` |
| CactpotDetector.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/CactpotDetector.cs` |
| BeastTribeDetector.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/BeastTribeDetector.cs` |
| MainWindow.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs` |
| SettingsWindow.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs` |
| PersistenceService.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Services/PersistenceService.cs` |
| ResetService.cs | `/home/user/MMORPG-Addon/DailiesChecklist/Services/ResetService.cs` |

---

*End of Phase 3 Review*
