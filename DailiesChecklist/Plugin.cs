using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DailiesChecklist.Detectors;
using DailiesChecklist.Models;
using DailiesChecklist.Services;
using DailiesChecklist.Windows;

namespace DailiesChecklist;

/// <summary>
/// Main plugin entry point for Dailies Checklist.
/// Implements IDalamudPlugin for Dalamud to recognize and load the plugin.
/// </summary>
public sealed class Plugin : IDalamudPlugin
{
    #region Dalamud Services

    /// <summary>
    /// Core plugin interface for configs, paths, UI hooks.
    /// </summary>
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    /// <summary>
    /// Command manager for registering slash commands.
    /// </summary>
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    /// <summary>
    /// Logging service for debug output.
    /// </summary>
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    /// <summary>
    /// Game client state (logged in, territory, etc.).
    /// </summary>
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    /// <summary>
    /// Access to game framework and main loop.
    /// </summary>
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    /// <summary>
    /// Access to Lumina game data sheets.
    /// </summary>
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    /// <summary>
    /// Player condition flags (in combat, mounted, etc.).
    /// </summary>
    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    /// <summary>
    /// Game UI access for addon reading.
    /// </summary>
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    /// <summary>
    /// Addon lifecycle events for monitoring native UI windows.
    /// </summary>
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    #endregion

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

    #endregion

    #region Constructor

    /// <summary>
    /// Plugin constructor. Called when the plugin is loaded by Dalamud.
    /// Initializes services, configuration, windows, commands, and event subscriptions.
    ///
    /// Initialization order:
    /// 1. Load Configuration
    /// 2. Create ResetService
    /// 3. Create PersistenceService
    /// 4. Load ChecklistState from persistence
    /// 5. Check and apply resets
    /// 6. Create DetectionService
    /// 7. Create Windows (passing state)
    /// 8. Subscribe to events
    /// </summary>
    public Plugin()
    {
        // 1. Load saved configuration or create new defaults
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Log.Debug("Configuration loaded.");

        // 2. Create ResetService
        ResetService = new ResetService();
        Log.Debug("ResetService initialized.");

        // 3. Create PersistenceService (using file path approach for simplicity, with logger for diagnostics)
        var configPath = Path.Combine(PluginInterface.ConfigDirectory.FullName, ChecklistStateFileName);
        PersistenceService = new PersistenceService(configPath, Log);
        Log.Debug("PersistenceService initialized with path: {Path}", configPath);

        // 4. Load ChecklistState from persistence
        ChecklistState = PersistenceService.Load();

        // Ensure tasks are populated if this is a fresh state or tasks were cleared
        if (ChecklistState.Tasks == null || ChecklistState.Tasks.Count == 0)
        {
            ChecklistState.Tasks = TaskRegistry.GetDefaultTasks();
            Log.Information("Initialized checklist with {Count} default tasks.", ChecklistState.Tasks.Count);
        }
        else
        {
            Log.Debug("Loaded existing checklist with {Count} tasks.", ChecklistState.Tasks.Count);
        }

        // 5. Check and apply resets
        var appliedResets = ResetService.CheckAndApplyResets(ChecklistState);
        foreach (var kvp in appliedResets)
        {
            if (kvp.Value)
            {
                Log.Information("Applied {ResetType} reset.", kvp.Key);
            }
        }

        // 6. Create DetectionService
        DetectionService = new DetectionService(Log);
        Log.Debug("DetectionService initialized.");

        // 7. Create Windows (passing state and services via dependency injection)
        MainWindow = new MainWindow(
            plugin: this,
            checklistState: ChecklistState,
            resetService: ResetService,
            onStateChanged: SaveChecklistState
        );
        SettingsWindow = new SettingsWindow(this);

        // Add windows to the window system
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(SettingsWindow);

        // Register command handler
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Dailies Checklist window"
        });

        // 8. Subscribe to events
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Subscribe to detection service events for auto-detection updates
        DetectionService.OnTaskStateChanged += OnTaskStateChanged;

        // Subscribe to persistence events for logging
        PersistenceService.OnSaveCompleted += OnSaveCompleted;
        PersistenceService.OnLoadCompleted += OnLoadCompleted;

        Log.Information("Dailies Checklist plugin loaded successfully!");
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
    /// </summary>
    public void Dispose()
    {
        Log.Information("Disposing Dailies Checklist plugin...");

        // 1. Unsubscribe from all events
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        // Unsubscribe from service events (null-safe)
        if (DetectionService != null)
        {
            DetectionService.OnTaskStateChanged -= OnTaskStateChanged;
        }

        if (PersistenceService != null)
        {
            PersistenceService.OnSaveCompleted -= OnSaveCompleted;
            PersistenceService.OnLoadCompleted -= OnLoadCompleted;
        }

        // 2. Dispose Windows
        WindowSystem.RemoveAllWindows();
        MainWindow?.Dispose();
        SettingsWindow?.Dispose();
        Log.Debug("Windows disposed.");

        // 3. Dispose DetectionService
        DetectionService?.Dispose();
        Log.Debug("DetectionService disposed.");

        // 4. Save state via PersistenceService (immediate save before disposal)
        if (PersistenceService != null && ChecklistState != null)
        {
            try
            {
                PersistenceService.SaveImmediate(ChecklistState);
                Log.Debug("ChecklistState saved on disposal.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save checklist state during disposal.");
            }
        }

        // 5. Dispose PersistenceService
        PersistenceService?.Dispose();
        Log.Debug("PersistenceService disposed.");

        // 6. Dispose ResetService
        ResetService?.Dispose();
        Log.Debug("ResetService disposed.");

        // Unregister command handlers
        CommandManager.RemoveHandler(CommandName);

        Log.Information("Dailies Checklist plugin unloaded.");
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
            Log.Warning("Received state change for unknown task '{TaskId}'.", taskId);
            return;
        }

        // Only update if not manually overridden
        if (task.IsManuallySet)
        {
            Log.Debug("Ignoring auto-detection for manually set task '{TaskId}'.", taskId);
            return;
        }

        // Update the task state
        if (task.IsCompleted != isCompleted)
        {
            task.IsCompleted = isCompleted;
            task.CompletedAt = isCompleted ? DateTime.UtcNow : null;

            Log.Debug("Task '{TaskId}' auto-detected as {State} by {Detector}.",
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
            Log.Debug("Checklist state saved successfully.");
        }
        else
        {
            Log.Warning("Failed to save checklist state.");
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
            Log.Debug("Checklist state loaded successfully.");
        }
        else
        {
            Log.Warning("Failed to load checklist state, using defaults.");
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
}
