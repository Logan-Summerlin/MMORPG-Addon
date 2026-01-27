using System;

namespace DailiesChecklist.Models
{
    /// <summary>
    /// Represents a single task in the dailies/weeklies checklist.
    /// </summary>
    public class ChecklistTask
    {
        /// <summary>
        /// Unique identifier for the task (e.g., "mini_cactpot", "roulette_expert").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown in the UI (e.g., "Mini Cactpot", "Expert Roulette").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Location where the task is performed (e.g., "Gold Saucer", "Duty Finder").
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Brief description of the task (e.g., "3 scratch tickets daily").
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a daily or weekly task.
        /// </summary>
        public TaskCategory Category { get; set; }

        /// <summary>
        /// How completion is detected (Manual, AutoDetected, or Hybrid).
        /// </summary>
        public DetectionType Detection { get; set; }

        /// <summary>
        /// Whether the user has enabled this task in their checklist.
        /// Default is true (all tasks enabled).
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether the task has been completed for the current reset period.
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Whether the completion status was manually set by the user
        /// (as opposed to auto-detected).
        /// </summary>
        public bool IsManuallySet { get; set; } = false;

        /// <summary>
        /// Timestamp when the task was marked complete.
        /// Null if not completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Display order within the category.
        /// Lower numbers appear first.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Maximum count for tasks with multiple completions (e.g., Mini Cactpot x3).
        /// Default is 1 for single-completion tasks.
        /// </summary>
        public int MaxCount { get; set; } = 1;

        /// <summary>
        /// Current completion count for tasks with multiple completions.
        /// When CurrentCount >= MaxCount, the task is considered complete.
        /// </summary>
        public int CurrentCount { get; set; } = 0;

        /// <summary>
        /// Creates a deep copy of this task.
        /// Used when creating fresh task lists from the registry.
        /// </summary>
        public ChecklistTask Clone()
        {
            return new ChecklistTask
            {
                Id = this.Id,
                Name = this.Name,
                Location = this.Location,
                Description = this.Description,
                Category = this.Category,
                Detection = this.Detection,
                IsEnabled = this.IsEnabled,
                IsCompleted = this.IsCompleted,
                IsManuallySet = this.IsManuallySet,
                CompletedAt = this.CompletedAt,
                SortOrder = this.SortOrder,
                MaxCount = this.MaxCount,
                CurrentCount = this.CurrentCount
            };
        }
    }
}
