# Project Manager Report: DailiesChecklist Plugin Review

**Date:** 2026-01-27
**Status:** Analysis Complete - Implementation Pending

---

## Executive Summary

Three parallel analyst reviews were conducted on the DailiesChecklist plugin following ChatGPT contributions. The plugin **passes security/compliance requirements** but requires functionality and code quality fixes before distribution.

## Compliance Status: PASS

- No gameplay automation
- No player tracking
- No network operations
- Local storage only (Dalamud config directory)

## Issues Requiring Fixes

### Critical (5)
| ID | Issue | File | Impact |
|----|-------|------|--------|
| C1 | Thread safety in debounced save | PersistenceService.cs:171-185 | Data corruption risk |
| C2 | Static Plugin reference in Config.Save() | Configuration.cs:134 | Crash on early init |
| C3 | Initialization order race condition | Plugin.cs:185-193 | Detector null refs |
| C4 | Silent exception swallowing (empty catch) | MainWindow.cs:437-439 | Hides bugs, impossible to debug |
| C5 | Dead code - unused EnsureUtc method | ResetService.cs:363-378 | Maintenance burden, incomplete refactor |

### High (7)
| ID | Issue | File |
|----|-------|------|
| H1 | JumboCactpot uses wrong reset timestamp | ResetService.cs:257-265 |
| H2 | RouletteId cleared before DutyCompleted event | RouletteDetector.cs:173-233 |
| H3 | MiniCactpot detection double-counts | CactpotDetector.cs:291-304 |
| H4 | Tooltip attached to wrong element | MainWindow.cs:362-378 |
| H5 | ResetTasksToDefaults breaks state sync | SettingsWindow.cs:501-519 |
| H6 | Null check race in Plugin.Dispose() | Plugin.cs:282-293 |
| H7 | Event handler cleanup on exception | DetectionService.cs:113-149 |

### Medium - Security (2)
| ID | Issue | File |
|----|-------|------|
| MS1 | Path not validated for traversal attacks | PersistenceService.cs:89-99 |
| MS2 | Character data stored without user disclosure | ChecklistState.cs:54-59 |

### Medium - Functionality (9)
| ID | Issue | File |
|----|-------|------|
| MF1 | Static mutable state in UIHelpers | UIHelpers.cs:397 |
| MF2 | Unused constructor params (_gameGui, _framework) | CactpotDetector, BeastTribeDetector |
| MF3 | Inefficient GetTaskById creates full list | TaskRegistry.cs:459-463 |
| MF4 | IDalamudConfigProvider constructor never used | PersistenceService.cs:77-81 |
| MF5 | FeatureFlags not versioned (new flags default false) | Configuration.cs:11-21 |
| MF6 | Global using alias `Log` causes confusion | Global.cs:46 |
| MF7 | No cancellation token support in services | Multiple services |
| MF8 | MaximumSize uses float.MaxValue | MainWindow.cs:69-73 |
| MF9 | ImRaii.Child success check missing log | SettingsWindow.cs:262 |

### Medium - Style/Syntax (15)
| ID | Issue | File |
|----|-------|------|
| MSS1 | Redundant using directives (global imports) | Multiple files |
| MSS2 | Unused event handler parameters | Detector files |
| MSS3 | Unnecessary `System.StringSplitOptions` qualification | Plugin.cs:322 |
| MSS4 | Unnecessary `System.Collections.Generic.List` qualification | PersistenceService.cs:332 |
| MSS5 | Inconsistent object init (`new object()` vs `new()`) | Multiple files |
| MSS6 | Redundant bool initialization `= false` | ChecklistTask.cs:49,55 |
| MSS7 | Redundant int initialization `= 0` | Configuration.cs:34 |
| MSS8 | Unnecessary else after return | ResetService.cs:283-291 |
| MSS9 | Disposed flag set but never checked before ops | ResetService.cs:69,380-390 |
| MSS10 | Missing null validation in Clone() | ChecklistTask.cs:85-103 |
| MSS11 | Potential NullReferenceException | MainWindow.cs:42,66 |
| MSS12 | Unused `_isInGoldSaucer` field | CactpotDetector.cs:44 |
| MSS13 | Inconsistent null check patterns (Empty vs WhiteSpace) | Multiple files |
| MSS14 | Magic numbers in UI code (350f, 18f, 100) | UIHelpers.cs |
| MSS15 | Text() vs TextUnformatted() for dynamic strings | MainWindow.cs:330,334 |

### Dead Code (Remove)
| Item | File |
|------|------|
| Unused `_framework` field | BeastTribeDetector.cs:37 |
| Unused `Log` type alias | Global.cs:46 |

## Recommended Test Scenarios

1. Save/Load cycle under rapid modifications
2. Daily reset at 15:00 UTC boundary
3. Roulette completion detection (expected to fail until H2 fixed)
4. MiniCactpot ticket counting (expected to fail until H3 fixed)
5. Settings reset synchronization with main window

## Next Steps

1. Implement Critical fixes (C1-C5)
2. Implement High fixes (H1-H7)
3. Address Medium issues (MS1-MS2, MF1-MF9, MSS1-MSS15)
4. Remove Dead Code items
5. Manual testing per test scenarios
6. Final review before distribution

---

**Issue Totals:** 5 Critical, 7 High, 26 Medium, 2 Dead Code items

*Report compiled from 3 analyst reviews (~6000 lines analyzed)*
