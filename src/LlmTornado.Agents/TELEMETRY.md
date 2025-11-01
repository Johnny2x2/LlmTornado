# OpenTelemetry Support in LlmTornado.Agents

LlmTornado.Agents includes built-in support for OpenTelemetry distributed tracing, allowing you to observe and monitor agent execution in production environments.

## Overview

The telemetry implementation is decoupled from the core agents library through the `ITelemetryProvider` interface, allowing you to:

- Use the built-in OpenTelemetry provider
- Implement custom telemetry providers
- Disable telemetry entirely (zero overhead with `NoOpTelemetryProvider`)

## Architecture

```
┌─────────────────────┐
│  TornadoAgent       │
│  Orchestration      │ ─── uses ──► ITelemetryProvider
└─────────────────────┘                    │
                                           │ implementations
                     ┌─────────────────────┼─────────────────────┐
                     │                     │                     │
              NoOpTelemetryProvider  OpenTelemetryProvider  CustomProvider
```

## Quick Start

### 1. Add OpenTelemetry Packages

For .NET 8.0+ projects:

```xml
<PackageReference Include="OpenTelemetry" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.10.0" />
```

### 2. Configure OpenTelemetry

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using LlmTornado.Agents.Telemetry;

// Create ActivitySource
var activitySource = new ActivitySource("MyApp.Agents", "1.0.0");

// Configure TracerProvider
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("MyApp"))
    .AddSource(activitySource.Name)
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();

// Create telemetry provider
var telemetryProvider = new OpenTelemetryProvider(activitySource);
```

### 3. Attach to Agent

```csharp
var agent = new TornadoAgent(api, model, name: "MyAgent")
{
    TelemetryProvider = telemetryProvider
};
```

### 4. Attach to Orchestration

```csharp
var orchestration = new Orchestration<Input, Output>
{
    TelemetryProvider = telemetryProvider
};
```

## What Gets Tracked

### Agent Runs

Each agent run creates a span with the following tags:

| Tag | Description |
|-----|-------------|
| `agent.id` | Unique agent identifier |
| `agent.name` | Human-readable agent name |
| `agent.model` | AI model being used |
| `agent.max_turns` | Maximum conversation turns allowed |
| `agent.single_turn` | Whether single-turn mode is enabled |
| `agent.streaming` | Whether streaming is enabled |
| `agent.total_turns` | Actual number of turns executed |

Events:
- `agent.cancelled` - Agent was cancelled
- `agent.max_turns_reached` - Maximum turns limit reached
- `agent.max_tokens_reached` - Token limit reached

### Agent Turns

Each conversation turn creates a nested span with:

| Tag | Description |
|-----|-------------|
| `turn.number` | Turn number in the conversation |

### Tool Calls

Tool invocations are tracked with:

| Tag | Description |
|-----|-------------|
| `tool.name` | Name of the called tool |
| `tool.type` | Type: `function`, `mcp`, or `agent` |
| `tool.arguments` | JSON arguments passed to tool |
| `tool.permission_granted` | Whether permission was granted |

Events:
- `tool.permission_denied` - Tool permission was denied

### Orchestration

Orchestration operations are tracked with:

| Tag | Description |
|-----|-------------|
| `orchestration.initial_runnable` | Initial runnable ID |
| `orchestration.steps` | Total number of steps executed |
| `orchestration.step` | Current step number |
| `orchestration.active_runnables_count` | Number of active runnables |
| `runnable.id` | Runnable identifier |
| `runnable.type` | Type of runnable |

## Built-in Providers

### NoOpTelemetryProvider

The default provider that does nothing. Zero overhead when telemetry is not needed.

```csharp
// This is the default - no action needed
var agent = new TornadoAgent(api, model);
// agent.TelemetryProvider is NoOpTelemetryProvider.Instance
```

### OpenTelemetryProvider

Full OpenTelemetry implementation using ActivitySource. Only available on .NET 8.0+.

```csharp
var telemetryProvider = new OpenTelemetryProvider("MyApp.Agents", "1.0.0");
agent.TelemetryProvider = telemetryProvider;
```

## Custom Telemetry Provider

Implement the `ITelemetryProvider` interface for custom telemetry:

```csharp
public class CustomTelemetryProvider : ITelemetryProvider
{
    public IDisposable? StartActivity(string name, ActivityKind kind)
    {
        // Your implementation
        Console.WriteLine($"Starting activity: {name}");
        return null;
    }

    public void SetTag(string key, object? value)
    {
        Console.WriteLine($"Tag: {key} = {value}");
    }

    public void AddEvent(string name, Dictionary<string, object?>? tags = null)
    {
        Console.WriteLine($"Event: {name}");
    }

    public void RecordException(Exception exception)
    {
        Console.WriteLine($"Exception: {exception.Message}");
    }

    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        Console.WriteLine($"Status: {status}");
    }
}

// Use it
agent.TelemetryProvider = new CustomTelemetryProvider();
```

## Integration with Observability Platforms

### Grafana + Tempo

See the [Grafana Demo](../LlmTornado.Agents.GrafanaDemo/README.md) for a complete example.

### Jaeger

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://jaeger:4317");
    options.Protocol = OtlpExportProtocol.Grpc;
})
```

### Azure Application Insights

```csharp
.AddAzureMonitorTraceExporter(options =>
{
    options.ConnectionString = "InstrumentationKey=...";
})
```

### AWS X-Ray

```csharp
.AddXRayTraceId()
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:2000");
})
```

## Best Practices

### 1. Use Resource Attributes

```csharp
.SetResourceBuilder(ResourceBuilder.CreateDefault()
    .AddService("MyApp", serviceVersion: "1.0.0")
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = "production",
        ["service.namespace"] = "ai-agents"
    }))
```

### 2. Configure Sampling

For high-traffic applications, use sampling to reduce overhead:

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1)) // Sample 10% of traces
```

### 3. Share ActivitySource

Share the same `ActivitySource` across multiple agents for unified tracing:

```csharp
var activitySource = new ActivitySource("MyApp.Agents", "1.0.0");
var telemetryProvider = new OpenTelemetryProvider(activitySource);

agent1.TelemetryProvider = telemetryProvider;
agent2.TelemetryProvider = telemetryProvider;
orchestration.TelemetryProvider = telemetryProvider;
```

### 4. Add Custom Tags

Add application-specific context:

```csharp
agent.TelemetryProvider.SetTag("user.id", userId);
agent.TelemetryProvider.SetTag("session.id", sessionId);
agent.TelemetryProvider.SetTag("custom.context", contextInfo);
```

## Performance Considerations

- **NoOpTelemetryProvider**: Zero overhead, all methods are no-ops
- **OpenTelemetryProvider**: Minimal overhead (~1-2% CPU), spans are buffered and exported in batches
- **Custom providers**: Performance depends on implementation

## Framework Compatibility

| Framework | OpenTelemetry Support |
|-----------|----------------------|
| .NET 8.0+ | ✅ Full support |
| .NET Standard 2.0 | ⚠️ NoOpTelemetryProvider only |

The telemetry interface uses conditional compilation to maintain compatibility with .NET Standard 2.0, but OpenTelemetry features require .NET 8.0+.

## Examples

### Basic Agent with Telemetry

```csharp
var activitySource = new ActivitySource("MyApp", "1.0.0");
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(activitySource.Name)
    .AddOtlpExporter()
    .Build();

var agent = new TornadoAgent(api, model, "Assistant")
{
    TelemetryProvider = new OpenTelemetryProvider(activitySource)
};

var response = await agent.Run("Hello!");
// Trace will show: TornadoAgent.Run -> TornadoAgent.Turn
```

### Tool Calling with Telemetry

```csharp
string GetWeather(string location) => $"Sunny in {location}";

var agent = new TornadoAgent(api, model, tools: [GetWeather])
{
    TelemetryProvider = telemetryProvider
};

var response = await agent.Run("What's the weather in Paris?");
// Trace will show: TornadoAgent.Run -> TornadoAgent.Turn -> TornadoAgent.ToolCall
```

### Orchestration with Telemetry

```csharp
var orchestration = new Orchestration<string, string>
{
    TelemetryProvider = telemetryProvider
};

orchestration.SetEntryRunnable(initialRunnable);
orchestration.SetRunnableWithResult(resultRunnable);

await orchestration.InvokeAsync("input");
// Trace will show: Orchestration.Invoke -> Orchestration.ProcessTick -> Orchestration.InitializeRunnable
```

## Troubleshooting

### No traces appearing

1. Verify the OTLP endpoint is accessible:
```bash
telnet localhost 4317
```

2. Enable console exporter to see traces locally:
```csharp
.AddConsoleExporter()
```

3. Check the TracerProvider is not disposed too early

### Incomplete traces

Ensure you're using `await` for all async operations - disposed activities won't be exported.

### High overhead

Reduce sampling rate or disable telemetry for specific agents:

```csharp
lowPriorityAgent.TelemetryProvider = NoOpTelemetryProvider.Instance;
```

## Related Documentation

- [Grafana Demo](../LlmTornado.Agents.GrafanaDemo/README.md)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [LlmTornado Documentation](https://llmtornado.ai)
