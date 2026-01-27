using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace DailiesChecklist.Detectors.Detectors;

/// <summary>
/// Detector for Beast Tribe (Allied Society) quest allowances.
/// Tracks daily quest allowance usage across all tribal factions.
/// </summary>
/// <remarks>
/// Detection Potential: HIGH
///
/// Allowance System:
/// - 12 total daily quest allowances shared across all tribes
/// - Per-tribe limit: 3 quests per tribe per day (Heavensward onwards)
/// - ARR tribes: Up to 12 quests with a single tribe
/// - Reset: Daily at 15:00 UTC
///
/// Supported Tribes by Expansion:
/// - ARR: Amalj'aa, Sylphs, Kobolds, Sahagin, Ixal (crafting)
/// - HW: Vath, Vanu Vanu, Moogles (crafting)
/// - SB: Kojin, Ananta, Namazu (crafting/gathering)
/// - ShB: Pixies, Qitari (gathering), Dwarves (crafting)
/// - EW: Arkasodara, Loporrits (crafting), Omicrons (gathering)
/// - DT: Pelupelu, Moblins
///
/// Detection approaches to investigate:
/// - Quest allowance tracking via game state
/// - Quest completion events
/// - Character data for remaining allowances
/// </remarks>
public sealed class BeastTribeDetector : ITaskDetector
{
    private readonly IPluginLog _log;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;

    private readonly object _lock = new();
    private bool _isInitialized;
    private bool _isDisposed;

    // Allowance tracking
    private int _allowancesUsed;
    private const int MaxDailyAllowances = 12;

    /// <summary>
    /// Task ID for beast tribe daily allowances.
    /// </summary>
    private static readonly string[] TaskIds =
    {
        "beast_tribe_daily",
    };

    /// <inheritdoc />
    public string[] SupportedTaskIds => TaskIds;

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public event Action<string, bool>? OnTaskStateChanged;

    /// <summary>
    /// Creates a new BeastTribeDetector instance.
    /// </summary>
    /// <param name="log">The plugin log service.</param>
    /// <param name="clientState">The Dalamud client state service.</param>
    /// <param name="framework">The Dalamud framework service.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public BeastTribeDetector(IPluginLog log, IClientState clientState, IFramework framework)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));
        _framework = framework ?? throw new ArgumentNullException(nameof(framework));

        _allowancesUsed = 0;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (_isDisposed)
        {
            _log.Warning("Cannot initialize disposed BeastTribeDetector.");
            return;
        }

        if (_isInitialized)
        {
            _log.Debug("BeastTribeDetector already initialized.");
            return;
        }

        try
        {
            // Subscribe to login/logout for state management
            _clientState.Login += OnLogin;
            _clientState.Logout += OnLogout;

            _isInitialized = true;
            _log.Information("BeastTribeDetector initialized.");

            // TODO: Phase 2 - Query initial allowance state
            // Need to determine how to read remaining allowances from game data
            // Possible approaches:
            // - Read from character info/timers
            // - Hook quest acceptance/completion
            // - Monitor relevant UI elements
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to initialize BeastTribeDetector.");
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

        if (taskId != "beast_tribe_daily")
            return null;

        lock (_lock)
        {
            // Task is "complete" when all 12 allowances are used
            return _allowancesUsed >= MaxDailyAllowances;
        }
    }

    /// <summary>
    /// Gets the number of beast tribe quest allowances used today.
    /// </summary>
    /// <returns>The number of allowances used (0-12), or -1 if unknown.</returns>
    public int GetAllowancesUsed()
    {
        if (_isDisposed || !IsEnabled)
            return -1;

        lock (_lock)
        {
            return _allowancesUsed;
        }
    }

    /// <summary>
    /// Gets the number of beast tribe quest allowances remaining today.
    /// </summary>
    /// <returns>The number of allowances remaining (0-12), or -1 if unknown.</returns>
    public int GetAllowancesRemaining()
    {
        if (_isDisposed || !IsEnabled)
            return -1;

        lock (_lock)
        {
            return MaxDailyAllowances - _allowancesUsed;
        }
    }

    /// <summary>
    /// Updates the allowance count.
    /// </summary>
    /// <param name="usedCount">The new used allowance count.</param>
    /// <remarks>
    /// TODO: Phase 2 - This method will be called when detection logic
    /// determines allowance usage has changed.
    /// </remarks>
    private void UpdateAllowanceCount(int usedCount)
    {
        if (_isDisposed || !IsEnabled)
            return;

        usedCount = Math.Clamp(usedCount, 0, MaxDailyAllowances);

        bool wasComplete;
        bool isNowComplete;

        lock (_lock)
        {
            wasComplete = _allowancesUsed >= MaxDailyAllowances;
            isNowComplete = usedCount >= MaxDailyAllowances;
            _allowancesUsed = usedCount;
        }

        _log.Information(
            "Beast Tribe allowances: {UsedCount}/{MaxAllowances} used ({Remaining} remaining).",
            usedCount,
            MaxDailyAllowances,
            MaxDailyAllowances - usedCount);

        // Fire event if completion state changed
        if (wasComplete != isNowComplete)
        {
            try
            {
                OnTaskStateChanged?.Invoke("beast_tribe_daily", isNowComplete);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error firing OnTaskStateChanged for beast_tribe_daily.");
            }
        }
    }

    /// <summary>
    /// Records completion of a beast tribe quest, using one allowance.
    /// </summary>
    /// <remarks>
    /// TODO: Phase 2 - Call this when a beast tribe quest completion is detected.
    ///
    /// Detection approaches:
    /// - Hook quest completion events
    /// - Monitor for beast tribe quest IDs
    /// - Track reputation gain events
    /// </remarks>
    public void RecordQuestCompletion()
    {
        if (_isDisposed || !IsEnabled)
            return;

        int newCount;
        lock (_lock)
        {
            newCount = _allowancesUsed + 1;
        }

        UpdateAllowanceCount(newCount);
    }

    /// <summary>
    /// Sets the allowance count directly.
    /// Used when reading state from game data.
    /// </summary>
    /// <param name="allowancesRemaining">The number of allowances still available.</param>
    /// <remarks>
    /// TODO: Phase 2 - Call this when querying initial state from game data.
    /// </remarks>
    public void SetAllowancesRemaining(int allowancesRemaining)
    {
        if (_isDisposed || !IsEnabled)
            return;

        allowancesRemaining = Math.Clamp(allowancesRemaining, 0, MaxDailyAllowances);
        var usedCount = MaxDailyAllowances - allowancesRemaining;

        UpdateAllowanceCount(usedCount);
    }

    /// <summary>
    /// Handles player login events.
    /// </summary>
    private void OnLogin()
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged in. BeastTribeDetector ready.");

        // TODO: Phase 2 - Query current allowance state on login
        // This should read from character data to restore the correct state
        QueryAllowanceState();
    }

    /// <summary>
    /// Queries the current beast tribe allowance state from game data.
    /// </summary>
    /// <remarks>
    /// TODO: Phase 2 - Implement actual game data query
    ///
    /// Possible data sources:
    /// - Character sheet / player state
    /// - Timers window data
    /// - Quest journal state
    /// </remarks>
    private void QueryAllowanceState()
    {
        // TODO: Phase 2 - Implement game data query
        // For now, this is a placeholder

        _log.Debug("TODO: Implement beast tribe allowance state query.");

        // Example of how state would be set after query:
        // var remainingAllowances = ReadFromGameData();
        // SetAllowancesRemaining(remainingAllowances);
    }

    /// <summary>
    /// Handles player logout events.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged out. Clearing BeastTribeDetector state.");
        ResetState();
    }

    /// <summary>
    /// Resets the allowance count to zero (all allowances available).
    /// Called on daily reset and logout.
    /// </summary>
    public void ResetState()
    {
        bool wasComplete;

        lock (_lock)
        {
            wasComplete = _allowancesUsed >= MaxDailyAllowances;
            _allowancesUsed = 0;
        }

        if (wasComplete)
        {
            try
            {
                OnTaskStateChanged?.Invoke("beast_tribe_daily", false);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error firing reset event for beast_tribe_daily.");
            }
        }

        _log.Debug("BeastTribeDetector state reset.");
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
            _clientState.Login -= OnLogin;
            _clientState.Logout -= OnLogout;
        }

        _log.Debug("BeastTribeDetector disposed.");
    }
}
