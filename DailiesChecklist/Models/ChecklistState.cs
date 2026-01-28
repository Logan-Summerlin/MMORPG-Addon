using System;
using System.Collections.Generic;
using System.Linq;
using DailiesChecklist.Services;

namespace DailiesChecklist.Models
{
    /// <summary>
    /// Container for the complete checklist state including all tasks and reset tracking.
    /// This class is serialized for persistence between sessions.
    /// Implements IChecklistState for use with ResetService.
    /// </summary>
    public class ChecklistState : IChecklistState
    {
        #region Data Storage Disclosure

        /// <summary>
        /// Documents what character data is stored locally by this plugin.
        /// This constant is provided for transparency and user trust.
        /// </summary>
        /// <remarks>
        /// DATA STORED LOCALLY:
        /// - CharacterId: A numeric identifier used to track per-character checklist state.
        /// - CharacterName: The character name, used for display and debugging only.
        /// - Task completion states and timestamps.
        /// - Reset tracking timestamps.
        ///
        /// DATA NOT STORED:
        /// - No passwords, credentials, or authentication tokens.
        /// - No network transmission of any data.
        /// - No tracking of other players.
        ///
        /// All data is stored in the local Dalamud plugin configuration directory only.
        /// Use ClearCharacterData() to remove character-identifying information.
        /// </remarks>
        public const string DataStorageDisclosure =
            "This plugin stores CharacterId and CharacterName locally for per-character " +
            "checklist tracking. No data is transmitted externally. Use ClearCharacterData() " +
            "to remove character-identifying information while preserving task settings.";

        #endregion

        #region Validation Constants

        /// <summary>
        /// Maximum number of tasks allowed in the checklist.
        /// Protects against corrupted config files or malicious edits causing performance issues.
        /// </summary>
        public const int MaxTaskCount = 100;

        #endregion

        /// <summary>
        /// Schema version for migration support.
        /// Increment when making breaking changes to the state structure.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// All checklist tasks with their current states.
        /// </summary>
        public List<ChecklistTask> Tasks { get; set; } = new List<ChecklistTask>();

        /// <summary>
        /// Timestamp of the last daily reset that was processed.
        /// Daily reset occurs at 15:00 UTC.
        /// </summary>
        public DateTime LastDailyReset { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Timestamp of the last weekly reset that was processed.
        /// Weekly reset occurs Tuesday at 08:00 UTC.
        /// </summary>
        public DateTime LastWeeklyReset { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Timestamp of the last Jumbo Cactpot reset that was processed.
        /// Jumbo Cactpot drawing occurs Saturday at 08:00 UTC.
        /// </summary>
        public DateTime LastJumboCactpotReset { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Timestamp of the last Grand Company reset that was processed.
        /// GC reset occurs at 20:00 UTC (different from standard daily reset!).
        /// Affects: Supply/Provisioning missions, Squadron Training.
        /// </summary>
        public DateTime LastGCReset { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Timestamp when the state was last saved to disk.
        /// </summary>
        public DateTime LastSaveTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Character ID this state belongs to.
        /// Allows per-character state tracking.
        /// </summary>
        public ulong? CharacterId { get; set; }

        /// <summary>
        /// Character name for display/debugging purposes.
        /// </summary>
        public string? CharacterName { get; set; }

        /// <summary>
        /// Resets all daily tasks (Category == Daily) to their uncompleted state.
        /// Called by ResetService when a daily reset is detected.
        /// </summary>
        public void ResetDailyTasks()
        {
            foreach (var task in Tasks.Where(t => t.Category == TaskCategory.Daily))
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
                task.CurrentCount = 0;
            }
        }

        /// <summary>
        /// Resets all Grand Company tasks (Category == GrandCompany) to their uncompleted state.
        /// Called by ResetService when a GC reset is detected (20:00 UTC daily).
        /// </summary>
        public void ResetGrandCompanyTasks()
        {
            foreach (var task in Tasks.Where(t => t.Category == TaskCategory.GrandCompany))
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
                task.CurrentCount = 0;
            }
        }

        /// <summary>
        /// Resets all weekly tasks (Category == Weekly) to their uncompleted state.
        /// Called by ResetService when a weekly reset is detected (Tuesday 08:00 UTC).
        /// </summary>
        public void ResetWeeklyTasks()
        {
            foreach (var task in Tasks.Where(t => t.Category == TaskCategory.Weekly))
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
                task.CurrentCount = 0;
            }
        }

        /// <summary>
        /// Resets all tasks regardless of category.
        /// Useful for testing or manual reset.
        /// </summary>
        public void ResetAllTasks()
        {
            foreach (var task in Tasks)
            {
                task.IsCompleted = false;
                task.IsManuallySet = false;
                task.CompletedAt = null;
                task.CurrentCount = 0;
            }
        }

        /// <summary>
        /// Gets all tasks for a specific category.
        /// </summary>
        public IEnumerable<ChecklistTask> GetTasksByCategory(TaskCategory category)
        {
            return Tasks.Where(t => t.Category == category);
        }

        /// <summary>
        /// Gets a task by its ID.
        /// </summary>
        public ChecklistTask? GetTaskById(string taskId)
        {
            return Tasks.FirstOrDefault(t => t.Id == taskId);
        }

        /// <summary>
        /// Creates a deep copy of this state.
        /// </summary>
        public ChecklistState Clone()
        {
            return new ChecklistState
            {
                Version = this.Version,
                Tasks = this.Tasks.Select(t => t.Clone()).ToList(),
                LastDailyReset = this.LastDailyReset,
                LastWeeklyReset = this.LastWeeklyReset,
                LastJumboCactpotReset = this.LastJumboCactpotReset,
                LastGCReset = this.LastGCReset,
                LastSaveTime = this.LastSaveTime,
                CharacterId = this.CharacterId,
                CharacterName = this.CharacterName
            };
        }

        #region Privacy and Data Management

        /// <summary>
        /// Clears all character-identifying information from this state.
        /// Task settings and completion states are preserved.
        /// Call this method to anonymize local data while keeping checklist functionality.
        /// </summary>
        public void ClearCharacterData()
        {
            CharacterId = null;
            CharacterName = null;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates and sanitizes the state to ensure data integrity.
        /// - Clamps task list to MaxTaskCount to prevent performance issues from corrupted configs.
        /// - Ensures Tasks list is never null.
        /// - Returns true if validation passed without modifications, false if corrections were made.
        /// </summary>
        /// <returns>True if state was already valid; false if corrections were applied.</returns>
        public bool Validate()
        {
            bool wasValid = true;

            // Ensure Tasks is never null
            if (Tasks == null)
            {
                Tasks = new List<ChecklistTask>();
                wasValid = false;
            }

            // Clamp task count to prevent performance issues from corrupted/malicious config
            if (Tasks.Count > MaxTaskCount)
            {
                // Keep the first MaxTaskCount tasks (preserves user's original tasks)
                Tasks = Tasks.Take(MaxTaskCount).ToList();
                wasValid = false;
            }

            // Validate individual tasks exist (remove nulls)
            int nullCount = Tasks.RemoveAll(t => t == null);
            if (nullCount > 0)
            {
                wasValid = false;
            }

            return wasValid;
        }

        /// <summary>
        /// Creates a validated copy of this state.
        /// Useful for loading from potentially corrupted configuration files.
        /// </summary>
        /// <returns>A validated clone of the state.</returns>
        public ChecklistState CloneValidated()
        {
            var clone = Clone();
            clone.Validate();
            return clone;
        }

        #endregion
    }
}
