using System;
using System.Collections.Generic;
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
    private readonly IFramework _framework;

    private readonly Dictionary<string, int> _ticketCounts;
    private readonly object _lock = new();
    private bool _isInitialized;
    private bool _isDisposed;

    // Gold Saucer territory type ID
    private const ushort GoldSaucerTerritoryId = 144;

    // Ticket limits
    private const int MiniCactpotMaxTickets = 3;
    private const int JumboCactpotMaxTickets = 3;

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
    /// <param name="framework">The Dalamud framework service for update events.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public CactpotDetector(IPluginLog log, IClientState clientState, IFramework framework)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _clientState = clientState ?? throw new ArgumentNullException(nameof(clientState));
        _framework = framework ?? throw new ArgumentNullException(nameof(framework));

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
            if (territoryType == GoldSaucerTerritoryId)
            {
                _log.Debug("Entered Gold Saucer. CactpotDetector active.");
                OnEnterGoldSaucer();
            }
            else
            {
                _log.Verbose("Left Gold Saucer territory.");
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
        // TODO: Phase 2 - Set up Gold Saucer-specific monitoring
        // Possible approaches:
        // 1. Monitor for Mini Cactpot addon opening
        // 2. Track MGP changes after Cactpot interactions
        // 3. Read Cactpot ticket count from game data
        // 4. Hook Cactpot NPC interaction

        _log.Debug("TODO: Implement Gold Saucer Cactpot monitoring.");
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

        // Unsubscribe from all events
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
