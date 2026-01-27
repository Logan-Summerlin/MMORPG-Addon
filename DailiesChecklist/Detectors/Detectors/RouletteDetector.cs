using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace DailiesChecklist.Detectors;

/// <summary>
/// Detector for Duty Roulette completions.
/// Uses IDutyState events to track when roulettes are completed.
/// </summary>
/// <remarks>
/// Detection Potential: HIGH
/// This detector subscribes to duty completion events and tracks which
/// roulette types have been completed for the current daily reset period.
///
/// Supported Roulettes:
/// - Expert
/// - Level Cap Dungeons
/// - High-Level Dungeons (50/60/70/80/90)
/// - Leveling
/// - Main Scenario
/// - Trials
/// - Alliance Raids
/// - Normal Raids
/// - Guildhests
/// - Frontline (PVP)
/// - Mentor
/// </remarks>
public sealed class RouletteDetector : ITaskDetector
{
    private readonly IPluginLog _log;
    private readonly IDutyState _dutyState;
    private readonly IClientState _clientState;

    private readonly Dictionary<string, bool> _completionStates;
    private readonly object _lock = new();
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// Task IDs for all supported duty roulettes.
    /// </summary>
    private static readonly string[] TaskIds =
    {
        "roulette_expert",
        "roulette_levelcap",
        "roulette_highlevel",
        "roulette_leveling",
        "roulette_mainscenario",
        "roulette_trials",
        "roulette_alliance",
        "roulette_normal",
        "roulette_guildhest",
        "roulette_frontline",
        "roulette_mentor",
    };

    /// <inheritdoc />
    public string[] SupportedTaskIds => TaskIds;

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public event Action<string, bool>? OnTaskStateChanged;

    /// <summary>
    /// Creates a new RouletteDetector instance.
    /// </summary>
    /// <param name="log">The plugin log service.</param>
    /// <param name="dutyState">The Dalamud duty state service.</param>
    /// <param name="clientState">The Dalamud client state service.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public RouletteDetector(IPluginLog log, IDutyState dutyState, IClientState clientState)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _dutyState = dutyState ?? throw new ArgumentNullException(nameof(dutyState));
        _clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));

        _completionStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Initialize all tasks as incomplete
        foreach (var taskId in TaskIds)
        {
            _completionStates[taskId] = false;
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (_isDisposed)
        {
            _log.Warning("Cannot initialize disposed RouletteDetector.");
            return;
        }

        if (_isInitialized)
        {
            _log.Debug("RouletteDetector already initialized.");
            return;
        }

        try
        {
            // Subscribe to duty events
            _dutyState.DutyCompleted += OnDutyCompleted;

            // Subscribe to login/logout for state management
            _clientState.Login += OnLogin;
            _clientState.Logout += OnLogout;

            _isInitialized = true;
            _log.Information("RouletteDetector initialized. Subscribed to DutyCompleted events.");

            // TODO: Phase 2 - Query initial state from game data
            // This would involve reading the Timers window data or equivalent
            // to determine which roulettes have already been completed today
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to initialize RouletteDetector.");
            throw;
        }
    }

    /// <inheritdoc />
    public bool? GetCompletionState(string taskId)
    {
        if (_isDisposed || !IsEnabled)
            return null;

        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        lock (_lock)
        {
            if (_completionStates.TryGetValue(taskId, out var isComplete))
            {
                return isComplete;
            }
        }

        // Task ID not supported by this detector
        return null;
    }

    /// <summary>
    /// Handles duty completion events from IDutyState.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="territoryType">The territory type ID of the completed duty.</param>
    private void OnDutyCompleted(object? sender, ushort territoryType)
    {
        if (!IsEnabled || _isDisposed)
            return;

        try
        {
            _log.Debug("Duty completed in territory {TerritoryType}.", territoryType);

            // TODO: Phase 2 - Determine which roulette type was completed
            // This requires:
            // 1. Tracking when the player queued via roulette (vs direct queue)
            // 2. Storing which roulette type was selected
            // 3. Matching the completed duty to the queued roulette
            //
            // Possible approaches:
            // - Hook the duty finder UI to detect roulette selection
            // - Read the "in roulette" flag from game state
            // - Track roulette bonus state changes
            //
            // For now, this is a stub that logs the event
            DetectRouletteCompletion(territoryType);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling duty completion event.");
        }
    }

    /// <summary>
    /// Attempts to detect which roulette was completed based on the duty.
    /// </summary>
    /// <param name="territoryType">The completed duty's territory type.</param>
    /// <remarks>
    /// TODO: Phase 2 - Implement actual roulette detection logic
    ///
    /// This method should:
    /// 1. Check if the player was in a roulette (not direct queue)
    /// 2. Determine which roulette type based on game state
    /// 3. Update the completion state for that roulette
    /// 4. Fire the OnTaskStateChanged event
    ///
    /// Detection strategies to investigate:
    /// - ContentFinderCondition data from IDataManager
    /// - Roulette bonus window/notification
    /// - Character data for roulette completion flags
    /// </remarks>
    private void DetectRouletteCompletion(ushort territoryType)
    {
        // TODO: Phase 2 - Implement roulette type detection
        // For now, this is a placeholder that demonstrates the pattern

        // Example of how completion would be reported:
        // string? detectedRouletteId = DetermineRouletteType(territoryType);
        // if (detectedRouletteId != null)
        // {
        //     SetTaskComplete(detectedRouletteId);
        // }

        _log.Debug(
            "TODO: Implement roulette type detection for territory {TerritoryType}.",
            territoryType);
    }

    /// <summary>
    /// Sets a task as complete and fires the state change event.
    /// </summary>
    /// <param name="taskId">The task ID to mark as complete.</param>
    private void SetTaskComplete(string taskId)
    {
        if (_isDisposed || !IsEnabled)
            return;

        lock (_lock)
        {
            if (!_completionStates.ContainsKey(taskId))
            {
                _log.Warning("Unknown task ID: {TaskId}", taskId);
                return;
            }

            if (_completionStates[taskId])
            {
                _log.Debug("Task {TaskId} already marked complete.", taskId);
                return;
            }

            _completionStates[taskId] = true;
        }

        _log.Information("Roulette '{TaskId}' detected as complete.", taskId);

        try
        {
            OnTaskStateChanged?.Invoke(taskId, true);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error firing OnTaskStateChanged event for task '{TaskId}'.", taskId);
        }
    }

    /// <summary>
    /// Handles player login events.
    /// </summary>
    private void OnLogin()
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged in. RouletteDetector ready.");

        // TODO: Phase 2 - Query current roulette completion state on login
        // This should read from the Timers window or equivalent game data
        // to restore the correct state for today's reset period
    }

    /// <summary>
    /// Handles player logout events.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged out. Clearing RouletteDetector state.");

        // Clear state on logout (will be re-queried on next login)
        ResetAllStates();
    }

    /// <summary>
    /// Resets all completion states to incomplete.
    /// Called on logout and daily reset.
    /// </summary>
    public void ResetAllStates()
    {
        lock (_lock)
        {
            foreach (var taskId in TaskIds)
            {
                if (_completionStates.TryGetValue(taskId, out var wasComplete) && wasComplete)
                {
                    _completionStates[taskId] = false;

                    try
                    {
                        OnTaskStateChanged?.Invoke(taskId, false);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error firing reset event for task '{TaskId}'.", taskId);
                    }
                }
            }
        }

        _log.Debug("RouletteDetector states reset.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from all events
        if (_isInitialized)
        {
            _dutyState.DutyCompleted -= OnDutyCompleted;
            _clientState.Login -= OnLogin;
            _clientState.Logout -= OnLogout;
        }

        lock (_lock)
        {
            _completionStates.Clear();
        }

        _log.Debug("RouletteDetector disposed.");
    }
}
