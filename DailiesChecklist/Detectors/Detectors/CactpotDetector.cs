using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

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
    public event Action<string, bool>? OnTaskStateChanged;

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
            _log.Information("CactpotDetector initialized.");

            // TODO: Phase 2 - Query initial ticket state
            // Need to determine how to read current ticket usage from game data
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
    /// TODO: The LotteryWeekly addon appears for both viewing results and purchasing tickets.
    /// Additional logic is needed to differentiate between these states:
    /// - Check addon node values to determine if in purchase mode
    /// - Track button clicks for "Purchase" confirmation
    /// - Monitor for the purchase confirmation dialog
    /// For now, this handler logs the event but does not automatically record a purchase.
    /// </remarks>
    private void OnJumboCactpotAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (_isDisposed || !IsEnabled)
            return;

        try
        {
            // TODO: Implement logic to differentiate purchase from viewing
            // The addon appears in both cases, so we need to:
            // 1. Read addon node data to check current state
            // 2. Or register for button click events to detect actual purchases
            _log.Debug("Jumbo Cactpot addon detected - needs additional logic to determine if purchasing.");

            // Do NOT automatically record purchase here - need to verify it's actually a purchase
            // RecordJumboCactpotPurchase();
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
    private void OnLogin()
    {
        if (_isDisposed)
            return;

        _log.Debug("Player logged in. CactpotDetector ready.");

        // TODO: Phase 2 - Query current Cactpot ticket state on login
        // This should read from game data to restore the correct state
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
