# API 14 Debugging Checklist Review: Phase 1 & Phase 2

**Plugin:** DailiesChecklist
**Analyst:** Analyst Subagent 1
**Date:** 2026-01-29
**Review Scope:** Phase 1 (Project & Namespace Hygiene) + Phase 2 (Initialization & Services)

---

## Executive Summary

**Overall Status:** MOSTLY PASSING with 2 CRITICAL ISSUES and 1 WARNING

The DailiesChecklist plugin is largely compliant with API 14 requirements. The primary issues identified are:

1. **CRITICAL:** Missing `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in csproj (required for unsafe code in RouletteDetector)
2. **CRITICAL:** Uses `Dalamud.Bindings.ImGui` namespace instead of `Dalamud.Interface.ImGui` (may cause compilation failures)
3. **WARNING:** Global.cs does not include `Dalamud.Interface.ImGui` global using

---

## PHASE 1: Project & Namespace Hygiene

### 1. Remove ImGuiNET
**Status:** PASS

**Finding:** No ImGui.NET NuGet package reference found in the csproj. The project uses the Dalamud.NET.Sdk which provides ImGui internally.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj` (lines 1-26)
- No `<PackageReference Include="ImGui.NET">` present
- Uses `Dalamud.NET.Sdk/14.0.1` which handles ImGui provision

---

### 2. Verify Bindings
**Status:** FAIL - CRITICAL

**Finding:** The codebase uses `Dalamud.Bindings.ImGui` instead of the expected `Dalamud.Interface.ImGui` namespace.

**Code References:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:10` - `using Dalamud.Bindings.ImGui;`
- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:10` - `using Dalamud.Bindings.ImGui;`
- `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:3` - `using Dalamud.Bindings.ImGui;`

**Impact:** May cause "type or namespace not found" errors if the actual API 14 namespace differs. Need to verify against official Dalamud API 14 documentation whether `Dalamud.Bindings.ImGui` is the correct namespace or if it should be `Dalamud.Interface.ImGui`.

---

### 3. Enum Capitalization (RGB/HSV)
**Status:** PASS

**Finding:** No problematic "RGB" or "HSV" enum usages found. The only match found was in a comment describing the color format:

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:17`
```csharp
/// Colors are Vector4 in RGBA format (0.0-1.0 range).
```

This is documentation text, not enum usage, so no changes required.

---

### 4. Target Framework
**Status:** PASS

**Finding:** Project correctly targets `net10.0-windows` as required for API 14.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj:4`
```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

---

### 5. Manifest API Level
**Status:** PASS

**Finding:** Both manifest files correctly specify DalamudApiLevel 14.

**Code References:**
- `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.json:8` - `"DalamudApiLevel": 14`
- `/home/user/MMORPG-Addon/pluginmaster.json:20` - `"DalamudApiLevel": 14`

---

### 6. Unsafe Context
**Status:** FAIL - CRITICAL

**Finding:** The project uses unsafe code but does NOT have `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in the csproj.

**Evidence of Unsafe Code Usage:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs:492`
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
    ...
}
```

**Missing in csproj:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj`
```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

**Impact:** Compilation will fail with error CS0227: "Unsafe code may only appear if compiling with /unsafe"

---

### 7. Service Interface
**Status:** PASS

**Finding:** No `[Inject]` attributes found. The codebase correctly uses constructor injection with interface types.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:119-129`
```csharp
public Plugin(
    IDalamudPluginInterface pluginInterface,
    ICommandManager commandManager,
    IPluginLog log,
    IClientState clientState,
    IFramework framework,
    IDataManager dataManager,
    ICondition condition,
    IGameGui gameGui,
    IAddonLifecycle addonLifecycle,
    IDutyState dutyState)
```

All services use the `I*` interface pattern correctly (e.g., `IClientState`, `IPluginLog`).

---

### 8. Reference Cleanup
**Status:** PASS

**Finding:** Project uses `Dalamud.NET.Sdk/14.0.1` which automatically handles Dalamud assembly references. No local Dalamud.dll references found.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj:2`
```xml
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
```

---

### 9. NuGet Hash (FFXIVClientStructs)
**Status:** WARNING

**Finding:** No explicit FFXIVClientStructs package reference found in csproj. The SDK may provide this, but the version cannot be verified.

**Code Reference:** The RouletteDetector uses FFXIVClientStructs:
- `/home/user/MMORPG-Addon/DailiesChecklist/Detectors/Detectors/RouletteDetector.cs:5`
```csharp
using FFXIVClientStructs.FFXIV.Client.Game.UI;
```

**Recommendation:** Verify that the Dalamud.NET.Sdk/14.0.1 provides an API 14-compatible version of FFXIVClientStructs, or add an explicit package reference with the correct hash.

---

### 10. Global Usings
**Status:** WARNING

**Finding:** Global.cs exists but does not include `Dalamud.Interface.ImGui` (or `Dalamud.Bindings.ImGui`).

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Core/Global.cs`

Current global usings:
```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Dalamud.Plugin.Services;
global using Dalamud.Interface.Windowing;
global using DailiesChecklist.Models;
global using DailiesChecklist.Services;
```

**Missing:** A global using for the ImGui namespace, which would reduce repetitive imports in window files.

---

## PHASE 2: Initialization & Services

### 1. Constructor Injection
**Status:** PASS

**Finding:** Plugin constructor correctly takes `IDalamudPluginInterface pluginInterface` as the first argument.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:119-120`
```csharp
public Plugin(
    IDalamudPluginInterface pluginInterface,
    ...
```

---

### 2. CreateProvider (IDataManager)
**Status:** PASS (N/A)

**Finding:** No Excel sheet access via IDataManager found in the codebase. The plugin does not use `GetExcelSheet<T>()` or legacy `GetFile()` calls.

---

### 3. Texture Provider
**Status:** PASS (N/A)

**Finding:** No texture loading found. The plugin does not use `GetImGuiTexture()` or `ITextureProvider`.

---

### 4. Legacy Attributes
**Status:** PASS

**Finding:** No `[GameService]` attributes found anywhere in the codebase. Grep search returned no matches.

---

### 5. Logger Injection
**Status:** PASS

**Finding:** Uses `IPluginLog` via constructor injection correctly. No static `PluginLog` class usage.

**Code References:**
- Constructor injection: `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:112` - `IPluginLog log`
- Service container: `/home/user/MMORPG-Addon/DailiesChecklist/Service.cs:29` - `public static IPluginLog Log { get; private set; }`

---

### 6. Command Registration
**Status:** PASS

**Finding:** Command handler includes a non-empty help string.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:209-212`
```csharp
Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
{
    HelpMessage = "Toggle the Dailies Checklist window"
});
```

---

### 7. Dispose Safety
**Status:** PASS

**Finding:** Dispose method includes null checks for command manager before removing handlers.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:372-379`
```csharp
// Unregister command handlers
try
{
    Service.CommandManager?.RemoveHandler(CommandName);
}
catch (Exception ex)
{
    Service.Log.Error(ex, "Error removing command handler.");
}
```

Additionally, the plugin properly:
- Captures local references before disposal (Issue #6 fix at line 273-281)
- Checks service null status throughout disposal
- Uses try-catch blocks for each disposal step

---

### 8. JSON Serialization
**Status:** PASS

**Finding:** Uses `System.Text.Json` instead of Newtonsoft.Json for .NET 10 native performance.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/Services/PersistenceService.cs:4-5`
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
```

No Newtonsoft.Json references found in the entire codebase.

---

### 9. Plugin Interface (Create<T>)
**Status:** PASS (N/A)

**Finding:** No usage of `Create<T>` for internal services. This is acceptable as the plugin uses constructor injection for all Dalamud services.

---

### 10. Dev Folder
**Status:** PASS

**Finding:** No explicit output directory configuration in csproj that would place builds inside the game installation folder. The SDK handles output paths appropriately.

**Code Reference:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj:5-6`
```xml
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>
```

These settings only affect path suffixing, not the base output directory.

---

## PROPOSAL FOR FIXES

### Critical Fix 1: Add AllowUnsafeBlocks to csproj

**File:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj`

**Current (lines 3-6):**
```xml
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>
```

**Proposed (add after line 6):**
```xml
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

---

### Critical Fix 2: Verify/Update ImGui Namespace

**Action Required:** Verify against official Dalamud API 14 documentation whether:
- `Dalamud.Bindings.ImGui` is the correct namespace for API 14, OR
- Should be changed to `Dalamud.Interface.ImGui`

**Files to update if namespace change required:**
1. `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:10`
2. `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:10`
3. `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:3`

**If change needed, update from:**
```csharp
using Dalamud.Bindings.ImGui;
```

**To:**
```csharp
using Dalamud.Interface.ImGui;
```

---

### Recommended: Update Global.cs

**File:** `/home/user/MMORPG-Addon/DailiesChecklist/Core/Global.cs`

**Add after line 29 (after `global using Dalamud.Interface.Windowing;`):**
```csharp
// Dalamud ImGui bindings (API 14)
global using Dalamud.Bindings.ImGui;  // or Dalamud.Interface.ImGui; per verification
```

This would allow removal of the using statement from individual window files.

---

### Recommended: Verify FFXIVClientStructs Version

**Action:** Check Dalamud Discord #dev-announcements for the API 14-compatible FFXIVClientStructs version hash. If the SDK-provided version is not compatible, add explicit package reference:

**File:** `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj`

**Add to ItemGroup (only if SDK version is incompatible):**
```xml
<ItemGroup>
  <PackageReference Include="FFXIVClientStructs" Version="[API14_COMPATIBLE_VERSION]" />
</ItemGroup>
```

---

## Summary Table

| Check | Status | Notes |
|-------|--------|-------|
| **PHASE 1** | | |
| 1. Remove ImGuiNET | PASS | No NuGet reference |
| 2. Verify Bindings | FAIL | Uses `Dalamud.Bindings.ImGui` - verify correctness |
| 3. Enum Capitalization | PASS | No RGB/HSV enum issues |
| 4. Target Framework | PASS | net10.0-windows |
| 5. Manifest API | PASS | DalamudApiLevel: 14 |
| 6. Unsafe Context | FAIL | Missing AllowUnsafeBlocks |
| 7. Service Interface | PASS | Correct constructor injection |
| 8. Reference Cleanup | PASS | Using SDK |
| 9. NuGet Hash | WARNING | Cannot verify FFXIVClientStructs version |
| 10. Global Usings | WARNING | Missing ImGui global using |
| **PHASE 2** | | |
| 1. Constructor Injection | PASS | IDalamudPluginInterface first |
| 2. CreateProvider | N/A | No Excel sheet usage |
| 3. Texture Provider | N/A | No texture usage |
| 4. Legacy Attributes | PASS | No [GameService] found |
| 5. Logger Injection | PASS | Uses IPluginLog |
| 6. Command Registration | PASS | Has help string |
| 7. Dispose Safety | PASS | Null checks present |
| 8. JSON Serialization | PASS | Uses System.Text.Json |
| 9. Plugin Interface | N/A | No Create<T> needed |
| 10. Dev Folder | PASS | SDK default paths |

---

**Report End**
