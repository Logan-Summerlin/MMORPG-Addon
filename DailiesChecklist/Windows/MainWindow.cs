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
/// Primary checklist window displaying daily and weekly activities.
/// Shows tasks with checkboxes, location, name, and auto-detect indicators.
/// Implements the UI wireframe from the project plan:
///
/// +------------------------------------------+
/// | Dailies Checklist                   [_][X]|
/// +------------------------------------------+
/// | v Daily Activities          [Reset All]  |
/// |   [ ] Gold Saucer - Mini Cactpot (0/3)   |
/// |   [x] Duty Finder - Leveling Roulette *  |
/// +------------------------------------------+
/// | v Weekly Activities         [Reset All]  |
/// |   [ ] Gold Saucer - Jumbo Cactpot (0/3)  |
/// +------------------------------------------+
/// | [Settings]              Last reset: 2h ago|
/// +------------------------------------------+
/// * = Auto-detected
/// </summary>
public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly ResetService? resetService;
    private readonly Action? onStateChanged;

    // Checklist state - managed externally via dependency injection
    private ChecklistState checklistState;

    private bool disposed = false;

    /// <summary>
    /// Initializes the main checklist window with dependency injection.
    /// </summary>
    /// <param name="plugin">Reference to the main plugin instance.</param>
    /// <param name="checklistState">The checklist state to display and modify. If null, creates default state.</param>
    /// <param name="resetService">Optional reset service for time formatting.</param>
    /// <param name="onStateChanged">Optional callback invoked when state changes (for persistence).</param>
    public MainWindow(
        Plugin plugin,
        ChecklistState? checklistState = null,
        ResetService? resetService = null,
        Action? onStateChanged = null)
        : base("Dailies Checklist###DailiesChecklistMain", ImGuiWindowFlags.None)
    {
        this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        this.configuration = plugin.Configuration;
        this.resetService = resetService;
        this.onStateChanged = onStateChanged;

        // Use provided state or create default for backward compatibility
        this.checklistState = checklistState ?? CreateDefaultState();

        // Window size constraints per requirements: MinimumSize 300x200, resizable
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        // Set initial size
        Size = new Vector2(350, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <summary>
    /// Creates a default checklist state for backward compatibility.
    /// Used when no state is provided via dependency injection.
    /// </summary>
    private static ChecklistState CreateDefaultState()
    {
        return new ChecklistState
        {
            Tasks = TaskRegistry.GetDefaultTasks(),
            LastDailyReset = GetLastDailyReset(),
            LastWeeklyReset = GetLastWeeklyReset()
        };
    }

    /// <summary>
    /// Calculates the last daily reset time (15:00 UTC).
    /// Used for default state creation.
    /// </summary>
    private static DateTime GetLastDailyReset()
    {
        var now = DateTime.UtcNow;
        var todayReset = now.Date.AddHours(15); // 15:00 UTC

        return now >= todayReset ? todayReset : todayReset.AddDays(-1);
    }

    /// <summary>
    /// Calculates the last weekly reset time (Tuesday 08:00 UTC).
    /// Used for default state creation.
    /// </summary>
    private static DateTime GetLastWeeklyReset()
    {
        var now = DateTime.UtcNow;
        var daysSinceTuesday = ((int)now.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;

        // If it's Tuesday but before 08:00 UTC, use last Tuesday
        if (daysSinceTuesday == 0 && now.TimeOfDay < TimeSpan.FromHours(8))
        {
            daysSinceTuesday = 7;
        }

        return now.Date.AddDays(-daysSinceTuesday).AddHours(8);
    }

    /// <summary>
    /// Cleanup when the window is disposed.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        // No event handlers to clean up in this window
        // All state is managed externally
    }

    /// <summary>
    /// Called before Draw() each frame. Use to modify flags dynamically.
    /// </summary>
    public override void PreDraw()
    {
        // Apply window lock setting before Draw() is called
        if (configuration.WindowLocked)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }

        // Apply opacity setting
        if (configuration.WindowOpacity < 1.0f)
        {
            ImGui.SetNextWindowBgAlpha(configuration.WindowOpacity);
        }
    }

    /// <summary>
    /// Main draw method called every frame when the window is open.
    /// </summary>
    public override void Draw()
    {
        // Draw Daily Activities section
        DrawCategorySection(TaskCategory.Daily, "Daily Activities");

        ImGui.Spacing();

        // Draw Weekly Activities section
        DrawCategorySection(TaskCategory.Weekly, "Weekly Activities");

        ImGui.Spacing();
        ImGui.Separator();

        // Draw footer with Settings button and last reset info
        DrawFooter();
    }

    /// <summary>
    /// Draws a collapsible section for a task category with Reset All button.
    /// </summary>
    private void DrawCategorySection(TaskCategory category, string headerLabel)
    {
        // Get tasks for this category that are enabled
        var categoryTasks = GetTasksForCategory(category);

        // Use ImGui.CollapsingHeader with AllowOverlap so we can add the Reset All button
        var isOpen = ImGui.CollapsingHeader(
            headerLabel,
            ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowOverlap);

        // Draw Reset All button on the same line as the header
        DrawResetAllButton(category, headerLabel);

        if (isOpen)
        {
            // Use ImRaii for automatic indent cleanup
            using (ImRaii.PushIndent(10f))
            {
                if (categoryTasks.Count == 0)
                {
                    ImGui.TextDisabled("No tasks enabled for this category.");
                }
                else
                {
                    foreach (var task in categoryTasks)
                    {
                        DrawTaskRow(task);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws the Reset All button aligned to the right of the header.
    /// </summary>
    private void DrawResetAllButton(TaskCategory category, string headerLabel)
    {
        // Calculate button position to align right
        var buttonLabel = "Reset All";
        var buttonWidth = ImGui.CalcTextSize(buttonLabel).X + ImGui.GetStyle().FramePadding.X * 2;
        var availableWidth = ImGui.GetContentRegionAvail().X;

        // Position button to the right of the header
        ImGui.SameLine(availableWidth - buttonWidth + ImGui.GetStyle().ItemSpacing.X);

        // Use unique ID to avoid conflicts
        using (ImRaii.PushId($"ResetAll_{category}"))
        {
            if (ImGui.SmallButton(buttonLabel))
            {
                ResetCategory(category);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Reset all {headerLabel.ToLower()} to unchecked");
        }
    }

    /// <summary>
    /// Resets all tasks in a category to unchecked.
    /// </summary>
    private void ResetCategory(TaskCategory category)
    {
        if (checklistState?.Tasks == null)
            return;

        foreach (var task in checklistState.Tasks)
        {
            if (task.Category == category)
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
            }
        }

        Plugin.Log.Information($"Reset all {category} tasks");

        // Notify that state has changed (for persistence)
        onStateChanged?.Invoke();
    }

    /// <summary>
    /// Draws a single task row with checkbox, location, name, and auto-detect indicator.
    /// Format: [x] Location - Task Name *
    /// </summary>
    private void DrawTaskRow(ChecklistTask task)
    {
        // Use unique ID for each task's checkbox
        using (ImRaii.PushId(task.Id))
        {
            // Checkbox for task completion
            var isCompleted = task.IsCompleted;
            if (ImGui.Checkbox("##TaskCheckbox", ref isCompleted))
            {
                // Only toggle if state actually changed
                if (isCompleted != task.IsCompleted)
                {
                    ToggleTask(task);
                }
            }

            ImGui.SameLine();

            // Task text: "Location - Task Name"
            var taskText = configuration.ShowLocations && !string.IsNullOrEmpty(task.Location)
                ? $"{task.Location} - {task.Name}"
                : task.Name;

            // Gray out completed tasks for visual distinction
            if (task.IsCompleted)
            {
                ImGui.TextDisabled(taskText);
            }
            else
            {
                ImGui.Text(taskText);
            }

            // Auto-detect indicator: show asterisk (*) for auto-detected tasks
            if (configuration.ShowAutoDetectIndicators && task.Detection != DetectionType.Manual)
            {
                ImGui.SameLine();
                if (task.IsManuallySet)
                {
                    // Manually overridden auto-detected task
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "(manual)");
                }
                else if (task.IsCompleted && task.Detection == DetectionType.AutoDetected)
                {
                    // Auto-detected as complete
                    ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), "*");
                }
                else if (task.Detection == DetectionType.AutoDetected || task.Detection == DetectionType.Hybrid)
                {
                    // Can be auto-detected but not yet
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "*");
                }
            }

            // Tooltip with task description
            if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(task.Description))
            {
                var tooltipText = task.Description;

                // Add detection type info to tooltip
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

    /// <summary>
    /// Toggles a task's completion state.
    /// </summary>
    private void ToggleTask(ChecklistTask task)
    {
        task.IsCompleted = !task.IsCompleted;
        task.IsManuallySet = true;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;

        Plugin.Log.Debug($"Task '{task.Name}' toggled to {(task.IsCompleted ? "completed" : "incomplete")}");

        // Notify that state has changed (for persistence)
        onStateChanged?.Invoke();
    }

    /// <summary>
    /// Draws the footer with Settings button and last reset time.
    /// </summary>
    private void DrawFooter()
    {
        // Settings button on the left
        if (ImGui.Button("Settings"))
        {
            plugin.ToggleSettingsUI();
        }

        // Last reset time on the right
        ImGui.SameLine();

        var lastResetText = GetLastResetText();
        var textWidth = ImGui.CalcTextSize(lastResetText).X;
        var availableWidth = ImGui.GetContentRegionAvail().X;

        // Position text to the right
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availableWidth - textWidth);
        ImGui.TextDisabled(lastResetText);
    }

    /// <summary>
    /// Gets the formatted text showing time until next reset.
    /// Uses ResetService if available, otherwise falls back to manual calculation.
    /// </summary>
    private string GetLastResetText()
    {
        // Use ResetService if available for accurate time until next reset
        if (resetService != null)
        {
            try
            {
                var timeUntilReset = resetService.GetFormattedTimeUntilReset(ResetType.Daily);
                return $"Next reset: {timeUntilReset}";
            }
            catch
            {
                // Fall through to legacy calculation on error
            }
        }

        // Fallback: Calculate time since last reset manually
        if (checklistState == null)
            return "Last reset: unknown";

        // Calculate time since last daily reset
        var timeSinceReset = DateTime.UtcNow - checklistState.LastDailyReset;

        // Handle negative time (reset hasn't happened yet today)
        if (timeSinceReset.TotalSeconds < 0)
        {
            timeSinceReset = TimeSpan.Zero;
        }

        if (timeSinceReset.TotalMinutes < 1)
        {
            return "Last reset: just now";
        }
        else if (timeSinceReset.TotalHours < 1)
        {
            var minutes = (int)timeSinceReset.TotalMinutes;
            return $"Last reset: {minutes}m ago";
        }
        else if (timeSinceReset.TotalDays < 1)
        {
            var hours = (int)timeSinceReset.TotalHours;
            return $"Last reset: {hours}h ago";
        }
        else
        {
            var days = (int)timeSinceReset.TotalDays;
            return $"Last reset: {days}d ago";
        }
    }

    /// <summary>
    /// Gets enabled tasks for a specific category, sorted by SortOrder.
    /// </summary>
    private List<ChecklistTask> GetTasksForCategory(TaskCategory category)
    {
        var result = new List<ChecklistTask>();

        if (checklistState?.Tasks == null)
            return result;

        foreach (var task in checklistState.Tasks)
        {
            if (task.Category == category && task.IsEnabled)
            {
                result.Add(task);
            }
        }

        // Sort by SortOrder
        result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

        return result;
    }

    /// <summary>
    /// Gets the current checklist state.
    /// Used by other components to access task data.
    /// </summary>
    public ChecklistState GetChecklistState() => checklistState;

    /// <summary>
    /// Sets the checklist state from external source.
    /// Called when state is loaded from persistence.
    /// </summary>
    public void SetChecklistState(ChecklistState state)
    {
        if (state != null)
        {
            checklistState = state;
        }
    }
}
