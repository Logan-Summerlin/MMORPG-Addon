# Dalamud Plugin Development Fundamentals Guide

This comprehensive guide covers the fundamentals of developing plugins for FFXIV using the Dalamud framework. It is designed for developers who want to create real, functional plugins for use with XIVLauncher.

---

## Table of Contents

1. [Overview and Architecture](#1-overview-and-architecture)
2. [Prerequisites and Development Environment](#2-prerequisites-and-development-environment)
3. [Project Creation and Setup](#3-project-creation-and-setup)
4. [Plugin Manifest Structure](#4-plugin-manifest-structure)
5. [Plugin Lifecycle](#5-plugin-lifecycle)
6. [Service Injection and Dependency Access](#6-service-injection-and-dependency-access)
7. [Building and Debugging](#7-building-and-debugging)
8. [Distribution Methods](#8-distribution-methods)
9. [Technical Considerations and Best Practices](#9-technical-considerations-and-best-practices)
10. [Common Pitfalls](#10-common-pitfalls)
11. [Official Resources](#11-official-resources)

---

## 1. Overview and Architecture

### What is Dalamud?

Dalamud is a plugin development framework for Final Fantasy XIV that provides:
- Access to game data through the Lumina library
- Native game interoperability via function hooks
- UI rendering through Dear ImGui integration
- A comprehensive service-based architecture for plugin development

### How It Works

1. **XIVLauncher** is a custom game launcher that manages Dalamud installation and injection
2. **Dalamud** is injected into the FFXIV process after the game starts
3. **Plugins** are loaded by Dalamud and given access to game state and UI systems

### Current Version

- **API Level**: 14 (API 14)
- **Target Framework**: .NET 10.0
- **Game Compatibility**: FFXIV Patch 7.4+

Starting with Dalamud v9, the API Level always matches the major version number (API 14 = Dalamud v14.x.x).

---

## 2. Prerequisites and Development Environment

### Required Software

| Component | Requirement | Notes |
|-----------|-------------|-------|
| Operating System | Windows 10/11 or Windows Server 2016+ | Required for native components |
| IDE | Visual Studio 2022+ or JetBrains Rider | Community Edition is sufficient |
| .NET SDK | .NET 10.0 SDK | Bundled with VS 2022+ or install separately |
| Game | FFXIV with XIVLauncher installed | Must have run the game at least once with Dalamud |
| XIVLauncher | Latest version | Default installation directories recommended |

### Environment Setup

1. **Install XIVLauncher** from https://goatcorp.github.io/
2. **Launch FFXIV** through XIVLauncher at least once to initialize Dalamud
3. **Install .NET 10.0 SDK** if not bundled with your IDE
4. **Verify Dalamud files** exist at `%APPDATA%\XIVLauncher\addon\Hooks\`

### Optional: Custom Dalamud Path

If using non-default installation directories, set the `DALAMUD_HOME` environment variable to point to your Dalamud installation.

---

## 3. Project Creation and Setup

### Option 1: Use the Official Template (Recommended)

The easiest way to start is using the official SamplePlugin template:

```bash
# Clone the template repository
git clone https://github.com/goatcorp/SamplePlugin.git MyPlugin
cd MyPlugin
```

Then rename all instances of "SamplePlugin" to your plugin name:
- `SamplePlugin.sln` -> `MyPlugin.sln`
- `SamplePlugin/` directory -> `MyPlugin/`
- `SamplePlugin.csproj` -> `MyPlugin.csproj`
- `SamplePlugin.json` -> `MyPlugin.json`
- All class names and namespaces in code files

### Option 2: Manual Project Creation

Create a new C# Class Library project and configure the `.csproj` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
  <PropertyGroup>
    <Version>1.0.0.0</Version>
    <PackageProjectUrl>https://github.com/yourusername/YourPlugin</PackageProjectUrl>
    <PackageLicenseExpression>AGPL-3.0-or-later</PackageLicenseExpression>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

### Project Structure

A standard Dalamud plugin follows this structure:

```
MyPlugin/
├── MyPlugin/
│   ├── MyPlugin.csproj         # Project configuration
│   ├── MyPlugin.json           # Plugin manifest
│   ├── packages.lock.json      # NuGet lock file
│   ├── Plugin.cs               # Main plugin entry point
│   ├── Configuration.cs        # User settings
│   └── Windows/
│       ├── ConfigWindow.cs     # Settings window
│       └── MainWindow.cs       # Primary UI window
└── MyPlugin.sln                # Solution file
```

### The Dalamud.NET.Sdk

The `Dalamud.NET.Sdk` package automatically provides:
- All Dalamud assembly references
- Correct target framework configuration
- Build output configuration for plugin loading
- DalamudPackager integration for distribution

---

## 4. Plugin Manifest Structure

Every plugin requires a manifest file (JSON or YAML) named after the plugin's internal name.

### Manifest File: `MyPlugin.json`

```json
{
  "Author": "Your Name",
  "Name": "My Plugin Display Name",
  "Punchline": "A short one-liner shown in /xlplugins.",
  "Description": "A longer description of your plugin. Include major features and slash commands.",
  "ApplicableVersion": "any",
  "RepoUrl": "https://github.com/yourusername/MyPlugin",
  "Tags": [
    "utility",
    "qol"
  ]
}
```

### Required Fields

| Field | Description |
|-------|-------------|
| `Name` | Display name shown in plugin listings |
| `Author` | Your name or handle |
| `Punchline` | Short one-line description |
| `Description` | Detailed description with feature list |

### Optional Fields

| Field | Description |
|-------|-------------|
| `ApplicableVersion` | Target game version (`"any"` for all versions) |
| `RepoUrl` | Link to source code repository |
| `Tags` | Array of category keywords for discoverability |
| `CategoryTags` | Additional categorization tags |
| `IconUrl` | URL to plugin icon |
| `ImageUrls` | Array of screenshot URLs |
| `LoadRequiredState` | When plugin should load |
| `LoadSync` | Whether to load synchronously |
| `LoadPriority` | Loading order priority |
| `CanUnloadAsync` | Whether plugin supports async unload |
| `Changelog` | Version history notes |

### Automatic Fields (Do Not Set Manually)

When using DalamudPackager, these fields are auto-generated:
- `AssemblyVersion`
- `InternalName`
- `DalamudApiLevel`

### YAML Alternative

You can use YAML format instead of JSON. Convert CamelCase keys to snake_case:

```yaml
name: My Plugin Display Name
author: Your Name
punchline: A short one-liner shown in /xlplugins.
description: A longer description of your plugin.
applicable_version: any
repo_url: https://github.com/yourusername/MyPlugin
tags:
  - utility
  - qol
```

---

## 5. Plugin Lifecycle

### The IDalamudPlugin Interface

All Dalamud plugins must implement `IDalamudPlugin`, which inherits from `IDisposable`:

```csharp
public interface IDalamudPlugin : IDisposable
{
    // No additional members - just constructor and Dispose
}
```

### Plugin Entry Point

Your main plugin class serves as the entry point. The constructor handles initialization:

```csharp
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace MyPlugin;

public sealed class Plugin : IDalamudPlugin
{
    // Service injection via static properties (alternative to constructor injection)
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // Window management
    public readonly WindowSystem WindowSystem = new("MyPlugin");

    private Configuration Configuration { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        // Load saved configuration or create defaults
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize windows
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        // Register command handler
        CommandManager.AddHandler("/myplugin", new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the main plugin window"
        });

        // Subscribe to UI events
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Log.Information("Plugin loaded successfully!");
    }

    public void Dispose()
    {
        // Unsubscribe from events
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        // Remove windows
        WindowSystem.RemoveAllWindows();

        // Dispose windows if they implement IDisposable
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        // Unregister commands
        CommandManager.RemoveHandler("/myplugin");
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
```

### Lifecycle Stages

| Stage | Method/Event | Description |
|-------|--------------|-------------|
| **Load** | Constructor | Plugin is instantiated; initialize services, commands, and UI |
| **Enable** | Constructor completes | Plugin becomes active and functional |
| **Draw** | `UiBuilder.Draw` event | Called every frame when UI needs rendering |
| **Disable** | `Dispose()` called | Plugin is being unloaded |
| **Unload** | After `Dispose()` | Plugin assembly unloaded from memory |

### Configuration Class

```csharp
using Dalamud.Configuration;

namespace MyPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    // Required: version for migration support
    public int Version { get; set; } = 0;

    // Your settings
    public bool SomeFeatureEnabled { get; set; } = true;
    public int SomeValue { get; set; } = 100;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
```

**Configuration Storage Location**: `%APPDATA%\XIVLauncher\pluginConfigs\MyPlugin.json`

---

## 6. Service Injection and Dependency Access

Dalamud uses an IoC (Inversion of Control) pattern to provide services to plugins.

### Injection Methods

**Method 1: Static Properties with Attribute** (Common pattern)

```csharp
[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
```

**Method 2: Constructor Injection**

```csharp
public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
{
    this.pluginInterface = pluginInterface;
    this.commandManager = commandManager;
}
```

### Available Services

#### Core Services

| Service | Purpose |
|---------|---------|
| `IDalamudPluginInterface` | Core plugin API, config storage, assembly info |
| `IFramework` | Access to game framework and main loop |
| `IPluginLog` | Structured logging to Dalamud log window |
| `ICommandManager` | Register and handle slash commands |

#### Client State Services

| Service | Purpose |
|---------|---------|
| `IClientState` | Game client state (logged in, territory, etc.) |
| `IPlayerState` | Local player information (job, level, stats) |
| `ICondition` | Player conditions (combat, mounted, crafting) |
| `IObjectTable` | Currently spawned game objects |
| `IPartyList` | Party and alliance member data |
| `ITargetManager` | Current target information |
| `IBuddyList` | Companion/chocobo data |
| `IFateTable` | Active FATE events |
| `IAetheryteList` | Available teleport destinations |
| `IJobGauges` | Job gauge data for all jobs |

#### UI Services

| Service | Purpose |
|---------|---------|
| `IChatGui` | Interact with native chat |
| `IGameGui` | Various in-game UI aspects |
| `IToastGui` | Native toast notifications |
| `IFlyTextGui` | Floating combat text |
| `INotificationManager` | Dalamud notifications |
| `IDtrBar` | Server info bar integration |
| `IContextMenu` | Context menu interactions |
| `INamePlateGui` | Nameplate modifications |

#### Data Services

| Service | Purpose |
|---------|---------|
| `IDataManager` | Access to Lumina game data sheets |
| `ITextureProvider` | Load and manage textures |
| `ISeStringEvaluator` | Localized text retrieval |

#### Advanced Services

| Service | Purpose |
|---------|---------|
| `IGameNetwork` | Game network events |
| `IGameInteropProvider` | Create function hooks |
| `ISigScanner` | Memory signature scanning |
| `IGameConfig` | Game configuration settings |
| `IDutyState` | Current duty/instance state |

---

## 7. Building and Debugging

### Building the Plugin

1. Open the solution in Visual Studio 2022 or JetBrains Rider
2. Select **Debug** or **Release** configuration
3. Build the solution (Ctrl+Shift+B or Build > Build Solution)
4. Output: `MyPlugin/bin/x64/Debug/MyPlugin.dll`

### Loading During Development

1. Open FFXIV and log in
2. Open Dalamud Settings: `/xlsettings`
3. Navigate to **Experimental** tab
4. In **Dev Plugin Locations**, add the full path to your DLL or its containing folder
5. Open Plugin Installer: `/xlplugins`
6. Go to **Dev Tools > Installed Dev Plugins**
7. Enable your plugin

### Hot Reloading

As of Dalamud API 4+, hot-reloading is supported:
- Add your plugin folder to Dev Plugin Locations
- Rebuild your plugin while FFXIV is running
- Dalamud automatically reloads the updated DLL

### Debugging with Visual Studio

1. **Disable Anti-Debug Protection**:
   - In-game: `/xldev`
   - Navigate to: Dalamud > Enable AntiDebug (toggle off)
   - This setting persists between launches

2. **Attach to Process**:
   - In Visual Studio: Debug > Attach to Process
   - Find and select `ffxiv_dx11.exe`
   - Click Attach

3. **Set Breakpoints** in your code as normal

### Useful Debug Commands

| Command | Purpose |
|---------|---------|
| `/xldev` | Open Dalamud developer menu |
| `/xllog` | Open Dalamud log window |
| `/xlplugins` | Open plugin installer |
| `/xlsettings` | Open Dalamud settings |

### Plugin Statistics

Monitor your plugin's performance impact:
- `/xldev` > Plugins > Open Plugin Stats

---

## 8. Distribution Methods

### Method 1: Official Repository (Recommended)

For public distribution, submit to the official DalamudPluginsD17 repository.

#### Preparation Checklist

1. Ensure your `.csproj` uses the latest `Dalamud.NET.Sdk`
2. Build in Release configuration
3. Commit your `packages.lock.json`
4. Create an icon (`icon.png`, 64x64 to 512x512 pixels)
5. Ensure version numbers are consistent (no timestamp-based versions)

#### Submission Process

1. Fork https://github.com/goatcorp/DalamudPluginsD17
2. Create a `manifest.toml` in the appropriate channel:
   - `testing/live/` for new plugins
   - `stable/` for established plugins

```toml
[plugin]
repository = "https://github.com/yourusername/MyPlugin.git"
commit = "abc123def456..."
owners = ["yourusername"]
project_path = "MyPlugin"
changelog = "Initial release with core features"
```

3. Add plugin images to an `images/` subfolder
4. Submit a pull request
5. Respond to reviewer feedback

#### Review Criteria

- Compliance with published guidelines
- Combat mechanics are informational only (no automation)
- Code quality and security review
- Configuration functionality works correctly
- Proper manifest formatting

### Method 2: Local/Dev Distribution

For personal use or private testing:

1. Build your plugin in Release mode
2. Share the output folder containing:
   - `MyPlugin.dll`
   - `MyPlugin.json` (manifest)
   - Any asset files
3. Recipients add the folder path to their Dev Plugin Locations

---

## 9. Technical Considerations and Best Practices

### UI Development

- **Use the Dalamud Windowing API** for standard windows to get native features:
  - Automatic close-order management
  - Window pinning support
  - Opacity controls
  - Integration with `/xlplugins` UI

```csharp
using Dalamud.Interface.Windowing;

public sealed class MainWindow : Window, IDisposable
{
    public MainWindow(Plugin plugin)
        : base("My Window##UniqueId", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        // ImGui drawing code here
        ImGui.Text("Hello, World!");
    }

    public void Dispose() { }
}
```

### Data Access

- **Prefer Lumina over XIVAPI** for game data access
- Lumina uses local game files for superior accuracy and performance
- Access via `IDataManager` service

```csharp
var territorySheet = dataManager.GetExcelSheet<TerritoryType>();
var territory = territorySheet?.GetRow(territoryId);
```

### Performance Guidelines

- Avoid heavy computation in the `Draw` event (runs every frame)
- Use timers or events for polling operations
- Monitor impact via Plugin Statistics window
- Rate-limit notifications and logging

### Backend Communication (If Required)

**Required Compliance:**
- Send only essential data
- Hash sensitive player information (Content ID, names) client-side
- Use HTTPS/TLS with trusted certificates
- Connect via DNS hostnames, never raw IPs
- Require explicit opt-in for telemetry
- Use pseudo-random, non-resettable identifiers

**Recommended Practices:**
- Allow custom backend server configuration
- Open-source backend code for transparency
- Implement connection retry logic
- Include version checking and notification systems

---

## 10. Common Pitfalls

### 1. Forgetting to Dispose Resources

**Problem**: Memory leaks and crashes when unloading plugins.

**Solution**: Always implement `Dispose()` thoroughly:
```csharp
public void Dispose()
{
    // Unsubscribe from ALL events
    PluginInterface.UiBuilder.Draw -= DrawUI;

    // Remove ALL command handlers
    CommandManager.RemoveHandler("/mycommand");

    // Dispose ALL windows and resources
    WindowSystem.RemoveAllWindows();
}
```

### 2. Modifying Internal Name After Release

**Problem**: The InternalName (from AssemblyName) becomes permanent identifiers for configs, logs, and repository submissions.

**Solution**: Choose your final name before any public release.

### 3. Heavy Work in Draw Loop

**Problem**: Performing expensive operations every frame causes lag.

**Solution**: Cache data, use timers, and perform work asynchronously:
```csharp
private DateTime lastUpdate = DateTime.MinValue;

public override void Draw()
{
    if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(1))
    {
        RefreshData();
        lastUpdate = DateTime.Now;
    }
    DrawCachedData();
}
```

### 4. Not Handling Null Game State

**Problem**: Crashes when accessing game state while logged out.

**Solution**: Always check state validity:
```csharp
if (clientState.LocalPlayer == null)
    return;
```

### 5. Incorrect Window IDs

**Problem**: Multiple windows with same ID conflict.

**Solution**: Use unique IDs with `##`:
```csharp
// "My Window" is displayed, "##UniqueId" is the actual ID
: base("My Window##UniqueId", ...)
```

### 6. Not Testing Across Contexts

**Problem**: Plugin works in cities but crashes in duties.

**Solution**: Test in all contexts:
- Cities and field zones
- Duties and raids
- During cutscenes
- After relog/zone changes
- PvP areas (if applicable)

---

## 11. Official Resources

### Documentation

- **Main Documentation**: https://dalamud.dev/
- **API Reference**: https://dalamud.dev/api/
- **Plugin Development Guide**: https://dalamud.dev/category/plugin-development/

### Templates and Examples

- **Official Template**: https://github.com/goatcorp/SamplePlugin
- **Dalamud Source**: https://github.com/goatcorp/Dalamud

### Distribution

- **Plugin Repository**: https://github.com/goatcorp/DalamudPluginsD17
- **XIVLauncher**: https://goatcorp.github.io/

### Community

- **Discord**: https://discord.gg/3NMcUV5 (Use #plugin-dev channel)
- **Developer FAQ**: https://goatcorp.github.io/faq/development

### Release Channels

| Channel | Description |
|---------|-------------|
| **Release** | Default stable channel for most users |
| **Canary** | Pre-release testing with early adopters |
| **Staging** | Latest development builds from master branch |

Switch channels via `/xldev` > Dalamud menu.

---

## Quick Reference: Minimal Plugin Template

```csharp
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace MinimalPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Plugin()
    {
        Log.Information("Plugin loaded!");
    }

    public void Dispose()
    {
        Log.Information("Plugin unloaded!");
    }
}
```

With manifest `MinimalPlugin.json`:
```json
{
  "Author": "Your Name",
  "Name": "Minimal Plugin",
  "Punchline": "A minimal example plugin.",
  "Description": "Demonstrates the minimum required for a Dalamud plugin."
}
```

---

*This guide was compiled from official Dalamud documentation at https://dalamud.dev/ and the official SamplePlugin repository at https://github.com/goatcorp/SamplePlugin.*
