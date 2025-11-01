using System;
using System.Collections.Generic;
#if MODERN
using System.Diagnostics;
#endif

namespace LlmTornado.Agents.Telemetry;

/// <summary>
/// A no-op telemetry provider that does nothing. Used as the default when telemetry is not enabled.
/// </summary>
public class NoOpTelemetryProvider : ITelemetryProvider
{
    /// <summary>
    /// Singleton instance of the NoOpTelemetryProvider.
    /// </summary>
    public static readonly NoOpTelemetryProvider Instance = new NoOpTelemetryProvider();

    private NoOpTelemetryProvider() { }

#if MODERN
    /// <inheritdoc />
    public IDisposable? StartActivity(string name, ActivityKind kind = ActivityKind.Internal) => null;

    /// <inheritdoc />
    public void SetTag(string key, object? value) { }

    /// <inheritdoc />
    public void AddEvent(string name, Dictionary<string, object?>? tags = null) { }

    /// <inheritdoc />
    public void RecordException(Exception exception) { }

    /// <inheritdoc />
    public void SetStatus(ActivityStatusCode status, string? description = null) { }
#else
    public IDisposable? StartActivity(string name, int kind = 0) => null;
    public void SetTag(string key, object? value) { }
    public void AddEvent(string name, Dictionary<string, object?>? tags = null) { }
    public void RecordException(Exception exception) { }
    public void SetStatus(int status, string? description = null) { }
#endif
}
