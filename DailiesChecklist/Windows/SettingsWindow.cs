using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using DailiesChecklist;
using DailiesChecklist.Models;
using DailiesChecklist.Services;
using DailiesChecklist.Utils;
using ImGuiNET;

namespace DailiesChecklist.Windows;

/// <summary>
/// Configuration window for plugin settings.
/// Provides display options, task enable/disable toggles, and save functionality.
///
/// Settings include:
/// - Display options: Opacity slider (25-100%), Lock position checkbox
/// - Task enable/disable: Checkbox list of all tasks organized by category
/// </summary>
public class SettingsWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly Configuration _configuration;
    private readonly Action? _onStateChanged;

    /// <summary>
    /// Reference to external checklist state (if provided via DI).
    /// </summary>
    private ChecklistState? _externalState;

    /// <summary>
    /// Cached task list for the settings UI (fallback when no external state).
    /// </summary>
    private List<ChecklistTask> _taskList;

    private bool _disposed;

    /// <summary>
    /// Initializes the settings window with dependency injection.
    /// </summary>
    /// <param name="plugin">Reference to the main plugin instance.</param>
    /// <param name="checklistState">Optional checklist state for task enable/disable. If null, creates default.</param>
    /// <param name="onStateChanged">Optional callback invoked when state changes (for persistence).</param>
    public SettingsWindow(
        Plugin plugin,
        ChecklistState? checklistState = null,
        Action? onStateChanged = null)
        : base("Dailies Checklist Settings###DailiesChecklistSettings",
            ImGuiWindowFlags.NoCollapse)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _configuration = plugin.Configuration;
        _onStateChanged = onStateChanged;
        _externalState = checklistState;

        // Use external state's tasks or fall back to default tasks
        _taskList = checklistState?.Tasks ?? TaskRegistry.GetDefaultTasks();

        // Size constraints for settings window
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 400),
            MaximumSize = new Vector2(500, 600)
        };

        // Default size
        Size = new Vector2(400, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <summary>
    /// Cleanup when the window is disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        // No event handlers to clean up
    }

    /// <summary>
    /// Called when the window is closed.
    /// Auto-saves settings.
    /// </summary>
    public override void OnClose()
    {
        // Auto-save when closing
        _configuration.Save();
        Plugin.Log.Debug("Settings saved on window close");
    }

    /// <summary>
    /// Main draw method called every frame when the window is open.
    /// </summary>
    public override void Draw()
    {
        // Use tabs for organized settings
        if (ImGui.BeginTabBar("SettingsTabBar"))
        {
            if (ImGui.BeginTabItem("Display"))
            {
                DrawDisplaySettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Tasks"))
            {
                DrawTaskSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Detection"))
            {
                DrawDetectionSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    /// <summary>
    /// Draws display-related settings: opacity, lock position, etc.
    /// </summary>
    private void DrawDisplaySettings()
    {
        ImGui.Spacing();

        // Window Opacity Slider (25% - 100%)
        ImGui.Text("Window Opacity");
        ImGui.SetNextItemWidth(-1);
        var opacity = _configuration.WindowOpacity * 100f; // Convert to percentage
        if (ImGui.SliderFloat("##Opacity", ref opacity, 25f, 100f, "%.0f%%"))
        {
            _configuration.WindowOpacity = opacity / 100f; // Convert back to 0-1 range
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Adjust the background transparency of the main window (25% - 100%)");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Lock Position Checkbox
        var windowLocked = _configuration.WindowLocked;
        if (ImGui.Checkbox("Lock Window Position", ref windowLocked))
        {
            _configuration.WindowLocked = windowLocked;
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Prevent the main window from being moved");
        }

        ImGui.Spacing();

        // Show Locations Checkbox
        var showLocations = _configuration.ShowLocations;
        if (ImGui.Checkbox("Show Task Locations", ref showLocations))
        {
            _configuration.ShowLocations = showLocations;
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Display the location for each task (e.g., \"Gold Saucer - Mini Cactpot\")");
        }

        ImGui.Spacing();

        // Show Auto-Detect Indicators Checkbox
        var showAutoDetect = _configuration.ShowAutoDetectIndicators;
        if (ImGui.Checkbox("Show Auto-Detect Indicators", ref showAutoDetect))
        {
            _configuration.ShowAutoDetectIndicators = showAutoDetect;
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Show asterisk (*) next to tasks that can be automatically detected");
        }

        ImGui.Spacing();

        // Show Progress Bars Checkbox
        var showProgressBars = _configuration.ShowProgressBars;
        if (ImGui.Checkbox("Show Progress Bars", ref showProgressBars))
        {
            _configuration.ShowProgressBars = showProgressBars;
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Show progress bars for each category (Daily/Weekly) completion");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Section collapse defaults
        ImGui.Text("Default Section States:");
        ImGui.Spacing();

        using (ImRaii.PushIndent(10f))
        {
            var collapseDailyByDefault = _configuration.CollapseDailyByDefault;
            if (ImGui.Checkbox("Collapse Daily Activities by default", ref collapseDailyByDefault))
            {
                _configuration.CollapseDailyByDefault = collapseDailyByDefault;
                _configuration.Save();
            }

            var collapseWeeklyByDefault = _configuration.CollapseWeeklyByDefault;
            if (ImGui.Checkbox("Collapse Weekly Activities by default", ref collapseWeeklyByDefault))
            {
                _configuration.CollapseWeeklyByDefault = collapseWeeklyByDefault;
                _configuration.Save();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Reset Window Position Button
        if (ImGui.Button("Reset Window Position"))
        {
            _configuration.WindowPosition = null;
            _configuration.WindowSize = null;
            _configuration.Save();
            Plugin.Log.Information("Window position reset to default");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Reset the main window to its default position and size");
        }
    }

    /// <summary>
    /// Draws task enable/disable settings organized by category.
    /// </summary>
    private void DrawTaskSettings()
    {
        ImGui.Spacing();

        ImGui.Text("Enable or disable individual tasks:");
        ImGui.TextDisabled("Changes are saved automatically.");
        ImGui.Spacing();

        // Create a scrollable child region for the task list
        var childHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - 10;
        using (var child = ImRaii.Child("TaskSettingsScrollRegion", new Vector2(0, childHeight), true))
        {
            if (child.Success)
            {
                // Draw Daily Tasks section
                if (ImGui.CollapsingHeader("Daily Activities", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    using (ImRaii.PushIndent(10f))
                    {
                        DrawTaskToggleList(TaskCategory.Daily);
                    }
                }

                ImGui.Spacing();

                // Draw Weekly Tasks section
                if (ImGui.CollapsingHeader("Weekly Activities", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    using (ImRaii.PushIndent(10f))
                    {
                        DrawTaskToggleList(TaskCategory.Weekly);
                    }
                }
            }
        }

        ImGui.Spacing();

        // Quick actions
        if (ImGui.Button("Enable All"))
        {
            SetAllTasksEnabled(true);
        }

        ImGui.SameLine();

        if (ImGui.Button("Disable All"))
        {
            SetAllTasksEnabled(false);
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset to Defaults"))
        {
            ResetTasksToDefaults();
        }
    }

    /// <summary>
    /// Draws detection-related settings: feature flags for auto-detection modules.
    /// </summary>
    private void DrawDetectionSettings()
    {
        ImGui.Spacing();

        ImGui.Text("Auto-Detection Modules");
        UIHelpers.HelpMarker(
            "Control which game state detectors are active. " +
            "Disable a detector if it causes issues or if you prefer manual tracking for certain tasks.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Roulette Detection
        var enableRouletteDetection = _configuration.FeatureFlags.EnableRouletteDetection;
        if (ImGui.Checkbox("Duty Roulette Detection", ref enableRouletteDetection))
        {
            _configuration.FeatureFlags.EnableRouletteDetection = enableRouletteDetection;
            _configuration.Save();
            _plugin.ApplyDetectorFeatureFlags();
            Plugin.Log.Information($"Roulette detection {(enableRouletteDetection ? "enabled" : "disabled")}");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically detect completion of duty roulettes");
        }

        ImGui.Spacing();

        // Cactpot Detection
        var enableCactpotDetection = _configuration.FeatureFlags.EnableCactpotDetection;
        if (ImGui.Checkbox("Cactpot Detection", ref enableCactpotDetection))
        {
            _configuration.FeatureFlags.EnableCactpotDetection = enableCactpotDetection;
            _configuration.Save();
            _plugin.ApplyDetectorFeatureFlags();
            Plugin.Log.Information($"Cactpot detection {(enableCactpotDetection ? "enabled" : "disabled")}");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically detect Mini and Jumbo Cactpot ticket purchases");
        }

        ImGui.Spacing();

        // Beast Tribe Detection
        var enableBeastTribeDetection = _configuration.FeatureFlags.EnableBeastTribeDetection;
        if (ImGui.Checkbox("Beast Tribe Quest Detection", ref enableBeastTribeDetection))
        {
            _configuration.FeatureFlags.EnableBeastTribeDetection = enableBeastTribeDetection;
            _configuration.Save();
            _plugin.ApplyDetectorFeatureFlags();
            Plugin.Log.Information($"Beast Tribe detection {(enableBeastTribeDetection ? "enabled" : "disabled")}");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically track beast tribe daily quest allowances");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Detection status info
        ImGui.TextColored(UIHelpers.Colors.TextDim, "Detection Status:");
        ImGui.Spacing();

        using (ImRaii.PushIndent(10f))
        {
            var detectorCount = 0;
            if (enableRouletteDetection) detectorCount++;
            if (enableCactpotDetection) detectorCount++;
            if (enableBeastTribeDetection) detectorCount++;

            var statusColor = detectorCount > 0 ? UIHelpers.Colors.Success : UIHelpers.Colors.Warning;
            var statusText = detectorCount > 0
                ? $"{detectorCount} detector(s) active"
                : "All detectors disabled";

            ImGui.TextColored(statusColor, statusText);

            if (detectorCount == 0)
            {
                ImGui.TextColored(UIHelpers.Colors.TextDim, "All tasks will require manual tracking.");
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Reset to defaults button
        if (ImGui.Button("Enable All Detectors"))
        {
            _configuration.FeatureFlags.EnableRouletteDetection = true;
            _configuration.FeatureFlags.EnableCactpotDetection = true;
            _configuration.FeatureFlags.EnableBeastTribeDetection = true;
            _configuration.Save();
            _plugin.ApplyDetectorFeatureFlags();
            Plugin.Log.Information("All detectors enabled");
        }

        ImGui.SameLine();

        if (ImGui.Button("Disable All Detectors"))
        {
            _configuration.FeatureFlags.EnableRouletteDetection = false;
            _configuration.FeatureFlags.EnableCactpotDetection = false;
            _configuration.FeatureFlags.EnableBeastTribeDetection = false;
            _configuration.Save();
            _plugin.ApplyDetectorFeatureFlags();
            Plugin.Log.Information("All detectors disabled");
        }
    }

    /// <summary>
    /// Draws checkbox list for enabling/disabling tasks in a category.
    /// </summary>
    private void DrawTaskToggleList(TaskCategory category)
    {
        var tasks = GetTasksForCategory(category);

        if (tasks.Count == 0)
        {
            ImGui.TextDisabled("No tasks available.");
            return;
        }

        foreach (var task in tasks)
        {
            using (ImRaii.PushId($"TaskToggle_{task.Id}"))
            {
                var isEnabled = task.IsEnabled;

                // Format: [x] Task Name (Location)
                var label = !string.IsNullOrEmpty(task.Location)
                    ? $"{task.Name} ({task.Location})"
                    : task.Name;

                if (ImGui.Checkbox(label, ref isEnabled))
                {
                    task.IsEnabled = isEnabled;
                    Plugin.Log.Debug($"Task '{task.Name}' enabled: {isEnabled}");

                    // Notify that state has changed (for persistence)
                    _onStateChanged?.Invoke();
                }

                // Show tooltip with task description
                if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(task.Description))
                {
                    var tooltipText = task.Description;

                    // Add detection type info
                    tooltipText += task.Detection switch
                    {
                        DetectionType.AutoDetected => "\n\n[Auto-detected]",
                        DetectionType.Hybrid => "\n\n[Auto-detected with manual override]",
                        DetectionType.Manual => "\n\n[Manual tracking only]",
                        _ => ""
                    };

                    ImGui.SetTooltip(tooltipText);
                }
            }
        }
    }

    /// <summary>
    /// Sets the enabled state for all tasks.
    /// </summary>
    private void SetAllTasksEnabled(bool enabled)
    {
        if (_taskList == null)
            return;

        foreach (var task in _taskList)
        {
            task.IsEnabled = enabled;
        }

        Plugin.Log.Information($"All tasks set to enabled: {enabled}");

        // Notify that state has changed (for persistence)
        _onStateChanged?.Invoke();
    }

    /// <summary>
    /// Resets all tasks to their default enabled states.
    /// Uses mutation-based reset to preserve list references, ensuring any UI
    /// components holding a reference to the task list will see the updated tasks
    /// without needing to rebind.
    /// </summary>
    private void ResetTasksToDefaults()
    {
        var defaultTasks = TaskRegistry.GetDefaultTasks();

        // Use mutation-based reset (Clear + AddRange) instead of replacing the list reference.
        // This ensures all UI components sharing the same list instance stay synchronized.
        if (_externalState != null)
        {
            _externalState.Tasks.Clear();
            _externalState.Tasks.AddRange(defaultTasks);
            // _taskList already references _externalState.Tasks, no reassignment needed
        }
        else
        {
            _taskList.Clear();
            _taskList.AddRange(defaultTasks);
        }

        Plugin.Log.Information("Tasks reset to defaults");

        // Notify that state has changed (for persistence)
        _onStateChanged?.Invoke();
    }

    /// <summary>
    /// Gets all tasks for a specific category, sorted by SortOrder.
    /// </summary>
    private List<ChecklistTask> GetTasksForCategory(TaskCategory category)
    {
        var result = new List<ChecklistTask>();

        if (_taskList == null)
            return result;

        foreach (var task in _taskList)
        {
            if (IsTaskInCategory(task, category))
            {
                result.Add(task);
            }
        }

        // Sort by SortOrder
        result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

        return result;
    }

    /// <summary>
    /// Gets the current task list.
    /// Used for synchronization with MainWindow.
    /// </summary>
    public List<ChecklistTask> GetTaskList() => _taskList;

    /// <summary>
    /// Updates the task list from an external source.
    /// Called to synchronize with MainWindow's ChecklistState.
    /// </summary>
    public void SetTaskList(List<ChecklistTask> tasks)
    {
        if (tasks != null)
        {
            _taskList = tasks;
        }
    }

    /// <summary>
    /// Updates the external state reference.
    /// Called when the checklist state is replaced (e.g., after loading from persistence).
    /// </summary>
    public void SetChecklistState(ChecklistState? state)
    {
        _externalState = state;
        _taskList = state?.Tasks ?? _taskList;
    }

    private static bool IsTaskInCategory(ChecklistTask task, TaskCategory category)
    {
        return category == TaskCategory.Daily
            ? task.Category == TaskCategory.Daily || task.Category == TaskCategory.GrandCompany
            : task.Category == category;
    }
}
