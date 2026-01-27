using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DailiesChecklist.Models;

namespace DailiesChecklist.Services
{
    /// <summary>
    /// Service responsible for persisting and loading checklist state.
    /// Features:
    /// - JSON serialization via Dalamud's configuration system
    /// - Debounced saves (2-second delay to prevent excessive writes)
    /// - Version-aware loading with migration support
    /// - Graceful handling of missing/corrupt configuration
    /// </summary>
    public class PersistenceService : IDisposable
    {
        // Current configuration version for migration support
        private const int CurrentConfigVersion = 1;

        // Debounce delay in milliseconds
        private const int DebounceDelayMs = 2000;

        private readonly object _saveLock = new object();
        private readonly JsonSerializerOptions _jsonOptions;

        private Timer? _debounceTimer;
        private ChecklistState? _pendingSave;
        private bool _disposed;

        // Dalamud plugin interface for config operations (injected)
        private readonly IDalamudConfigProvider? _configProvider;

        // File path for standalone operation (when not using Dalamud)
        private readonly string? _configFilePath;

        /// <summary>
        /// Event raised when a save operation completes.
        /// </summary>
        public event Action<bool>? OnSaveCompleted;

        /// <summary>
        /// Event raised when a load operation completes.
        /// </summary>
        public event Action<bool>? OnLoadCompleted;

        /// <summary>
        /// Event raised when a configuration migration occurs.
        /// </summary>
        public event Action<int, int>? OnConfigMigrated;

        /// <summary>
        /// Gets the last time the configuration was successfully saved.
        /// </summary>
        public DateTime? LastSaveTime { get; private set; }

        /// <summary>
        /// Gets the last time the configuration was successfully loaded.
        /// </summary>
        public DateTime? LastLoadTime { get; private set; }

        /// <summary>
        /// Gets whether there is a pending save operation.
        /// </summary>
        public bool HasPendingSave => _pendingSave != null;

        /// <summary>
        /// Creates a new PersistenceService using Dalamud's plugin configuration system.
        /// </summary>
        /// <param name="configProvider">The Dalamud configuration provider.</param>
        public PersistenceService(IDalamudConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _jsonOptions = CreateJsonOptions();
        }

        /// <summary>
        /// Creates a new PersistenceService using a direct file path.
        /// Useful for testing or standalone operation.
        /// </summary>
        /// <param name="configFilePath">The path to the configuration file.</param>
        public PersistenceService(string configFilePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                throw new ArgumentException("Config file path cannot be null or empty.", nameof(configFilePath));
            }

            _configFilePath = configFilePath;
            _jsonOptions = CreateJsonOptions();
        }

        /// <summary>
        /// Creates JSON serializer options with appropriate settings.
        /// </summary>
        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        /// <summary>
        /// Saves the state immediately without debouncing.
        /// </summary>
        /// <param name="state">The state to save.</param>
        /// <returns>True if save was successful, false otherwise.</returns>
        public bool SaveImmediate(ChecklistState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            lock (_saveLock)
            {
                // Cancel any pending debounced save
                _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _pendingSave = null;

                return SaveInternal(state);
            }
        }

        /// <summary>
        /// Saves the state with debouncing.
        /// If called multiple times within the debounce window, only the last state is saved.
        /// </summary>
        /// <param name="state">The state to save.</param>
        public void Save(ChecklistState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            lock (_saveLock)
            {
                _pendingSave = state;

                // Reset or create the debounce timer
                if (_debounceTimer == null)
                {
                    _debounceTimer = new Timer(OnDebounceTimerElapsed, null, DebounceDelayMs, Timeout.Infinite);
                }
                else
                {
                    _debounceTimer.Change(DebounceDelayMs, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Called when the debounce timer elapses.
        /// </summary>
        private void OnDebounceTimerElapsed(object? state)
        {
            ChecklistState? stateToSave;

            lock (_saveLock)
            {
                stateToSave = _pendingSave;
                _pendingSave = null;
            }

            if (stateToSave != null)
            {
                SaveInternal(stateToSave);
            }
        }

        /// <summary>
        /// Internal save implementation.
        /// </summary>
        private bool SaveInternal(ChecklistState state)
        {
            try
            {
                // Update metadata
                state.Version = CurrentConfigVersion;
                state.LastSaveTime = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(state, _jsonOptions);

                if (_configProvider != null)
                {
                    // Use Dalamud's configuration system
                    _configProvider.SaveConfiguration(json);
                }
                else if (_configFilePath != null)
                {
                    // Use direct file I/O
                    var directory = Path.GetDirectoryName(_configFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(_configFilePath, json);
                }
                else
                {
                    throw new InvalidOperationException("No configuration target specified.");
                }

                LastSaveTime = DateTime.UtcNow;
                OnSaveCompleted?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error (in a real implementation, use IPluginLog)
                System.Diagnostics.Debug.WriteLine($"[PersistenceService] Save failed: {ex.Message}");
                OnSaveCompleted?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// Loads the state from storage.
        /// Handles missing or corrupt configuration gracefully.
        /// </summary>
        /// <returns>The loaded state, or a default state if loading fails.</returns>
        public ChecklistState Load()
        {
            try
            {
                string? json = null;

                if (_configProvider != null)
                {
                    json = _configProvider.LoadConfiguration();
                }
                else if (_configFilePath != null && File.Exists(_configFilePath))
                {
                    json = File.ReadAllText(_configFilePath);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    // No existing configuration, return defaults
                    var defaultState = CreateDefaultState();
                    LastLoadTime = DateTime.UtcNow;
                    OnLoadCompleted?.Invoke(true);
                    return defaultState;
                }

                var state = JsonSerializer.Deserialize<ChecklistState>(json, _jsonOptions);

                if (state == null)
                {
                    // Deserialization returned null, use defaults
                    var defaultState = CreateDefaultState();
                    LastLoadTime = DateTime.UtcNow;
                    OnLoadCompleted?.Invoke(true);
                    return defaultState;
                }

                // Check for migration
                if (state.Version < CurrentConfigVersion)
                {
                    var oldVersion = state.Version;
                    state = MigrateState(state);
                    OnConfigMigrated?.Invoke(oldVersion, CurrentConfigVersion);
                }

                // Validate and repair the state
                state = ValidateAndRepair(state);

                LastLoadTime = DateTime.UtcNow;
                OnLoadCompleted?.Invoke(true);
                return state;
            }
            catch (JsonException ex)
            {
                // JSON parsing error - configuration is corrupt
                System.Diagnostics.Debug.WriteLine($"[PersistenceService] JSON parse error: {ex.Message}");
                var defaultState = CreateDefaultState();
                OnLoadCompleted?.Invoke(false);
                return defaultState;
            }
            catch (Exception ex)
            {
                // Other error during load
                System.Diagnostics.Debug.WriteLine($"[PersistenceService] Load failed: {ex.Message}");
                var defaultState = CreateDefaultState();
                OnLoadCompleted?.Invoke(false);
                return defaultState;
            }
        }

        /// <summary>
        /// Asynchronously loads the state from storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded state, or a default state if loading fails.</returns>
        public async Task<ChecklistState> LoadAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Load(), cancellationToken);
        }

        /// <summary>
        /// Creates a default state with sensible initial values.
        /// </summary>
        /// <returns>A new ChecklistState with default values.</returns>
        public static ChecklistState CreateDefaultState()
        {
            var now = DateTime.UtcNow;

            return new ChecklistState
            {
                Version = CurrentConfigVersion,
                LastDailyReset = now,
                LastGCReset = now,
                LastWeeklyReset = now,
                LastSaveTime = now,
                Tasks = new System.Collections.Generic.List<ChecklistTask>()
            };
        }

        /// <summary>
        /// Migrates a state from an older version to the current version.
        /// </summary>
        /// <param name="state">The state to migrate.</param>
        /// <returns>The migrated state.</returns>
        private static ChecklistState MigrateState(ChecklistState state)
        {
            // Migration logic for future versions
            // Example:
            // if (state.Version < 2)
            // {
            //     // Migrate from v1 to v2
            //     // Add new fields, transform data, etc.
            // }

            state.Version = CurrentConfigVersion;
            return state;
        }

        /// <summary>
        /// Validates a loaded state and repairs any invalid values.
        /// </summary>
        /// <param name="state">The state to validate.</param>
        /// <returns>The validated and potentially repaired state.</returns>
        private static ChecklistState ValidateAndRepair(ChecklistState state)
        {
            // Ensure tasks list exists
            state.Tasks ??= new System.Collections.Generic.List<ChecklistTask>();

            // Ensure reset timestamps are valid (not in the future)
            var now = DateTime.UtcNow;

            if (state.LastDailyReset > now)
            {
                state.LastDailyReset = now;
            }

            if (state.LastGCReset > now)
            {
                state.LastGCReset = now;
            }

            if (state.LastWeeklyReset > now)
            {
                state.LastWeeklyReset = now;
            }

            // Ensure reset timestamps have UTC kind
            if (state.LastDailyReset.Kind != DateTimeKind.Utc)
            {
                state.LastDailyReset = DateTime.SpecifyKind(state.LastDailyReset, DateTimeKind.Utc);
            }

            if (state.LastGCReset.Kind != DateTimeKind.Utc)
            {
                state.LastGCReset = DateTime.SpecifyKind(state.LastGCReset, DateTimeKind.Utc);
            }

            if (state.LastWeeklyReset.Kind != DateTimeKind.Utc)
            {
                state.LastWeeklyReset = DateTime.SpecifyKind(state.LastWeeklyReset, DateTimeKind.Utc);
            }

            // Validate individual tasks
            foreach (var task in state.Tasks)
            {
                // Ensure CurrentCount is within valid range
                if (task.CurrentCount < 0)
                {
                    task.CurrentCount = 0;
                }

                if (task.MaxCount < 1)
                {
                    task.MaxCount = 1;
                }

                if (task.CurrentCount > task.MaxCount)
                {
                    task.CurrentCount = task.MaxCount;
                }

                // Ensure completion state is consistent
                if (task.MaxCount > 1)
                {
                    task.IsCompleted = task.CurrentCount >= task.MaxCount;
                }
            }

            return state;
        }

        /// <summary>
        /// Flushes any pending saves immediately.
        /// </summary>
        public void Flush()
        {
            ChecklistState? stateToSave;

            lock (_saveLock)
            {
                stateToSave = _pendingSave;
                _pendingSave = null;
                _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }

            if (stateToSave != null)
            {
                SaveInternal(stateToSave);
            }
        }

        /// <summary>
        /// Deletes the configuration file, resetting to defaults.
        /// </summary>
        /// <returns>True if deletion was successful or file didn't exist.</returns>
        public bool DeleteConfiguration()
        {
            try
            {
                if (_configFilePath != null && File.Exists(_configFilePath))
                {
                    File.Delete(_configFilePath);
                }

                // Dalamud's config system doesn't have a delete method,
                // so we save an empty/default state instead
                if (_configProvider != null)
                {
                    var defaultState = CreateDefaultState();
                    SaveImmediate(defaultState);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersistenceService] Delete failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Flush any pending saves before disposing
            Flush();

            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    /// <summary>
    /// Interface for Dalamud plugin configuration operations.
    /// Abstracted for testability and to decouple from Dalamud's concrete implementation.
    /// </summary>
    public interface IDalamudConfigProvider
    {
        /// <summary>
        /// Saves the configuration JSON string.
        /// </summary>
        void SaveConfiguration(string json);

        /// <summary>
        /// Loads the configuration JSON string.
        /// </summary>
        string? LoadConfiguration();
    }
}
