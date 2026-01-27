# Dalamud SamplePlugin Code Patterns Guide

This guide documents the code patterns, conventions, and templates found in the official Dalamud SamplePlugin. Use this as a reference when developing new FFXIV plugins.

---

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Project File Configuration](#2-project-file-configuration)
3. [Plugin Manifest](#3-plugin-manifest)
4. [Plugin Class Patterns](#4-plugin-class-patterns)
5. [Dependency Injection and Service Access](#5-dependency-injection-and-service-access)
6. [Configuration Patterns](#6-configuration-patterns)
7. [Window Class Patterns](#7-window-class-patterns)
8. [Command Registration Patterns](#8-command-registration-patterns)
9. [Event Handling Patterns](#9-event-handling-patterns)
10. [Dispose and Cleanup Patterns](#10-dispose-and-cleanup-patterns)
11. [ImGui Integration Patterns](#11-imgui-integration-patterns)
12. [Game Data Access Patterns](#12-game-data-access-patterns)
13. [Reusable Templates](#13-reusable-templates)
14. [Best Practices and Anti-Patterns](#14-best-practices-and-anti-patterns)

---

## 1. Project Structure

The SamplePlugin follows a clean, modular structure:

```
SamplePlugin/
├── SamplePlugin.csproj      # Project configuration
├── SamplePlugin.json        # Plugin manifest (metadata)
├── Plugin.cs                # Main plugin entry point
├── Configuration.cs         # Settings/configuration class
└── Windows/
    ├── ConfigWindow.cs      # Settings window
    └── MainWindow.cs        # Primary UI window
```

**Why This Structure?**
- Separation of concerns: UI, configuration, and core logic are isolated
- The `Windows/` folder keeps UI components organized as plugins grow
- Manifest and project files at root level follow Dalamud conventions

---

## 2. Project File Configuration

**File: `SamplePlugin.csproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
  <PropertyGroup>
    <Version>0.0.0.1</Version>
    <PackageProjectUrl>https://github.com/goatcorp/SamplePlugin</PackageProjectUrl>
    <PackageLicenseExpression>AGPL-3.0-or-later</PackageLicenseExpression>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="..\Data\goat.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Visible>false</Visible>
    </Content>
  </ItemGroup>
</Project>
```

### Key Elements Explained

| Element | Purpose |
|---------|---------|
| `Sdk="Dalamud.NET.Sdk/14.0.1"` | Uses the Dalamud SDK which provides all necessary dependencies and build targets |
| `<Version>` | Semantic version displayed in plugin listings |
| `<IsPackable>false</IsPackable>` | Prevents NuGet packaging (plugins are distributed differently) |
| `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` | Ensures assets (images, etc.) are copied to build output |

**Why Use Dalamud.NET.Sdk?**
- Automatically references all Dalamud assemblies
- Handles plugin deployment during debug builds
- Configures correct output formats for Dalamud loading

---

## 3. Plugin Manifest

**File: `SamplePlugin.json`**

```json
{
  "Author": "your name here",
  "Name": "Sample Plugin",
  "Punchline": "A short one-liner that shows up in /xlplugins.",
  "Description": "A description that shows up in /xlplugins. List any major slash-command(s).",
  "ApplicableVersion": "any",
  "Tags": [
    "sample",
    "plugin",
    "goats"
  ]
}
```

### Manifest Fields

| Field | Required | Purpose |
|-------|----------|---------|
| `Author` | Yes | Your name/handle for attribution |
| `Name` | Yes | Display name in plugin listings |
| `Punchline` | Yes | One-line summary (keep under 80 chars) |
| `Description` | Yes | Full description; document slash commands here |
| `ApplicableVersion` | Yes | Game version compatibility; use `"any"` for general plugins |
| `Tags` | No | Searchable keywords for plugin discovery |

**Best Practice:** Include your primary slash command in the Description so users can find it.

---

## 4. Plugin Class Patterns

**File: `Plugin.cs`**

The main plugin class implements `IDalamudPlugin` - the required interface for all Dalamud plugins.

```csharp
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    // Service injection (see Section 5)
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    // ... additional services ...

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        // Initialization logic here
    }

    public void Dispose()
    {
        // Cleanup logic here
    }
}
```

### Key Patterns

1. **Sealed Class**: Use `sealed` to prevent inheritance and enable compiler optimizations
2. **IDalamudPlugin Interface**: Required for Dalamud to recognize and load your plugin
3. **Constructor as Entry Point**: All initialization happens in the constructor
4. **Dispose Pattern**: Cleanup is handled via `IDisposable` (implicit in IDalamudPlugin)

**Why `sealed`?**
- Plugin classes are typically not designed for inheritance
- Allows the JIT compiler to devirtualize method calls
- Signals intent that this is a final implementation

---

## 5. Dependency Injection and Service Access

Dalamud uses attribute-based dependency injection to provide services.

### Static Service Properties Pattern

```csharp
[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
[PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
[PluginService] internal static IClientState ClientState { get; private set; } = null!;
[PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
[PluginService] internal static IPluginLog Log { get; private set; } = null!;
```

### Understanding the Pattern

| Element | Purpose |
|---------|---------|
| `[PluginService]` | Attribute that tells Dalamud to inject the service |
| `internal static` | Static allows access from anywhere; internal limits scope to assembly |
| `{ get; private set; }` | Readable everywhere, but only Dalamud can set the value |
| `= null!` | Null-forgiving operator; tells compiler Dalamud will initialize this |

### Common Dalamud Services

| Service | Purpose |
|---------|---------|
| `IDalamudPluginInterface` | Core plugin interface for configs, paths, UI hooks |
| `ICommandManager` | Register and handle slash commands |
| `IClientState` | Current game state (logged in, territory, etc.) |
| `IPlayerState` | Current player information (job, level, etc.) |
| `IDataManager` | Access game data sheets (Lumina) |
| `ITextureProvider` | Load and manage textures/images |
| `IPluginLog` | Logging (visible via `/xllog`) |
| `IChatGui` | Send messages to chat |
| `IFramework` | Hook into game's update loop |
| `ICondition` | Check player conditions (in combat, mounted, etc.) |
| `IGameGui` | Access game UI elements |

**Why Static Services?**
- Provides convenient access from any class without passing references
- Window classes can access services via `Plugin.ServiceName`
- Trade-off: Makes unit testing harder (consider instance injection for testable code)

### Alternative: Constructor Injection (More Testable)

```csharp
public sealed class Plugin : IDalamudPlugin
{
    private readonly ICommandManager commandManager;

    public Plugin(ICommandManager commandManager)
    {
        this.commandManager = commandManager;
    }
}
```

---

## 6. Configuration Patterns

**File: `Configuration.cs`**

```csharp
using Dalamud.Configuration;
using System;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
```

### Key Elements

| Element | Purpose |
|---------|---------|
| `[Serializable]` | Required for JSON serialization by Dalamud |
| `IPluginConfiguration` | Interface that includes the `Version` property |
| `Version` property | Used for configuration migration between plugin versions |
| Default values | Always provide sensible defaults for new installations |
| `Save()` method | Convenience wrapper for persisting configuration |

### Loading Configuration

In `Plugin.cs`:

```csharp
Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
```

**Why the null-coalescing pattern?**
- `GetPluginConfig()` returns `null` if no config file exists (first run)
- Creates a new Configuration with default values as fallback
- Handles corrupted config files gracefully

### Configuration Migration Pattern

When you need to change configuration structure between versions:

```csharp
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // Version 1: Added new setting
    public string NewSetting { get; set; } = "default";

    public void Migrate()
    {
        if (Version < 1)
        {
            // Migration logic for version 0 -> 1
            NewSetting = "migrated_default";
            Version = 1;
            Save();
        }
    }
}
```

---

## 7. Window Class Patterns

Windows in Dalamud extend the `Window` base class and implement `IDisposable`.

### Config Window Pattern

**File: `Windows/ConfigWindow.cs`**

```csharp
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // Window with constant ID using ###
    public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Modify flags before Draw() is called
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        // ImGui drawing code here
    }
}
```

### Window ID Patterns

ImGui identifies windows by their ID, which can be separated from the display title.

**Constant ID with `###`:**
```csharp
base("Visible Title###ConstantID")
```
- The ID is `###ConstantID`
- The title can change dynamically while keeping the same window ID
- Use case: FPS counters, dynamic titles like `"{FPS}fps###XYZ counter window"`

**Hidden ID with `##`:**
```csharp
base("My Amazing Window##With a hidden ID")
```
- The ID is `My Amazing Window##With a hidden ID`
- User sees only "My Amazing Window"
- Use case: Multiple windows with same visible title but different IDs

### Main Window Pattern

**File: `Windows/MainWindow.cs`**

```csharp
public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Window content here
    }
}
```

### Window Lifecycle Methods

| Method | When Called | Use Case |
|--------|-------------|----------|
| `PreDraw()` | Before each frame's Draw | Modify flags, check conditions |
| `Draw()` | Every frame when window is open | Render UI content |
| `PostDraw()` | After Draw completes | Cleanup, state resets |
| `OnOpen()` | When window becomes visible | Initialize state, load data |
| `OnClose()` | When window is closed | Save state, cleanup |

### Window Flags Reference

```csharp
// Common flag combinations
Flags = ImGuiWindowFlags.NoResize         // Prevent resizing
      | ImGuiWindowFlags.NoCollapse       // Hide collapse button
      | ImGuiWindowFlags.NoScrollbar      // Hide scrollbar
      | ImGuiWindowFlags.NoScrollWithMouse // Disable mouse wheel scroll
      | ImGuiWindowFlags.NoMove           // Prevent dragging
      | ImGuiWindowFlags.AlwaysAutoResize // Auto-fit to content
      | ImGuiWindowFlags.NoTitleBar;      // Hide title bar
```

---

## 8. Command Registration Patterns

### Basic Command Registration

```csharp
private const string CommandName = "/pmycommand";

public Plugin()
{
    CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
    {
        HelpMessage = "A useful message to display in /xlhelp"
    });
}

private void OnCommand(string command, string args)
{
    // Handle the command
    MainWindow.Toggle();
}
```

### Command Handler Signature

```csharp
private void OnCommand(string command, string args)
```

| Parameter | Contents |
|-----------|----------|
| `command` | The full command string (e.g., `/pmycommand`) |
| `args` | Everything after the command, trimmed |

### Advanced Command Pattern with Arguments

```csharp
private void OnCommand(string command, string args)
{
    var argList = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (argList.Length == 0)
    {
        MainWindow.Toggle();
        return;
    }

    switch (argList[0].ToLowerInvariant())
    {
        case "config":
            ConfigWindow.Toggle();
            break;
        case "help":
            ShowHelp();
            break;
        default:
            Log.Warning($"Unknown subcommand: {argList[0]}");
            break;
    }
}
```

### Multiple Commands Pattern

```csharp
public Plugin()
{
    CommandManager.AddHandler("/myplugin", new CommandInfo(OnMainCommand)
    {
        HelpMessage = "Open the main window"
    });

    CommandManager.AddHandler("/mypluginconfig", new CommandInfo(OnConfigCommand)
    {
        HelpMessage = "Open settings"
    });
}

public void Dispose()
{
    CommandManager.RemoveHandler("/myplugin");
    CommandManager.RemoveHandler("/mypluginconfig");
}
```

---

## 9. Event Handling Patterns

### UI Builder Events

```csharp
public Plugin()
{
    // Register for UI drawing
    PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

    // Hook into plugin installer buttons
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
    PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
}

public void Dispose()
{
    // Always unregister in Dispose!
    PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
    PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
    PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
}

public void ToggleConfigUi() => ConfigWindow.Toggle();
public void ToggleMainUi() => MainWindow.Toggle();
```

### Event Types Explained

| Event | When Fired | Purpose |
|-------|------------|---------|
| `UiBuilder.Draw` | Every frame | Main rendering callback |
| `UiBuilder.OpenConfigUi` | User clicks cog icon in /xlplugins | Open settings |
| `UiBuilder.OpenMainUi` | User clicks plugin name in /xlplugins | Open main UI |
| `UiBuilder.BuildFonts` | When fonts need rebuilding | Custom font loading |

### Framework Update Pattern

For logic that needs to run every frame:

```csharp
[PluginService] internal static IFramework Framework { get; private set; } = null!;

public Plugin()
{
    Framework.Update += OnFrameworkUpdate;
}

public void Dispose()
{
    Framework.Update -= OnFrameworkUpdate;
}

private void OnFrameworkUpdate(IFramework framework)
{
    // Called every frame - keep this fast!
    // Good: Check conditions, update cached values
    // Bad: Heavy computation, file I/O
}
```

### Client State Events

```csharp
public Plugin()
{
    ClientState.Login += OnLogin;
    ClientState.Logout += OnLogout;
    ClientState.TerritoryChanged += OnTerritoryChanged;
}

public void Dispose()
{
    ClientState.Login -= OnLogin;
    ClientState.Logout -= OnLogout;
    ClientState.TerritoryChanged -= OnTerritoryChanged;
}

private void OnLogin() { /* Player logged in */ }
private void OnLogout(int type, int code) { /* Player logged out */ }
private void OnTerritoryChanged(ushort territoryId) { /* Zone changed */ }
```

---

## 10. Dispose and Cleanup Patterns

Proper cleanup prevents memory leaks and crashes when plugins are unloaded.

### Complete Dispose Pattern

```csharp
public void Dispose()
{
    // 1. Unregister ALL event handlers
    PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
    PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
    PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

    // 2. Remove windows from window system
    WindowSystem.RemoveAllWindows();

    // 3. Dispose individual windows
    ConfigWindow.Dispose();
    MainWindow.Dispose();

    // 4. Remove command handlers
    CommandManager.RemoveHandler(CommandName);

    // 5. Dispose any other resources (textures, hooks, etc.)
}
```

### Order of Operations

The dispose order matters:

1. **Unregister events first** - Prevents callbacks during disposal
2. **Remove windows** - Stops them from being drawn
3. **Dispose windows** - Clean up window-specific resources
4. **Remove commands** - Prevents command execution during shutdown
5. **Final cleanup** - Any remaining resources

### Window Dispose Pattern

```csharp
public class MainWindow : Window, IDisposable
{
    private bool disposed = false;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        // Clean up window-specific resources
        // (textures are typically managed by TextureProvider, not here)
    }
}
```

---

## 11. ImGui Integration Patterns

### Basic Controls

```csharp
public override void Draw()
{
    // Text display
    ImGui.Text("Static text");
    ImGui.Text($"Dynamic text: {someValue}");

    // Checkbox (requires ref to local variable)
    var configValue = configuration.SomeBool;
    if (ImGui.Checkbox("Enable Feature", ref configValue))
    {
        configuration.SomeBool = configValue;
        configuration.Save();
    }

    // Button
    if (ImGui.Button("Click Me"))
    {
        DoSomething();
    }

    // Spacing
    ImGui.Spacing();
    ImGui.Separator();
}
```

### Checkbox Pattern (Property Limitation)

ImGui's `Checkbox` requires a `ref` parameter, but C# doesn't allow `ref` to properties. Use a local variable:

```csharp
// WRONG - won't compile
if (ImGui.Checkbox("Option", ref configuration.SomeBool))

// CORRECT - use local copy
var localValue = configuration.SomeBool;
if (ImGui.Checkbox("Option", ref localValue))
{
    configuration.SomeBool = localValue;
    configuration.Save();
}
```

### ImRaii Pattern (Automatic Cleanup)

`ImRaii` provides RAII-style wrappers for ImGui that handle cleanup automatically:

```csharp
using Dalamud.Interface.Utility.Raii;

public override void Draw()
{
    // Child region with automatic EndChild
    using (var child = ImRaii.Child("ScrollableRegion", Vector2.Zero, true))
    {
        if (child.Success)
        {
            // Content here
        }
    }

    // Indentation with automatic cleanup
    using (ImRaii.PushIndent(55f))
    {
        ImGui.Text("This is indented");
    }

    // Color push with automatic pop
    using (ImRaii.PushColor(ImGuiCol.Text, 0xFF0000FF))
    {
        ImGui.Text("This is red");
    }
}
```

**Why ImRaii?**
- Traditional ImGui: `BeginChild()` must be followed by `EndChild()`
- Forgetting `End*()` causes crashes or visual bugs
- `ImRaii` handles this via C#'s `using` statement (dispose pattern)

### Image Display Pattern

```csharp
var texture = Plugin.TextureProvider.GetFromFile(imagePath).GetWrapOrDefault();
if (texture != null)
{
    ImGui.Image(texture.Handle, texture.Size);
}
else
{
    ImGui.Text("Image not found.");
}
```

### Scaled/DPI-Aware Elements

```csharp
using Dalamud.Interface.Utility;

// Scaled spacing that respects UI scale settings
ImGuiHelpers.ScaledDummy(20.0f);

// Scaled button size
var buttonSize = ImGuiHelpers.ScaledVector2(100, 30);
if (ImGui.Button("Scaled Button", buttonSize))
{
    // ...
}
```

---

## 12. Game Data Access Patterns

### Player State Access

```csharp
var playerState = Plugin.PlayerState;

// Check if player is loaded
if (!playerState.IsLoaded)
{
    ImGui.Text("Not logged in.");
    return;
}

// Check if job data is valid
if (!playerState.ClassJob.IsValid)
{
    ImGui.Text("Invalid job data.");
    return;
}

// Access player information
var jobAbbr = playerState.ClassJob.Value.Abbreviation;
var level = playerState.Level;
ImGui.Text($"Job: {jobAbbr} (Lv. {level})");
```

### Lumina Data Access

Access game data sheets (Excel files) via DataManager:

```csharp
using Lumina.Excel.Sheets;

// Get current territory/zone name
var territoryId = Plugin.ClientState.TerritoryType;
if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var row))
{
    var zoneName = row.PlaceName.Value.Name;
    ImGui.Text($"Location: {zoneName}");
}
```

### Safe Data Access Pattern

Always validate before accessing:

```csharp
// Pattern: Check validity at each step
var sheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
if (sheet == null)
{
    Log.Error("Failed to load TerritoryType sheet");
    return;
}

if (!sheet.TryGetRow(territoryId, out var row))
{
    Log.Warning($"Unknown territory: {territoryId}");
    return;
}

// Now safe to use row
```

---

## 13. Reusable Templates

### Template: Minimal Plugin

```csharp
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace MyPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/mycommand";

    public Plugin()
    {
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Description of what this command does"
        });

        Log.Information("MyPlugin loaded!");
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        Log.Information($"Command executed with args: {args}");
    }
}
```

### Template: Configuration Class

```csharp
using Dalamud.Configuration;
using System;

namespace MyPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // Add your settings here with sensible defaults
    public bool FeatureEnabled { get; set; } = true;
    public int SomeNumber { get; set; } = 100;
    public string SomeText { get; set; } = "default";

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
```

### Template: Basic Window

```csharp
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace MyPlugin.Windows;

public class MyWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MyWindow(Plugin plugin)
        : base("Window Title##UniqueID", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Hello, World!");

        if (ImGui.Button("Close"))
        {
            IsOpen = false;
        }
    }
}
```

### Template: Plugin with Window System

```csharp
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MyPlugin.Windows;

namespace MyPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/mycommand";

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("MyPlugin");

    private MainWindow MainWindow { get; init; }
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the main window"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Log.Information("MyPlugin loaded!");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) => ToggleMainUi();
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
```

### Template: Project File

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
  <PropertyGroup>
    <Version>1.0.0.0</Version>
    <PackageProjectUrl>https://github.com/username/MyPlugin</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

### Template: Manifest File

```json
{
  "Author": "YourName",
  "Name": "My Plugin",
  "Punchline": "Brief description of what it does.",
  "Description": "Detailed description.\n\nCommands:\n/mycommand - Opens the main window",
  "ApplicableVersion": "any",
  "Tags": [
    "utility",
    "qol"
  ]
}
```

---

## 14. Best Practices and Anti-Patterns

### Best Practices

1. **Always Unregister Events**
   ```csharp
   // Register in constructor
   SomeEvent += Handler;

   // ALWAYS unregister in Dispose
   SomeEvent -= Handler;
   ```

2. **Validate Game State Before Access**
   ```csharp
   if (!playerState.IsLoaded) return;
   if (!someData.IsValid) return;
   ```

3. **Use Null-Conditional and Null-Coalescing**
   ```csharp
   var name = player?.Name ?? "Unknown";
   ```

4. **Keep Draw() Fast**
   - No file I/O
   - No heavy computation
   - Cache expensive calculations

5. **Save Configuration Immediately on Change**
   ```csharp
   if (ImGui.Checkbox("Option", ref value))
   {
       configuration.Option = value;
       configuration.Save();  // Save right away
   }
   ```

6. **Use ImRaii for Automatic Cleanup**
   ```csharp
   using (var child = ImRaii.Child("name")) { }
   // No need to call EndChild()
   ```

7. **Provide Sensible Defaults**
   ```csharp
   public bool IsEnabled { get; set; } = true;  // Not just bool
   ```

8. **Log Important Events**
   ```csharp
   Log.Information("Plugin initialized");
   Log.Error("Failed to load: {Message}", ex.Message);
   ```

### Anti-Patterns to Avoid

1. **Forgetting to Unregister Events**
   ```csharp
   // BAD - Memory leak, crashes on unload
   public void Dispose()
   {
       // Forgot: Framework.Update -= OnUpdate;
   }
   ```

2. **Heavy Work in Draw/Update**
   ```csharp
   // BAD - Runs every frame
   public override void Draw()
   {
       var data = File.ReadAllText("config.json");  // Don't do this!
   }
   ```

3. **Not Handling Null/Invalid State**
   ```csharp
   // BAD - Will crash if not logged in
   var name = PlayerState.LocalPlayer.Name;

   // GOOD
   if (PlayerState.IsLoaded)
   {
       var name = PlayerState.LocalPlayer.Name;
   }
   ```

4. **Hardcoding Window IDs**
   ```csharp
   // BAD - Will conflict with same-named windows
   base("Settings")

   // GOOD - Unique ID
   base("Settings###MyPluginSettings")
   ```

5. **Not Using the Window System**
   ```csharp
   // BAD - Manual management
   if (showWindow)
   {
       ImGui.Begin("Window");
       // ...
       ImGui.End();
   }

   // GOOD - Use WindowSystem
   WindowSystem.AddWindow(myWindow);
   myWindow.Toggle();
   ```

6. **Storing Sensitive Data in Config**
   ```csharp
   // BAD - Don't store these
   public string Password { get; set; }
   public string ApiKey { get; set; }
   ```

7. **Using Thread-Unsafe Operations**
   ```csharp
   // BAD - ImGui is not thread-safe
   Task.Run(() => {
       ImGui.Text("This will crash!");
   });
   ```

---

## Quick Reference Card

### Essential Imports
```csharp
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;
```

### Service Injection
```csharp
[PluginService] internal static IServiceType ServiceName { get; private set; } = null!;
```

### Window Toggle
```csharp
MyWindow.Toggle();        // Toggle visibility
MyWindow.IsOpen = true;   // Force open
MyWindow.IsOpen = false;  // Force close
```

### Command Registration
```csharp
CommandManager.AddHandler("/cmd", new CommandInfo(Handler) { HelpMessage = "Help" });
CommandManager.RemoveHandler("/cmd");
```

### Configuration
```csharp
var config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
PluginInterface.SavePluginConfig(config);
```

---

*Document generated from SamplePlugin analysis. Last updated: 2026-01-27*
