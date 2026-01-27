// -----------------------------------------------------------------------------
// <copyright file="Global.cs" company="DailiesChecklist">
//     Copyright (c) DailiesChecklist. All rights reserved.
// </copyright>
// <summary>
//     Centralized global using statements for the DailiesChecklist plugin.
//     This file provides common namespace imports and type aliases that are
//     automatically available throughout the entire project.
// </summary>
// -----------------------------------------------------------------------------

// =============================================================================
// SYSTEM NAMESPACES
// =============================================================================

// Core .NET types used throughout the codebase
global using System;
global using System.Collections.Generic;
global using System.Linq;

// =============================================================================
// DALAMUD FRAMEWORK NAMESPACES
// =============================================================================

// Dalamud plugin services (IPluginLog, IClientState, IFramework, etc.)
global using Dalamud.Plugin.Services;

// Dalamud windowing system for ImGui-based windows
global using Dalamud.Interface.Windowing;

// =============================================================================
// PROJECT NAMESPACES
// =============================================================================

// Models: ChecklistState, ChecklistTask, TaskCategory, etc.
global using DailiesChecklist.Models;

// Services: ResetService, PersistenceService, TaskRegistry, etc.
global using DailiesChecklist.Services;

// =============================================================================
// TYPE ALIASES
// =============================================================================

// Convenient alias for the logging service
global using Log = Dalamud.Plugin.Services.IPluginLog;
