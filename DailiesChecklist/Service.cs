using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DailiesChecklist;

/// <summary>
/// Static service container following the established Dalamud plugin pattern.
/// Provides centralized access to Dalamud services throughout the plugin.
/// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
internal class Service
{
    /// <summary>
    /// Core plugin interface for configs, paths, UI hooks.
    /// </summary>
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }

    /// <summary>
    /// Command manager for registering slash commands.
    /// </summary>
    [PluginService] public static ICommandManager CommandManager { get; private set; }

    /// <summary>
    /// Logging service for debug output.
    /// </summary>
    [PluginService] public static IPluginLog Log { get; private set; }

    /// <summary>
    /// Game client state (logged in, territory, etc.).
    /// </summary>
    [PluginService] public static IClientState ClientState { get; private set; }

    /// <summary>
    /// Access to game framework and main loop.
    /// </summary>
    [PluginService] public static IFramework Framework { get; private set; }

    /// <summary>
    /// Access to Lumina game data sheets.
    /// </summary>
    [PluginService] public static IDataManager DataManager { get; private set; }

    /// <summary>
    /// Player condition flags (in combat, mounted, etc.).
    /// </summary>
    [PluginService] public static ICondition Condition { get; private set; }

    /// <summary>
    /// Game UI access for addon reading.
    /// </summary>
    [PluginService] public static IGameGui GameGui { get; private set; }

    /// <summary>
    /// Addon lifecycle events for monitoring native UI windows.
    /// </summary>
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; }

    /// <summary>
    /// Duty state service for roulette completion detection.
    /// </summary>
    [PluginService] public static IDutyState DutyState { get; private set; }

    /// <summary>
    /// Initializes the service container with Dalamud services.
    /// Must be called at the start of the plugin constructor.
    /// </summary>
    /// <param name="pluginInterface">The plugin interface provided by Dalamud.</param>
    public static void Initialize(IDalamudPluginInterface pluginInterface)
        => pluginInterface.Create<Service>();
}
#pragma warning restore CS8618
