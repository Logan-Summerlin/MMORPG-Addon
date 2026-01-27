using System;
using System.Collections.Generic;

namespace DailiesChecklist.Services
{
    /// <summary>
    /// Defines the different types of reset schedules in FFXIV.
    /// </summary>
    public enum ResetType
    {
        /// <summary>
        /// Daily reset at 15:00 UTC - Roulettes, Beast Tribes, Mini Cactpot, Daily Hunts
        /// </summary>
        Daily,

        /// <summary>
        /// Grand Company reset at 20:00 UTC - Supply/Provisioning, Squadron Training
        /// </summary>
        GrandCompany,

        /// <summary>
        /// Weekly reset on Tuesday at 08:00 UTC - Raids, Challenge Log, Custom Deliveries, Wondrous Tails
        /// </summary>
        Weekly,

        /// <summary>
        /// Jumbo Cactpot drawing on Saturday at 08:00 UTC
        /// </summary>
        JumboCactpot,

        /// <summary>
        /// Fashion Report judging begins Friday at 08:00 UTC
        /// </summary>
        FashionReport
    }

    /// <summary>
    /// Service responsible for calculating and managing FFXIV reset times.
    /// All times are handled in UTC to avoid daylight saving time issues.
    ///
    /// Reset Times Reference:
    /// - Daily Reset: 15:00 UTC (Roulettes, Beast Tribes, Mini Cactpot)
    /// - Grand Company Reset: 20:00 UTC (Supply/Provisioning, Squadron Training)
    /// - Weekly Reset: Tuesday 08:00 UTC (Raids, Challenge Log, Custom Deliveries)
    /// - Jumbo Cactpot Drawing: Saturday 08:00 UTC
    /// - Fashion Report Judging: Friday 08:00 UTC
    /// </summary>
    public class ResetService : IDisposable
    {
        // Reset time constants (all in UTC)
        private const int DailyResetHour = 15;      // 15:00 UTC
        private const int DailyResetMinute = 0;

        private const int GCResetHour = 20;         // 20:00 UTC
        private const int GCResetMinute = 0;

        private const int WeeklyResetHour = 8;      // 08:00 UTC
        private const int WeeklyResetMinute = 0;
        private const DayOfWeek WeeklyResetDay = DayOfWeek.Tuesday;

        private const int JumboCactpotHour = 8;     // 08:00 UTC
        private const int JumboCactpotMinute = 0;
        private const DayOfWeek JumboCactpotDay = DayOfWeek.Saturday;

        private const int FashionReportHour = 8;    // 08:00 UTC
        private const int FashionReportMinute = 0;
        private const DayOfWeek FashionReportDay = DayOfWeek.Friday;

        private bool _disposed;

        /// <summary>
        /// Gets the next daily reset time (15:00 UTC).
        /// </summary>
        /// <returns>DateTime of the next daily reset in UTC.</returns>
        public DateTime GetNextDailyReset()
        {
            return GetNextOccurrence(DailyResetHour, DailyResetMinute);
        }

        /// <summary>
        /// Gets the next weekly reset time (Tuesday 08:00 UTC).
        /// </summary>
        /// <returns>DateTime of the next weekly reset in UTC.</returns>
        public DateTime GetNextWeeklyReset()
        {
            return GetNextWeekdayOccurrence(WeeklyResetDay, WeeklyResetHour, WeeklyResetMinute);
        }

        /// <summary>
        /// Gets the next Grand Company reset time (20:00 UTC).
        /// </summary>
        /// <returns>DateTime of the next GC reset in UTC.</returns>
        public DateTime GetNextGCReset()
        {
            return GetNextOccurrence(GCResetHour, GCResetMinute);
        }

        /// <summary>
        /// Gets the next Jumbo Cactpot drawing time (Saturday 08:00 UTC).
        /// </summary>
        /// <returns>DateTime of the next Jumbo Cactpot drawing in UTC.</returns>
        public DateTime GetNextJumboCactpotReset()
        {
            return GetNextWeekdayOccurrence(JumboCactpotDay, JumboCactpotHour, JumboCactpotMinute);
        }

        /// <summary>
        /// Gets the next Fashion Report judging time (Friday 08:00 UTC).
        /// </summary>
        /// <returns>DateTime of the next Fashion Report judging in UTC.</returns>
        public DateTime GetNextFashionReportReset()
        {
            return GetNextWeekdayOccurrence(FashionReportDay, FashionReportHour, FashionReportMinute);
        }

        /// <summary>
        /// Gets the next reset time for any reset type.
        /// </summary>
        /// <param name="resetType">The type of reset to query.</param>
        /// <returns>DateTime of the next reset in UTC.</returns>
        public DateTime GetNextReset(ResetType resetType)
        {
            return resetType switch
            {
                ResetType.Daily => GetNextDailyReset(),
                ResetType.GrandCompany => GetNextGCReset(),
                ResetType.Weekly => GetNextWeeklyReset(),
                ResetType.JumboCactpot => GetNextJumboCactpotReset(),
                ResetType.FashionReport => GetNextFashionReportReset(),
                _ => throw new ArgumentOutOfRangeException(nameof(resetType), resetType, "Unknown reset type")
            };
        }

        /// <summary>
        /// Gets the previous reset time for a given reset type.
        /// </summary>
        /// <param name="resetType">The type of reset to query.</param>
        /// <returns>DateTime of the most recent past reset in UTC.</returns>
        public DateTime GetLastReset(ResetType resetType)
        {
            var nextReset = GetNextReset(resetType);

            return resetType switch
            {
                ResetType.Daily => nextReset.AddDays(-1),
                ResetType.GrandCompany => nextReset.AddDays(-1),
                ResetType.Weekly => nextReset.AddDays(-7),
                ResetType.JumboCactpot => nextReset.AddDays(-7),
                ResetType.FashionReport => nextReset.AddDays(-7),
                _ => throw new ArgumentOutOfRangeException(nameof(resetType), resetType, "Unknown reset type")
            };
        }

        /// <summary>
        /// Gets the time elapsed since the last reset of the specified type.
        /// </summary>
        /// <param name="resetType">The type of reset to query.</param>
        /// <returns>TimeSpan since the last reset.</returns>
        public TimeSpan GetTimeSinceLastReset(ResetType resetType)
        {
            var lastReset = GetLastReset(resetType);
            return DateTime.UtcNow - lastReset;
        }

        /// <summary>
        /// Gets the time remaining until the next reset of the specified type.
        /// </summary>
        /// <param name="resetType">The type of reset to query.</param>
        /// <returns>TimeSpan until the next reset.</returns>
        public TimeSpan GetTimeUntilNextReset(ResetType resetType)
        {
            var nextReset = GetNextReset(resetType);
            return nextReset - DateTime.UtcNow;
        }

        /// <summary>
        /// Formats the time until the next reset as a human-readable string.
        /// </summary>
        /// <param name="resetType">The type of reset to query.</param>
        /// <returns>Formatted string like "2h 30m" or "1d 5h".</returns>
        public string GetFormattedTimeUntilReset(ResetType resetType)
        {
            var timeRemaining = GetTimeUntilNextReset(resetType);
            return FormatTimeSpan(timeRemaining);
        }

        /// <summary>
        /// Checks whether a reset has occurred since a given timestamp.
        /// </summary>
        /// <param name="resetType">The type of reset to check.</param>
        /// <param name="lastChecked">The timestamp to compare against (UTC).</param>
        /// <returns>True if a reset occurred between lastChecked and now.</returns>
        public bool HasResetOccurredSince(ResetType resetType, DateTime lastChecked)
        {
            if (lastChecked.Kind != DateTimeKind.Utc)
            {
                lastChecked = DateTime.SpecifyKind(lastChecked, DateTimeKind.Utc);
            }

            var lastReset = GetLastReset(resetType);
            return lastReset > lastChecked;
        }

        /// <summary>
        /// Checks the checklist state and applies any pending resets.
        /// Returns information about which resets were applied.
        /// </summary>
        /// <param name="state">The checklist state to check and potentially modify.</param>
        /// <returns>A dictionary of reset types and whether they were applied.</returns>
        public Dictionary<ResetType, bool> CheckAndApplyResets(IChecklistState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var appliedResets = new Dictionary<ResetType, bool>();

            // Check daily reset
            if (HasResetOccurredSince(ResetType.Daily, state.LastDailyReset))
            {
                state.ResetDailyTasks();
                state.LastDailyReset = GetLastReset(ResetType.Daily);
                appliedResets[ResetType.Daily] = true;
            }
            else
            {
                appliedResets[ResetType.Daily] = false;
            }

            // Check Grand Company reset (separate from daily!)
            if (HasResetOccurredSince(ResetType.GrandCompany, state.LastGCReset))
            {
                state.ResetGrandCompanyTasks();
                state.LastGCReset = GetLastReset(ResetType.GrandCompany);
                appliedResets[ResetType.GrandCompany] = true;
            }
            else
            {
                appliedResets[ResetType.GrandCompany] = false;
            }

            // Check weekly reset
            if (HasResetOccurredSince(ResetType.Weekly, state.LastWeeklyReset))
            {
                state.ResetWeeklyTasks();
                state.LastWeeklyReset = GetLastReset(ResetType.Weekly);
                appliedResets[ResetType.Weekly] = true;
            }
            else
            {
                appliedResets[ResetType.Weekly] = false;
            }

            // Check Jumbo Cactpot reset (uses same timestamp as weekly for simplicity,
            // but could be tracked separately if needed)
            if (HasResetOccurredSince(ResetType.JumboCactpot, state.LastWeeklyReset))
            {
                // Jumbo Cactpot is typically included in weekly tasks
                appliedResets[ResetType.JumboCactpot] = true;
            }
            else
            {
                appliedResets[ResetType.JumboCactpot] = false;
            }

            return appliedResets;
        }

        /// <summary>
        /// Calculates the next occurrence of a specific daily time.
        /// </summary>
        /// <param name="hour">Hour in UTC (0-23).</param>
        /// <param name="minute">Minute (0-59).</param>
        /// <returns>The next DateTime when the specified time occurs.</returns>
        private static DateTime GetNextOccurrence(int hour, int minute)
        {
            var now = DateTime.UtcNow;
            var todayTarget = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Utc);

            // If we haven't passed today's target time, return today
            // If we have passed it, return tomorrow
            if (now < todayTarget)
            {
                return todayTarget;
            }
            else
            {
                return todayTarget.AddDays(1);
            }
        }

        /// <summary>
        /// Calculates the next occurrence of a specific weekday and time.
        /// </summary>
        /// <param name="targetDay">The day of the week.</param>
        /// <param name="hour">Hour in UTC (0-23).</param>
        /// <param name="minute">Minute (0-59).</param>
        /// <returns>The next DateTime when the specified weekday and time occurs.</returns>
        private static DateTime GetNextWeekdayOccurrence(DayOfWeek targetDay, int hour, int minute)
        {
            var now = DateTime.UtcNow;

            // Calculate days until the target day
            var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

            // Calculate the target time on that day
            var targetDate = now.Date.AddDays(daysUntilTarget);
            var targetDateTime = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day,
                                               hour, minute, 0, DateTimeKind.Utc);

            // If it's the same day but we've already passed the time, go to next week
            if (daysUntilTarget == 0 && now >= targetDateTime)
            {
                targetDateTime = targetDateTime.AddDays(7);
            }

            return targetDateTime;
        }

        /// <summary>
        /// Formats a TimeSpan into a human-readable string.
        /// </summary>
        /// <param name="timeSpan">The TimeSpan to format.</param>
        /// <returns>Formatted string like "2h 30m" or "1d 5h".</returns>
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
            {
                return "Now";
            }

            if (timeSpan.TotalDays >= 1)
            {
                var days = (int)timeSpan.TotalDays;
                var hours = timeSpan.Hours;
                return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
            }

            if (timeSpan.TotalHours >= 1)
            {
                var hours = (int)timeSpan.TotalHours;
                var minutes = timeSpan.Minutes;
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                var minutes = (int)timeSpan.TotalMinutes;
                var seconds = timeSpan.Seconds;
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
            }

            return $"{timeSpan.Seconds}s";
        }

        /// <summary>
        /// Validates that a DateTime is in UTC.
        /// </summary>
        /// <param name="dateTime">The DateTime to validate.</param>
        /// <param name="paramName">Parameter name for exception message.</param>
        /// <returns>The validated UTC DateTime.</returns>
        private static DateTime EnsureUtc(DateTime dateTime, string paramName)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume unspecified times are UTC
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            // Convert local time to UTC
            return dateTime.ToUniversalTime();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            // No resources to dispose currently, but implementing IDisposable
            // for future extensibility (e.g., if timers are added)
        }
    }

    /// <summary>
    /// Interface for the checklist state that the ResetService operates on.
    /// This allows for loose coupling with the actual ChecklistState implementation.
    /// </summary>
    public interface IChecklistState
    {
        /// <summary>
        /// The timestamp of the last daily reset that was applied.
        /// </summary>
        DateTime LastDailyReset { get; set; }

        /// <summary>
        /// The timestamp of the last Grand Company reset that was applied.
        /// </summary>
        DateTime LastGCReset { get; set; }

        /// <summary>
        /// The timestamp of the last weekly reset that was applied.
        /// </summary>
        DateTime LastWeeklyReset { get; set; }

        /// <summary>
        /// Resets all daily tasks to their uncompleted state.
        /// </summary>
        void ResetDailyTasks();

        /// <summary>
        /// Resets all Grand Company tasks to their uncompleted state.
        /// </summary>
        void ResetGrandCompanyTasks();

        /// <summary>
        /// Resets all weekly tasks to their uncompleted state.
        /// </summary>
        void ResetWeeklyTasks();
    }
}
