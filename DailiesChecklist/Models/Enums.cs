namespace DailiesChecklist.Models
{
    /// <summary>
    /// Defines the reset category for a checklist task.
    /// </summary>
    public enum TaskCategory
    {
        /// <summary>
        /// Daily tasks reset at 15:00 UTC each day.
        /// Includes: Duty Roulettes, Beast Tribes, Mini Cactpot, Daily Hunts.
        /// </summary>
        Daily,

        /// <summary>
        /// Grand Company tasks reset at 20:00 UTC each day (DIFFERENT from standard daily!).
        /// Includes: Supply/Provisioning missions, Squadron Training.
        /// </summary>
        GrandCompany,

        /// <summary>
        /// Weekly tasks reset on Tuesday at 08:00 UTC.
        /// Includes: Raids, Challenge Log, Custom Deliveries, Wondrous Tails.
        /// </summary>
        Weekly
    }

    /// <summary>
    /// Defines how task completion is detected.
    /// </summary>
    public enum DetectionType
    {
        /// <summary>
        /// User must manually mark the task as complete.
        /// </summary>
        Manual,

        /// <summary>
        /// Plugin can automatically detect completion via game state.
        /// </summary>
        AutoDetected,

        /// <summary>
        /// Auto-detection available with manual override capability.
        /// </summary>
        Hybrid
    }
}
