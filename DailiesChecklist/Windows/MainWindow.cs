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
    private readonly Plugin _plugin;
    private readonly Configuration _configuration;
    private readonly ResetService? _resetService;
    private readonly Action? _onStateChanged;

    /// <summary>
    /// Checklist state - managed externally via dependency injection.
    /// </summary>
    private ChecklistState _checklistState;

    private bool _disposed;

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
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _configuration = plugin.Configuration;
        _resetService = resetService;
        _onStateChanged = onStateChanged;

        // Use provided state or create default for backward compatibility
        _checklistState = checklistState ?? CreateDefaultState();

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
        if (_disposed)
            return;

        _disposed = true;
        // No event handlers to clean up in this window
        // All state is managed externally
    }

    /// <summary>
    /// Called before Draw() each frame. Use to modify flags dynamically.
    /// </summary>
    public override void PreDraw()
    {
        // Apply window lock setting before Draw() is called
        if (_configuration.WindowLocked)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }

        // Apply opacity setting
        if (_configuration.WindowOpacity < 1.0f)
        {
            ImGui.SetNextWindowBgAlpha(_configuration.WindowOpacity);
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
    /// Draws a collapsible section for a task category with progress indicator and Reset All button.
    /// </summary>
    private void DrawCategorySection(TaskCategory category, string headerLabel)
    {
        // Get tasks for this category that are enabled
        var categoryTasks = GetTasksForCategory(category);

        // Calculate completion stats for progress indicator
        var completedCount = 0;
        var totalCount = categoryTasks.Count;
        foreach (var task in categoryTasks)
        {
            if (task.IsCompleted)
                completedCount++;
        }

        // Use ImGui.CollapsingHeader with AllowOverlap so we can add the progress and Reset All button
        var isOpen = ImGui.CollapsingHeader(
            headerLabel,
            GetCategoryHeaderFlags(category));

        // Draw progress indicator and Reset All button on the same line as the header
        DrawHeaderControls(category, headerLabel, completedCount, totalCount);

        if (isOpen)
        {
            // Show progress bar when section is open and has tasks
            if (totalCount > 0 && _configuration.ShowProgressBars)
            {
                using (ImRaii.PushIndent(10f))
                {
                    UIHelpers.ProgressBar(completedCount, totalCount, -1f, 14f, true);
                    ImGui.Spacing();
                }
            }

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
    /// Draws the header controls (progress text and Reset All button) aligned to the right.
    /// </summary>
    private void DrawHeaderControls(TaskCategory category, string headerLabel, int completedCount, int totalCount)
    {
        // Calculate positions for progress text and button
        var resetButtonLabel = "Reset";
        var resetButtonWidth = ImGui.CalcTextSize(resetButtonLabel).X + ImGui.GetStyle().FramePadding.X * 2;
        var progressText = $"{completedCount}/{totalCount}";
        var progressWidth = ImGui.CalcTextSize(progressText).X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var availableWidth = ImGui.GetContentRegionAvail().X;

        // Position for progress text (left of button)
        var progressPosX = availableWidth - resetButtonWidth - progressWidth - spacing * 2 + ImGui.GetStyle().ItemSpacing.X;

        // Draw progress text
        ImGui.SameLine(progressPosX);
        var progressColor = completedCount >= totalCount ? UIHelpers.Colors.Success : UIHelpers.Colors.TextDim;
        ImGui.TextColored(progressColor, progressText);

        // Draw Reset All button
        ImGui.SameLine();
        using (ImRaii.PushId($"ResetAll_{category}"))
        {
            if (ImGui.SmallButton(resetButtonLabel))
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
        if (_checklistState?.Tasks == null)
            return;

        foreach (var task in _checklistState.Tasks)
        {
            if (IsTaskInCategory(task, category))
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
                task.CurrentCount = 0;
            }
        }

        Service.Log.Information($"Reset all {category} tasks");

        // Notify that state has changed (for persistence)
        _onStateChanged?.Invoke();
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
            var taskText = _configuration.ShowLocations && !string.IsNullOrEmpty(task.Location)
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

            // IMPORTANT: Capture hover state immediately after drawing the task text.
            // This fixes a UX bug where the tooltip was attached to the last drawn item
            // (count text or auto-detect indicator) instead of the task label.
            var isTaskTextHovered = ImGui.IsItemHovered();

            // Auto-detect indicator: show asterisk (*) for auto-detected tasks
            if (_configuration.ShowAutoDetectIndicators && task.Detection != DetectionType.Manual)
            {
                ImGui.SameLine();
                if (task.IsManuallySet)
                {
                    // Manually overridden auto-detected task
                    ImGui.TextColored(UIHelpers.Colors.ManualOverride, "(manual)");
                }
                else if (task.IsCompleted && task.Detection == DetectionType.AutoDetected)
                {
                    // Auto-detected as complete
                    ImGui.TextColored(UIHelpers.Colors.AutoDetect, "*");
                }
                else if (task.Detection == DetectionType.AutoDetected || task.Detection == DetectionType.Hybrid)
                {
                    // Can be auto-detected but not yet
                    ImGui.TextColored(UIHelpers.Colors.Subtle, "*");
                }
            }

            // Show count progress for multi-count tasks (e.g., Mini Cactpot 2/3)
            if (task.MaxCount > 1)
            {
                ImGui.SameLine();
                var countColor = task.CurrentCount >= task.MaxCount ? UIHelpers.Colors.Success : UIHelpers.Colors.TextDim;
                ImGui.TextColored(countColor, $"({task.CurrentCount}/{task.MaxCount})");
            }

            // Tooltip with task description - shown when hovering the task text
            // Uses the captured hover state from immediately after drawing the task label
            if (isTaskTextHovered && !string.IsNullOrEmpty(task.Description))
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
        if (task.MaxCount > 1)
        {
            task.CurrentCount = task.IsCompleted ? task.MaxCount : 0;
        }

        Service.Log.Debug($"Task '{task.Name}' toggled to {(task.IsCompleted ? "completed" : "incomplete")}");

        // Notify that state has changed (for persistence)
        _onStateChanged?.Invoke();
    }

    /// <summary>
    /// Draws the footer with Settings button and last reset time.
    /// </summary>
    private void DrawFooter()
    {
        // Settings button on the left
        if (ImGui.Button("Settings"))
        {
            _plugin.ToggleSettingsUI();
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
        if (_resetService != null)
        {
            try
            {
                var timeUntilReset = _resetService.GetFormattedTimeUntilReset(ResetType.Daily);
                return $"Next reset: {timeUntilReset}";
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "Failed to calculate reset text from ResetService.");
                // Fall through to legacy calculation on error
            }
        }

        // Fallback: Calculate time since last reset manually
        if (_checklistState == null)
            return "Last reset: unknown";

        // Calculate time since last daily reset
        var timeSinceReset = DateTime.UtcNow - _checklistState.LastDailyReset;

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

        if (_checklistState?.Tasks == null)
            return result;

        foreach (var task in _checklistState.Tasks)
        {
            if (IsTaskInCategory(task, category) && task.IsEnabled)
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
    public ChecklistState GetChecklistState() => _checklistState;

    /// <summary>
    /// Sets the checklist state from external source.
    /// Called when state is loaded from persistence.
    /// </summary>
    public void SetChecklistState(ChecklistState state)
    {
        if (state != null)
        {
            _checklistState = state;
        }
    }

    private bool IsTaskInCategory(ChecklistTask task, TaskCategory category)
    {
        return category == TaskCategory.Daily
            ? task.Category == TaskCategory.Daily || task.Category == TaskCategory.GrandCompany
            : task.Category == category;
    }

    private ImGuiTreeNodeFlags GetCategoryHeaderFlags(TaskCategory category)
    {
        var flags = ImGuiTreeNodeFlags.AllowOverlap;
        var defaultOpen = category switch
        {
            TaskCategory.Daily => !_configuration.CollapseDailyByDefault,
            TaskCategory.Weekly => !_configuration.CollapseWeeklyByDefault,
            _ => true
        };

        if (defaultOpen)
        {
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        }

        return flags;
    }
}
