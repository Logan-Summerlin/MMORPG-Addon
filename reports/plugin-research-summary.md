# FFXIV Plugin Research Summary

**Date:** 2026-01-27
**Compiled By:** Project Manager
**Purpose:** Competitive analysis of popular FFXIV plugins for Dailies Checklist improvement

---

## Plugins Analyzed

| Plugin | Author | Repository | Primary Function |
|--------|--------|------------|------------------|
| WrathCombo | PunishXIV | github.com/PunishXIV/WrathCombo | Combat rotation automation |
| Splatoon | PunishXIV | github.com/PunishXIV/Splatoon | Accessibility waymarks/overlays |
| AutoRetainer | PunishXIV | github.com/PunishXIV/AutoRetainer | Retainer venture automation |
| Lifestream | NightmareXIV | github.com/NightmareXIV/Lifestream | World/DC travel navigation |
| Artisan | PunishXIV | github.com/PunishXIV/Artisan | Crafting automation |

---

## Key Patterns Identified

### 1. Code Organization
- **Global Usings (Global.cs)**: All plugins use centralized global using statements
- **Static Service Locator**: Common pattern for accessing services via `S.ServiceName`
- **Modular Architecture**: Features organized into domain-specific directories

### 2. Configuration Management
- **Version-based Migration**: All plugins support config schema updates
- **Per-item Configs**: Dictionary-based storage for customization (e.g., per-recipe, per-task)
- **Feature Flags**: Granular enable/disable for graceful degradation

### 3. Error Handling
- **Safe Wrappers**: Try-catch around all event handlers
- **Graceful Degradation**: Features disable cleanly when broken
- **User Notifications**: Toast messages for important errors

### 4. UI Patterns
- **Tab-based Organization**: Multiple tabs for complex settings
- **DTR Bar Integration**: Status indicators in server info bar
- **ImRaii Usage**: Scoped cleanup for ImGui resources

---

## Security & Compliance Comparison

| Aspect | Analyzed Plugins | Dailies Checklist | Assessment |
|--------|-----------------|-------------------|------------|
| Automation | Yes (all 5) | No | DC Safer |
| Memory Access | Extensive | None | DC Safer |
| Network Calls | Some (API/updates) | None | DC Safer |
| Combat Impact | WrathCombo only | None | DC Compliant |
| Data Collection | Minimal | Minimal | Equal |

**Conclusion**: Dailies Checklist has the strongest compliance posture of all analyzed plugins.

---

## Priority Improvements for Dailies Checklist

### High Priority (Implement Now)

1. **Add Global.cs**
   - Centralize common using statements
   - Add type aliases for frequently used types
   - Reduces boilerplate across all files

2. **Fix Task ID Mismatches**
   - TaskRegistry uses: `roulette_msq`, `roulette_normal_raid`
   - RouletteDetector uses: `roulette_mainscenario`, `roulette_normal`
   - Must normalize for detection to work

3. **Complete Phase 2 Detectors**
   - RouletteDetector: Implement roulette type detection via ContentFinderCondition
   - CactpotDetector: Monitor Gold Saucer addon for ticket state
   - BeastTribeDetector: Track quest completion events

4. **Enhance Error Handling**
   - Add safe wrapper methods for event handlers
   - Implement feature flags per detector
   - Add user-facing error notifications

### Medium Priority (Next Sprint)

5. **Configuration Migration**
   - Add version-based migration support
   - Clean orphaned entries on load
   - Backup before migration

6. **UI Enhancements**
   - Add DTR bar entry showing completion progress
   - Consider minimal overlay option
   - Add diagnostics/debug tab

7. **Service Manager Pattern**
   - Consider static ServiceManager for cleaner access
   - Alternative: Keep current DI (better for testing)

### Low Priority (Future)

8. **IPC Support**: Enable other plugins to query task state
9. **Per-Character Profiles**: Track different characters separately
10. **Import/Export**: Settings backup and sharing
11. **Unit Tests**: Test ResetService, PersistenceService logic

---

## What NOT to Copy

- **Automation patterns**: WrathCombo/AutoRetainer/Artisan automate gameplay
- **Memory hooks**: Splatoon's extensive memory reading
- **External APIs**: Artisan's Universalis market board calls
- **Unsafe code**: Memory manipulation patterns

---

## Architecture Recommendations

### Current Structure (Good)
```
DailiesChecklist/
├── Detectors/          # Detection system
├── Models/             # Data models
├── Services/           # Business logic
├── Windows/            # UI windows
├── Configuration.cs
└── Plugin.cs
```

### Suggested Additions
```
+ Core/
+   ├── Global.cs       # Global usings
+   └── Utils.cs        # Shared helpers
+ Services/
+   └── ConfigMigrator.cs
```

---

## Dailies Checklist Strengths to Maintain

1. **Clean DI Pattern**: Constructor injection is more testable than service locators
2. **Comprehensive Documentation**: XML docs exceed all analyzed plugins
3. **Information-Only Design**: No automation = lowest compliance risk
4. **Event-Driven Architecture**: Proper observer pattern
5. **Debounced Persistence**: Prevents rapid disk writes

---

## Conclusion

The Dailies Checklist plugin has a solid foundation that compares favorably to popular community plugins. The primary gaps are:

1. **Incomplete detection** (Phase 2 stub implementations)
2. **Task ID inconsistencies** (registry vs detector mismatch)
3. **Missing utilities** (global usings, error wrappers)

Addressing these issues will bring the plugin to production quality suitable for distribution.

**Compliance Note**: Unlike the analyzed plugins which automate gameplay, our plugin remains strictly informational, making it the safest choice for users concerned about ToS compliance.

---

*Research compiled from 5 analyst subagent reports analyzing 78KB+ of source documentation.*
