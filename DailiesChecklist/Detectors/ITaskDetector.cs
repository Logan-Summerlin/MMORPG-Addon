using System;
using System.Collections.Generic;

namespace DailiesChecklist.Detectors;

/// <summary>
/// Describes a limitation in a detector's ability to track task completion.
/// Used to communicate to the UI when detection may be incomplete.
/// </summary>
/// <param name="TaskId">The task ID this limitation applies to, or null if it applies to all tasks.</param>
/// <param name="LimitationType">The type of limitation (e.g., "SessionOnly", "NotImplemented", "PartialDetection").</param>
/// <param name="Description">A user-friendly description of the limitation.</param>
/// <param name="TechnicalReason">A technical explanation for developers/logs.</param>
public sealed record DetectionLimitation(
    string? TaskId,
    DetectionLimitationType LimitationType,
    string Description,
    string TechnicalReason);

/// <summary>
/// Types of detection limitations that can affect auto-detection accuracy.
/// </summary>
public enum DetectionLimitationType
{
    /// <summary>
    /// Detection only works for activities performed during the current game session.
    /// Activities completed before plugin load or in previous sessions are not detected.
    /// </summary>
    SessionOnly,

    /// <summary>
    /// Detection feature is not yet implemented (TODO).
    /// The task appears in the detector but cannot be auto-detected.
    /// </summary>
    NotImplemented,

    /// <summary>
    /// Detection works but may miss some completions due to technical limitations.
    /// </summary>
    PartialDetection,

    /// <summary>
    /// Initial state cannot be queried; only changes during the session are tracked.
    /// </summary>
    NoInitialStateQuery
}

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
/// - Implement GetDetectionLimitations to communicate any detection gaps to the UI
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

    /// <summary>
    /// Gets the detection limitations for this detector.
    /// </summary>
    /// <returns>
    /// A collection of limitations describing gaps in detection capability.
    /// An empty collection indicates full detection capability.
    /// </returns>
    /// <remarks>
    /// Implementations should return limitations that affect user experience, such as:
    /// - Session-only detection (cannot detect activities from before plugin load)
    /// - Unimplemented features (TODOs that affect behavior)
    /// - Partial detection (may miss some completions)
    ///
    /// The UI can use this information to display appropriate warnings or
    /// adjust how it presents auto-detected vs manually-tracked tasks.
    /// </remarks>
    IReadOnlyList<DetectionLimitation> GetDetectionLimitations();

    /// <summary>
    /// Gets whether this detector has limited detection capability.
    /// </summary>
    /// <remarks>
    /// A convenience property that returns true if GetDetectionLimitations()
    /// returns any limitations. The UI can use this for quick checks without
    /// needing to enumerate all limitations.
    /// </remarks>
    bool HasLimitedDetection { get; }
}
