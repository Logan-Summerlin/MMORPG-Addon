using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DailiesChecklist.Detectors;
using DailiesChecklist.Models;
using DailiesChecklist.Services;
using DailiesChecklist.Utils;
using DailiesChecklist.Windows;

namespace DailiesChecklist;

/// <summary>
/// Main plugin entry point for Dailies Checklist.
/// Implements IDalamudPlugin for Dalamud to recognize and load the plugin.
/// </summary>
public sealed class Plugin : IDalamudPlugin
{
    #region Constants

    /// <summary>
    /// Primary command to toggle the main window.
    /// </summary>
    private const string CommandName = "/dailies";

    /// <summary>
    /// Filename for the checklist state persistence file.
    /// </summary>
    private const string ChecklistStateFileName = "checklist_state.json";

    #endregion

    #region Properties

    /// <summary>
    /// Plugin configuration instance.
    /// </summary>
    public Configuration Configuration { get; init; }

    /// <summary>
    /// Window system for managing all plugin windows.
    /// </summary>
    public readonly WindowSystem WindowSystem = new("DailiesChecklist");

    /// <summary>
    /// Main checklist window.
    /// </summary>
    private MainWindow MainWindow { get; init; }

    /// <summary>
    /// Settings/configuration window.
    /// </summary>
    private SettingsWindow SettingsWindow { get; init; }

    /// <summary>
    /// Service for calculating and managing reset times.
    /// </summary>
    private ResetService ResetService { get; init; }

    /// <summary>
    /// Service for persisting checklist state to disk.
    /// </summary>
    private PersistenceService PersistenceService { get; init; }

    /// <summary>
    /// Service for auto-detecting task completion from game state.
    /// </summary>
    private DetectionService DetectionService { get; init; }

    /// <summary>
    /// Current checklist state containing all tasks and their completion status.
    /// </summary>
    private ChecklistState ChecklistState { get; set; }

    /// <summary>
    /// Next time to check for resets (throttled via framework update).
    /// </summary>
    private DateTime _nextResetCheckUtc = DateTime.MinValue;

    /// <summary>
    /// Flag indicating whether the initial reset sync is pending.
    /// Deferred to first framework tick to ensure detectors are fully initialized.
    /// </summary>
    private bool _pendingInitialResetSync = true;

    #endregion

    #region Constructor

    /// <summary>
    /// Plugin constructor. Called when the plugin is loaded by Dalamud.
    /// Initializes services, configuration, windows, commands, and event subscriptions.
    ///
    /// Services are injected via Dalamud's constructor injection mechanism.
    ///
    /// Initialization order:
    /// 1. Initialize Service container (via constructor injection)
    /// 2. Load Configuration
    /// 3. Create ResetService
    /// 4. Create PersistenceService
    /// 5. Load ChecklistState from persistence
    /// 6. Check and apply resets
    /// 7. Create DetectionService
    /// 8. Create Windows (passing state)
    /// 9. Subscribe to events
    /// </summary>
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    /// <param name="commandManager">Command manager for slash commands.</param>
    /// <param name="log">Plugin logging service.</param>
    /// <param name="clientState">Game client state service.</param>
    /// <param name="framework">Game framework service.</param>
    /// <param name="dataManager">Lumina data manager service.</param>
    /// <param name="condition">Player condition service.</param>
    /// <param name="gameGui">Game GUI service.</param>
    /// <param name="addonLifecycle">Addon lifecycle service.</param>
    /// <param name="dutyState">Duty state service.</param>
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IPluginLog log,
        IClientState clientState,
        IFramework framework,
        IDataManager dataManager,
        ICondition condition,
        IGameGui gameGui,
        IAddonLifecycle addonLifecycle,
        IDutyState dutyState)
    {
        // Pre-initialization breadcrumb (before Service is available)
        // This helps diagnose failures that occur before logging is set up
        System.Diagnostics.Debug.WriteLine("[DailiesChecklist] Plugin constructor started");

        try
        {
            // 1. Initialize the Service container with injected services
            Service.Initialize(
                pluginInterface,
                commandManager,
                log,
                clientState,
                framework,
                dataManager,
                condition,
                gameGui,
                addonLifecycle,
                dutyState);

            // EARLY BREADCRUMB - Log immediately after Service is available
            // This confirms the service container initialized successfully
            Service.Log.Information("[DailiesChecklist] Plugin loading - Service container initialized");

            // 2. Load saved configuration or create new defaults
            Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Service.Log.Debug("Configuration loaded.");

            // 3. Create ResetService
            ResetService = new ResetService();
            Service.Log.Debug("ResetService initialized.");

            // 4. Create PersistenceService (using file path approach for simplicity, with logger for diagnostics)
            var configPath = Path.Combine(Service.PluginInterface.ConfigDirectory.FullName, ChecklistStateFileName);
            PersistenceService = new PersistenceService(configPath, Service.Log);
            Service.Log.Debug("PersistenceService initialized with path: {Path}", configPath);

            // 5. Load ChecklistState from persistence
            ChecklistState = PersistenceService.Load();

            // Ensure tasks are populated if this is a fresh state or tasks were cleared
            if (ChecklistState.Tasks == null || ChecklistState.Tasks.Count == 0)
            {
                ChecklistState.Tasks = TaskRegistry.GetDefaultTasks();
                Service.Log.Information("Initialized checklist with {Count} default tasks.", ChecklistState.Tasks.Count);
            }
            else
            {
                Service.Log.Debug("Loaded existing checklist with {Count} tasks.", ChecklistState.Tasks.Count);
            }

            // 6. Create DetectionService
            // Note: Reset sync is deferred to first framework tick (Issue #4 fix)
            // to ensure detectors are fully initialized before receiving reset signals.
            DetectionService = new DetectionService(Service.Log);
            Service.Log.Debug("DetectionService initialized.");

            // Register detectors based on configuration feature flags
            // Reset sync will occur on first framework tick via _pendingInitialResetSync flag
            RegisterDetectors();

            // 7. Create Windows (passing state and services via dependency injection)
            MainWindow = new MainWindow(
                plugin: this,
                checklistState: ChecklistState,
                resetService: ResetService,
                onStateChanged: SaveChecklistState
            );
            SettingsWindow = new SettingsWindow(
                plugin: this,
                checklistState: ChecklistState,
                onStateChanged: SaveChecklistState
            );

            // Add windows to the window system
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(SettingsWindow);

            // Register command handler
            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggle the Dailies Checklist window"
            });

            // 8. Subscribe to events
            Service.PluginInterface.UiBuilder.Draw += DrawUI;
            Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUI;
            Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            Service.Framework.Update += OnFrameworkUpdate;

            // Subscribe to detection service events for auto-detection updates
            DetectionService.OnTaskStateChanged += OnTaskStateChanged;

            // Subscribe to persistence events for logging
            PersistenceService.OnSaveCompleted += OnSaveCompleted;
            PersistenceService.OnLoadCompleted += OnLoadCompleted;

            Service.Log.Information("Dailies Checklist plugin loaded successfully!");
        }
        catch (Exception ex)
        {
            // Log the exception before it propagates to Dalamud
            // This ensures we have diagnostic information for load failures
            try
            {
                Service.Log?.Error(ex, "FATAL: DailiesChecklist plugin failed to load during initialization!");
            }
            catch
            {
                // Service.Log may not be available if initialization failed early
                // Fall back to Debug output which can be captured by debuggers
                System.Diagnostics.Debug.WriteLine($"[DailiesChecklist] FATAL: Plugin failed to load: {ex}");
            }

            // Re-throw to let Dalamud handle the failure
            // The plugin will show as "Load Error" but we now have logged the reason
            throw;
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Cleanup when the plugin is unloaded.
    /// Unregisters all events, removes windows, and disposes resources.
    ///
    /// Disposal order (reverse of initialization):
    /// 1. Unsubscribe events
    /// 2. Dispose Windows
    /// 3. Dispose DetectionService
    /// 4. Save state via PersistenceService
    /// 5. Dispose PersistenceService
    /// 6. Dispose ResetService
    ///
    /// Issue #6 fix: All service references are captured locally at the start
    /// to ensure safe access even if partial initialization or disposal occurs.
    /// </summary>
    public void Dispose()
    {
        Service.Log.Information("Disposing Dailies Checklist plugin...");

        // Issue #6 fix: Capture all service/window references locally before disposal.
        // This prevents null reference issues if services were partially initialized
        // or if disposal is called during failure recovery.
        var localDetectionService = DetectionService;
        var localPersistenceService = PersistenceService;
        var localResetService = ResetService;
        var localChecklistState = ChecklistState;
        var localMainWindow = MainWindow;
        var localSettingsWindow = SettingsWindow;

        // 1. Unsubscribe from all events (using Service references with null checks)
        if (Service.PluginInterface?.UiBuilder != null)
        {
            Service.PluginInterface.UiBuilder.Draw -= DrawUI;
            Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsUI;
            Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        }

        if (Service.Framework != null)
        {
            Service.Framework.Update -= OnFrameworkUpdate;
        }

        // Unsubscribe from service events
        if (localDetectionService != null)
        {
            localDetectionService.OnTaskStateChanged -= OnTaskStateChanged;
        }

        if (localPersistenceService != null)
        {
            localPersistenceService.OnSaveCompleted -= OnSaveCompleted;
            localPersistenceService.OnLoadCompleted -= OnLoadCompleted;
        }

        // 2. Dispose Windows
        WindowSystem.RemoveAllWindows();
        try
        {
            localMainWindow?.Dispose();
            localSettingsWindow?.Dispose();
            Service.Log.Debug("Windows disposed.");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Error disposing windows.");
        }

        // Clear popup state to prevent memory leaks
        UIHelpers.ClearPopupState();

        // 3. Dispose DetectionService
        try
        {
            localDetectionService?.Dispose();
            Service.Log.Debug("DetectionService disposed.");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Error disposing DetectionService.");
        }

        // 4. Save state via PersistenceService (immediate save before disposal)
        if (localPersistenceService != null && localChecklistState != null)
        {
            try
            {
                localPersistenceService.SaveImmediate(localChecklistState);
                Service.Log.Debug("ChecklistState saved on disposal.");
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "Failed to save checklist state during disposal.");
            }
        }

        // 5. Dispose PersistenceService
        try
        {
            localPersistenceService?.Dispose();
            Service.Log.Debug("PersistenceService disposed.");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Error disposing PersistenceService.");
        }

        // 6. Dispose ResetService
        try
        {
            localResetService?.Dispose();
            Service.Log.Debug("ResetService disposed.");
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Error disposing ResetService.");
        }

        // Unregister command handlers
        try
        {
            Service.CommandManager?.RemoveHandler(CommandName);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Error removing command handler.");
        }

        Service.Log.Information("Dailies Checklist plugin unloaded.");
    }

    #endregion

    #region Command Handlers

    /// <summary>
    /// Handler for the /dailies command.
    /// Toggles the main checklist window visibility.
    /// </summary>
    /// <param name="command">The command string.</param>
    /// <param name="args">Any arguments passed with the command.</param>
    private void OnCommand(string command, string args)
    {
        // Parse subcommands if any
        var argList = args.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (argList.Length == 0)
        {
            // No arguments: toggle main window
            ToggleMainUI();
            return;
        }

        switch (argList[0].ToLowerInvariant())
        {
            case "config":
            case "settings":
                ToggleSettingsUI();
                break;
            default:
                // Unknown subcommand: toggle main window
                ToggleMainUI();
                break;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handler for detection service task state changes.
    /// Updates the checklist state when auto-detection reports a change
    /// and triggers a debounced save.
    /// </summary>
    /// <param name="taskId">The ID of the task that changed.</param>
    /// <param name="isCompleted">Whether the task is now completed.</param>
    /// <param name="detectorType">The type of detector that reported the change.</param>
    private void OnTaskStateChanged(string taskId, bool isCompleted, Type detectorType)
    {
        if (ChecklistState == null)
        {
            return;
        }

        var task = ChecklistState.GetTaskById(taskId);
        if (task == null)
        {
            Service.Log.Warning("Received state change for unknown task '{TaskId}'.", taskId);
            return;
        }

        // Only update if not manually overridden
        if (task.IsManuallySet)
        {
            Service.Log.Debug("Ignoring auto-detection for manually set task '{TaskId}'.", taskId);
            return;
        }

        // Update the task state
        if (task.IsCompleted != isCompleted)
        {
            task.IsCompleted = isCompleted;
            task.CompletedAt = isCompleted ? DateTime.UtcNow : null;
            if (task.MaxCount > 1)
            {
                task.CurrentCount = isCompleted ? task.MaxCount : 0;
            }

            Service.Log.Debug("Task '{TaskId}' auto-detected as {State} by {Detector}.",
                taskId,
                isCompleted ? "complete" : "incomplete",
                detectorType.Name);

            // Trigger debounced save
            PersistenceService?.Save(ChecklistState);
        }
    }

    /// <summary>
    /// Handler for persistence save completion events.
    /// </summary>
    /// <param name="success">Whether the save was successful.</param>
    private void OnSaveCompleted(bool success)
    {
        if (success)
        {
            Service.Log.Debug("Checklist state saved successfully.");
        }
        else
        {
            Service.Log.Warning("Failed to save checklist state.");
        }
    }

    /// <summary>
    /// Handler for persistence load completion events.
    /// </summary>
    /// <param name="success">Whether the load was successful.</param>
    private void OnLoadCompleted(bool success)
    {
        if (success)
        {
            Service.Log.Debug("Checklist state loaded successfully.");
        }
        else
        {
            Service.Log.Warning("Failed to load checklist state, using defaults.");
        }
    }

    #endregion

    #region Reset Handling

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Issue #4 fix: Perform initial reset sync on first framework tick
        // This ensures detectors have completed their async initialization
        // before receiving reset signals.
        if (_pendingInitialResetSync)
        {
            _pendingInitialResetSync = false;
            Service.Log.Debug("Performing deferred initial reset sync on first framework tick.");
            ApplyResetsAndSyncDetectors();
            return;
        }

        var now = DateTime.UtcNow;
        if (now < _nextResetCheckUtc)
        {
            return;
        }

        _nextResetCheckUtc = now.AddMinutes(1);
        ApplyResetsAndSyncDetectors();
    }

    private void ApplyResetsAndSyncDetectors()
    {
        var appliedResets = ResetService.CheckAndApplyResets(ChecklistState);
        var resetApplied = false;

        foreach (var kvp in appliedResets)
        {
            if (kvp.Value)
            {
                resetApplied = true;
                Service.Log.Information("Applied {ResetType} reset.", kvp.Key);
            }
        }

        if (appliedResets.TryGetValue(ResetType.Daily, out var dailyResetApplied) && dailyResetApplied)
        {
            DetectionService.GetDetector<RouletteDetector>()?.ResetAllStates();
            DetectionService.GetDetector<BeastTribeDetector>()?.ResetState();
            DetectionService.GetDetector<CactpotDetector>()?.ResetStates(resetWeekly: false);
        }

        if (appliedResets.TryGetValue(ResetType.Weekly, out var weeklyResetApplied) && weeklyResetApplied)
        {
            DetectionService.GetDetector<CactpotDetector>()?.ResetStates(resetWeekly: true);
        }

        if (resetApplied)
        {
            SaveChecklistState();
        }
    }

    #endregion

    #region UI Methods

    /// <summary>
    /// Draw callback for the UI. Called every frame.
    /// </summary>
    private void DrawUI() => WindowSystem.Draw();

    /// <summary>
    /// Toggles the main checklist window visibility.
    /// </summary>
    public void ToggleMainUI() => MainWindow.Toggle();

    /// <summary>
    /// Toggles the settings window visibility.
    /// </summary>
    public void ToggleSettingsUI() => SettingsWindow.Toggle();

    #endregion

    #region Public Service Access

    /// <summary>
    /// Triggers a debounced save of the current checklist state.
    /// Called by windows when user makes manual changes.
    /// </summary>
    public void SaveChecklistState()
    {
        if (PersistenceService != null && ChecklistState != null)
        {
            PersistenceService.Save(ChecklistState);
        }
    }

    /// <summary>
    /// Gets the current checklist state.
    /// Used by windows to access task data.
    /// </summary>
    public ChecklistState? GetChecklistState() => ChecklistState;

    /// <summary>
    /// Gets the reset service for time calculations.
    /// </summary>
    public ResetService? GetResetService() => ResetService;

    /// <summary>
    /// Gets the detection service for registering detectors.
    /// </summary>
    public DetectionService? GetDetectionService() => DetectionService;

    #endregion

    #region Detector Registration

    /// <summary>
    /// Registers available detectors based on configuration feature flags.
    /// </summary>
    private void RegisterDetectors()
    {
        try
        {
            DetectionService.AddDetector(
                new RouletteDetector(Service.Log, Service.DutyState, Service.ClientState),
                Configuration.FeatureFlags.EnableRouletteDetection);

            DetectionService.AddDetector(
                new CactpotDetector(Service.Log, Service.ClientState, Service.AddonLifecycle),
                Configuration.FeatureFlags.EnableCactpotDetection);

            DetectionService.AddDetector(
                new BeastTribeDetector(Service.Log, Service.ClientState),
                Configuration.FeatureFlags.EnableBeastTribeDetection);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Failed to register one or more detectors.");
        }
    }

    /// <summary>
    /// Applies current configuration feature flags to active detectors.
    /// Called after settings changes.
    /// </summary>
    public void ApplyDetectorFeatureFlags()
    {
        DetectionService.SetDetectorEnabled<RouletteDetector>(Configuration.FeatureFlags.EnableRouletteDetection);
        DetectionService.SetDetectorEnabled<CactpotDetector>(Configuration.FeatureFlags.EnableCactpotDetection);
        DetectionService.SetDetectorEnabled<BeastTribeDetector>(Configuration.FeatureFlags.EnableBeastTribeDetection);
    }

    #endregion
}
