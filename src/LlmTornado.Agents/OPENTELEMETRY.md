# OpenTelemetry Integration for LlmTornado.Agents

LlmTornado.Agents now includes built-in OpenTelemetry instrumentation for comprehensive observability of your AI agent workflows.

## Overview

The OpenTelemetry integration automatically traces:
- **TornadoAgent execution**: Complete agent lifecycle including model selection, turns, and completion status
- **Orchestration operations**: Full orchestration workflows with step-by-step tracking
- **Process ticks**: Individual orchestration tick execution with runnable count and status
- **Error tracking**: Automatic error capture and status reporting

## Features

- 🔍 **Automatic instrumentation** - No code changes needed in your agent logic
- 📊 **Rich metadata** - Captures agent names, model names, turn counts, and more
- 🎯 **Distributed tracing** - Track multi-agent workflows across complex orchestrations
- 🚀 **Performance insights** - Duration tracking for optimization opportunities
- 🔧 **OTLP support** - Export to any OpenTelemetry-compatible backend (Grafana, Jaeger, Zipkin, etc.)

## Quick Start

### 1. Add NuGet Packages

The core telemetry is already included in `LlmTornado.Agents`. To export traces, add an exporter:

```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
```

### 2. Configure OpenTelemetry

Add this to your application startup:

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using LlmTornado.Agents.Telemetry;

// Configure telemetry
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("YourServiceName", serviceVersion: "1.0.0"))
    .AddSource(AgentTelemetry.ActivitySourceName)  // This is the key line!
    .AddOtlpExporter()  // Export to OTLP endpoint
    .Build();

// Your agent code - telemetry is automatic!
var agent = new TornadoAgent(...);
await agent.Run("Hello");
```

That's it! All agent and orchestration operations will be automatically traced.

### 3. View Traces

Export to your preferred backend:
- **Grafana + Tempo**: See the demo in `src/LlmTornado.Agents.OpenTelemetryDemo`
- **Jaeger**: `docker run -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one`
- **Azure Application Insights**: Use `OpenTelemetry.Exporter.AzureMonitor`
- **AWS X-Ray**: Use `OpenTelemetry.Exporter.OpenTelemetryProtocol` with AWS settings

## What Gets Traced?

### TornadoAgent.Run

Traces the complete execution of an agent with these tags:
- `agent.name` - Name of the agent
- `agent.id` - Unique agent instance ID
- `agent.model` - Model being used
- `agent.max_turns` - Maximum turns configured
- `agent.streaming` - Whether streaming is enabled
- `agent.single_turn` - Single turn mode flag
- `agent.turns_executed` - Actual number of turns executed
- `agent.completed` - Whether the agent completed successfully

### Orchestration.InvokeAsync

Traces the full orchestration lifecycle:
- `orchestration.type` - Type of orchestration
- `orchestration.has_input` - Whether input was provided
- `orchestration.completed` - Completion status

### Orchestration.RunToCompletion

Tracks the orchestration run with:
- `orchestration.steps_executed` - Number of steps executed
- `orchestration.is_completed` - Whether all steps completed
- `orchestration.is_cancelled` - Whether the run was cancelled

### Orchestration.ProcessTick

Individual orchestration tick with:
- `orchestration.tick.step` - Current step number
- `orchestration.tick.runnable_count` - Number of runnables in this tick
- `orchestration.tick.no_processes` - Flag when no processes are available

## Demo Project

A complete demo with Grafana + Tempo visualization is available at:
`src/LlmTornado.Agents.OpenTelemetryDemo`

Features:
- Console output exporter for immediate visibility
- OTLP exporter for Grafana/Tempo
- Docker Compose setup for Grafana + Tempo
- Mock mode (no API key required)
- Real agent execution mode

See the demo's [README](../LlmTornado.Agents.OpenTelemetryDemo/README.md) for details.

## Example Trace Hierarchy

When you run an orchestration, you'll see a trace hierarchy like:

```
TornadoAgent.Run (if called from orchestration)
│
Orchestration.InvokeAsync
├── orchestration.type: SimpleOrchestrationConfig
├── Orchestration.RunToCompletion
│   ├── orchestration.steps_executed: 3
│   ├── Orchestration.ProcessTick (Step 1)
│   │   ├── orchestration.tick.step: 1
│   │   ├── orchestration.tick.runnable_count: 1
│   │   └── TornadoAgent.Run (if runnable calls agent)
│   ├── Orchestration.ProcessTick (Step 2)
│   │   ├── orchestration.tick.step: 2
│   │   └── orchestration.tick.runnable_count: 2
│   └── Orchestration.ProcessTick (Step 3)
│       ├── orchestration.tick.step: 3
│       └── orchestration.tick.runnable_count: 1
└── orchestration.completed: true
```

## Configuration

### OTLP Endpoint

By default, OTLP exporter connects to `http://localhost:4317`. Override with:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
```

Or in code:

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://your-collector:4317");
})
```

### Multiple Exporters

You can export to multiple backends simultaneously:

```csharp
.AddConsoleExporter()  // Immediate console visibility
.AddOtlpExporter()     // Production backend
.AddZipkinExporter()   // Secondary analytics
```

### Sampling

Control which traces are recorded:

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1))  // Sample 10%
```

## Integration with ASP.NET Core

For web applications, use the hosted service:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(AgentTelemetry.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

## Best Practices

1. **Service Naming**: Use descriptive service names to identify your application in traces
2. **Resource Attributes**: Add environment, version, and deployment information
3. **Sampling**: Use sampling in high-volume production environments
4. **Error Handling**: Traces automatically capture exceptions - check the `ActivityStatusCode`
5. **Performance**: OpenTelemetry has minimal overhead when properly configured

## Performance Considerations

OpenTelemetry instrumentation in LlmTornado.Agents is designed to be lightweight:

- Activities are only created when a listener is registered
- Tags are lazy-evaluated
- No allocation when tracing is disabled
- Minimal overhead when enabled (~microseconds per operation)

## Troubleshooting

### Traces not appearing?

1. Verify the `ActivitySource` is added:
   ```csharp
   .AddSource(AgentTelemetry.ActivitySourceName)
   ```

2. Check the exporter endpoint is correct

3. Ensure the exporter is built:
   ```csharp
   using var tracerProvider = Sdk.CreateTracerProviderBuilder()
       // ... configuration ...
       .Build();  // Don't forget this!
   ```

### Want more detail?

Add custom spans in your runnables:

```csharp
using var activity = AgentTelemetry.ActivitySource.StartActivity("MyCustomOperation");
activity?.SetTag("custom.data", "value");
// Your code here
```

## Migration from Other Telemetry

If you're using Application Insights, Datadog, or other telemetry:

1. OpenTelemetry is vendor-neutral and compatible
2. Use OpenTelemetry as the instrumentation layer
3. Export to your existing backend via OTLP
4. Gradually migrate other instrumentation to OpenTelemetry

## Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [OTLP Specification](https://opentelemetry.io/docs/specs/otlp/)
- [Grafana Tempo](https://grafana.com/docs/tempo/latest/)
- [Demo Project](../LlmTornado.Agents.OpenTelemetryDemo/README.md)

## Support

For issues or questions about OpenTelemetry integration:
- Create an issue on [GitHub](https://github.com/lofcz/LlmTornado/issues)
- Refer to the [demo project](../LlmTornado.Agents.OpenTelemetryDemo) for working examples
