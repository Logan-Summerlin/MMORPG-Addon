using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace DailiesChecklist.Detectors;

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

    private readonly object _lock = new();
    private bool _isInitialized;
    private bool _isDisposed;

    // Allowance tracking
    private int _allowancesUsed;
    private const int MaxDailyAllowances = 12;

    /// <summary>
    /// Task ID for beast tribe daily allowances.
    /// Must match the ID in TaskRegistry.
    /// </summary>
    private static readonly string[] TaskIds =
    {
        "beast_tribe_quests",
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
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public BeastTribeDetector(IPluginLog log, IClientState clientState)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));

        _allowancesUsed = 0;
    }

    /// <summary>
    /// Safely invokes an action with exception handling.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <param name="context">A description of the context for logging.</param>
    private void SafeInvoke(Action action, string context)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error in BeastTribeDetector.{Context}", context);
        }
    }

    /// <summary>
    /// Attempts to read the current beast tribe allowance count from game data.
    /// </summary>
    /// <returns>Remaining allowances, or null if unavailable.</returns>
    /// <remarks>
    /// Implementation note: Beast tribe allowances can potentially be read from
    /// the PlayerState game structure. This requires FFXIVClientStructs integration.
    /// For now, returns null to indicate data is not available.
    /// </remarks>
    private int? TryReadAllowancesFromGameData()
    {
        // TODO: Implement using FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState
        // The BeastTribeAllowances field contains remaining daily allowances
        return null;
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

        if (taskId != "beast_tribe_quests")
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
            SafeInvoke(() =>
            {
                OnTaskStateChanged?.Invoke("beast_tribe_quests", isNowComplete);
            }, "UpdateAllowanceCount.OnTaskStateChanged");
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
        SafeInvoke(() =>
        {
            if (_isDisposed)
                return;

            _log.Debug("Player logged in. BeastTribeDetector ready.");

            // TODO: Phase 2 - Query current allowance state on login
            // This should read from character data to restore the correct state
            QueryAllowanceState();
        }, "OnLogin");
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
        SafeInvoke(() =>
        {
            // Attempt to read from game data
            var remainingAllowances = TryReadAllowancesFromGameData();

            if (remainingAllowances.HasValue)
            {
                _log.Debug("Beast tribe allowances read from game data: {Remaining} remaining.",
                    remainingAllowances.Value);
                SetAllowancesRemaining(remainingAllowances.Value);
            }
            else
            {
                _log.Debug("Beast tribe allowance data not available from game data.");
            }
        }, "QueryAllowanceState");
    }

    /// <summary>
    /// Handles player logout events.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        SafeInvoke(() =>
        {
            if (_isDisposed)
                return;

            _log.Debug("Player logged out. Clearing BeastTribeDetector state.");
            ResetState();
        }, "OnLogout");
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
            SafeInvoke(() =>
            {
                OnTaskStateChanged?.Invoke("beast_tribe_quests", false);
            }, "ResetState.OnTaskStateChanged");
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
