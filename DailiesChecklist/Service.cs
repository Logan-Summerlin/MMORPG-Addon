using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DailiesChecklist;

/// <summary>
/// Static service container following the established Dalamud plugin pattern.
/// Provides centralized access to Dalamud services throughout the plugin.
///
/// Services are initialized via constructor injection in the Plugin class
/// and passed to Initialize() for centralized access.
/// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
internal static class Service
{
    /// <summary>
    /// Core plugin interface for configs, paths, UI hooks.
    /// </summary>
    public static IDalamudPluginInterface PluginInterface { get; private set; }

    /// <summary>
    /// Command manager for registering slash commands.
    /// </summary>
    public static ICommandManager CommandManager { get; private set; }

    /// <summary>
    /// Logging service for debug output.
    /// </summary>
    public static IPluginLog Log { get; private set; }

    /// <summary>
    /// Game client state (logged in, territory, etc.).
    /// </summary>
    public static IClientState ClientState { get; private set; }

    /// <summary>
    /// Access to game framework and main loop.
    /// </summary>
    public static IFramework Framework { get; private set; }

    /// <summary>
    /// Access to Lumina game data sheets.
    /// </summary>
    public static IDataManager DataManager { get; private set; }

    /// <summary>
    /// Player condition flags (in combat, mounted, etc.).
    /// </summary>
    public static ICondition Condition { get; private set; }

    /// <summary>
    /// Game UI access for addon reading.
    /// </summary>
    public static IGameGui GameGui { get; private set; }

    /// <summary>
    /// Addon lifecycle events for monitoring native UI windows.
    /// </summary>
    public static IAddonLifecycle AddonLifecycle { get; private set; }

    /// <summary>
    /// Duty state service for roulette completion detection.
    /// </summary>
    public static IDutyState DutyState { get; private set; }

    /// <summary>
    /// Initializes the service container with Dalamud services.
    /// Must be called at the start of the plugin constructor.
    ///
    /// Services are injected via Dalamud's constructor injection into the Plugin class
    /// and then passed here for centralized access throughout the plugin.
    /// </summary>
    /// <param name="pluginInterface">The plugin interface provided by Dalamud.</param>
    /// <param name="commandManager">Command manager for slash commands.</param>
    /// <param name="log">Plugin logging service.</param>
    /// <param name="clientState">Game client state service.</param>
    /// <param name="framework">Game framework service.</param>
    /// <param name="dataManager">Lumina data manager service.</param>
    /// <param name="condition">Player condition service.</param>
    /// <param name="gameGui">Game GUI service.</param>
    /// <param name="addonLifecycle">Addon lifecycle service.</param>
    /// <param name="dutyState">Duty state service.</param>
    public static void Initialize(
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
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        Log = log;
        ClientState = clientState;
        Framework = framework;
        DataManager = dataManager;
        Condition = condition;
        GameGui = gameGui;
        AddonLifecycle = addonLifecycle;
        DutyState = dutyState;
    }
}
#pragma warning restore CS8618
