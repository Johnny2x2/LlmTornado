using System;
using System.Collections.Generic;
#if MODERN
using System.Diagnostics;
#endif

namespace LlmTornado.Agents.Telemetry;

/// <summary>
/// Interface for telemetry providers that can be used to instrument LlmTornado.Agents.
/// This abstraction allows for different telemetry implementations to be plugged in.
/// </summary>
public interface ITelemetryProvider
{
#if MODERN
    /// <summary>
    /// Starts a new activity/span for tracking an operation.
    /// </summary>
    /// <param name="name">The name of the activity</param>
    /// <param name="kind">The kind of activity (Server, Client, Internal, etc.)</param>
    /// <returns>An IDisposable that represents the activity and should be disposed when the activity completes</returns>
    IDisposable? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Sets a tag/attribute on the current activity.
    /// </summary>
    /// <param name="key">The tag key</param>
    /// <param name="value">The tag value</param>
    void SetTag(string key, object? value);

    /// <summary>
    /// Records an event on the current activity.
    /// </summary>
    /// <param name="name">The event name</param>
    /// <param name="tags">Optional tags/attributes for the event</param>
    void AddEvent(string name, Dictionary<string, object?>? tags = null);

    /// <summary>
    /// Records an exception on the current activity.
    /// </summary>
    /// <param name="exception">The exception to record</param>
    void RecordException(Exception exception);

    /// <summary>
    /// Sets the status of the current activity.
    /// </summary>
    /// <param name="status">The status (Ok, Error, etc.)</param>
    /// <param name="description">Optional description of the status</param>
    void SetStatus(ActivityStatusCode status, string? description = null);
#else
    // For netstandard2.0, provide stub methods
    IDisposable? StartActivity(string name, int kind = 0);
    void SetTag(string key, object? value);
    void AddEvent(string name, Dictionary<string, object?>? tags = null);
    void RecordException(Exception exception);
    void SetStatus(int status, string? description = null);
#endif
}
