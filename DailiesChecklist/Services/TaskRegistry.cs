using System.Collections.Generic;
using DailiesChecklist.Models;

namespace DailiesChecklist.Services
{
    /// <summary>
    /// Static factory for creating the default task definitions.
    /// Contains all daily and weekly tasks with accurate metadata.
    ///
    /// Reset Times Reference:
    /// - Daily Reset: 15:00 UTC
    /// - Weekly Reset: Tuesday 08:00 UTC
    /// - GC Reset: 20:00 UTC (DIFFERENT from standard daily!)
    /// - Jumbo Cactpot Drawing: Saturday 08:00 UTC
    /// </summary>
    public static class TaskRegistry
    {
        /// <summary>
        /// Returns a fresh list of all tasks with default states.
        /// All tasks are enabled (IsEnabled=true) and not completed (IsCompleted=false).
        /// </summary>
        public static List<ChecklistTask> GetDefaultTasks()
        {
            var tasks = new List<ChecklistTask>();

            // ========================================
            // DAILY TASKS (Reset at 15:00 UTC)
            // ========================================

            // Gold Saucer - Mini Cactpot
            tasks.Add(new ChecklistTask
            {
                Id = "mini_cactpot",
                Name = "Mini Cactpot",
                Location = "Gold Saucer",
                Description = "3 scratch tickets daily (10 MGP each)",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 10,
                MaxCount = 3,
                CurrentCount = 0
            });

            // Duty Roulettes (sorted by typical priority)
            tasks.Add(new ChecklistTask
            {
                Id = "roulette_expert",
                Name = "Expert Roulette",
                Location = "Duty Finder",
                Description = "Current max-level dungeons for tomestones",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 20
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_leveling",
                Name = "Leveling Roulette",
                Location = "Duty Finder",
                Description = "Large EXP bonus for leveling jobs",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 21
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_msq",
                Name = "Main Scenario Roulette",
                Location = "Duty Finder",
                Description = "Castrum/Praetorium/Porta for large tomestone rewards",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 22
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_alliance",
                Name = "Alliance Raid Roulette",
                Location = "Duty Finder",
                Description = "24-man raids for high EXP and tomestones",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 23
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_normal_raid",
                Name = "Normal Raid Roulette",
                Location = "Duty Finder",
                Description = "8-man normal raids for tomestones",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 24
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_trials",
                Name = "Trials Roulette",
                Location = "Duty Finder",
                Description = "Trial fights for tomestones and EXP",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 25
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_5060708090",
                Name = "Level 50/60/70/80/90 Dungeons",
                Location = "Duty Finder",
                Description = "High-level dungeons for Poetics and Aesthetics",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 26
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_frontline",
                Name = "Frontline Roulette",
                Location = "Duty Finder",
                Description = "PvP roulette for EXP, Wolf Marks, tomestones",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 27
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_guildhests",
                Name = "Guildhests Roulette",
                Location = "Duty Finder",
                Description = "Small group tutorials for minor EXP",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 28
            });

            tasks.Add(new ChecklistTask
            {
                Id = "roulette_mentor",
                Name = "Mentor Roulette",
                Location = "Duty Finder",
                Description = "Mentor-only roulette (requires Battle Mentor)",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = false, // Disabled by default - mentor specific
                IsCompleted = false,
                SortOrder = 29
            });

            // Beast Tribe / Allied Society Quests
            tasks.Add(new ChecklistTask
            {
                Id = "beast_tribe_quests",
                Name = "Beast Tribe Quests",
                Location = "Various Tribal Areas",
                Description = "12 daily allowances across all tribes",
                Category = TaskCategory.Daily,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 30,
                MaxCount = 12,
                CurrentCount = 0
            });

            // Daily Hunts by Expansion
            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_arr",
                Name = "Daily Hunts (ARR)",
                Location = "Grand Company HQ",
                Description = "Hunt bills for Allied Seals",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 40
            });

            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_hw",
                Name = "Daily Hunts (HW)",
                Location = "Foundation",
                Description = "Hunt bills for Centurio Seals",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 41
            });

            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_sb",
                Name = "Daily Hunts (SB)",
                Location = "Kugane / Rhalgr's Reach",
                Description = "Hunt bills for Centurio Seals",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 42
            });

            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_shb",
                Name = "Daily Hunts (ShB)",
                Location = "Crystarium / Eulmore",
                Description = "Hunt bills for Sacks of Nuts",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 43
            });

            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_ew",
                Name = "Daily Hunts (EW)",
                Location = "Old Sharlayan / Radz-at-Han",
                Description = "Hunt bills for Sacks of Nuts",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 44
            });

            tasks.Add(new ChecklistTask
            {
                Id = "daily_hunts_dt",
                Name = "Daily Hunts (DT)",
                Location = "Tuliyollal / Solution Nine",
                Description = "Hunt bills for current hunt currency",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 45
            });

            // Grand Company - Uses different reset time (20:00 UTC)
            tasks.Add(new ChecklistTask
            {
                Id = "gc_supply_provisioning",
                Name = "GC Supply/Provisioning",
                Location = "Grand Company HQ",
                Description = "Turn in crafted/gathered items (resets 20:00 UTC)",
                Category = TaskCategory.GrandCompany,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 50
            });

            // Treasure Map Gathering
            tasks.Add(new ChecklistTask
            {
                Id = "treasure_map",
                Name = "Treasure Map Gathering",
                Location = "Gathering Nodes (Lv40+)",
                Description = "One map can be gathered per 18 hours",
                Category = TaskCategory.Daily,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 60
            });

            // ========================================
            // WEEKLY TASKS (Reset Tuesday 08:00 UTC)
            // ========================================

            // Gold Saucer - Jumbo Cactpot (Drawing Saturday 08:00 UTC)
            tasks.Add(new ChecklistTask
            {
                Id = "jumbo_cactpot",
                Name = "Jumbo Cactpot",
                Location = "Gold Saucer",
                Description = "3 lottery tickets (drawing Saturday 08:00 UTC)",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 100,
                MaxCount = 3,
                CurrentCount = 0
            });

            // Wondrous Tails
            tasks.Add(new ChecklistTask
            {
                Id = "wondrous_tails",
                Name = "Wondrous Tails",
                Location = "Idyllshire (Khloe Aliapoh)",
                Description = "Complete journal duties for stickers and rewards",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 110
            });

            // Custom Deliveries
            tasks.Add(new ChecklistTask
            {
                Id = "custom_deliveries",
                Name = "Custom Deliveries",
                Location = "Various NPCs",
                Description = "12 deliveries total (max 6 per NPC) for scrips",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 120,
                MaxCount = 12,
                CurrentCount = 0
            });

            // Fashion Report (Theme Tuesday, Judging Friday)
            tasks.Add(new ChecklistTask
            {
                Id = "fashion_report",
                Name = "Fashion Report",
                Location = "Gold Saucer (Masked Rose)",
                Description = "Glamour judging for 60,000+ MGP (judging starts Friday)",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.AutoDetected,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 130
            });

            // Weekly Elite Hunts
            tasks.Add(new ChecklistTask
            {
                Id = "weekly_hunts",
                Name = "Weekly Elite Marks",
                Location = "Hunt Boards",
                Description = "B-rank elite hunt bills for bonus seals",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 140
            });

            // Doman Enclave Reconstruction
            tasks.Add(new ChecklistTask
            {
                Id = "doman_enclave",
                Name = "Doman Enclave Donations",
                Location = "Doman Enclave",
                Description = "Donate items for bonus Gil (up to 40,000/week)",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 150
            });

            // Challenge Log
            tasks.Add(new ChecklistTask
            {
                Id = "challenge_log",
                Name = "Challenge Log",
                Location = "Logs Menu",
                Description = "Various weekly objectives for bonus EXP/Gil/MGP",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 160
            });

            // Faux Hollows / Unreal Trial
            tasks.Add(new ChecklistTask
            {
                Id = "faux_hollows",
                Name = "Faux Hollows",
                Location = "Idyllshire (Faux Commander)",
                Description = "Complete Unreal Trial for Faux Leaves currency",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.Hybrid,
                IsEnabled = true,
                IsCompleted = false,
                SortOrder = 170
            });

            // Masked Carnivale (Blue Mage Weekly)
            tasks.Add(new ChecklistTask
            {
                Id = "masked_carnivale",
                Name = "Masked Carnivale Weekly",
                Location = "Ul'dah (Steps of Thal)",
                Description = "Blue Mage weekly targets for Allied Seals",
                Category = TaskCategory.Weekly,
                Detection = DetectionType.AutoDetected,
                IsEnabled = false, // Disabled by default - Blue Mage specific
                IsCompleted = false,
                SortOrder = 180
            });

            return tasks;
        }

        /// <summary>
        /// Gets all daily task definitions.
        /// </summary>
        public static List<ChecklistTask> GetDailyTasks()
        {
            var allTasks = GetDefaultTasks();
            return allTasks.FindAll(t => t.Category == TaskCategory.Daily);
        }

        /// <summary>
        /// Gets all weekly task definitions.
        /// </summary>
        public static List<ChecklistTask> GetWeeklyTasks()
        {
            var allTasks = GetDefaultTasks();
            return allTasks.FindAll(t => t.Category == TaskCategory.Weekly);
        }

        /// <summary>
        /// Gets a specific task definition by ID.
        /// Returns null if not found.
        /// </summary>
        public static ChecklistTask? GetTaskById(string id)
        {
            var allTasks = GetDefaultTasks();
            return allTasks.Find(t => t.Id == id);
        }

        /// <summary>
        /// Total count of all registered tasks.
        /// </summary>
        public static int TotalTaskCount => GetDefaultTasks().Count;

        /// <summary>
        /// Count of daily tasks.
        /// </summary>
        public static int DailyTaskCount => GetDailyTasks().Count;

        /// <summary>
        /// Count of weekly tasks.
        /// </summary>
        public static int WeeklyTaskCount => GetWeeklyTasks().Count;
    }
}
