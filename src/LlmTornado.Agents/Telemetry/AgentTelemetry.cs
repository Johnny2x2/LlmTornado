using System.Diagnostics;

namespace LlmTornado.Agents.Telemetry;

/// <summary>
/// Provides OpenTelemetry instrumentation for LlmTornado.Agents
/// </summary>
public static class AgentTelemetry
{
    /// <summary>
    /// The name of the activity source for LlmTornado.Agents
    /// </summary>
    public const string ActivitySourceName = "LlmTornado.Agents";
    
    /// <summary>
    /// The version of the activity source
    /// </summary>
    public const string ActivitySourceVersion = "1.0.9";

    /// <summary>
    /// The activity source for LlmTornado.Agents
    /// </summary>
    public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName, ActivitySourceVersion);
}
