# DailiesChecklist Plugin Release Guide

This document describes how to build and distribute the DailiesChecklist plugin via a custom Dalamud repository.

## Custom Repository URL

Users can add this plugin to their Dalamud installation by adding the following URL to their Custom Plugin Repositories:

```
https://raw.githubusercontent.com/Logan-Summerlin/MMORPG-Addon/master/pluginmaster.json
```

### How to Add:
1. In-game, open Dalamud Settings: `/xlsettings`
2. Navigate to the **Experimental** tab
3. Find the **Custom Plugin Repositories** section
4. Paste the URL above into an empty text field
5. Click the **+** button to add it
6. Open the Plugin Installer: `/xlplugins`
7. Search for "Dailies Checklist" and install

## Building the Plugin

### Prerequisites
- Visual Studio 2022+ or JetBrains Rider
- .NET 10.0 SDK
- XIVLauncher with Dalamud installed (for local testing)

### Build Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/Logan-Summerlin/MMORPG-Addon.git
   cd MMORPG-Addon
   ```

2. Build in Release mode:
   ```bash
   dotnet build --configuration Release
   ```

3. The output will be in `DailiesChecklist/bin/Release/net10.0-windows/`

### Local Testing

1. Add the build output folder to Dalamud's Dev Plugin Locations:
   - `/xlsettings` > Experimental > Dev Plugin Locations
   - Add: `<path-to-repo>/DailiesChecklist/bin/Release/net10.0-windows`

2. Enable the plugin in `/xlplugins` > Dev Tools > Installed Dev Plugins

## Creating a Release

### IMPORTANT: First Release Required

Before the plugin can be installed via the plugin installer, you **must create at least one GitHub release**. The `pluginmaster.json` download URLs point to GitHub Releases, so without a release, the plugin installer will fail with a download error.

### Automated (Recommended)

The repository includes a GitHub Actions workflow that automatically builds and releases the plugin.

**To create a new release:**

1. Ensure version numbers are consistent in:
   - `DailiesChecklist/DailiesChecklist.csproj` (Version property)
   - `DailiesChecklist/DailiesChecklist.json` (AssemblyVersion field)
   - `pluginmaster.json` (AssemblyVersion field)

2. Commit and push any pending changes

3. Create and push a version tag:
   ```bash
   git tag v1.0.0.0
   git push origin v1.0.0.0
   ```

4. The workflow will:
   - Build the plugin in Release mode
   - Package it as `DailiesChecklist.zip`
   - Create a GitHub Release with the ZIP attached
   - Update `pluginmaster.json` with the new version and timestamp

**Alternative: Manual Workflow Trigger**

You can also trigger the release workflow manually from the GitHub Actions tab:
1. Go to the repository's Actions tab
2. Select "Build and Release Plugin" workflow
3. Click "Run workflow"
4. Enter the version number (e.g., 1.0.0.0)
5. Click "Run workflow"

### Manual Release

If you need to create a release manually:

1. Build the plugin in Release mode

2. Create a ZIP file named `DailiesChecklist.zip` containing:
   ```
   DailiesChecklist/
   ├── DailiesChecklist.dll
   ├── DailiesChecklist.json
   └── icon.png (optional)
   ```

3. Create a GitHub Release and upload the ZIP

4. Update `pluginmaster.json`:
   - Set `AssemblyVersion` to match the DLL version
   - Set `LastUpdate` to current Unix timestamp
   - Verify `DownloadLinkInstall` and `DownloadLinkUpdate` point to the release

## Repository Structure for Distribution

```
MMORPG-Addon/
├── pluginmaster.json          # Repository manifest (users add this URL)
├── DailiesChecklist/
│   ├── DailiesChecklist.csproj
│   ├── DailiesChecklist.json  # Plugin manifest (packaged in ZIP)
│   ├── icon.png               # Plugin icon
│   └── ...                    # Source files
└── .github/
    └── workflows/
        └── release.yml        # Automated release workflow
```

## Troubleshooting

### Plugin installation fails (download error / 404)
This is the most common issue and occurs when **no GitHub release exists**.
- Check if a release exists: `https://github.com/Logan-Summerlin/MMORPG-Addon/releases`
- If no releases exist, create one using the instructions above
- Verify the `DownloadLinkInstall` URL in `pluginmaster.json` returns a valid ZIP file

### Plugin not appearing in installer
- Verify the `pluginmaster.json` URL is accessible
- Check that `DalamudApiLevel` matches your Dalamud version
- Ensure the download URLs point to valid ZIP files
- Verify `LastUpdate` in `pluginmaster.json` is a number, not a string

### Plugin fails to load
- Check the Dalamud log: `/xllog`
- Verify the ZIP contains `DailiesChecklist.dll` and `DailiesChecklist.json`
- Ensure `InternalName` in the manifest matches the DLL name (without .dll)
- Verify the plugin was built for the correct .NET version (net10.0-windows)

### Version not updating
- Verify `AssemblyVersion` in `pluginmaster.json` differs from installed version
- Check that GitHub Release contains the new ZIP
- Try removing and re-adding the custom repository

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0.0 | 2026-01-28 | Initial release |
