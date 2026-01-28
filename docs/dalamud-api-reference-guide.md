# Dalamud API Reference Guide

A comprehensive technical reference for developing plugins using the Dalamud framework for Final Fantasy XIV.

**Current API Version:** 14
**Supported .NET Version:** .NET 10.0
**Last Updated:** January 2026

---

## Table of Contents

1. [Service Architecture and Dependency Injection](#service-architecture-and-dependency-injection)
2. [Core Services Reference](#core-services-reference)
3. [ImGui UI Development](#imgui-ui-development)
4. [Game Data Access](#game-data-access)
5. [Event System and Hooks](#event-system-and-hooks)
6. [Command Registration](#command-registration)
7. [Chat and Notifications](#chat-and-notifications)
8. [Configuration Persistence](#configuration-persistence)
9. [Additional Services](#additional-services)
10. [Best Practices](#best-practices)

---

## Service Architecture and Dependency Injection

Dalamud uses a service-based architecture with dependency injection (DI) to provide plugins with access to game state, UI systems, and framework utilities.

### Plugin Entry Point

Every Dalamud plugin must contain exactly one class implementing `IDalamudPlugin`:

```csharp
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

public class MyPlugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private readonly ICommandManager commandManager;

    public MyPlugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog log,
        ICommandManager commandManager)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.commandManager = commandManager;

        this.log.Information("Plugin loaded!");
    }

    public void Dispose()
    {
        // Clean up resources
    }
}
```

### Service Injection Methods

**Constructor Injection (Recommended):**
```csharp
public MyPlugin(IClientState clientState, IDataManager dataManager)
{
    // Services automatically injected by Dalamud
}
```

**Property Injection:**
```csharp
[PluginService] public static IClientState ClientState { get; private set; } = null!;
```

**Manual Injection:**
```csharp
// Using IDalamudPluginInterface
pluginInterface.Inject(myObject);

// Creating objects with injection
var myWindow = pluginInterface.Create<MyWindow>();
```

### IDalamudPluginInterface

The primary interface for plugin-framework interaction.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `InternalName` | `string` | Plugin's internal identifier |
| `ConfigDirectory` | `DirectoryInfo` | Plugin configuration folder |
| `ConfigFile` | `FileInfo` | Plugin configuration file path |
| `AssemblyLocation` | `FileInfo` | Plugin assembly location |
| `UiBuilder` | `IUiBuilder` | ImGui drawing interface |
| `UiLanguage` | `string` | Current UI language (ISO format) |
| `IsDevMenuOpen` | `bool` | Debug mode status |
| `IsDev` | `bool` | Whether this is a development plugin |

**Key Methods:**
```csharp
// Configuration
void SavePluginConfig(IPluginConfiguration config);
T? GetPluginConfig<T>() where T : IPluginConfiguration;

// Object creation with DI
T Create<T>(params object[] args);
Task<T> CreateAsync<T>(params object[] args);
void Inject(object instance);

// IPC (Inter-Plugin Communication)
ICallGateProvider<TRet> GetIpcProvider<TRet>(string name);
ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name);

// Data sharing
T GetOrCreateData<T>(string tag, Func<T> dataGenerator);
bool TryGetData<T>(string tag, out T data);
```

---

## Core Services Reference

All services are located in the `Dalamud.Plugin.Services` namespace.

### IClientState

Represents the game client state at time of access.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `ClientLanguage` | `ClientLanguage` | Game client language |
| `TerritoryType` | `ushort` | Current territory/zone ID |
| `MapId` | `uint` | Current map ID |
| `Instance` | `uint` | Zone instance number |
| `LocalPlayer` | `IPlayerCharacter?` | Local player character |
| `LocalContentId` | `ulong` | Local character content ID |
| `IsLoggedIn` | `bool` | Character login status |
| `IsPvP` | `bool` | PvP area status |
| `IsGPosing` | `bool` | Group Pose mode status |

**Events:**
```csharp
event Action<ushort> TerritoryChanged;
event Action<uint> MapIdChanged;
event Action<uint> InstanceChanged;
event ClassJobChangeDelegate ClassJobChanged;
event LevelChangeDelegate LevelChanged;
event Action Login;
event LogoutDelegate Logout;
event Action EnterPvP;
event Action LeavePvP;
event Action<ContentFinderCondition> CfPop;  // Duty Finder pop
```

**Example:**
```csharp
public MyPlugin(IClientState clientState)
{
    clientState.TerritoryChanged += OnTerritoryChanged;

    if (clientState.IsLoggedIn)
    {
        var player = clientState.LocalPlayer;
        log.Info($"Logged in as {player?.Name}");
    }
}

private void OnTerritoryChanged(ushort territoryId)
{
    log.Info($"Entered territory: {territoryId}");
}
```

### IFramework

Represents the game framework for update loop access.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `LastUpdate` | `DateTime` | Last framework update time |
| `LastUpdateUTC` | `DateTime` | Last update time (UTC) |
| `UpdateDelta` | `TimeSpan` | Delta between updates |
| `IsInFrameworkUpdateThread` | `bool` | On framework thread |
| `IsFrameworkUnloading` | `bool` | Framework unloading status |

**Methods:**
```csharp
// Execute on framework thread
Task Run(Action action);
Task<T> Run<T>(Func<T> func);

// Run on next framework tick
Task RunOnTick(Action action, TimeSpan delay = default, int numTicks = 0);

// Get task factory for framework execution
TaskFactory GetTaskFactory();

// Delay execution
Task DelayTicks(long ticks, CancellationToken token = default);
```

**Events:**
```csharp
event Action<IFramework> Update;  // Fires every frame
```

**Example:**
```csharp
private void SubscribeToFramework(IFramework framework)
{
    framework.Update += OnFrameworkUpdate;
}

private void OnFrameworkUpdate(IFramework framework)
{
    // Called every frame - keep this lightweight!
    if (someCondition)
    {
        // Do something
    }
}

// Execute something on the framework thread
await framework.Run(() => {
    // This code runs on the game's main thread
});
```

### ICondition

Provides access to player condition flags.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `MaxEntries` | `int` | Maximum condition count |
| `this[ConditionFlag]` | `bool` | Check specific flag |

**Methods:**
```csharp
IReadOnlySet<ConditionFlag> AsReadOnlySet();
bool Any();
bool Any(params ConditionFlag[] flags);
bool AnyExcept(params ConditionFlag[] flags);
bool EqualTo(params ConditionFlag[] flags);
```

**Events:**
```csharp
event ConditionChangeDelegate ConditionChange;
```

**Example:**
```csharp
public void CheckConditions(ICondition condition)
{
    // Check if in combat
    if (condition[ConditionFlag.InCombat])
    {
        log.Info("Player is in combat");
    }

    // Check if mounted
    if (condition[ConditionFlag.Mounted])
    {
        log.Info("Player is mounted");
    }

    // Check multiple conditions
    if (condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.InDutyQueue))
    {
        log.Info("Player is in duty or queue");
    }
}
```

**Common ConditionFlags:**
- `ConditionFlag.InCombat`
- `ConditionFlag.Mounted`
- `ConditionFlag.BoundByDuty`
- `ConditionFlag.Casting`
- `ConditionFlag.Crafting`
- `ConditionFlag.Gathering`
- `ConditionFlag.InDutyQueue`
- `ConditionFlag.WatchingCutscene`

### IObjectTable

Access to spawned game objects.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Length` | `int` | Total object count |
| `LocalPlayer` | `IPlayerCharacter?` | Current player |
| `PlayerObjects` | `IEnumerable<IBattleChara>` | Battle characters |
| `CharacterManagerObjects` | `IEnumerable<IGameObject>` | Indexes 0-199 |
| `ClientObjects` | `IEnumerable<IGameObject>` | Indexes 200-448 |
| `EventObjects` | `IEnumerable<IGameObject>` | Indexes 449-488 |
| `this[int]` | `IGameObject?` | Index accessor |

**Methods:**
```csharp
IGameObject? SearchById(ulong gameObjectId);
IGameObject? SearchByEntityId(uint entityId);
nint GetObjectAddress(int index);
IGameObject? CreateObjectReference(nint address);
```

**Example:**
```csharp
public void FindNearbyEnemies(IObjectTable objectTable, IClientState clientState)
{
    var player = clientState.LocalPlayer;
    if (player == null) return;

    foreach (var obj in objectTable)
    {
        if (obj is IBattleNpc npc && npc.BattleNpcKind == BattleNpcSubKind.Enemy)
        {
            var distance = Vector3.Distance(player.Position, npc.Position);
            if (distance < 30f)
            {
                log.Info($"Enemy nearby: {npc.Name} at {distance:F1}y");
            }
        }
    }
}
```

### ITargetManager

Get and set player targets.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Target` | `IGameObject?` | Current target (settable) |
| `MouseOverTarget` | `IGameObject?` | Mouse-over target |
| `FocusTarget` | `IGameObject?` | Focus target (settable) |
| `PreviousTarget` | `IGameObject?` | Previous target |
| `SoftTarget` | `IGameObject?` | Soft target |
| `GPoseTarget` | `IGameObject?` | GPose target |
| `MouseOverNameplateTarget` | `IGameObject?` | Nameplate hover target |

**Example:**
```csharp
public void ShowTargetInfo(ITargetManager targetManager)
{
    var target = targetManager.Target;
    if (target != null)
    {
        log.Info($"Current target: {target.Name} (ID: {target.GameObjectId})");
    }

    var focus = targetManager.FocusTarget;
    if (focus != null)
    {
        log.Info($"Focus target: {focus.Name}");
    }
}
```

### IPluginLog

Structured logging service.

**Log Levels:**
```csharp
void Verbose(string message, params object[] args);
void Debug(string message, params object[] args);
void Information(string message, params object[] args);  // Also: Info()
void Warning(string message, params object[] args);
void Error(string message, params object[] args);
void Error(Exception exception, string message, params object[] args);
void Fatal(string message, params object[] args);
void Fatal(Exception exception, string message, params object[] args);
```

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Logger` | `ILogger` | Serilog logger instance |
| `MinimumLogLevel` | `LogEventLevel` | Minimum log level |

**Example:**
```csharp
public void DoSomething(IPluginLog log)
{
    log.Info("Starting operation...");

    try
    {
        // Do work
        log.Debug("Processing item {ItemId}", itemId);
    }
    catch (Exception ex)
    {
        log.Error(ex, "Failed to process item {ItemId}", itemId);
    }
}
```

---

## ImGui UI Development

### IUiBuilder

The primary interface for drawing UI overlays.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `DefaultFontHandle` | `IFontHandle` | Default Dalamud font |
| `IconFontHandle` | `IFontHandle` | FontAwesome icons |
| `MonoFontHandle` | `IFontHandle` | Monospace font |
| `FrameCount` | `ulong` | Draw call count |
| `CutsceneActive` | `bool` | Cutscene playing |
| `ShouldModifyUi` | `bool` | UI modification allowed |

**Visibility Controls:**
```csharp
bool DisableAutomaticUiHide { get; set; }
bool DisableUserUiHide { get; set; }
bool DisableCutsceneUiHide { get; set; }
bool DisableGposeUiHide { get; set; }
```

**Events:**
```csharp
event Action Draw;           // Main draw event
event Action OpenConfigUi;   // Settings button clicked
event Action OpenMainUi;     // Main UI button clicked
event Action ShowUi;         // UI shown
event Action HideUi;         // UI hidden
```

**Example:**
```csharp
public MyPlugin(IDalamudPluginInterface pluginInterface)
{
    pluginInterface.UiBuilder.Draw += OnDraw;
    pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfig;
}

private void OnDraw()
{
    // Direct ImGui calls
    if (ImGui.Begin("My Window"))
    {
        ImGui.Text("Hello, World!");
        ImGui.End();
    }
}
```

### WindowSystem

Dalamud's managed window system with UX integration.

**Setup:**
```csharp
using Dalamud.Interface.Windowing;

public class MyPlugin : IDalamudPlugin
{
    private readonly WindowSystem windowSystem = new("MyPlugin");
    private readonly MyMainWindow mainWindow;

    public MyPlugin(IDalamudPluginInterface pluginInterface)
    {
        mainWindow = new MyMainWindow();
        windowSystem.AddWindow(mainWindow);

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += () => mainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
    }
}
```

### Window Class

Base class for creating managed windows.

**Key Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `WindowName` | `string` | Display name |
| `IsOpen` | `bool` | Visibility state |
| `IsFocused` | `bool` | Focus state |
| `Flags` | `ImGuiWindowFlags` | Window behavior flags |
| `Position` | `Vector2?` | Window position |
| `Size` | `Vector2?` | Window size |
| `SizeConstraints` | `WindowSizeConstraints?` | Min/max size |
| `BgAlpha` | `float` | Background opacity |
| `RespectCloseHotkey` | `bool` | Escape closes window |
| `ShowCloseButton` | `bool` | Show X button |
| `AllowPinning` | `bool` | Allow window pinning |

**Lifecycle Methods:**
```csharp
public abstract void Draw();              // Main rendering
public virtual void PreDraw() { }         // Before rendering
public virtual void PostDraw() { }        // After rendering
public virtual void Update() { }          // Every frame
public virtual void OnOpen() { }          // When opened
public virtual void OnClose() { }         // When closed
public virtual bool DrawConditions() => true;  // Visibility check
```

**Example Window:**
```csharp
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

public class MyMainWindow : Window
{
    public MyMainWindow()
        : base("My Plugin###MyPluginMainWindow",
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(800, 600)
        };
    }

    public override void Draw()
    {
        ImGui.Text("Welcome to My Plugin!");

        if (ImGui.Button("Click Me"))
        {
            // Handle click
        }

        ImGui.Separator();

        ImGui.BeginChild("ScrollRegion", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()));
        // Scrollable content
        ImGui.EndChild();
    }

    public override void OnClose()
    {
        // Save state or cleanup
    }
}
```

### Common ImGui Patterns

**Tables:**
```csharp
if (ImGui.BeginTable("MyTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
{
    ImGui.TableSetupColumn("Name");
    ImGui.TableSetupColumn("Value");
    ImGui.TableSetupColumn("Actions");
    ImGui.TableHeadersRow();

    foreach (var item in items)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(item.Name);
        ImGui.TableNextColumn();
        ImGui.Text(item.Value.ToString());
        ImGui.TableNextColumn();
        if (ImGui.Button($"Edit##{item.Id}"))
        {
            // Edit item
        }
    }

    ImGui.EndTable();
}
```

**Tabs:**
```csharp
if (ImGui.BeginTabBar("MyTabs"))
{
    if (ImGui.BeginTabItem("General"))
    {
        // General tab content
        ImGui.EndTabItem();
    }

    if (ImGui.BeginTabItem("Settings"))
    {
        // Settings tab content
        ImGui.EndTabItem();
    }

    ImGui.EndTabBar();
}
```

**Collapsing Headers:**
```csharp
if (ImGui.CollapsingHeader("Advanced Options", ImGuiTreeNodeFlags.DefaultOpen))
{
    // Advanced content
}
```

---

## Game Data Access

### IDataManager

Access to game data files and Excel sheets.

**Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `Language` | `ClientLanguage` | Client language |
| `GameData` | `GameData` | Lumina GameData instance |
| `Excel` | `ExcelModule` | Excel sheet module |
| `HasModifiedGameDataFiles` | `bool` | Modified data detected |

**Methods:**
```csharp
// Get Excel sheet by row type
ExcelSheet<T>? GetExcelSheet<T>(ClientLanguage? language = null, string? name = null)
    where T : struct, IExcelRow<T>;

// Get subrow-based Excel sheet
SubrowExcelSheet<T>? GetSubrowExcelSheet<T>(ClientLanguage? language = null, string? name = null)
    where T : struct, IExcelSubrow<T>;

// Get game files
FileResource? GetFile(string path);
T? GetFile<T>(string path) where T : FileResource;
Task<T?> GetFileAsync<T>(string path, CancellationToken token = default);
bool FileExists(string path);
```

**Excel Sheet Example:**
```csharp
using Lumina.Excel.Sheets;

public void GetItemData(IDataManager dataManager)
{
    var itemSheet = dataManager.GetExcelSheet<Item>();
    if (itemSheet == null) return;

    // Get specific item by row ID
    var ironOre = itemSheet.GetRow(5111);
    log.Info($"Item: {ironOre.Name}, Level: {ironOre.LevelItem.RowId}");

    // Iterate all items
    foreach (var item in itemSheet)
    {
        if (item.ItemUICategory.RowId == 1)  // Pugilist weapons
        {
            log.Debug($"Weapon: {item.Name}");
        }
    }
}

public void GetTerritoryInfo(IDataManager dataManager, ushort territoryId)
{
    var territorySheet = dataManager.GetExcelSheet<TerritoryType>();
    var territory = territorySheet?.GetRow(territoryId);

    if (territory != null)
    {
        log.Info($"Zone: {territory.PlaceName.Value.Name}");
    }
}
```

**Common Excel Sheets:**
- `Item` - All game items
- `Action` - Combat actions/skills
- `TerritoryType` - Zones/areas
- `ClassJob` - Classes and jobs
- `ContentFinderCondition` - Duties
- `Quest` - Quest data
- `ENpcResident` - NPCs
- `BNpcName` - Battle NPC names
- `Status` - Status effects/buffs
- `Mount` - Mounts
- `Companion` - Minions

### ITextureProvider

Load and display game textures and icons.

**Methods:**
```csharp
// Load game icons
ISharedImmediateTexture GetFromGameIcon(GameIconLookup lookup);
bool TryGetFromGameIcon(GameIconLookup lookup, out ISharedImmediateTexture texture);
string? GetIconPath(GameIconLookup lookup);

// Load from game files
ISharedImmediateTexture GetFromGame(string path);

// Load from filesystem
ISharedImmediateTexture GetFromFile(string path);
ISharedImmediateTexture GetFromFileAbsolute(string path);

// Load from assembly resources
ISharedImmediateTexture GetFromManifestResource(Assembly assembly, string name);

// Create textures
Task<IDalamudTextureWrap> CreateFromImageAsync(byte[] data);
IDalamudTextureWrap CreateFromRaw(RawImageSpecification specs, ReadOnlySpan<byte> data);
```

**Example:**
```csharp
public class IconWindow : Window
{
    private readonly ITextureProvider textureProvider;

    public IconWindow(ITextureProvider textureProvider) : base("Icons")
    {
        this.textureProvider = textureProvider;
    }

    public override void Draw()
    {
        // Load and display a game icon (e.g., item icon)
        var iconLookup = new GameIconLookup(65001);  // Example icon ID
        var texture = textureProvider.GetFromGameIcon(iconLookup);

        if (texture.TryGetWrap(out var wrap, out _))
        {
            ImGui.Image(wrap.ImGuiHandle, new Vector2(64, 64));
        }

        // Load from game texture path
        var gameTexture = textureProvider.GetFromGame("ui/icon/000000/000001.tex");
        if (gameTexture.TryGetWrap(out var wrap2, out _))
        {
            ImGui.Image(wrap2.ImGuiHandle, new Vector2(32, 32));
        }
    }
}
```

---

## Event System and Hooks

### Framework Update Events

**Polling Pattern:**
```csharp
public class HealthWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private uint lastHp;

    public HealthWatcher(IFramework framework, IClientState clientState, IPluginLog log)
    {
        this.framework = framework;
        this.clientState = clientState;
        this.log = log;

        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework fw)
    {
        var player = clientState.LocalPlayer;
        if (player == null) return;

        if (player.CurrentHp != lastHp)
        {
            log.Info($"HP changed: {lastHp} -> {player.CurrentHp}");
            lastHp = player.CurrentHp;
        }
    }

    public void Dispose()
    {
        framework.Update -= OnUpdate;
    }
}
```

### IGameInteropProvider (Hooking)

Create hooks to intercept game function calls.

**Methods:**
```csharp
// Initialize hooks from attributes
void InitializeFromAttributes(object instance);

// Create hooks from memory address
Hook<T> HookFromAddress<T>(nint address, T detour) where T : Delegate;

// Create hooks from signature pattern
Hook<T> HookFromSignature<T>(string signature, T detour) where T : Delegate;

// Create hooks from symbol/export
Hook<T> HookFromSymbol<T>(string moduleName, string exportName, T detour);
```

**Hook Example:**
```csharp
using Dalamud.Hooking;

public class MyHooks : IDisposable
{
    // Define delegate matching the function signature
    private delegate void ProcessChatDelegate(IntPtr uiModule, IntPtr message, IntPtr a3, uint a4);

    private readonly Hook<ProcessChatDelegate>? chatHook;
    private readonly IPluginLog log;

    public MyHooks(IGameInteropProvider gameInterop, ISigScanner sigScanner, IPluginLog log)
    {
        this.log = log;

        // Find function address via signature
        var chatAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 4C 24 ?? 48 85 C9");

        // Create the hook (not enabled yet)
        chatHook = gameInterop.HookFromAddress<ProcessChatDelegate>(
            chatAddress,
            ProcessChatDetour);

        // Enable the hook
        chatHook.Enable();
    }

    private void ProcessChatDetour(IntPtr uiModule, IntPtr message, IntPtr a3, uint a4)
    {
        log.Debug("Chat message intercepted");

        // Call the original function
        chatHook!.Original(uiModule, message, a3, a4);
    }

    public void Dispose()
    {
        chatHook?.Dispose();
    }
}
```

**Attribute-Based Hooks:**
```csharp
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

public class MyPlugin : IDalamudPlugin
{
    [Signature("E8 ?? ?? ?? ?? 48 8B 4C 24 ?? 48 85 C9", DetourName = nameof(ProcessChatDetour))]
    private Hook<ProcessChatDelegate>? chatHook;

    private delegate void ProcessChatDelegate(IntPtr a1, IntPtr a2, IntPtr a3, uint a4);

    public MyPlugin(IGameInteropProvider gameInterop)
    {
        gameInterop.InitializeFromAttributes(this);
        chatHook?.Enable();
    }

    private void ProcessChatDetour(IntPtr a1, IntPtr a2, IntPtr a3, uint a4)
    {
        chatHook!.Original(a1, a2, a3, a4);
    }

    public void Dispose()
    {
        chatHook?.Dispose();
    }
}
```

**Hook Safety Guidelines:**
1. Always dispose hooks when done
2. Wrap detour code in try-catch blocks
3. Always call the original function unless intentionally blocking
4. Keep hook execution fast to avoid game lag
5. Be aware multiple plugins may hook the same function

### IAddonLifecycle

Monitor native UI addon (ATK) lifecycle events.

```csharp
public class AddonWatcher : IDisposable
{
    private readonly IAddonLifecycle addonLifecycle;

    public AddonWatcher(IAddonLifecycle addonLifecycle)
    {
        this.addonLifecycle = addonLifecycle;

        addonLifecycle.RegisterListener(AddonEvent.PostSetup, "InventoryLarge", OnInventorySetup);
        addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InventoryLarge", OnInventoryClose);
    }

    private void OnInventorySetup(AddonEvent type, AddonArgs args)
    {
        // Inventory window opened
    }

    private void OnInventoryClose(AddonEvent type, AddonArgs args)
    {
        // Inventory window closing
    }

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "InventoryLarge", OnInventorySetup);
        addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "InventoryLarge", OnInventoryClose);
    }
}
```

---

## Command Registration

### ICommandManager

Register and manage slash commands.

**Properties:**
```csharp
ReadOnlyDictionary<string, IReadOnlyCommandInfo> Commands { get; }
```

**Methods:**
```csharp
bool AddHandler(string command, CommandInfo info);
bool RemoveHandler(string command);
bool ProcessCommand(string content);
```

**Example:**
```csharp
using Dalamud.Game.Command;

public class MyPlugin : IDalamudPlugin
{
    private readonly ICommandManager commandManager;

    public MyPlugin(ICommandManager commandManager)
    {
        this.commandManager = commandManager;

        // Register main command
        commandManager.AddHandler("/myplugin", new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens My Plugin settings.",
            ShowInHelp = true
        });

        // Register alias
        commandManager.AddHandler("/mp", new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /myplugin",
            ShowInHelp = false
        });
    }

    private void OnCommand(string command, string args)
    {
        // Parse arguments
        var argList = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (argList.Length == 0)
        {
            // Open main window
            mainWindow.IsOpen = true;
            return;
        }

        switch (argList[0].ToLowerInvariant())
        {
            case "config":
            case "settings":
                configWindow.IsOpen = true;
                break;
            case "help":
                PrintHelp();
                break;
            default:
                log.Info($"Unknown subcommand: {argList[0]}");
                break;
        }
    }

    public void Dispose()
    {
        commandManager.RemoveHandler("/myplugin");
        commandManager.RemoveHandler("/mp");
    }
}
```

---

## Chat and Notifications

### IChatGui

Interact with the game's chat window.

**Methods:**
```csharp
// Send messages to chat
void Print(string message, string? tag = null, ushort? tagColor = null);
void Print(SeString message, string? tag = null, ushort? tagColor = null);
void PrintError(string message, string? tag = null, ushort? tagColor = null);

// Link handlers
DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> callback);
void RemoveChatLinkHandler(uint commandId);
void RemoveChatLinkHandler();  // Remove all handlers
```

**Events:**
```csharp
event OnMessageDelegate ChatMessage;
event OnCheckMessageHandledDelegate CheckMessageHandled;
event OnMessageHandledDelegate ChatMessageHandled;
```

**Example:**
```csharp
public class ChatHandler
{
    private readonly IChatGui chatGui;
    private readonly string pluginName = "MyPlugin";

    public ChatHandler(IChatGui chatGui)
    {
        this.chatGui = chatGui;

        // Register a clickable chat link
        chatGui.AddChatLinkHandler(0, OnChatLinkClicked);

        // Subscribe to chat messages
        chatGui.ChatMessage += OnChatMessage;
    }

    public void SendMessage(string text)
    {
        chatGui.Print(text, pluginName, 57);  // 57 = light blue color
    }

    public void SendError(string text)
    {
        chatGui.PrintError(text, pluginName);
    }

    private void OnChatMessage(
        XivChatType type,
        int timestamp,
        ref SeString sender,
        ref SeString message,
        ref bool isHandled)
    {
        // Process incoming chat message
        if (type == XivChatType.Say)
        {
            // Handle say chat
        }
    }

    private void OnChatLinkClicked(uint commandId, SeString message)
    {
        // Handle chat link click
    }
}
```

### INotificationManager

Display ImGui-based notifications.

**Methods:**
```csharp
IActiveNotification AddNotification(Notification notification);
```

**Example:**
```csharp
using Dalamud.Interface.ImGuiNotification;

public void ShowNotification(INotificationManager notificationManager)
{
    var notification = new Notification
    {
        Title = "My Plugin",
        Content = "Operation completed successfully!",
        Type = NotificationType.Success,
        Minimized = false
    };

    notificationManager.AddNotification(notification);
}

public void ShowWarning(INotificationManager notificationManager)
{
    var notification = new Notification
    {
        Title = "Warning",
        Content = "Something needs attention.",
        Type = NotificationType.Warning,
        InitialDuration = TimeSpan.FromSeconds(10)
    };

    var activeNotification = notificationManager.AddNotification(notification);

    // Later, you can dismiss it
    activeNotification.Dismiss();
}
```

### IToastGui

Native game toast notifications.

```csharp
public class ToastHandler
{
    private readonly IToastGui toastGui;

    public ToastHandler(IToastGui toastGui)
    {
        this.toastGui = toastGui;
    }

    public void ShowToast(string message)
    {
        toastGui.ShowNormal(message);
    }

    public void ShowQuestToast(string message)
    {
        toastGui.ShowQuest(message);
    }

    public void ShowErrorToast(string message)
    {
        toastGui.ShowError(message);
    }
}
```

---

## Configuration Persistence

### IPluginConfiguration

Interface for plugin configuration classes.

**Implementation:**
```csharp
using Dalamud.Configuration;
using System.Text.Json.Serialization;

[Serializable]
public class MyPluginConfig : IPluginConfiguration
{
    // Required version field for migration
    public int Version { get; set; } = 1;

    // Your settings
    public bool EnableFeatureA { get; set; } = true;
    public int RefreshInterval { get; set; } = 5;
    public string CustomMessage { get; set; } = "Hello!";
    public List<string> SavedItems { get; set; } = new();

    // Ignore transient data
    [JsonIgnore]
    public bool IsProcessing { get; set; }
}
```

**Usage:**
```csharp
public class MyPlugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private MyPluginConfig config;

    public MyPlugin(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;

        // Load configuration
        config = pluginInterface.GetPluginConfig() as MyPluginConfig ?? new MyPluginConfig();

        // Migrate if needed
        MigrateConfig();
    }

    private void MigrateConfig()
    {
        if (config.Version < 2)
        {
            // Migration logic for version 2
            config.Version = 2;
            SaveConfig();
        }
    }

    public void SaveConfig()
    {
        pluginInterface.SavePluginConfig(config);
    }

    public void ResetConfig()
    {
        config = new MyPluginConfig();
        SaveConfig();
    }
}
```

**Configuration Window:**
```csharp
public class ConfigWindow : Window
{
    private readonly MyPluginConfig config;
    private readonly Action saveConfig;

    public ConfigWindow(MyPluginConfig config, Action saveConfig)
        : base("My Plugin Settings###MyPluginConfig")
    {
        this.config = config;
        this.saveConfig = saveConfig;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(500, 400)
        };
    }

    public override void Draw()
    {
        var changed = false;

        // Boolean setting
        var enableA = config.EnableFeatureA;
        if (ImGui.Checkbox("Enable Feature A", ref enableA))
        {
            config.EnableFeatureA = enableA;
            changed = true;
        }

        // Integer setting with slider
        var interval = config.RefreshInterval;
        if (ImGui.SliderInt("Refresh Interval", ref interval, 1, 60))
        {
            config.RefreshInterval = interval;
            changed = true;
        }

        // String setting
        var message = config.CustomMessage;
        if (ImGui.InputText("Custom Message", ref message, 256))
        {
            config.CustomMessage = message;
            changed = true;
        }

        ImGui.Separator();

        if (ImGui.Button("Save"))
        {
            saveConfig();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset to Defaults"))
        {
            // Reset logic
        }

        // Auto-save on change (optional)
        if (changed)
        {
            saveConfig();
        }
    }
}
```

---

## Additional Services

### ISigScanner

Memory signature scanning.

```csharp
public class MyPlugin : IDalamudPlugin
{
    public MyPlugin(ISigScanner sigScanner)
    {
        // Scan for a function signature
        var address = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 4C 24 ?? 48 85 C9");

        // Get module base address
        var baseAddress = sigScanner.Module.BaseAddress;

        // Scan in data section
        var dataAddress = sigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0");
    }
}
```

### IGameConfig

Access game configuration.

```csharp
public void CheckGameConfig(IGameConfig gameConfig)
{
    // Read system config
    if (gameConfig.System.TryGet(SystemConfigOption.Fps, out uint fps))
    {
        log.Info($"FPS limit: {fps}");
    }

    // Read UI config
    if (gameConfig.UiConfig.TryGet(UiConfigOption.PartyListSortTypeSetting, out uint sortType))
    {
        log.Info($"Party sort type: {sortType}");
    }
}
```

### IKeyState

Keyboard state monitoring.

```csharp
public void CheckKeys(IKeyState keyState)
{
    // Check if a key is held
    if (keyState[VirtualKey.SHIFT])
    {
        log.Debug("Shift is held");
    }

    // Check modifier combinations
    if (keyState[VirtualKey.CONTROL] && keyState[VirtualKey.MENU])  // Ctrl+Alt
    {
        log.Debug("Ctrl+Alt pressed");
    }
}
```

### IDutyState

Current duty/instance state.

```csharp
public class DutyWatcher : IDisposable
{
    private readonly IDutyState dutyState;

    public DutyWatcher(IDutyState dutyState)
    {
        this.dutyState = dutyState;

        dutyState.DutyStarted += OnDutyStarted;
        dutyState.DutyWiped += OnDutyWiped;
        dutyState.DutyCompleted += OnDutyCompleted;
    }

    private void OnDutyStarted(object? sender, ushort territoryId)
    {
        log.Info($"Duty started in {territoryId}");
    }

    private void OnDutyWiped(object? sender, ushort territoryId)
    {
        log.Info("Party wiped!");
    }

    private void OnDutyCompleted(object? sender, ushort territoryId)
    {
        log.Info("Duty completed!");
    }

    public void Dispose()
    {
        dutyState.DutyStarted -= OnDutyStarted;
        dutyState.DutyWiped -= OnDutyWiped;
        dutyState.DutyCompleted -= OnDutyCompleted;
    }
}
```

### IDtrBar

Server info bar entries.

```csharp
public class DtrBarEntry : IDisposable
{
    private readonly IDtrBarEntry? entry;

    public DtrBarEntry(IDtrBar dtrBar)
    {
        entry = dtrBar.Get("MyPlugin");
        if (entry != null)
        {
            entry.Text = "My Info";
            entry.Tooltip = "Click for details";
            entry.OnClick += OnClick;
            entry.Shown = true;
        }
    }

    public void Update(string text)
    {
        if (entry != null)
        {
            entry.Text = text;
        }
    }

    private void OnClick()
    {
        // Handle click
    }

    public void Dispose()
    {
        entry?.Remove();
    }
}
```

### IPartyList

Access party/alliance members.

```csharp
public void ShowPartyInfo(IPartyList partyList)
{
    log.Info($"Party size: {partyList.Length}");

    foreach (var member in partyList)
    {
        log.Info($"  {member.Name} - {member.ClassJob.Value.Name}");
    }

    // Check if in alliance
    if (partyList.PartyId != 0)
    {
        log.Info($"Alliance party ID: {partyList.PartyId}");
    }
}
```

---

## Best Practices

### Performance

1. **Minimize work in Update/Draw loops:**
   ```csharp
   // BAD: Heavy work every frame
   private void OnUpdate(IFramework fw)
   {
       var allItems = dataManager.GetExcelSheet<Item>()!.ToList();  // Don't do this!
   }

   // GOOD: Cache data, check conditions
   private List<Item>? cachedItems;
   private DateTime lastUpdate;

   private void OnUpdate(IFramework fw)
   {
       if (DateTime.Now - lastUpdate < TimeSpan.FromSeconds(5))
           return;

       if (cachedItems == null)
           cachedItems = dataManager.GetExcelSheet<Item>()!.ToList();

       lastUpdate = DateTime.Now;
   }
   ```

2. **Use framework thread for game operations:**
   ```csharp
   await framework.Run(() => {
       // Game API calls here
   });
   ```

### Error Handling

```csharp
public override void Draw()
{
    try
    {
        DrawContent();
    }
    catch (Exception ex)
    {
        log.Error(ex, "Error drawing window");
        ImGui.TextColored(new Vector4(1, 0, 0, 1), "An error occurred.");
    }
}
```

### Resource Cleanup

```csharp
public class MyPlugin : IDalamudPlugin
{
    private readonly WindowSystem windowSystem;
    private readonly Hook<MyDelegate>? myHook;

    public void Dispose()
    {
        // Unsubscribe events
        framework.Update -= OnUpdate;
        clientState.TerritoryChanged -= OnTerritoryChanged;

        // Dispose hooks
        myHook?.Dispose();

        // Clean up windows
        windowSystem.RemoveAllWindows();

        // Remove commands
        commandManager.RemoveHandler("/myplugin");
    }
}
```

### Thread Safety

```csharp
// Use locks for shared state
private readonly object lockObj = new();
private List<string> sharedData = new();

private void UpdateData(string item)
{
    lock (lockObj)
    {
        sharedData.Add(item);
    }
}

// Or use concurrent collections
private readonly ConcurrentDictionary<string, int> concurrentData = new();
```

---

## References

- [Dalamud Developer Documentation](https://dalamud.dev/)
- [Dalamud API Reference](https://dalamud.dev/api/)
- [SamplePlugin Repository](https://github.com/goatcorp/SamplePlugin)
- [Dalamud Source Code](https://github.com/goatcorp/Dalamud)
- [Lumina Documentation](https://lumina.xiv.dev/)
- [ImGui .NET Reference](https://github.com/ImGuiNET/ImGui.NET)
