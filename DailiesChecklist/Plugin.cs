using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
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

    #endregion

    #region Constants

    /// <summary>
    /// Primary command to toggle the main window.
    /// </summary>
    private const string CommandName = "/dailies";

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

    #endregion

    #region Constructor

    /// <summary>
    /// Plugin constructor. Called when the plugin is loaded by Dalamud.
    /// Initializes configuration, windows, commands, and event subscriptions.
    /// </summary>
    public Plugin()
    {
        // Load saved configuration or create new defaults
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize windows
        MainWindow = new MainWindow(this);
        SettingsWindow = new SettingsWindow(this);

        // Add windows to the window system
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(SettingsWindow);

        // Register command handler
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Dailies Checklist window"
        });

        // Subscribe to UI events
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Log.Information("Dailies Checklist plugin loaded successfully!");
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Cleanup when the plugin is unloaded.
    /// Unregisters all events, removes windows, and disposes resources.
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from UI events
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        // Remove windows from window system
        WindowSystem.RemoveAllWindows();

        // Dispose individual windows
        MainWindow.Dispose();
        SettingsWindow.Dispose();

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
}
