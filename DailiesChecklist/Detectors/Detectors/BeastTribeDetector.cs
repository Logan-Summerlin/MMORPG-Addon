using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Detection limitations for this detector.
    /// Describes gaps in detection capability for UI display.
    /// </summary>
    private static readonly IReadOnlyList<DetectionLimitation> Limitations = new ReadOnlyCollection<DetectionLimitation>(
        new List<DetectionLimitation>
        {
            new DetectionLimitation(
                TaskId: "beast_tribe_quests",
                LimitationType: DetectionLimitationType.SessionOnly,
                Description: "Beast tribe quest completions are only tracked during this session. " +
                             "Quests completed before logging in will not be counted.",
                TechnicalReason: "Quest completion event hooking is not yet implemented. " +
                                 "Current implementation only supports manual tracking and " +
                                 "programmatic updates via RecordQuestCompletion()."
            ),
            new DetectionLimitation(
                TaskId: "beast_tribe_quests",
                LimitationType: DetectionLimitationType.NoInitialStateQuery,
                Description: "Cannot determine how many beast tribe allowances were already used when logging in.",
                TechnicalReason: "TryReadAllowancesFromGameData() is not implemented. " +
                                 "Implementation requires FFXIVClientStructs to read PlayerState.BeastTribeAllowances " +
                                 "from game memory. This is technically feasible but not yet integrated."
            ),
            new DetectionLimitation(
                TaskId: "beast_tribe_quests",
                LimitationType: DetectionLimitationType.PartialDetection,
                Description: "Automatic detection of quest completions is not implemented. " +
                             "Please manually track your beast tribe allowance usage.",
                TechnicalReason: "Detecting beast tribe quest completion requires hooking quest completion events " +
                                 "and filtering for beast tribe quest IDs, which is not yet implemented."
            )
        });

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
    public bool HasLimitedDetection => true;

    /// <inheritdoc />
    public event Action<string, bool>? OnTaskStateChanged;

    /// <inheritdoc />
    public IReadOnlyList<DetectionLimitation> GetDetectionLimitations() => Limitations;

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
    /// <para>
    /// <strong>KNOWN LIMITATION:</strong> This method is not yet implemented.
    /// It always returns null, meaning initial state cannot be queried.
    /// </para>
    /// <para>
    /// <strong>Technical Details:</strong>
    /// Beast tribe allowances CAN be read from the game memory using FFXIVClientStructs:
    /// <code>
    /// // Potential implementation (requires FFXIVClientStructs package):
    /// unsafe {
    ///     var playerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
    ///     if (playerState != null)
    ///         return playerState->BeastTribeAllowances;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Why Not Implemented:</strong>
    /// <list type="bullet">
    ///   <item>Requires adding FFXIVClientStructs as a dependency</item>
    ///   <item>Unsafe code requires careful memory management</item>
    ///   <item>Game structure offsets may change between patches</item>
    ///   <item>Need to verify the field location in current game version</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Current Workaround:</strong>
    /// Users can manually set their allowance count, or the plugin can track
    /// quest completions during the session (if quest completion hooks are added).
    /// </para>
    /// </remarks>
    private int? TryReadAllowancesFromGameData()
    {
        // LIMITATION: Initial state query is not implemented.
        //
        // This COULD be implemented using FFXIVClientStructs:
        //
        // unsafe {
        //     var playerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
        //     if (playerState != null)
        //         return playerState->BeastTribeAllowances;
        // }
        //
        // However, this requires:
        // 1. Adding FFXIVClientStructs NuGet package
        // 2. Enabling unsafe code in the project
        // 3. Testing with current game version to verify offsets
        //
        // For now, return null to indicate data is not available.
        // The HasLimitedDetection property communicates this to the UI.

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
            _log.Information(
                "BeastTribeDetector initialized. Note: Detection has significant limitations - " +
                "see HasLimitedDetection and GetDetectionLimitations() for details.");

            // LIMITATION: Beast tribe detection is currently very limited.
            //
            // What's NOT implemented:
            // 1. Initial allowance state query (TryReadAllowancesFromGameData returns null)
            // 2. Quest completion event hooks (no automatic tracking)
            // 3. Territory/NPC interaction detection
            //
            // What IS available:
            // 1. Manual tracking via RecordQuestCompletion() and SetAllowancesRemaining()
            // 2. Reset handling on daily reset and logout
            // 3. Event notifications when state changes
            //
            // Future implementation paths:
            // 1. FFXIVClientStructs for PlayerState.BeastTribeAllowances (initial state)
            // 2. Quest completion event hooks via Dalamud.Game.Addon.Lifecycle
            // 3. IFramework.Update polling to read game state periodically
            //
            // The HasLimitedDetection property = true communicates these gaps to the UI.
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
    /// <para>
    /// <strong>Current Usage:</strong>
    /// This method is publicly exposed for manual tracking or external integration.
    /// There is no automatic detection that calls this method.
    /// </para>
    /// <para>
    /// <strong>KNOWN LIMITATION:</strong> Automatic quest completion detection is not implemented.
    /// This method must be called manually (e.g., via UI button) or by future detection logic.
    /// </para>
    /// <para>
    /// <strong>Future Implementation (not yet done):</strong>
    /// <list type="bullet">
    ///   <item>Hook quest completion events via Dalamud services</item>
    ///   <item>Filter for beast tribe quest IDs from game data</item>
    ///   <item>Monitor reputation gain events (BeastReputationRank changes)</item>
    ///   <item>Track NPC interaction with beast tribe quest givers</item>
    /// </list>
    /// </para>
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
    /// <para>
    /// <strong>KNOWN LIMITATION:</strong> This method currently cannot retrieve initial state.
    /// TryReadAllowancesFromGameData() returns null because game memory reading is not implemented.
    /// </para>
    /// <para>
    /// <strong>Possible Data Sources (not yet implemented):</strong>
    /// <list type="bullet">
    ///   <item>PlayerState.BeastTribeAllowances via FFXIVClientStructs</item>
    ///   <item>Timers window addon node data</item>
    ///   <item>Character info game structures</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Current Behavior:</strong>
    /// Logs that data is unavailable and returns without setting state.
    /// Users must manually track their allowance usage.
    /// </para>
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
                // LIMITATION: Game data reading is not implemented.
                // This is expected behavior - log at Verbose level to avoid spam.
                _log.Verbose(
                    "BeastTribeDetector: Initial allowance state unavailable. " +
                    "Detection is limited - see GetDetectionLimitations() for details.");
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
