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
    /// Must match IDs defined in TaskRegistry.cs exactly.
    /// </summary>
    private static readonly string[] TaskIds =
    {
        "roulette_expert",
        "roulette_leveling",
        "roulette_msq",
        "roulette_alliance",
        "roulette_normal_raid",
        "roulette_trials",
        "roulette_5060708090",
        "roulette_frontline",
        "roulette_guildhests",
        "roulette_mentor",
    };

    /// <summary>
    /// Maps game ContentRouletteId values to task IDs.
    /// These IDs correspond to the ContentRoulette Excel sheet in the game data.
    /// </summary>
    private static readonly Dictionary<byte, string> RouletteIdToTaskId = new()
    {
        { 1, "roulette_leveling" },
        { 2, "roulette_5060708090" },      // 50/60/70/80/90 dungeons
        { 3, "roulette_msq" },             // Main Scenario
        { 4, "roulette_guildhests" },
        { 5, "roulette_expert" },
        { 6, "roulette_trials" },
        { 7, "roulette_alliance" },        // Alliance Raids
        { 8, "roulette_normal_raid" },     // Normal Raids
        { 9, "roulette_mentor" },
        { 17, "roulette_frontline" },      // Frontline PvP
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

            // If already logged in, query initial state
            if (_clientState.IsLoggedIn)
            {
                QueryInitialState();
            }
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

            // Detect if this was a roulette completion and update state accordingly
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
    /// Uses IDutyState.ContentRouletteId to determine if the completed duty
    /// was entered via a roulette queue. ContentRouletteId will be greater
    /// than 0 when in a roulette duty, and maps to specific roulette types.
    /// </remarks>
    private void DetectRouletteCompletion(ushort territoryType)
    {
        // Check if the duty was entered via roulette using ContentRouletteId
        var rouletteId = _dutyState.ContentRouletteId;

        if (rouletteId == 0)
        {
            _log.Debug(
                "Duty in territory {TerritoryType} was not a roulette (ContentRouletteId=0).",
                territoryType);
            return;
        }

        _log.Debug(
            "Roulette duty completed. TerritoryType={TerritoryType}, ContentRouletteId={RouletteId}.",
            territoryType,
            rouletteId);

        // Map the roulette ID to our task ID
        if (RouletteIdToTaskId.TryGetValue(rouletteId, out var taskId))
        {
            _log.Information(
                "Detected roulette completion: {TaskId} (RouletteId={RouletteId}).",
                taskId,
                rouletteId);
            SetTaskComplete(taskId);
        }
        else
        {
            _log.Warning(
                "Unknown ContentRouletteId {RouletteId} - no mapping defined.",
                rouletteId);
        }
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

        try
        {
            _log.Debug("Player logged in. RouletteDetector ready.");

            // Query initial roulette completion state
            QueryInitialState();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling login event in RouletteDetector.");
        }
    }

    /// <summary>
    /// Queries the initial roulette completion state from game data on login.
    /// </summary>
    /// <remarks>
    /// This method attempts to read the current roulette completion state
    /// so the checklist accurately reflects what the player has already done today.
    /// Reading this state requires accessing game memory structures which is complex
    /// and may need to be deferred or implemented using game UI agents.
    /// </remarks>
    private void QueryInitialState()
    {
        try
        {
            // Initial state query is not yet implemented.
            // This requires reading game memory structures (e.g., the Timers window data
            // or ContentsInfo agent) which is complex and may vary between game versions.
            // For now, we rely on detecting completions as they happen during the session.
            _log.Information(
                "RouletteDetector: Initial state query not yet implemented. " +
                "Roulette completions will be detected as they occur during this session.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error querying initial roulette state.");
        }
    }

    /// <summary>
    /// Handles player logout events.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        if (_isDisposed)
            return;

        try
        {
            _log.Debug("Player logged out (type={Type}, code={Code}). Clearing RouletteDetector state.", type, code);

            // Clear state on logout (will be re-queried on next login)
            ResetAllStates();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling logout event in RouletteDetector.");
        }
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
