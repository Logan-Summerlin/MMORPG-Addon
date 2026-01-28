# Technical Analyst Report: DailiesChecklist Plugin Configuration Review
**Date:** 2026-01-28
**Analyst:** Technical Analyst 1
**Task:** Investigate "Load Error, This Plugin Failed To Load" in Dalamud plugin installer

---

## Executive Summary

After thorough review of the project configuration files, I have identified **one critical issue** that is likely causing the plugin load error: the GitHub Actions workflow downloads the **staging** version of Dalamud instead of the **release** version. This creates a version mismatch between the build environment and user environments.

---

## Files Reviewed

1. `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.csproj`
2. `/home/user/MMORPG-Addon/.github/workflows/release.yml`
3. `/home/user/MMORPG-Addon/DailiesChecklist/DailiesChecklist.json`
4. `/home/user/MMORPG-Addon/pluginmaster.json`
5. `/home/user/MMORPG-Addon/DailiesChecklist/Plugin.cs`
6. `/home/user/MMORPG-Addon/DailiesChecklist/Service.cs`
7. `/home/user/MMORPG-Addon/DailiesChecklist/Configuration.cs`

---

## Checklist Verification Results

### 1. Dalamud.NET.Sdk Version
**Status:** PASS
**Finding:** Project correctly uses `Dalamud.NET.Sdk/14.0.1`
```xml
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
```

### 2. Target Framework
**Status:** PASS
**Finding:** Project correctly targets `net10.0-windows` as required for API 14
```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

### 3. Output Path Configuration
**Status:** PASS
**Finding:** Both path append settings are correctly configured
```xml
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<AppendPlatformToOutputPath>false</AppendPlatformToOutputPath>
```

### 4. Workflow .NET SDK Version
**Status:** PASS
**Finding:** Workflow correctly uses .NET 10.0 SDK
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
```

### 5. Framework DLL Copying
**Status:** PASS
**Finding:** Using Dalamud.NET.Sdk automatically handles this - no explicit Dalamud.dll references found in the csproj. The SDK handles dependency resolution properly.

### 6. Build Configuration
**Status:** PASS
**Finding:** Workflow correctly builds in Release mode
```yaml
- name: Build Release
  run: dotnet build --configuration Release --no-restore
```

### 7. Package Structure
**Status:** PASS
**Finding:** The packaging step creates the correct structure:
- Creates `package/DailiesChecklist/` directory
- Copies `DailiesChecklist.dll` from `bin/Release`
- Copies `DailiesChecklist.json` manifest
- Copies `icon.png` (verified exists at `/home/user/MMORPG-Addon/DailiesChecklist/icon.png`)
- Creates zip containing the folder structure

---

## Critical Issue Identified

### Issue: Incorrect Dalamud Distribution URL

**Location:** `/home/user/MMORPG-Addon/.github/workflows/release.yml`, lines 42-43

**Current Configuration (INCORRECT):**
```yaml
Invoke-WebRequest -Uri https://goatcorp.github.io/dalamud-distrib/stg/latest.zip -OutFile latest.zip
```

**Problem:** The workflow downloads Dalamud from the **staging** (`stg`) distribution channel. Users running Dalamud on the **release** channel will have a different version of Dalamud installed. This version mismatch can cause:
- ABI incompatibilities
- Missing or changed API methods
- Plugin load failures with generic "This Plugin Failed To Load" error

**Required Fix:**
```yaml
Invoke-WebRequest -Uri https://goatcorp.github.io/dalamud-distrib/latest.zip -OutFile latest.zip
```

**Alternative (if API 14 is only on staging):** If API 14 is currently only available on the staging/testing channel and not yet on release, the pluginmaster.json should specify `"IsTestingExclusive": true` to indicate this is a testing plugin. However, the current pluginmaster.json has:
```json
"IsTestingExclusive": false
```

---

## Additional Verification (All Passed)

### Plugin Structure
- `Plugin.cs` correctly implements `IDalamudPlugin` interface
- Constructor accepts `IDalamudPluginInterface pluginInterface` as required
- Implements `Dispose()` for proper cleanup
- Uses proper service injection pattern via `Service.Initialize(pluginInterface)`

### Manifest Files
- `DailiesChecklist.json` has correct `DalamudApiLevel: 14`
- `pluginmaster.json` has matching `DalamudApiLevel: 14`
- InternalName matches across all files: `DailiesChecklist`

### Configuration
- `Configuration.cs` correctly implements `IPluginConfiguration`
- Uses `[Serializable]` attribute
- Implements `Version` property as required

### Icon File
- `icon.png` exists at `/home/user/MMORPG-Addon/DailiesChecklist/icon.png` (3,350 bytes)

---

## Recommended Fixes

### Fix 1: Update Dalamud Distribution URL (CRITICAL)
**File:** `/home/user/MMORPG-Addon/.github/workflows/release.yml`
**Line:** 42

Change:
```yaml
Invoke-WebRequest -Uri https://goatcorp.github.io/dalamud-distrib/stg/latest.zip -OutFile latest.zip
```

To:
```yaml
Invoke-WebRequest -Uri https://goatcorp.github.io/dalamud-distrib/latest.zip -OutFile latest.zip
```

This ensures the plugin is built against the same Dalamud version that users on the release channel are running.

---

## Summary Table

| Checklist Item | Status | Notes |
|----------------|--------|-------|
| Dalamud.NET.Sdk/14.0.1 | PASS | Correctly configured |
| net10.0-windows target | PASS | Correctly configured |
| AppendTargetFrameworkToOutputPath=false | PASS | Correctly configured |
| AppendPlatformToOutputPath=false | PASS | Correctly configured |
| .NET 10.0 SDK in workflow | PASS | Correctly configured |
| No framework DLLs copied | PASS | SDK handles automatically |
| Release build configuration | PASS | Correctly configured |
| Correct zip packaging | PASS | Correctly configured |
| **Dalamud distribution URL** | **FAIL** | Uses staging instead of release |

---

## Conclusion

The root cause of the plugin load error is the Dalamud distribution URL mismatch. The workflow builds against the **staging** Dalamud version while users are running the **release** version. Changing the URL from `stg/latest.zip` to `latest.zip` should resolve the load error.

All other configuration items have been verified and are correctly set up for API 14.
