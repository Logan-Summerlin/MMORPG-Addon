# Phase 4: UI (ImGui & WindowSystem) - API 14 Compatibility Review

**Reviewer:** Analyst Subagent 3
**Date:** 2026-01-29
**Plugin:** DailiesChecklist
**Branch:** claude/dalamud-api-14-review-aFVDN

---

## Executive Summary

The DailiesChecklist plugin demonstrates **good UI implementation practices** overall. The codebase correctly uses WindowSystem, properly extends the Window class, and follows Dalamud's ImRaii patterns for ID management. Most checklist items pass or are not applicable due to the plugin's limited UI feature set (no textures, custom fonts, or combo boxes).

**Key Finding:** One warning identified regarding hardcoded UI sizing values that may not scale properly on 4K monitors.

---

## Checklist Review

### 1. WindowSystem Registration
**Status:** PASS

WindowSystem.AddWindow() is correctly called during plugin initialization in the constructor, not in the Draw method.

**Evidence:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs:204-206`
```csharp
// Add windows to the window system
WindowSystem.AddWindow(MainWindow);
WindowSystem.AddWindow(SettingsWindow);
```

This occurs within the constructor's initialization sequence (after window creation, before event subscription), which is the correct pattern.

---

### 2. Draw Loop - No Manual ImGui.Begin()
**Status:** PASS

Both window classes properly extend `Dalamud.Interface.Windowing.Window` and override the `Draw()` method. No manual `ImGui.Begin()` calls were found.

**Evidence:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:33`
```csharp
public class MainWindow : Window, IDisposable
```

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:163-178`
```csharp
public override void Draw()
{
    // Draw Daily Activities section
    DrawCategorySection(TaskCategory.Daily, "Daily Activities");
    // ... (no ImGui.Begin() call)
}
```

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:22`
```csharp
public class SettingsWindow : Window, IDisposable
```

---

### 3. ID Stack - PushID for List Items
**Status:** PASS

The plugin correctly uses `ImRaii.PushId()` (Dalamud's RAII wrapper for ImGui ID stack) for all dynamically generated UI elements in lists.

**Evidence:**

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:258-264` (Reset buttons per category)
```csharp
using (ImRaii.PushId($"ResetAll_{category}"))
{
    if (ImGui.SmallButton(resetButtonLabel))
    {
        ResetCategory(category);
    }
}
```

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:304` (Task row checkboxes)
```csharp
using (ImRaii.PushId(task.Id))
{
    // Checkbox for task completion
    var isCompleted = task.IsCompleted;
    if (ImGui.Checkbox("##TaskCheckbox", ref isCompleted))
    // ...
}
```

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:442` (Task toggle list)
```csharp
using (ImRaii.PushId($"TaskToggle_{task.Id}"))
{
    // ...
    if (ImGui.Checkbox(label, ref isEnabled))
    // ...
}
```

All task IDs are unique strings (e.g., "mini_cactpot", "leveling_roulette"), ensuring proper ID stack isolation.

---

### 4. Texture Disposal
**Status:** N/A (Not Applicable)

The plugin does not use any textures. No `IDalamudTextureWrap`, `ISharedImmediateTexture`, or `ImGui.Image()` calls were found in the codebase.

**Search Results:** No matches for texture-related patterns.

---

### 5. Popup Context
**Status:** PASS

The plugin uses `ImGui.BeginPopupModal()` for confirmation dialogs, which is the correct pattern for modal popups. No context menu usage (`BeginPopupContextItem` or `BeginPopupContextWindow`) was found.

**Evidence:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:375-396`
```csharp
if (ImGui.BeginPopupModal(id, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
{
    ImGui.Text(message);
    // ... button handling ...
    ImGui.EndPopup();
}
```

The popup implementation includes proper state management with automatic cleanup of stale entries.

---

### 6. Input Passthrough (Overlay Flag)
**Status:** N/A (Not Applicable)

This plugin is not an overlay. Both windows are standard windowed UIs with normal input handling:

- `MainWindow`: Uses `ImGuiWindowFlags.None` by default, with optional `NoMove` flag for position locking
- `SettingsWindow`: Uses `ImGuiWindowFlags.NoCollapse`

**Evidence:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:59`
```csharp
: base("Dailies Checklist###DailiesChecklistMain", ImGuiWindowFlags.None)
```

- `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:50-51`
```csharp
: base("Dailies Checklist Settings###DailiesChecklistSettings",
    ImGuiWindowFlags.NoCollapse)
```

---

### 7. Font Atlas - Custom Fonts
**Status:** N/A (Not Applicable)

The plugin does not add custom fonts. No `BuildFonts` event subscription or font atlas manipulation was found.

**Search Results:** No matches for font-related patterns.

---

### 8. Color Formatting - Vector4 vs uint
**Status:** PASS

All color definitions use `Vector4` format, which is the preferred format for API 14. No packed `uint` colors were found.

**Evidence:**
- `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:19-42`
```csharp
public static class Colors
{
    // Status Colors
    public static readonly Vector4 Success = new(0.4f, 0.8f, 0.4f, 1.0f);
    public static readonly Vector4 Warning = new(0.9f, 0.7f, 0.2f, 1.0f);
    public static readonly Vector4 Error = new(0.9f, 0.3f, 0.3f, 1.0f);
    public static readonly Vector4 Info = new(0.4f, 0.6f, 0.9f, 1.0f);
    // ... all colors use Vector4 ...
}
```

Configuration.cs does not store any color preferences (only opacity as float).

---

### 9. Combo Boxes
**Status:** N/A (Not Applicable)

The plugin does not use combo boxes. No `ImGui.BeginCombo()`, `ImGui.Combo()`, or `ImGui.EndCombo()` calls were found.

**Search Results:** No matches for combo-related patterns.

---

### 10. Scaling - GlobalScale for 4K Support
**Status:** WARNING

The plugin uses hardcoded pixel values for various UI elements without applying `ImGuiHelpers.GlobalScale`. While this works on standard DPI displays, it may cause issues on 4K monitors or with UI scaling enabled.

**Issues Found:**

1. **Window Size Constraints** - `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:70-78`
```csharp
SizeConstraints = new WindowSizeConstraints
{
    MinimumSize = new Vector2(300, 200),  // Hardcoded
    MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
};
Size = new Vector2(350, 400);  // Hardcoded
```

2. **Settings Window Size** - `/home/user/MMORPG-Addon/DailiesChecklist/Windows/SettingsWindow.cs:62-70`
```csharp
SizeConstraints = new WindowSizeConstraints
{
    MinimumSize = new Vector2(350, 400),  // Hardcoded
    MaximumSize = new Vector2(500, 600)   // Hardcoded
};
Size = new Vector2(400, 500);  // Hardcoded
```

3. **Indent Values** - Multiple locations
   - `MainWindow.cs:210, 218`: `ImRaii.PushIndent(10f)`
   - `SettingsWindow.cs:213, 268, 279, 379`: `ImRaii.PushIndent(10f)`

4. **Button Sizes** - `/home/user/MMORPG-Addon/DailiesChecklist/Utils/UIHelpers.cs:382, 388`
```csharp
if (ImGui.Button(confirmText, new Vector2(100, 0)))  // Hardcoded width
if (ImGui.Button(cancelText, new Vector2(100, 0)))   // Hardcoded width
```

5. **Progress Bar Height** - `/home/user/MMORPG-Addon/DailiesChecklist/Windows/MainWindow.cs:212`
```csharp
UIHelpers.ProgressBar(completedCount, totalCount, -1f, 14f, true);  // Hardcoded 14f height
```

---

## Summary Table

| # | Checklist Item | Status | Notes |
|---|----------------|--------|-------|
| 1 | WindowSystem Registration | PASS | AddWindow called in Initialize, not Draw |
| 2 | Draw Loop (no manual Begin) | PASS | Properly extends Window class |
| 3 | ID Stack (PushID for lists) | PASS | ImRaii.PushId used correctly |
| 4 | Texture Disposal | N/A | No textures used |
| 5 | Popup Context | PASS | BeginPopupModal used correctly |
| 6 | Input Passthrough | N/A | Not an overlay |
| 7 | Font Atlas | N/A | No custom fonts |
| 8 | Color Formatting | PASS | All colors use Vector4 |
| 9 | Combo Boxes | N/A | No combos used |
| 10 | Scaling (GlobalScale) | WARNING | Hardcoded values need scaling |

---

## Proposal for Fixes

### Issue: Hardcoded UI Values Without GlobalScale

**Severity:** Medium
**Impact:** UI may appear too small or too large on 4K monitors or when users have UI scaling enabled.

**Recommended Fix:**

1. **Add ImGuiHelpers using directive** to window files:
```csharp
using Dalamud.Interface.Utility;
```

2. **Scale window size constraints:**

`MainWindow.cs` (lines 70-78):
```csharp
SizeConstraints = new WindowSizeConstraints
{
    MinimumSize = new Vector2(300, 200) * ImGuiHelpers.GlobalScale,
    MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
};
Size = new Vector2(350, 400) * ImGuiHelpers.GlobalScale;
```

`SettingsWindow.cs` (lines 62-70):
```csharp
SizeConstraints = new WindowSizeConstraints
{
    MinimumSize = new Vector2(350, 400) * ImGuiHelpers.GlobalScale,
    MaximumSize = new Vector2(500, 600) * ImGuiHelpers.GlobalScale
};
Size = new Vector2(400, 500) * ImGuiHelpers.GlobalScale;
```

3. **Scale indent values:**

Replace all occurrences of `ImRaii.PushIndent(10f)` with:
```csharp
ImRaii.PushIndent(10f * ImGuiHelpers.GlobalScale)
```

Affected locations:
- `MainWindow.cs:210, 218`
- `SettingsWindow.cs:213, 268, 279, 379`

4. **Scale button sizes in UIHelpers.cs** (lines 382, 388):
```csharp
if (ImGui.Button(confirmText, new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
if (ImGui.Button(cancelText, new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
```

5. **Scale progress bar height:**

`MainWindow.cs:212`:
```csharp
UIHelpers.ProgressBar(completedCount, totalCount, -1f, 14f * ImGuiHelpers.GlobalScale, true);
```

Or update the ProgressBar method default parameter:
```csharp
public static void ProgressBar(int current, int max, float width = -1f, float height = 18f, bool showText = true)
{
    // Apply scale internally
    height *= ImGuiHelpers.GlobalScale;
    // ...
}
```

---

## Additional Observations

### Positive Patterns Observed

1. **Proper use of ImRaii patterns**: The codebase consistently uses `ImRaii.PushIndent()`, `ImRaii.PushId()`, and `ImRaii.Child()` for automatic cleanup via `using` statements.

2. **Window lifecycle management**: Both windows implement `IDisposable` and are properly cleaned up during plugin disposal.

3. **Event unsubscription**: The `Draw` event is properly subscribed and unsubscribed:
   - `Plugin.cs:215`: `Service.PluginInterface.UiBuilder.Draw += DrawUI;`
   - `Plugin.cs:286`: `Service.PluginInterface.UiBuilder.Draw -= DrawUI;`

4. **Popup state cleanup**: The UIHelpers.ClearPopupState() is called during disposal to prevent memory leaks.

### Minor Style Note

The WindowSystem is declared as a public readonly field rather than a property:
- `Plugin.cs:45`: `public readonly WindowSystem WindowSystem = new("DailiesChecklist");`

This was flagged in a previous style report and could be converted to an auto-property for consistency, but has no functional impact.

---

## Conclusion

The DailiesChecklist plugin is **mostly compliant** with Dalamud API 14 UI best practices. The only actionable item is applying `ImGuiHelpers.GlobalScale` to hardcoded pixel values for proper 4K monitor support.

**Risk Assessment:** Low. The scaling issue affects visual appearance on high-DPI displays but does not cause crashes or functional problems.

**Recommendation:** Apply the proposed GlobalScale fixes before the next release to ensure proper display on all monitor configurations.
