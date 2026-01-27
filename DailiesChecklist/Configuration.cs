using Dalamud.Configuration;
using System;
using System.Numerics;

namespace DailiesChecklist;

/// <summary>
/// Feature flags for graceful degradation.
/// Allows disabling specific detectors if they cause issues.
/// </summary>
public class DetectorFeatureFlags
{
    /// <summary>Enable/disable RouletteDetector auto-detection.</summary>
    public bool EnableRouletteDetection { get; set; } = true;

    /// <summary>Enable/disable CactpotDetector auto-detection.</summary>
    public bool EnableCactpotDetection { get; set; } = true;

    /// <summary>Enable/disable BeastTribeDetector auto-detection.</summary>
    public bool EnableBeastTribeDetection { get; set; } = true;
}

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
    private int _version = 0;

    /// <summary>
    /// Configuration version with bounds validation.
    /// Negative values are not allowed.
    /// </summary>
    public int Version
    {
        get => _version;
        set => _version = Math.Max(0, value);
    }

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
    private float _windowOpacity = 1.0f;

    /// <summary>
    /// Window opacity with bounds validation (0.25 to 1.0).
    /// Values outside this range are clamped automatically.
    /// </summary>
    public float WindowOpacity
    {
        get => _windowOpacity;
        set => _windowOpacity = Math.Clamp(value, 0.25f, 1.0f);
    }

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

    /// <summary>
    /// Whether to show progress bars for task categories.
    /// </summary>
    public bool ShowProgressBars { get; set; } = true;

    #endregion

    #region Feature Flags

    /// <summary>
    /// Feature flags for detector modules.
    /// </summary>
    public DetectorFeatureFlags FeatureFlags { get; set; } = new();

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
