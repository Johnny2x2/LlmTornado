using System;
using System.Collections.Generic;
#if MODERN
using System.Diagnostics;
#endif

namespace LlmTornado.Agents.Telemetry;

#if MODERN
/// <summary>
/// OpenTelemetry-based telemetry provider that uses ActivitySource for distributed tracing.
/// Only available for .NET 8.0+
/// </summary>
public class OpenTelemetryProvider : ITelemetryProvider
{
    private readonly ActivitySource _activitySource;
    private Activity? _currentActivity;

    /// <summary>
    /// Creates a new OpenTelemetryProvider with the specified activity source.
    /// </summary>
    /// <param name="activitySource">The ActivitySource to use for creating activities</param>
    public OpenTelemetryProvider(ActivitySource activitySource)
    {
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    /// <summary>
    /// Creates a new OpenTelemetryProvider with the specified source name and version.
    /// </summary>
    /// <param name="sourceName">The name of the activity source</param>
    /// <param name="version">The version of the activity source</param>
    public OpenTelemetryProvider(string sourceName, string? version = null)
    {
        _activitySource = new ActivitySource(sourceName, version);
    }

    /// <inheritdoc />
    public IDisposable? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        _currentActivity = _activitySource.StartActivity(name, kind);
        return _currentActivity;
    }

    /// <inheritdoc />
    public void SetTag(string key, object? value)
    {
        _currentActivity?.SetTag(key, value);
    }

    /// <inheritdoc />
    public void AddEvent(string name, Dictionary<string, object?>? tags = null)
    {
        if (_currentActivity == null) return;

        if (tags == null || tags.Count == 0)
        {
            _currentActivity.AddEvent(new ActivityEvent(name));
        }
        else
        {
            var activityTags = new ActivityTagsCollection();
            foreach (var tag in tags)
            {
                activityTags[tag.Key] = tag.Value;
            }
            _currentActivity.AddEvent(new ActivityEvent(name, tags: activityTags));
        }
    }

    /// <inheritdoc />
    public void RecordException(Exception exception)
    {
        if (_currentActivity == null) return;

        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        };

        _currentActivity.AddEvent(new ActivityEvent("exception", tags: tags));
        _currentActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    /// <inheritdoc />
    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        _currentActivity?.SetStatus(status, description);
    }

    /// <summary>
    /// Gets the underlying ActivitySource.
    /// </summary>
    public ActivitySource ActivitySource => _activitySource;
}
#endif
