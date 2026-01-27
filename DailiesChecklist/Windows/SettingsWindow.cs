using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using DailiesChecklist.Models;
using DailiesChecklist.Services;
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
    private readonly Plugin plugin;
    private readonly Configuration configuration;

    // Cached task list for the settings UI
    private List<ChecklistTask> taskList;

    private bool disposed = false;

    /// <summary>
    /// Initializes the settings window.
    /// </summary>
    /// <param name="plugin">Reference to the main plugin instance.</param>
    public SettingsWindow(Plugin plugin)
        : base("Dailies Checklist Settings###DailiesChecklistSettings",
            ImGuiWindowFlags.NoCollapse)
    {
        this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        this.configuration = plugin.Configuration;

        // Initialize task list from registry
        taskList = TaskRegistry.GetDefaultTasks();

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
        if (disposed)
            return;

        disposed = true;
        // No event handlers to clean up
    }

    /// <summary>
    /// Called when the window is closed.
    /// Auto-saves settings.
    /// </summary>
    public override void OnClose()
    {
        // Auto-save when closing
        configuration.Save();
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
        var opacity = configuration.WindowOpacity * 100f; // Convert to percentage
        if (ImGui.SliderFloat("##Opacity", ref opacity, 25f, 100f, "%.0f%%"))
        {
            configuration.WindowOpacity = opacity / 100f; // Convert back to 0-1 range
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Adjust the background transparency of the main window (25% - 100%)");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Lock Position Checkbox
        var windowLocked = configuration.WindowLocked;
        if (ImGui.Checkbox("Lock Window Position", ref windowLocked))
        {
            configuration.WindowLocked = windowLocked;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Prevent the main window from being moved");
        }

        ImGui.Spacing();

        // Show Locations Checkbox
        var showLocations = configuration.ShowLocations;
        if (ImGui.Checkbox("Show Task Locations", ref showLocations))
        {
            configuration.ShowLocations = showLocations;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Display the location for each task (e.g., \"Gold Saucer - Mini Cactpot\")");
        }

        ImGui.Spacing();

        // Show Auto-Detect Indicators Checkbox
        var showAutoDetect = configuration.ShowAutoDetectIndicators;
        if (ImGui.Checkbox("Show Auto-Detect Indicators", ref showAutoDetect))
        {
            configuration.ShowAutoDetectIndicators = showAutoDetect;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Show asterisk (*) next to tasks that can be automatically detected");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Section collapse defaults
        ImGui.Text("Default Section States:");
        ImGui.Spacing();

        using (ImRaii.PushIndent(10f))
        {
            var collapseDailyByDefault = configuration.CollapseDailyByDefault;
            if (ImGui.Checkbox("Collapse Daily Activities by default", ref collapseDailyByDefault))
            {
                configuration.CollapseDailyByDefault = collapseDailyByDefault;
                configuration.Save();
            }

            var collapseWeeklyByDefault = configuration.CollapseWeeklyByDefault;
            if (ImGui.Checkbox("Collapse Weekly Activities by default", ref collapseWeeklyByDefault))
            {
                configuration.CollapseWeeklyByDefault = collapseWeeklyByDefault;
                configuration.Save();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Reset Window Position Button
        if (ImGui.Button("Reset Window Position"))
        {
            configuration.WindowPosition = null;
            configuration.WindowSize = null;
            configuration.Save();
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
                    // Note: This updates the local task list. For full persistence,
                    // the MainWindow's ChecklistState should be synchronized.
                    Plugin.Log.Debug($"Task '{task.Name}' enabled: {isEnabled}");
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
        if (taskList == null)
            return;

        foreach (var task in taskList)
        {
            task.IsEnabled = enabled;
        }

        Plugin.Log.Information($"All tasks set to enabled: {enabled}");
    }

    /// <summary>
    /// Resets all tasks to their default enabled states.
    /// </summary>
    private void ResetTasksToDefaults()
    {
        taskList = TaskRegistry.GetDefaultTasks();
        Plugin.Log.Information("Tasks reset to defaults");
    }

    /// <summary>
    /// Gets all tasks for a specific category, sorted by SortOrder.
    /// </summary>
    private List<ChecklistTask> GetTasksForCategory(TaskCategory category)
    {
        var result = new List<ChecklistTask>();

        if (taskList == null)
            return result;

        foreach (var task in taskList)
        {
            if (task.Category == category)
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
    public List<ChecklistTask> GetTaskList() => taskList;

    /// <summary>
    /// Updates the task list from an external source.
    /// Called to synchronize with MainWindow's ChecklistState.
    /// </summary>
    public void SetTaskList(List<ChecklistTask> tasks)
    {
        if (tasks != null)
        {
            taskList = tasks;
        }
    }
}
