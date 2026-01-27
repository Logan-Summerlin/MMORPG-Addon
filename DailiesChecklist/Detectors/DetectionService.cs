using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;

namespace DailiesChecklist.Detectors;

/// <summary>
/// Service that orchestrates multiple task detectors, aggregating their state
/// and providing a unified interface for the plugin to query task completion.
/// </summary>
/// <remarks>
/// The DetectionService is responsible for:
/// - Managing detector registration and lifecycle
/// - Mapping task IDs to their responsible detectors
/// - Aggregating state change events from all detectors
/// - Providing feature flags to enable/disable individual detectors
/// - Ensuring graceful degradation when detectors fail
/// </remarks>
public sealed class DetectionService : IDisposable
{
    private readonly IPluginLog _log;
    private readonly Dictionary<Type, ITaskDetector> _detectors;
    private readonly Dictionary<string, ITaskDetector> _taskIdToDetector;
    private readonly Dictionary<Type, bool> _detectorFeatureFlags;
    private readonly Dictionary<Type, Action<string, bool>> _detectorEventHandlers;
    private readonly object _lock = new();
    private bool _isDisposed;

    /// <summary>
    /// Event fired when any detector reports a task state change.
    /// </summary>
    /// <remarks>
    /// Parameters:
    /// - taskId: The ID of the task whose state changed
    /// - isCompleted: true if the task is now complete, false otherwise
    /// - detectorType: The type of detector that reported the change
    /// </remarks>
    public event Action<string, bool, Type>? OnTaskStateChanged;

    /// <summary>
    /// Event fired when a detector encounters an error.
    /// </summary>
    /// <remarks>
    /// Parameters:
    /// - detectorType: The type of detector that encountered the error
    /// - exception: The exception that was caught
    /// </remarks>
    public event Action<Type, Exception>? OnDetectorError;

    /// <summary>
    /// Creates a new DetectionService instance.
    /// </summary>
    /// <param name="log">The plugin log service for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if log is null.</exception>
    public DetectionService(IPluginLog log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _detectors = new Dictionary<Type, ITaskDetector>();
        _taskIdToDetector = new Dictionary<string, ITaskDetector>(StringComparer.OrdinalIgnoreCase);
        _detectorFeatureFlags = new Dictionary<Type, bool>();
        _detectorEventHandlers = new Dictionary<Type, Action<string, bool>>();
    }

    /// <summary>
    /// Registers and initializes a new detector instance.
    /// </summary>
    /// <typeparam name="T">The type of detector to add.</typeparam>
    /// <param name="detector">The detector instance to register.</param>
    /// <param name="enabled">Whether the detector should be enabled by default.</param>
    /// <returns>true if the detector was added successfully, false if it was already registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown if detector is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public bool AddDetector<T>(T detector, bool enabled = true) where T : class, ITaskDetector
    {
        ThrowIfDisposed();

        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        var detectorType = typeof(T);

        lock (_lock)
        {
            if (_detectors.ContainsKey(detectorType))
            {
                _log.Warning("Detector of type {DetectorType} is already registered.", detectorType.Name);
                return false;
            }

            try
            {
                // Register the detector
                _detectors[detectorType] = detector;
                _detectorFeatureFlags[detectorType] = enabled;
                detector.IsEnabled = enabled;

                // Map task IDs to this detector
                foreach (var taskId in detector.SupportedTaskIds)
                {
                    if (_taskIdToDetector.ContainsKey(taskId))
                    {
                        _log.Warning(
                            "Task ID '{TaskId}' is already registered to another detector. Skipping.",
                            taskId);
                        continue;
                    }

                    _taskIdToDetector[taskId] = detector;
                }

                // Create and store the event handler so we can unsubscribe later
                Action<string, bool> eventHandler = (taskId, isCompleted) =>
                    HandleDetectorStateChange(detectorType, taskId, isCompleted);
                _detectorEventHandlers[detectorType] = eventHandler;

                // Subscribe to state change events
                detector.OnTaskStateChanged += eventHandler;

                // Initialize the detector
                detector.Initialize();

                _log.Information(
                    "Registered detector {DetectorType} for {TaskCount} tasks.",
                    detectorType.Name,
                    detector.SupportedTaskIds.Length);

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to initialize detector {DetectorType}.", detectorType.Name);

                // Clean up on failure
                if (_detectorEventHandlers.TryGetValue(detectorType, out var handler))
                {
                    detector.OnTaskStateChanged -= handler;
                    _detectorEventHandlers.Remove(detectorType);
                }
                _detectors.Remove(detectorType);
                _detectorFeatureFlags.Remove(detectorType);
                foreach (var taskId in detector.SupportedTaskIds)
                {
                    _taskIdToDetector.Remove(taskId);
                }

                OnDetectorError?.Invoke(detectorType, ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Removes and disposes a registered detector.
    /// </summary>
    /// <typeparam name="T">The type of detector to remove.</typeparam>
    /// <returns>true if the detector was removed, false if it was not registered.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public bool RemoveDetector<T>() where T : class, ITaskDetector
    {
        ThrowIfDisposed();

        var detectorType = typeof(T);

        lock (_lock)
        {
            if (!_detectors.TryGetValue(detectorType, out var detector))
            {
                _log.Warning("Detector of type {DetectorType} is not registered.", detectorType.Name);
                return false;
            }

            try
            {
                // Remove task ID mappings
                foreach (var taskId in detector.SupportedTaskIds)
                {
                    _taskIdToDetector.Remove(taskId);
                }

                // Unsubscribe from the event handler before disposing
                if (_detectorEventHandlers.TryGetValue(detectorType, out var handler))
                {
                    detector.OnTaskStateChanged -= handler;
                    _detectorEventHandlers.Remove(detectorType);
                }

                // Remove from collections
                _detectors.Remove(detectorType);
                _detectorFeatureFlags.Remove(detectorType);

                // Dispose the detector
                detector.Dispose();

                _log.Information("Removed detector {DetectorType}.", detectorType.Name);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error disposing detector {DetectorType}.", detectorType.Name);
                OnDetectorError?.Invoke(detectorType, ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the detector responsible for a specific task ID.
    /// </summary>
    /// <param name="taskId">The task ID to look up.</param>
    /// <returns>The detector for the task, or null if no detector handles this task.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public ITaskDetector? GetDetector(string taskId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        lock (_lock)
        {
            return _taskIdToDetector.TryGetValue(taskId, out var detector) ? detector : null;
        }
    }

    /// <summary>
    /// Gets a registered detector by type.
    /// </summary>
    /// <typeparam name="T">The type of detector to retrieve.</typeparam>
    /// <returns>The detector instance, or null if not registered.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public T? GetDetector<T>() where T : class, ITaskDetector
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _detectors.TryGetValue(typeof(T), out var detector) ? detector as T : null;
        }
    }

    /// <summary>
    /// Gets the completion state for a task from its registered detector.
    /// </summary>
    /// <param name="taskId">The task ID to query.</param>
    /// <returns>
    /// true if complete, false if incomplete, null if unknown or no detector registered.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public bool? GetTaskCompletionState(string taskId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        ITaskDetector? detector;
        lock (_lock)
        {
            if (!_taskIdToDetector.TryGetValue(taskId, out detector))
                return null;

            if (!detector.IsEnabled)
                return null;
        }

        try
        {
            return detector.GetCompletionState(taskId);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error getting completion state for task '{TaskId}'.", taskId);
            OnDetectorError?.Invoke(detector.GetType(), ex);
            return null;
        }
    }

    /// <summary>
    /// Sets the enabled state (feature flag) for a detector type.
    /// </summary>
    /// <typeparam name="T">The type of detector to configure.</typeparam>
    /// <param name="enabled">Whether the detector should be enabled.</param>
    /// <returns>true if the detector was found and updated, false otherwise.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public bool SetDetectorEnabled<T>(bool enabled) where T : class, ITaskDetector
    {
        ThrowIfDisposed();

        var detectorType = typeof(T);

        lock (_lock)
        {
            if (!_detectors.TryGetValue(detectorType, out var detector))
            {
                _log.Warning("Cannot set enabled state: detector {DetectorType} not registered.", detectorType.Name);
                return false;
            }

            _detectorFeatureFlags[detectorType] = enabled;
            detector.IsEnabled = enabled;

            _log.Information(
                "Detector {DetectorType} {State}.",
                detectorType.Name,
                enabled ? "enabled" : "disabled");

            return true;
        }
    }

    /// <summary>
    /// Gets whether a detector type is enabled.
    /// </summary>
    /// <typeparam name="T">The type of detector to check.</typeparam>
    /// <returns>true if enabled, false if disabled or not registered.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public bool IsDetectorEnabled<T>() where T : class, ITaskDetector
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _detectorFeatureFlags.TryGetValue(typeof(T), out var enabled) && enabled;
        }
    }

    /// <summary>
    /// Gets all registered detector types.
    /// </summary>
    /// <returns>An array of registered detector types.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public Type[] GetRegisteredDetectorTypes()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _detectors.Keys.ToArray();
        }
    }

    /// <summary>
    /// Gets all task IDs that have registered detectors.
    /// </summary>
    /// <returns>An array of task IDs with auto-detection support.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public string[] GetDetectableTaskIds()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return _taskIdToDetector.Keys.ToArray();
        }
    }

    /// <summary>
    /// Handles state change events from detectors, with error handling.
    /// </summary>
    private void HandleDetectorStateChange(Type detectorType, string taskId, bool isCompleted)
    {
        try
        {
            // Check if detector is still enabled
            lock (_lock)
            {
                if (!_detectorFeatureFlags.TryGetValue(detectorType, out var enabled) || !enabled)
                {
                    _log.Debug(
                        "Ignoring state change from disabled detector {DetectorType}.",
                        detectorType.Name);
                    return;
                }
            }

            _log.Debug(
                "Task '{TaskId}' state changed to {State} (via {DetectorType}).",
                taskId,
                isCompleted ? "complete" : "incomplete",
                detectorType.Name);

            OnTaskStateChanged?.Invoke(taskId, isCompleted, detectorType);
        }
        catch (Exception ex)
        {
            _log.Error(
                ex,
                "Error handling state change for task '{TaskId}' from detector {DetectorType}.",
                taskId,
                detectorType.Name);
        }
    }

    /// <summary>
    /// Throws if this service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DetectionService));
    }

    /// <summary>
    /// Disposes all registered detectors and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        lock (_lock)
        {
            _isDisposed = true;

            foreach (var kvp in _detectors)
            {
                try
                {
                    // Unsubscribe from event handler before disposing
                    if (_detectorEventHandlers.TryGetValue(kvp.Key, out var handler))
                    {
                        kvp.Value.OnTaskStateChanged -= handler;
                    }

                    kvp.Value.Dispose();
                    _log.Debug("Disposed detector {DetectorType}.", kvp.Key.Name);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error disposing detector {DetectorType}.", kvp.Key.Name);
                }
            }

            _detectors.Clear();
            _taskIdToDetector.Clear();
            _detectorFeatureFlags.Clear();
            _detectorEventHandlers.Clear();

            _log.Information("DetectionService disposed.");
        }
    }
}
