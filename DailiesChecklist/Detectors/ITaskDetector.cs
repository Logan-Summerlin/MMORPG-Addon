using System;

namespace DailiesChecklist.Detectors;

/// <summary>
/// Interface for task detectors that monitor game state to automatically detect
/// completion of daily/weekly activities.
/// </summary>
/// <remarks>
/// Implementations should:
/// - Subscribe to relevant Dalamud events in Initialize()
/// - Unsubscribe from all events in Dispose()
/// - Handle exceptions gracefully to avoid crashing the plugin
/// - Fire OnTaskStateChanged when detection state updates
/// </remarks>
public interface ITaskDetector : IDisposable
{
    /// <summary>
    /// Gets the array of task IDs that this detector can provide completion state for.
    /// Task IDs should match those defined in the task registry.
    /// </summary>
    /// <example>
    /// For RouletteDetector: ["roulette_leveling", "roulette_expert", "roulette_trials", ...]
    /// </example>
    string[] SupportedTaskIds { get; }

    /// <summary>
    /// Gets or sets whether this detector is enabled.
    /// When disabled, the detector should not process events or update state.
    /// This acts as a feature flag for graceful degradation.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initializes the detector, subscribing to relevant Dalamud events.
    /// Should be called after the detector is registered with the DetectionService.
    /// </summary>
    /// <remarks>
    /// This method should:
    /// - Subscribe to necessary Dalamud service events
    /// - Perform any initial state queries
    /// - Be idempotent (safe to call multiple times)
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Gets the current completion state for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID to query (must be in SupportedTaskIds).</param>
    /// <returns>
    /// true if the task is detected as complete,
    /// false if the task is detected as incomplete,
    /// null if the completion state is unknown or cannot be determined.
    /// </returns>
    /// <remarks>
    /// Returning null indicates the detector cannot determine state, which allows
    /// the UI to fall back to manual tracking or show an "unknown" indicator.
    /// </remarks>
    bool? GetCompletionState(string taskId);

    /// <summary>
    /// Event fired when a task's completion state changes.
    /// </summary>
    /// <remarks>
    /// Parameters:
    /// - taskId: The ID of the task whose state changed
    /// - isCompleted: true if the task is now complete, false otherwise
    ///
    /// Implementations should fire this event whenever detection logic
    /// determines a state change, allowing the DetectionService to
    /// aggregate updates and notify the UI.
    /// </remarks>
    event Action<string, bool>? OnTaskStateChanged;
}
