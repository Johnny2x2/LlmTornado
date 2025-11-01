# LlmTornado.Agents Grafana Demo

This demo application showcases OpenTelemetry integration with LlmTornado.Agents, allowing you to visualize agent execution traces in Grafana.

## Features Demonstrated

1. **Simple Agent Telemetry**: Track basic agent runs with spans showing execution time and metadata
2. **Tool Calling Telemetry**: Observe tool invocations, permissions, and results
3. **Orchestration Telemetry**: Visualize multi-agent workflows and state transitions

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- OpenAI API key

## Quick Start

### 1. Start the Telemetry Stack

Start Grafana and Tempo using Docker Compose:

```bash
cd src/LlmTornado.Agents.GrafanaDemo
docker-compose up -d
```

This will start:
- **Grafana** on http://localhost:3000 (admin/admin)
- **Tempo** (tracing backend) on port 4317 (OTLP gRPC)

### 2. Set Your OpenAI API Key

```bash
export OPENAI_API_KEY=your-api-key-here
```

### 3. Run the Demo

```bash
dotnet run --project src/LlmTornado.Agents.GrafanaDemo/LlmTornado.Agents.GrafanaDemo.csproj
```

### 4. View Traces in Grafana

1. Open http://localhost:3000 in your browser
2. Login with username: `admin`, password: `admin`
3. Navigate to **Explore** (compass icon in left sidebar)
4. Select **Tempo** as the data source
5. Click **Search** to see all traces
6. Click on any trace to see the detailed span waterfall

## Understanding the Telemetry

### Agent Spans

Each agent run creates a span with tags:
- `agent.id`: Unique agent identifier
- `agent.name`: Human-readable agent name
- `agent.model`: AI model being used
- `agent.max_turns`: Maximum conversation turns
- `agent.total_turns`: Actual turns executed

### Tool Call Spans

Tool invocations are tracked with:
- `tool.name`: Name of the called tool
- `tool.type`: Type (function, mcp, or agent)
- `tool.arguments`: JSON arguments passed to tool
- `tool.permission_granted`: Whether permission was granted

### Orchestration Spans

Orchestration tracking includes:
- `orchestration.step`: Current step number
- `orchestration.active_runnables_count`: Number of active runnables
- `runnable.id`: Runnable identifier
- `runnable.type`: Type of runnable

## Integration Guide

To add telemetry to your own LlmTornado.Agents application:

### 1. Add OpenTelemetry Packages

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
```

### 3. Create Telemetry Provider

```csharp
using LlmTornado.Agents.Telemetry;

var telemetryProvider = new OpenTelemetryProvider(activitySource);
```

### 4. Assign to Agent

```csharp
var agent = new TornadoAgent(api, model, name: "MyAgent")
{
    TelemetryProvider = telemetryProvider
};
```

### 5. For Orchestrations

```csharp
var orchestration = new Orchestration<Input, Output>
{
    TelemetryProvider = telemetryProvider
};
```

## Decoupled Architecture

The telemetry implementation is decoupled from the agents library via the `ITelemetryProvider` interface:

- **NoOpTelemetryProvider**: Default provider that does nothing (zero overhead)
- **OpenTelemetryProvider**: Full OpenTelemetry implementation (requires .NET 8.0+)
- **Custom Providers**: You can implement your own telemetry provider

Example custom provider:

```csharp
public class CustomTelemetryProvider : ITelemetryProvider
{
    public IDisposable? StartActivity(string name, ActivityKind kind)
    {
        // Your custom implementation
        return null;
    }
    
    public void SetTag(string key, object? value)
    {
        // Your custom implementation
    }
    
    // ... implement other methods
}
```

## Advanced Configuration

### Custom OTLP Endpoint

Set the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://your-endpoint:4317
```

### Sampling

Configure sampling in your TracerProvider:

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.5)) // Sample 50% of traces
```

### Custom Attributes

Add custom attributes to spans:

```csharp
agent.TelemetryProvider.SetTag("custom.attribute", "value");
```

## Troubleshooting

### No traces appearing in Grafana

1. Check Docker containers are running:
   ```bash
   docker-compose ps
   ```

2. Verify Tempo is receiving traces:
   ```bash
   curl http://localhost:3200/api/search
   ```

3. Check the console output - traces are also exported there

### Connection refused errors

Ensure the OTLP endpoint (port 4317) is accessible:
```bash
telnet localhost 4317
```

### .NET Standard 2.0 Compatibility

OpenTelemetry features are only available on .NET 8.0+. For .NET Standard 2.0, the `NoOpTelemetryProvider` is automatically used.

## Cleanup

Stop and remove the containers:

```bash
docker-compose down -v
```

## Additional Resources

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Grafana Tempo](https://grafana.com/oss/tempo/)
- [LlmTornado Documentation](https://llmtornado.ai)

## Architecture

```
┌─────────────────┐
│   Your App      │
│  (Demo.cs)      │
└────────┬────────┘
         │
         │ uses
         ▼
┌─────────────────┐
│ LlmTornado      │
│   .Agents       │ ◄── Instrumented with ITelemetryProvider
└────────┬────────┘
         │
         │ emits traces via
         ▼
┌─────────────────┐
│ OpenTelemetry   │
│   Exporter      │
└────────┬────────┘
         │
         │ OTLP/gRPC
         ▼
┌─────────────────┐
│ Grafana Tempo   │ ◄── Stores traces
└────────┬────────┘
         │
         │ queries
         ▼
┌─────────────────┐
│    Grafana      │ ◄── Visualizes traces
└─────────────────┘
```

## License

This demo is part of the LlmTornado project and follows the same MIT license.
