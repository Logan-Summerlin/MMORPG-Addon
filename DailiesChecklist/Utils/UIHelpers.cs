using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace DailiesChecklist.Utils;

/// <summary>
/// Centralized UI helper methods for common ImGui operations.
/// Inspired by BozjaBuddy's UtilsGUI pattern for consistent UI components.
/// </summary>
public static class UIHelpers
{
    #region Color Constants

    /// <summary>
    /// Predefined color palette for consistent theming across the plugin UI.
    /// Colors are Vector4 in RGBA format (0.0-1.0 range).
    /// </summary>
    public static class Colors
    {
        // Status Colors
        public static readonly Vector4 Success = new(0.4f, 0.8f, 0.4f, 1.0f);       // Green for completed
        public static readonly Vector4 Warning = new(0.9f, 0.7f, 0.2f, 1.0f);       // Yellow/amber for attention
        public static readonly Vector4 Error = new(0.9f, 0.3f, 0.3f, 1.0f);         // Red for errors
        public static readonly Vector4 Info = new(0.4f, 0.6f, 0.9f, 1.0f);          // Blue for information

        // UI Element Colors
        public static readonly Vector4 Disabled = new(0.5f, 0.5f, 0.5f, 1.0f);      // Gray for disabled items
        public static readonly Vector4 Subtle = new(0.6f, 0.6f, 0.6f, 1.0f);        // Subtle gray for hints
        public static readonly Vector4 AutoDetect = new(0.4f, 0.8f, 0.4f, 1.0f);    // Green asterisk
        public static readonly Vector4 ManualOverride = new(0.7f, 0.7f, 0.7f, 1.0f); // Gray for manual

        // Progress Bar Colors
        public static readonly Vector4 ProgressBarBg = new(0.2f, 0.2f, 0.2f, 1.0f);
        public static readonly Vector4 ProgressBarFill = new(0.35f, 0.65f, 0.35f, 1.0f);
        public static readonly Vector4 ProgressBarComplete = new(0.3f, 0.7f, 0.3f, 1.0f);

        // Text Colors
        public static readonly Vector4 TextNormal = new(1.0f, 1.0f, 1.0f, 1.0f);
        public static readonly Vector4 TextDim = new(0.7f, 0.7f, 0.7f, 1.0f);
        public static readonly Vector4 TextHighlight = new(1.0f, 0.9f, 0.4f, 1.0f);
    }

    #endregion

    #region Tooltip Helpers

    /// <summary>
    /// Displays a tooltip for the last rendered item when hovered.
    /// Automatically wraps text at the specified width.
    /// </summary>
    /// <param name="text">The tooltip text to display.</param>
    /// <param name="maxWidth">Maximum width before text wrapping (default 350).</param>
    public static void SetTooltipForLastItem(string text, float maxWidth = 350f)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(maxWidth);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// <summary>
    /// Displays a help marker "(?) " with tooltip on hover.
    /// Useful for providing additional context about settings or features.
    /// </summary>
    /// <param name="description">The help text to display.</param>
    /// <param name="sameLine">Whether to render on the same line as previous item.</param>
    public static void HelpMarker(string description, bool sameLine = true)
    {
        if (sameLine)
            ImGui.SameLine();

        ImGui.TextDisabled("(?)");

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(350f);
            ImGui.TextUnformatted(description);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// <summary>
    /// Renders text with a help marker on the same line.
    /// </summary>
    /// <param name="text">The main text to display.</param>
    /// <param name="helpText">The help tooltip text.</param>
    /// <param name="textColor">Optional color for the main text.</param>
    public static void TextWithHelpMarker(string text, string helpText, Vector4? textColor = null)
    {
        if (textColor.HasValue)
        {
            ImGui.TextColored(textColor.Value, text);
        }
        else
        {
            ImGui.Text(text);
        }

        HelpMarker(helpText);
    }

    #endregion

    #region Progress Indicators

    /// <summary>
    /// Draws a simple progress bar with completion count text.
    /// </summary>
    /// <param name="current">Current progress value.</param>
    /// <param name="max">Maximum progress value.</param>
    /// <param name="width">Width of the progress bar (-1 for full width).</param>
    /// <param name="height">Height of the progress bar.</param>
    /// <param name="showText">Whether to overlay text showing current/max.</param>
    public static void ProgressBar(int current, int max, float width = -1f, float height = 18f, bool showText = true)
    {
        if (max <= 0)
            max = 1;

        var fraction = Math.Clamp((float)current / max, 0f, 1f);
        var isComplete = current >= max;

        // Choose color based on completion state
        var barColor = isComplete ? Colors.ProgressBarComplete : Colors.ProgressBarFill;

        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Colors.ProgressBarBg);

        var size = new Vector2(width < 0 ? ImGui.GetContentRegionAvail().X : width, height);
        var label = showText ? $"{current}/{max}" : "";

        ImGui.ProgressBar(fraction, size, label);

        ImGui.PopStyleColor(2);
    }

    /// <summary>
    /// Draws a compact progress indicator as text (e.g., "5/12").
    /// </summary>
    /// <param name="current">Current progress value.</param>
    /// <param name="max">Maximum progress value.</param>
    /// <param name="prefix">Optional prefix text.</param>
    public static void ProgressText(int current, int max, string? prefix = null)
    {
        var isComplete = current >= max;
        var color = isComplete ? Colors.Success : Colors.TextNormal;
        var text = prefix != null ? $"{prefix} {current}/{max}" : $"{current}/{max}";

        ImGui.TextColored(color, text);
    }

    /// <summary>
    /// Draws a section header with integrated progress indicator.
    /// </summary>
    /// <param name="label">The section header text.</param>
    /// <param name="completedCount">Number of completed items.</param>
    /// <param name="totalCount">Total number of items.</param>
    /// <returns>True if the header is expanded, false if collapsed.</returns>
    public static bool CollapsingHeaderWithProgress(string label, int completedCount, int totalCount)
    {
        // Draw the collapsing header with AllowOverlap
        var isOpen = ImGui.CollapsingHeader(
            label,
            ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowOverlap);

        // Calculate progress text position (right-aligned)
        var progressText = $"{completedCount}/{totalCount}";
        var progressWidth = ImGui.CalcTextSize(progressText).X + ImGui.GetStyle().FramePadding.X * 2;
        var availableWidth = ImGui.GetContentRegionAvail().X;

        ImGui.SameLine(availableWidth - progressWidth + ImGui.GetStyle().ItemSpacing.X);

        // Color based on completion
        var color = completedCount >= totalCount ? Colors.Success : Colors.TextDim;
        ImGui.TextColored(color, progressText);

        return isOpen;
    }

    #endregion

    #region Status Indicators

    /// <summary>
    /// Draws an auto-detection status indicator.
    /// </summary>
    /// <param name="isAutoDetected">Whether the item was auto-detected.</param>
    /// <param name="isManuallyOverridden">Whether the item was manually overridden.</param>
    public static void AutoDetectIndicator(bool isAutoDetected, bool isManuallyOverridden = false)
    {
        ImGui.SameLine();

        if (isManuallyOverridden)
        {
            ImGui.TextColored(Colors.ManualOverride, "(manual)");
            SetTooltipForLastItem("Manually overridden - auto-detection paused for this item");
        }
        else if (isAutoDetected)
        {
            ImGui.TextColored(Colors.AutoDetect, "*");
            SetTooltipForLastItem("Auto-detected from game state");
        }
        else
        {
            ImGui.TextColored(Colors.Subtle, "*");
            SetTooltipForLastItem("Can be auto-detected (not yet detected)");
        }
    }

    /// <summary>
    /// Draws a reset countdown timer.
    /// </summary>
    /// <param name="label">The label for the timer.</param>
    /// <param name="timeRemaining">Time remaining until reset.</param>
    public static void ResetCountdown(string label, TimeSpan timeRemaining)
    {
        var text = FormatTimeSpan(timeRemaining);
        var color = timeRemaining.TotalHours < 1 ? Colors.Warning : Colors.TextDim;

        ImGui.TextColored(color, $"{label}: {text}");
    }

    #endregion

    #region Section Separators

    /// <summary>
    /// Draws a styled section separator with optional label.
    /// </summary>
    /// <param name="label">Optional label to display in the separator.</param>
    public static void SectionSeparator(string? label = null)
    {
        ImGui.Spacing();

        if (string.IsNullOrEmpty(label))
        {
            ImGui.Separator();
        }
        else
        {
            // Centered label separator
            var windowWidth = ImGui.GetContentRegionAvail().X;
            var textWidth = ImGui.CalcTextSize(label).X;
            var lineWidth = (windowWidth - textWidth - 20) / 2;

            if (lineWidth > 0)
            {
                var cursorY = ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() / 2;

                // Left line
                ImGui.GetWindowDrawList().AddLine(
                    new Vector2(ImGui.GetCursorScreenPos().X, cursorY),
                    new Vector2(ImGui.GetCursorScreenPos().X + lineWidth, cursorY),
                    ImGui.GetColorU32(Colors.Subtle));

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + lineWidth + 10);
                ImGui.TextColored(Colors.Subtle, label);
                ImGui.SameLine();

                // Right line
                var rightLineStart = ImGui.GetCursorScreenPos().X + 10;
                ImGui.GetWindowDrawList().AddLine(
                    new Vector2(rightLineStart, cursorY),
                    new Vector2(rightLineStart + lineWidth, cursorY),
                    ImGui.GetColorU32(Colors.Subtle));

                ImGui.NewLine();
            }
            else
            {
                ImGui.TextColored(Colors.Subtle, label);
            }
        }

        ImGui.Spacing();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Formats a TimeSpan into a human-readable string.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format.</param>
    /// <returns>Formatted string like "2h 30m" or "1d 5h".</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 0)
        {
            return "Now";
        }

        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            var hours = timeSpan.Hours;
            return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
        }

        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes}m";
        }

        return $"{timeSpan.Seconds}s";
    }

    /// <summary>
    /// Adjusts the alpha (transparency) of a color.
    /// </summary>
    /// <param name="color">The color to adjust.</param>
    /// <param name="alpha">The new alpha value (0.0-1.0).</param>
    /// <returns>The color with adjusted alpha.</returns>
    public static Vector4 WithAlpha(Vector4 color, float alpha)
    {
        return new Vector4(color.X, color.Y, color.Z, alpha);
    }

    /// <summary>
    /// Lerps between two colors based on a progress value.
    /// </summary>
    /// <param name="from">Starting color.</param>
    /// <param name="to">Ending color.</param>
    /// <param name="progress">Progress value (0.0-1.0).</param>
    /// <returns>The interpolated color.</returns>
    public static Vector4 LerpColor(Vector4 from, Vector4 to, float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        return new Vector4(
            from.X + (to.X - from.X) * progress,
            from.Y + (to.Y - from.Y) * progress,
            from.Z + (to.Z - from.Z) * progress,
            from.W + (to.W - from.W) * progress);
    }

    #endregion

    #region Confirmation Dialogs

    /// <summary>
    /// Draws a confirmation popup modal.
    /// </summary>
    /// <param name="id">Unique ID for the popup.</param>
    /// <param name="message">The confirmation message.</param>
    /// <param name="onConfirm">Action to execute on confirmation.</param>
    /// <param name="confirmText">Text for the confirm button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    public static void ConfirmationPopup(
        string id,
        string message,
        Action onConfirm,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        if (!_popupOpen.TryGetValue(id, out var popupOpen))
        {
            popupOpen = true;
        }

        if (ImGui.BeginPopupModal(id, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(message);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button(confirmText, new Vector2(100, 0)))
            {
                onConfirm();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button(cancelText, new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _popupOpen[id] = popupOpen;
    }

    // Helper for popup state
    private static readonly Dictionary<string, bool> _popupOpen = new(StringComparer.Ordinal);

    /// <summary>
    /// Opens a confirmation popup by ID.
    /// </summary>
    /// <param name="id">The popup ID to open.</param>
    public static void OpenConfirmationPopup(string id)
    {
        _popupOpen[id] = true;
        ImGui.OpenPopup(id);
    }

    #endregion
}
