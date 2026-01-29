using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

using Service = DailiesChecklist.Service;

namespace DailiesChecklist.Detectors;

/// <summary>
/// Detector for Gold Saucer Cactpot lottery activities.
/// Tracks Mini Cactpot (daily) and Jumbo Cactpot (weekly) ticket usage.
/// </summary>
/// <remarks>
/// Detection Potential: HIGH
///
/// Mini Cactpot:
/// - 3 tickets available per day
/// - Reset: Daily at 15:00 UTC
/// - Location: Gold Saucer (X:5.1, Y:6.5)
///
/// Jumbo Cactpot:
/// - 3 tickets available per week
/// - Drawing: Saturday 08:00 UTC
/// - Location: Gold Saucer (X:8, Y:5)
///
/// Detection approaches to investigate:
/// - Gold Saucer addon state reading
/// - MGP transaction tracking
/// - Cactpot ticket item count
/// - UI state when interacting with NPCs
/// </remarks>
public sealed class CactpotDetector : ITaskDetector
{
    private readonly IPluginLog _log;
    private readonly IClientState _clientState;
    private readonly IAddonLifecycle _addonLifecycle;

    private readonly Dictionary<string, int> _ticketCounts;
    private readonly object _lock = new();
    private bool _isInitialized;
    private bool _isDisposed;
    private bool _isInGoldSaucer;
    private bool _addonListenersRegistered;
    private nint _lastMiniCactpotAddon;
    private DateTime _lastMiniCactpotRecordedUtc = DateTime.MinValue;

    private static readonly TimeSpan MiniCactpotDuplicateWindow = TimeSpan.FromSeconds(2);

    // Gold Saucer territory type ID
    private const ushort GoldSaucerTerritoryId = 144;

    // Ticket limits
    private const int MiniCactpotMaxTickets = 3;
    private const int JumboCactpotMaxTickets = 3;

    // Addon names for detection
    private const string MiniCactpotResultAddonName = "MiniCactpotResult";
    private const string JumboCactpotAddonName = "LotteryWeekly";

    /// <summary>
    /// Detection limitations for this detector.
    /// Describes gaps in detection capability for UI display.
    /// </summary>
    private static readonly IReadOnlyList<DetectionLimitation> Limitations = new ReadOnlyCollection<DetectionLimitation>(
        new List<DetectionLimitation>
        {
            new DetectionLimitation(
                TaskId: "mini_cactpot",
                LimitationType: DetectionLimitationType.SessionOnly,
                Description: "Mini Cactpot tickets played are only counted during this session. " +
                             "Tickets used before logging in will not be detected.",
                TechnicalReason: "Initial ticket state query is not implemented. " +
                                 "Detection relies on MiniCactpotResult addon events during the session."
            ),
            new DetectionLimitation(
                TaskId: "jumbo_cactpot",
                LimitationType: DetectionLimitationType.NotImplemented,
                Description: "Jumbo Cactpot ticket purchases cannot be automatically detected yet. " +
                             "Please manually track your weekly Jumbo Cactpot tickets.",
                TechnicalReason: "The LotteryWeekly addon appears for both viewing results and purchasing tickets. " +
                                 "Differentiating between these states requires reading addon node data or " +
                                 "tracking button click events, which is not yet implemented."
            ),
            new DetectionLimitation(
                TaskId: null, // Applies to all tasks
                LimitationType: DetectionLimitationType.NoInitialStateQuery,
                Description: "Cannot determine how many Cactpot tickets were already used when logging in.",
                TechnicalReason: "Query of current ticket usage from game data is not implemented. " +
                                 "Would require reading Gold Saucer state from game memory structures."
            )
        });

    /// <summary>
    /// Task IDs for Cactpot activities.
    /// </summary>
    private static readonly string[] TaskIds =
    {
        "mini_cactpot",
        "jumbo_cactpot",
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
    /// Creates a new CactpotDetector instance.
    /// </summary>
    /// <param name="log">The plugin log service.</param>
    /// <param name="clientState">The Dalamud client state service.</param>
    /// <param name="addonLifecycle">The Dalamud addon lifecycle service for tracking addon events.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public CactpotDetector(
        IPluginLog log,
        IClientState clientState,
        IAddonLifecycle addonLifecycle)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));
        _addonLifecycle = addonLifecycle ?? throw new ArgumentNullException(nameof(addonLifecycle));

        _ticketCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "mini_cactpot", 0 },
            { "jumbo_cactpot", 0 },
        };
    }

    /// <inheritdoc />
    public void Initialize()
    {
        if (_isDisposed)
        {
            _log.Warning("Cannot initialize disposed CactpotDetector.");
            return;
        }

        if (_isInitialized)
        {
            _log.Debug("CactpotDetector already initialized.");
            return;
        }

        try
        {
            // Subscribe to territory changes to detect Gold Saucer entry
            _clientState.TerritoryChanged += OnTerritoryChanged;

            // Subscribe to login/logout for state management
            _clientState.Login += OnLogin;
            _clientState.Logout += OnLogout;

            _isInitialized = true;
            _log.Information(
                "CactpotDetector initialized. Note: Detection is session-only and " +
                "Jumbo Cactpot purchase tracking is not yet implemented.");

            // LIMITATION: Initial ticket state query is not implemented.
            //
            // Implementation would require:
            // 1. Reading Gold Saucer player data from game memory
            // 2. FFXIVClientStructs integration for safe memory access
            // 3. Finding the correct data structures for ticket usage tracking
            //
            // Current behavior: Tickets are counted only as they are used during the session.
            // The HasLimitedDetection property communicates this to the UI.
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to initialize CactpotDetector.");
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
            if (!_ticketCounts.TryGetValue(taskId, out var usedCount))
                return null;

            var maxTickets = taskId == "mini_cactpot" ? MiniCactpotMaxTickets : JumboCactpotMaxTickets;
            return usedCount >= maxTickets;
        }
    }

    /// <summary>
    /// Gets the number of tickets used for a specific Cactpot type.
    /// </summary>
    /// <param name="taskId">The task ID (mini_cactpot or jumbo_cactpot).</param>
    /// <returns>The number of tickets used, or -1 if unknown.</returns>
    public int GetTicketsUsed(string taskId)
    {
        if (_isDisposed || !IsEnabled)
            return -1;

        lock (_lock)
        {
            return _ticketCounts.TryGetValue(taskId, out var count) ? count : -1;
        }
    }

    /// <summary>
    /// Handles territory change events.
    /// </summary>
    /// <param name="territoryType">The new territory type ID.</param>
    private void OnTerritoryChanged(ushort territoryType)
    {
        if (!IsEnabled || _isDisposed)
            return;

        // Guard against loading screens - game state may be invalid during area transitions
        if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51])
            return;

        try
        {
            var wasInGoldSaucer = _isInGoldSaucer;

            if (territoryType == GoldSaucerTerritoryId)
            {
                _isInGoldSaucer = true;
                _log.Debug("Entered Gold Saucer. CactpotDetector active.");
                OnEnterGoldSaucer();
            }
            else if (wasInGoldSaucer)
            {
                _isInGoldSaucer = false;
                _log.Debug("Left Gold Saucer. Stopping Cactpot addon monitoring.");
                OnLeaveGoldSaucer();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling territory change in CactpotDetector.");
        }
    }

    /// <summary>
    /// Called when the player enters the Gold Saucer.
    /// Sets up additional monitoring for Cactpot activities.
    /// </summary>
    private void OnEnterGoldSaucer()
    {
        if (_addonListenersRegistered)
        {
            _log.Verbose("Addon listeners already registered.");
            return;
        }

        try
        {
            // Register for Mini Cactpot result addon - this appears after playing a ticket
            _addonLifecycle.RegisterListener(
                AddonEvent.PostSetup,
                MiniCactpotResultAddonName,
                OnMiniCactpotResultAddonSetup);

            // Register for Jumbo Cactpot addon
            // TODO: The LotteryWeekly addon appears both when viewing results AND when buying tickets.
            // Need to differentiate between these two states by reading addon data or tracking
            // interaction sequence. For now, we register but the handler needs additional logic.
            _addonLifecycle.RegisterListener(
                AddonEvent.PostSetup,
                JumboCactpotAddonName,
                OnJumboCactpotAddonSetup);

            _addonListenersRegistered = true;
            _log.Information("Gold Saucer Cactpot addon monitoring started.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to register addon lifecycle listeners for Cactpot detection.");
        }
    }

    /// <summary>
    /// Called when the player leaves the Gold Saucer.
    /// Cleans up addon monitoring.
    /// </summary>
    private void OnLeaveGoldSaucer()
    {
        UnregisterAddonListeners();
    }

    /// <summary>
    /// Unregisters all addon lifecycle listeners.
    /// </summary>
    private void UnregisterAddonListeners()
    {
        if (!_addonListenersRegistered)
            return;

        try
        {
            _addonLifecycle.UnregisterListener(
                AddonEvent.PostSetup,
                MiniCactpotResultAddonName,
                OnMiniCactpotResultAddonSetup);

            _addonLifecycle.UnregisterListener(
                AddonEvent.PostSetup,
                JumboCactpotAddonName,
                OnJumboCactpotAddonSetup);

            _addonListenersRegistered = false;
            _log.Debug("Cactpot addon listeners unregistered.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error unregistering addon lifecycle listeners.");
        }
    }

    /// <summary>
    /// Handles the Mini Cactpot result addon appearing.
    /// This indicates a Mini Cactpot ticket was played.
    /// </summary>
    /// <param name="type">The addon event type.</param>
    /// <param name="args">The addon event arguments.</param>
    private void OnMiniCactpotResultAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (_isDisposed || !IsEnabled)
            return;

        try
        {
            var now = DateTime.UtcNow;
            var addonPtr = args.Addon;
            if (addonPtr != nint.Zero)
            {
                if (addonPtr == _lastMiniCactpotAddon && now - _lastMiniCactpotRecordedUtc < MiniCactpotDuplicateWindow)
                {
                    _log.Debug("Mini Cactpot addon reopened; skipping duplicate ticket count.");
                    return;
                }
            }
            else if (now - _lastMiniCactpotRecordedUtc < MiniCactpotDuplicateWindow)
            {
                _log.Debug("Mini Cactpot addon reopened quickly; skipping duplicate ticket count.");
                return;
            }

            _lastMiniCactpotAddon = addonPtr;
            _lastMiniCactpotRecordedUtc = now;
            _log.Debug("Mini Cactpot result addon detected - recording ticket play.");
            RecordMiniCactpotPlay();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling Mini Cactpot result addon event.");
        }
    }

    /// <summary>
    /// Handles the Jumbo Cactpot addon appearing.
    /// </summary>
    /// <param name="type">The addon event type.</param>
    /// <param name="args">The addon event arguments.</param>
    /// <remarks>
    /// <para>
    /// <strong>KNOWN LIMITATION:</strong> Jumbo Cactpot purchase detection is NOT implemented.
    /// The LotteryWeekly addon appears for both viewing results AND purchasing tickets,
    /// making it impossible to detect purchases without additional logic.
    /// </para>
    /// <para>
    /// <strong>Why This Is Difficult:</strong>
    /// <list type="bullet">
    ///   <item>The addon opens when clicking "Purchase Ticket" OR "View Results"</item>
    ///   <item>Differentiating requires reading addon node text/state values</item>
    ///   <item>Purchase confirmation happens in a separate dialog or within the addon</item>
    ///   <item>Button click tracking requires unsafe addon interaction hooks</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Possible Future Implementations:</strong>
    /// <list type="number">
    ///   <item>Read addon nodes to check for "Purchase" vs "Results" UI state</item>
    ///   <item>Monitor MGP changes to detect ticket purchase (10 MGP per ticket)</item>
    ///   <item>Track inventory for Jumbo Cactpot ticket items</item>
    ///   <item>Hook the purchase confirmation button callback</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Current Workaround:</strong>
    /// Users must manually track Jumbo Cactpot tickets. The UI should indicate this
    /// via the HasLimitedDetection property and GetDetectionLimitations() method.
    /// </para>
    /// </remarks>
    private void OnJumboCactpotAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (_isDisposed || !IsEnabled)
            return;

        try
        {
            // LIMITATION: Cannot automatically detect Jumbo Cactpot purchases.
            //
            // The LotteryWeekly addon appears for both purchasing and viewing results.
            // Without reading addon node state or tracking button clicks, we cannot
            // reliably determine if the user is purchasing a ticket.
            //
            // This limitation is documented in GetDetectionLimitations() and communicated
            // to the UI via HasLimitedDetection = true.

            _log.Verbose(
                "Jumbo Cactpot addon detected. Purchase detection not implemented - " +
                "users must manually track weekly tickets.");

            // IMPORTANT: Do NOT call RecordJumboCactpotPurchase() here!
            // We cannot distinguish between viewing results and purchasing.
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error handling Jumbo Cactpot addon event.");
        }
    }

    /// <summary>
    /// Updates the ticket count for a Cactpot type.
    /// </summary>
    /// <param name="taskId">The task ID to update.</param>
    /// <param name="usedCount">The new used ticket count.</param>
    /// <remarks>
    /// TODO: Phase 2 - This method will be called when detection logic
    /// determines ticket usage has changed.
    /// </remarks>
    private void UpdateTicketCount(string taskId, int usedCount)
    {
        if (_isDisposed || !IsEnabled)
            return;

        var maxTickets = taskId == "mini_cactpot" ? MiniCactpotMaxTickets : JumboCactpotMaxTickets;
        usedCount = Math.Clamp(usedCount, 0, maxTickets);

        bool wasComplete;
        bool isNowComplete;

        lock (_lock)
        {
            if (!_ticketCounts.ContainsKey(taskId))
            {
                _log.Warning("Unknown Cactpot task ID: {TaskId}", taskId);
                return;
            }

            var previousCount = _ticketCounts[taskId];
            wasComplete = previousCount >= maxTickets;
            isNowComplete = usedCount >= maxTickets;

            _ticketCounts[taskId] = usedCount;
        }

        _log.Information(
            "{TaskId}: {UsedCount}/{MaxTickets} tickets used.",
            taskId,
            usedCount,
            maxTickets);

        // Fire event if completion state changed
        if (wasComplete != isNowComplete)
        {
            try
            {
                OnTaskStateChanged?.Invoke(taskId, isNowComplete);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error firing OnTaskStateChanged for {TaskId}.", taskId);
            }
        }
    }

    /// <summary>
    /// Records a Mini Cactpot ticket being played.
    /// </summary>
    /// <remarks>
    /// TODO: Phase 2 - Call this when Mini Cactpot play is detected.
    /// </remarks>
    public void RecordMiniCactpotPlay()
    {
        if (_isDisposed || !IsEnabled)
            return;

        int newCount;
        lock (_lock)
        {
            newCount = _ticketCounts["mini_cactpot"] + 1;
        }

        UpdateTicketCount("mini_cactpot", newCount);
    }

    /// <summary>
    /// Records a Jumbo Cactpot ticket being purchased.
    /// </summary>
    /// <remarks>
    /// TODO: Phase 2 - Call this when Jumbo Cactpot purchase is detected.
    /// </remarks>
    public void RecordJumboCactpotPurchase()
    {
        if (_isDisposed || !IsEnabled)
            return;

        int newCount;
        lock (_lock)
        {
            newCount = _ticketCounts["jumbo_cactpot"] + 1;
        }

        UpdateTicketCount("jumbo_cactpot", newCount);
    }

    /// <summary>
    /// Handles player login events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>KNOWN LIMITATION:</strong> Initial ticket state is not queried on login.
    /// Tickets used before this session will not be reflected in the count.
    /// </para>
    /// <para>
    /// Implementation would require reading Gold Saucer state from game memory,
    /// which needs FFXIVClientStructs integration and reverse engineering of
    /// the relevant data structures.
    /// </para>
    /// </remarks>
    private void OnLogin()
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged in. CactpotDetector ready.");

        // LIMITATION: Initial ticket state query is not implemented.
        // Detection starts fresh each session - tickets used before login are not counted.
        // This limitation is documented in GetDetectionLimitations().

        _log.Information(
            "CactpotDetector: Mini Cactpot detection is session-only. " +
            "Jumbo Cactpot detection is not implemented. " +
            "Please manually track tickets used before this session.");
    }

    /// <summary>
    /// Handles player logout events.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged out. Clearing CactpotDetector state.");
        ResetAllStates();
    }

    /// <summary>
    /// Resets ticket counts based on reset type.
    /// </summary>
    /// <param name="resetWeekly">If true, also resets weekly (Jumbo) counts.</param>
    public void ResetStates(bool resetWeekly = false)
    {
        lock (_lock)
        {
            _lastMiniCactpotAddon = nint.Zero;
            _lastMiniCactpotRecordedUtc = DateTime.MinValue;

            // Always reset daily (Mini Cactpot)
            if (_ticketCounts["mini_cactpot"] > 0)
            {
                _ticketCounts["mini_cactpot"] = 0;
                try
                {
                    OnTaskStateChanged?.Invoke("mini_cactpot", false);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error firing reset event for mini_cactpot.");
                }
            }

            // Reset weekly if requested
            if (resetWeekly && _ticketCounts["jumbo_cactpot"] > 0)
            {
                _ticketCounts["jumbo_cactpot"] = 0;
                try
                {
                    OnTaskStateChanged?.Invoke("jumbo_cactpot", false);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error firing reset event for jumbo_cactpot.");
                }
            }
        }

        _log.Debug("CactpotDetector states reset (weekly: {ResetWeekly}).", resetWeekly);
    }

    /// <summary>
    /// Resets all states (both daily and weekly).
    /// </summary>
    private void ResetAllStates()
    {
        ResetStates(resetWeekly: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Unsubscribe from addon lifecycle listeners
        UnregisterAddonListeners();

        // Unsubscribe from all client state events
        if (_isInitialized)
        {
            _clientState.TerritoryChanged -= OnTerritoryChanged;
            _clientState.Login -= OnLogin;
            _clientState.Logout -= OnLogout;
        }

        lock (_lock)
        {
            _ticketCounts.Clear();
        }

        _log.Debug("CactpotDetector disposed.");
    }
}
