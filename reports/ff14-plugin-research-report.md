# FFXIV Dalamud Plugin Research Report

**Date:** February 1, 2026
**Purpose:** Research popular FFXIV Dalamud plugins and compare distribution configurations to DailiesChecklist
**API Level Focus:** Dalamud API 14

---

## 1. Executive Summary

This report examines popular FFXIV Dalamud plugins to identify best practices for plugin distribution and configuration. Our DailiesChecklist plugin is generally well-configured for custom repository distribution, but several improvements can enhance compatibility, discoverability, and user experience.

### Key Findings

1. **SDK Version**: DailiesChecklist uses Dalamud.NET.Sdk/14.0.1 - this is correct and current
2. **Target Framework**: Our `net10.0-windows` target is appropriate for API 14 (.NET 10 required)
3. **Manifest Structure**: Our manifest includes all required fields but is missing some optional fields used by popular plugins
4. **Distribution Method**: Custom repository via `pluginmaster.json` is valid; official repository submission uses a different format (manifest.toml)

### Priority Recommendations

| Priority | Recommendation | Impact |
|----------|----------------|--------|
| High | Add `LoadRequiredState` and `LoadSync` fields | Stability |
| High | Add preview images to ImageUrls | Discoverability |
| Medium | Consider adding `SupportsProfiles` field | Feature completeness |
| Medium | Add `CategoryTags` for better categorization | Discoverability |
| Low | Add `TestingDalamudApiLevel` for future testing track | Distribution flexibility |

---

## 2. Plugins Researched

### Official Repository & Templates

| Plugin/Resource | Repository | Purpose |
|-----------------|------------|---------|
| **SamplePlugin** (Template) | https://github.com/goatcorp/SamplePlugin | Official plugin template |
| **DalamudPluginsD17** | https://github.com/goatcorp/DalamudPluginsD17 | Official plugin submission repository |
| **Dalamud Framework** | https://github.com/goatcorp/Dalamud | Core plugin framework |

### Popular Utility Plugins

| Plugin | Repository | Description | Stars |
|--------|------------|-------------|-------|
| **SimpleTweaks** | https://github.com/Caraxi/SimpleTweaksPlugin | QoL tweaks collection | 184 |
| **HaselTweaks** | https://github.com/Haselnussbomber/HaselTweaks | QoL tweaks and helpers | - |
| **DelvUI** | https://github.com/DelvUI/DelvUI | Custom UI replacement | 199 |
| **ChatBubbles** | https://github.com/Haplo064/ChatBubbles | Chat bubble display | 32 |

### Modding Ecosystem Plugins

| Plugin | Repository | Description | Stars |
|--------|------------|-------------|-------|
| **Penumbra** | https://github.com/xivdev/Penumbra | Runtime mod loader | 414 releases |
| **Glamourer** | https://github.com/Ottermandias/Glamourer | Appearance customization | 175 |
| **Sea of Stars** (Collection) | https://github.com/Ottermandias/SeaOfStars | Multi-plugin repository | - |

---

## 3. Manifest Comparison

### 3.1 Required Fields Comparison

| Field | DailiesChecklist | SamplePlugin | Penumbra | Glamourer | SimpleTweaks |
|-------|------------------|--------------|----------|-----------|--------------|
| Name | Yes | Yes | Yes | Yes | Yes |
| Author | Yes | Yes | Yes | Yes | Yes |
| Punchline | Yes | Yes | Yes | Yes | Yes |
| Description | Yes | Yes | Yes | Yes | Yes |
| InternalName | Yes | Yes | Yes | Yes | Yes |
| AssemblyVersion | Yes | Yes | Yes | Yes | Yes |
| DalamudApiLevel | 14 | Auto | 14 | 14 | Auto |

### 3.2 Optional Fields Comparison

| Field | DailiesChecklist | Penumbra | Glamourer | HaselTweaks |
|-------|------------------|----------|-----------|-------------|
| RepoUrl | Yes | Yes | Yes | Yes |
| IconUrl | Yes | Yes | Yes | Yes |
| ImageUrls | Empty [] | Not set | Not set | Not set |
| Tags | Yes (6) | Not set | Yes (10) | Not set |
| CategoryTags | **Missing** | Not set | Not set | Not set |
| Changelog | Yes | Not set | Not set | Not set |
| AcceptsFeedback | Yes | Not set | Not set | Not set |
| FeedbackMessage | Yes | Not set | Not set | Not set |
| ApplicableVersion | "any" | "any" | "any" | Not set |

### 3.3 Advanced Configuration Fields

| Field | DailiesChecklist | Penumbra | Glamourer | HaselTweaks |
|-------|------------------|----------|-----------|-------------|
| LoadRequiredState | **Missing** | 2 | Not set | 1 |
| LoadSync | **Missing** | true | Not set | Not set |
| LoadPriority | **Missing** | 69420 | Not set | Not set |
| CanUnloadAsync | **Missing** | Not set | Not set | Not set |
| SupportsProfiles | **Missing** | Not set | Not set | Not set |
| TestingAssemblyVersion | **Missing** | Yes | Yes | Not set |
| TestingDalamudApiLevel | **Missing** | 14 | 14 | Not set |

### 3.4 Distribution-Specific Fields (pluginmaster.json)

| Field | DailiesChecklist | Penumbra | Glamourer |
|-------|------------------|----------|-----------|
| DownloadLinkInstall | Yes | Yes | Yes |
| DownloadLinkUpdate | Yes | Yes | Yes |
| DownloadLinkTesting | Empty | Yes | Yes |
| DownloadCount | 0 | 0 | 1 |
| LastUpdate | Yes | 0 | Yes |
| IsHide | false | False | False |
| IsTestingExclusive | false | False | False |

---

## 4. Project Configuration Comparison

### 4.1 .csproj SDK and Framework

| Plugin | SDK Version | Target Framework |
|--------|-------------|------------------|
| **DailiesChecklist** | Dalamud.NET.Sdk/14.0.1 | net10.0-windows |
| **SamplePlugin** | Dalamud.NET.Sdk/14.0.1 | (SDK default) |
| **SimpleTweaks** | Dalamud.NET.Sdk/14.0.1 | (SDK default) |
| **HaselTweaks** | Dalamud.NET.Sdk/14.0.1 | (SDK default) |

**Finding:** DailiesChecklist is using the correct and current SDK version.

### 4.2 Common .csproj Configuration Patterns

#### DailiesChecklist Current Configuration
```xml
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>
    <Version>1.0.0.0</Version>
    <Authors>Logan Summerlin</Authors>
    <PackageProjectUrl>https://github.com/Logan-Summerlin/MMORPG-Addon</PackageProjectUrl>
    <PackageLicenseExpression>AGPL-3.0-or-later</PackageLicenseExpression>
    <IsPackable>false</IsPackable>
    <RootNamespace>DailiesChecklist</RootNamespace>
    <AssemblyName>DailiesChecklist</AssemblyName>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
```

#### Popular Plugin Patterns (SimpleTweaks/HaselTweaks)
```xml
<!-- Additional warnings commonly suppressed -->
<NoWarn>CS0649;CS0414;CS8618;Dalamud001;MSB3277</NoWarn>

<!-- Multiple build configurations -->
<Configurations>Debug;Release;DebugSteamDeck;CustomCS;Test</Configurations>

<!-- Dalamud dependency management (HaselTweaks) -->
<DalamudLibNewtonsoft>false</DalamudLibNewtonsoft>
<DalamudLibFFXIVClientStructs>false</DalamudLibFFXIVClientStructs>
```

### 4.3 Dalamud v14 .csproj Inline Manifest (New Feature)

Dalamud SDK v14 allows specifying manifest properties directly in .csproj:

```xml
<PropertyGroup>
    <Author>Logan Summerlin</Author>
    <Name>Dailies Checklist</Name>
    <InternalName>DailiesChecklist</InternalName>
    <Punchline>Track your daily and weekly FFXIV activities</Punchline>
    <Description>A quality-of-life overlay...</Description>
    <DalamudApiLevel>14</DalamudApiLevel>
    <LoadRequiredState>1</LoadRequiredState>
    <LoadSync>false</LoadSync>
    <Tags>checklist;dailies;weeklies;tracker;qol;utility</Tags>
</PropertyGroup>
```

**Note:** DailiesChecklist uses the traditional separate JSON manifest approach, which is still fully supported.

---

## 5. Distribution Methods Analysis

### 5.1 Official Repository (DalamudPluginsD17)

**Structure Required:**
```
stable/MyPlugin/
├── manifest.toml
└── images/
    ├── icon.png (required, 64x64 to 512x512, 1:1 aspect ratio)
    ├── image1.png (optional)
    ├── image2.png (optional)
    └── image3.png (optional)
```

**manifest.toml Format:**
```toml
[plugin]
repository = "https://github.com/username/plugin.git"
commit = "765d9bb434ac99a27e9a3f2ba0a555b55fe6269d"
owners = ["github_username"]
project_path = "PluginFolder"
changelog = "Version notes"
```

**Process:**
1. New plugins must submit to `testing/live/` first
2. One week testing period before stable promotion
3. Updates require new PR with updated commit hash
4. Builds are automated via Plogon system

### 5.2 Custom Repository (Current DailiesChecklist Method)

**Structure:**
```
Repository Root/
├── pluginmaster.json
└── releases/
    └── DailiesChecklist.zip
```

**pluginmaster.json Format:**
```json
[
  {
    "Author": "...",
    "Name": "...",
    "DownloadLinkInstall": "https://github.com/.../releases/latest/download/Plugin.zip",
    "DownloadLinkUpdate": "https://github.com/.../releases/latest/download/Plugin.zip",
    ...
  }
]
```

**User Installation:**
1. Add raw pluginmaster.json URL to Dalamud settings (Experimental tab)
2. Plugin appears in plugin installer
3. Updates delivered via GitHub releases

### 5.3 Multi-Plugin Custom Repository (Sea of Stars Pattern)

Popular for related plugin collections (e.g., Penumbra ecosystem):

**repo.json Structure:**
```json
[
  { "Name": "Penumbra", "InternalName": "Penumbra", ... },
  { "Name": "Glamourer", "InternalName": "Glamourer", ... },
  { "Name": "Ktisis", "InternalName": "Ktisis", ... }
]
```

**Benefits:**
- Single repository URL for users to add
- Coordinated versioning across related plugins
- Unified distribution infrastructure

---

## 6. Gap Analysis

### 6.1 Missing Fields in DailiesChecklist

| Field | Importance | Current State | Recommended Action |
|-------|------------|---------------|-------------------|
| LoadRequiredState | High | Missing | Add value 0, 1, or 2 based on when plugin should load |
| LoadSync | Medium | Missing | Add `false` unless synchronous loading required |
| TestingDalamudApiLevel | Low | Missing | Add if planning testing track support |
| TestingAssemblyVersion | Low | Missing | Add if planning testing track support |
| DownloadLinkTesting | Low | Empty string | Populate if offering beta versions |
| CategoryTags | Medium | Missing | Add for improved categorization |
| SupportsProfiles | Low | Missing | Add `true` if plugin supports Dalamud profiles |
| ImageUrls | High | Empty | Add 1-3 preview images for better discoverability |

### 6.2 LoadRequiredState Values

| Value | Meaning | Use Case |
|-------|---------|----------|
| 0 | Always load | Background services, always-on features |
| 1 | Load when character is loaded | Most QoL plugins (recommended for DailiesChecklist) |
| 2 | Load when character is loaded and in gameplay | Combat/duty plugins |

### 6.3 Configuration Improvements

**Current DailiesChecklist.json:**
- Missing: LoadRequiredState, LoadSync, CategoryTags

**Current pluginmaster.json:**
- Empty: DownloadLinkTesting, ImageUrls
- Missing: TestingAssemblyVersion, TestingDalamudApiLevel, LoadRequiredState, LoadSync

---

## 7. Recommendations

### 7.1 Immediate Actions (High Priority)

#### A. Update DailiesChecklist.json Manifest

Add these fields:
```json
{
  "LoadRequiredState": 1,
  "LoadSync": false,
  "CategoryTags": ["utility", "qol"]
}
```

#### B. Update pluginmaster.json

Add these fields to the plugin entry:
```json
{
  "LoadRequiredState": 1,
  "LoadSync": false,
  "TestingAssemblyVersion": "1.0.0.0",
  "TestingDalamudApiLevel": 14
}
```

#### C. Add Preview Images

1. Create 2-3 screenshots showing the plugin UI
2. Upload to GitHub (e.g., in a `/images/` folder)
3. Update ImageUrls in both manifests:
```json
"ImageUrls": [
  "https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/DailiesChecklist/images/preview1.png",
  "https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/DailiesChecklist/images/preview2.png"
]
```

### 7.2 Medium Priority Improvements

#### A. Consider Official Repository Submission

**Benefits:**
- Wider visibility (default plugin list)
- Automated builds and distribution
- Community review and trust

**Requirements:**
1. Create manifest.toml following DalamudPluginsD17 format
2. Ensure icon meets size requirements (64x64 to 512x512, 1:1 ratio)
3. Submit to testing/live/ track first

#### B. Enhance Tags for Discoverability

Current tags are good but could be expanded:
```json
"Tags": [
  "checklist",
  "dailies",
  "weeklies",
  "tracker",
  "qol",
  "utility",
  "reset",
  "roulette",
  "tasks"
]
```

### 7.3 Low Priority / Future Considerations

#### A. Testing Track Support

If planning beta releases:
1. Set up separate testing builds
2. Populate `DownloadLinkTesting`
3. Update `TestingAssemblyVersion` for test builds

#### B. Profile Support

If the plugin could benefit from Dalamud's profile system:
```json
"SupportsProfiles": true
```

#### C. .csproj Inline Manifest Migration

Consider migrating manifest to .csproj for simpler maintenance (optional - current approach is valid).

---

## 8. References

### Official Documentation
- [Dalamud Developer Documentation](https://dalamud.dev)
- [Plugin Metadata Guide](https://dalamud.dev/plugin-development/plugin-metadata/)
- [Project Layout Guide](https://dalamud.dev/plugin-development/project-layout/)
- [Submission Process](https://dalamud.dev/plugin-publishing/submission/)
- [Dalamud v14 Changes](https://dalamud.dev/versions/v14/)

### Official Repositories
- [goatcorp/Dalamud](https://github.com/goatcorp/Dalamud) - Framework source
- [goatcorp/SamplePlugin](https://github.com/goatcorp/SamplePlugin) - Official template
- [goatcorp/DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17) - Official plugin manifests
- [goatcorp/DalamudPackager](https://github.com/goatcorp/DalamudPackager) - Build packaging tool

### Popular Plugin Repositories (Examined)
- [Caraxi/SimpleTweaksPlugin](https://github.com/Caraxi/SimpleTweaksPlugin)
- [Haselnussbomber/HaselTweaks](https://github.com/Haselnussbomber/HaselTweaks)
- [xivdev/Penumbra](https://github.com/xivdev/Penumbra)
- [Ottermandias/Glamourer](https://github.com/Ottermandias/Glamourer)
- [Ottermandias/SeaOfStars](https://github.com/Ottermandias/SeaOfStars)
- [DelvUI/DelvUI](https://github.com/DelvUI/DelvUI)
- [Haplo064/ChatBubbles](https://github.com/Haplo064/ChatBubbles)

### API Reference
- [IPluginManifest Interface](https://dalamud.dev/api/Dalamud.Plugin.Internal.Types.Manifest/Interfaces/IPluginManifest/)

---

## Appendix A: Complete Manifest Field Reference

### Required Fields
| Field | Type | Description |
|-------|------|-------------|
| Name | string | Public display name |
| Author | string | Plugin author(s) |
| Punchline | string | Short one-line description |
| Description | string | Full description |

### Auto-Generated Fields (by DalamudPackager)
| Field | Type | Description |
|-------|------|-------------|
| InternalName | string | Matches AssemblyName |
| AssemblyVersion | Version | From project version |
| DalamudApiLevel | int | From SDK version |

### Optional Metadata Fields
| Field | Type | Description |
|-------|------|-------------|
| RepoUrl | string | Source code URL |
| IconUrl | string | Plugin icon URL |
| ImageUrls | string[] | Preview screenshot URLs |
| Tags | string[] | Search tags |
| CategoryTags | string[] | Category classification |
| Changelog | string | Version history |
| ApplicableVersion | string | Game version compatibility |

### Optional Behavior Fields
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| LoadRequiredState | int | 0 | When to load (0=always, 1=character, 2=gameplay) |
| LoadSync | bool | false | Synchronous loading |
| LoadPriority | int | 0 | Loading order priority |
| CanUnloadAsync | bool | false | Async unload support |
| SupportsProfiles | bool | false | Dalamud profile support |

### Optional Feedback Fields
| Field | Type | Description |
|-------|------|-------------|
| AcceptsFeedback | bool | Enable feedback button |
| FeedbackMessage | string | Instructions for feedback |

### Distribution Fields (pluginmaster.json only)
| Field | Type | Description |
|-------|------|-------------|
| DownloadLinkInstall | string | Initial install URL |
| DownloadLinkUpdate | string | Update download URL |
| DownloadLinkTesting | string | Testing build URL |
| DownloadCount | long | Download statistics |
| LastUpdate | long | Unix timestamp of last update |
| IsHide | bool | Hide from plugin list |
| IsTestingExclusive | bool | Testing track only |

### Testing Fields
| Field | Type | Description |
|-------|------|-------------|
| TestingAssemblyVersion | Version | Testing build version |
| TestingDalamudApiLevel | int | Testing API level |

---

## Appendix B: DailiesChecklist Current vs Recommended Configuration

### DailiesChecklist.json - Current
```json
{
  "Author": "Logan Summerlin",
  "Name": "Dailies Checklist",
  "InternalName": "DailiesChecklist",
  "Punchline": "Track your daily and weekly FFXIV activities in one convenient overlay.",
  "Description": "A quality-of-life overlay...",
  "ApplicableVersion": "any",
  "DalamudApiLevel": 14,
  "AssemblyVersion": "1.0.0.0",
  "RepoUrl": "https://github.com/Logan-Summerlin/MMORPG-Addon",
  "IconUrl": "https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/DailiesChecklist/icon.png",
  "ImageUrls": [],
  "AcceptsFeedback": true,
  "FeedbackMessage": "Please report issues on GitHub or use the Dalamud feedback system.",
  "Changelog": "Initial release - Daily and weekly activity tracking with auto-detection support",
  "Tags": ["checklist", "dailies", "weeklies", "tracker", "qol", "utility"]
}
```

### DailiesChecklist.json - Recommended
```json
{
  "Author": "Logan Summerlin",
  "Name": "Dailies Checklist",
  "InternalName": "DailiesChecklist",
  "Punchline": "Track your daily and weekly FFXIV activities in one convenient overlay.",
  "Description": "A quality-of-life overlay...",
  "ApplicableVersion": "any",
  "DalamudApiLevel": 14,
  "AssemblyVersion": "1.0.0.0",
  "RepoUrl": "https://github.com/Logan-Summerlin/MMORPG-Addon",
  "IconUrl": "https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/DailiesChecklist/icon.png",
  "ImageUrls": [
    "https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/DailiesChecklist/images/preview1.png"
  ],
  "AcceptsFeedback": true,
  "FeedbackMessage": "Please report issues on GitHub or use the Dalamud feedback system.",
  "Changelog": "Initial release - Daily and weekly activity tracking with auto-detection support",
  "Tags": ["checklist", "dailies", "weeklies", "tracker", "qol", "utility", "reset", "roulette"],
  "CategoryTags": ["utility", "qol"],
  "LoadRequiredState": 1,
  "LoadSync": false
}
```

---

*Report generated: February 1, 2026*
