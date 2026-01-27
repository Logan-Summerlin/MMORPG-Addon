using Dalamud.Configuration;
using System;
using System.Numerics;

namespace DailiesChecklist;

/// <summary>
/// Plugin configuration that persists between sessions.
/// Implements IPluginConfiguration for Dalamud's configuration serialization system.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    /// <summary>
    /// Configuration version for migration support.
    /// Increment this when making breaking changes to the configuration schema.
    /// </summary>
    public int Version { get; set; } = 0;

    #region Window Display Settings

    /// <summary>
    /// Whether the main checklist window is currently visible.
    /// </summary>
    public bool MainWindowVisible { get; set; } = true;

    /// <summary>
    /// Whether the settings window is currently visible.
    /// </summary>
    public bool SettingsWindowVisible { get; set; } = false;

    /// <summary>
    /// Window opacity (0.25 to 1.0).
    /// </summary>
    public float WindowOpacity { get; set; } = 1.0f;

    /// <summary>
    /// Whether the window position is locked (prevents dragging).
    /// </summary>
    public bool WindowLocked { get; set; } = false;

    /// <summary>
    /// Saved window position. Null means use default position.
    /// </summary>
    public Vector2? WindowPosition { get; set; } = null;

    /// <summary>
    /// Saved window size. Null means use default size.
    /// </summary>
    public Vector2? WindowSize { get; set; } = null;

    #endregion

    #region Display Preferences

    /// <summary>
    /// Whether to show location text for each task.
    /// </summary>
    public bool ShowLocations { get; set; } = true;

    /// <summary>
    /// Whether to show indicators for auto-detected tasks.
    /// </summary>
    public bool ShowAutoDetectIndicators { get; set; } = true;

    /// <summary>
    /// Whether to collapse the daily section by default.
    /// </summary>
    public bool CollapseDailyByDefault { get; set; } = false;

    /// <summary>
    /// Whether to collapse the weekly section by default.
    /// </summary>
    public bool CollapseWeeklyByDefault { get; set; } = false;

    #endregion

    /// <summary>
    /// Saves the configuration to disk.
    /// Uses Dalamud's plugin configuration serialization system.
    /// </summary>
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
