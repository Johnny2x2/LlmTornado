# LlmTornado.Agents OpenTelemetry Demo

This demo project showcases the OpenTelemetry integration with LlmTornado.Agents, including distributed tracing for agent orchestration and visualization with Grafana.

## Features

- **OpenTelemetry Integration**: Automatic instrumentation of agent operations and orchestrations
- **Distributed Tracing**: Track agent execution, orchestration steps, and performance metrics
- **Grafana Visualization**: View traces in Grafana using Tempo as the backend
- **Console Export**: Immediate visibility of traces in console output
- **OTLP Support**: Export traces using OpenTelemetry Protocol (OTLP)

## What Gets Traced?

The OpenTelemetry integration automatically traces:

1. **TornadoAgent.Run()**: Full agent execution with tags for:
   - Agent name and ID
   - Model used
   - Max turns and streaming settings
   - Completion status

2. **Orchestration.InvokeAsync()**: Complete orchestration execution with tags for:
   - Orchestration type
   - Input presence
   - Completion status

3. **Orchestration.RunToCompletion()**: Orchestration lifecycle with:
   - Number of steps executed
   - Completion and cancellation status

4. **Orchestration.ProcessTick()**: Individual orchestration ticks with:
   - Step counter
   - Number of runnables
   - Process completion status

## Quick Start

### Option 1: Run Without Grafana (Console Only)

Simply build and run the demo:

```bash
cd src/LlmTornado.Agents.OpenTelemetryDemo
dotnet build
dotnet run
```

This will output traces to the console. No additional setup required!

### Option 2: Run With Grafana (Full Experience)

#### Prerequisites

- Docker and Docker Compose installed
- .NET 8.0 SDK
- OpenAI API key (optional for mock demo)

#### Step 1: Start Grafana and Tempo

In the demo directory:

```bash
docker-compose up -d
```

This will start:
- **Grafana** on http://localhost:3000
- **Tempo** on http://localhost:4317 (OTLP gRPC) and http://localhost:4318 (OTLP HTTP)

#### Step 2: Set Your API Key (Optional)

If you want to run the full demo with actual agent calls:

```bash
# Linux/macOS
export OPENAI_API_KEY=your_api_key_here

# Windows PowerShell
$env:OPENAI_API_KEY="your_api_key_here"

# Windows CMD
set OPENAI_API_KEY=your_api_key_here
```

If you don't set an API key, the demo will run in mock mode (no actual API calls).

#### Step 3: Run the Demo

```bash
dotnet run
```

The application will:
1. Configure OpenTelemetry with console and OTLP exporters
2. Run demo scenarios (simple agent and orchestration)
3. Export traces to both console and Grafana/Tempo

#### Step 4: View Traces in Grafana

1. Open http://localhost:3000 in your browser
2. Navigate to **Explore** (compass icon on the left)
3. Select **Tempo** as the data source
4. Click **Search** tab
5. Set filters:
   - **Service Name**: `LlmTornado.Agents.Demo`
6. Click **Run query**

You'll see a list of traces with timing information. Click on any trace to see:
- Span hierarchy (parent-child relationships)
- Duration of each operation
- Tags and attributes
- Error information (if any)

## Project Structure

```
LlmTornado.Agents.OpenTelemetryDemo/
├── Program.cs                      # Main demo application
├── docker-compose.yml              # Docker setup for Grafana + Tempo
├── tempo-config.yaml               # Tempo configuration
├── grafana-datasources.yaml        # Grafana data source configuration
└── README.md                       # This file
```

## Understanding the Traces

### Simple Agent Trace
When you run a simple agent, you'll see traces like:
```
TornadoAgent.Run
├── agent.name: SimpleAssistant
├── agent.model: gpt-4o
├── agent.max_turns: 10
└── agent.completed: true
```

### Orchestration Trace
When you run an orchestration, you'll see a hierarchy like:
```
Orchestration.InvokeAsync
├── orchestration.type: SimpleOrchestrationConfig
├── Orchestration.RunToCompletion
│   ├── orchestration.steps_executed: 2
│   ├── Orchestration.ProcessTick (Step 1)
│   │   ├── orchestration.tick.step: 1
│   │   └── orchestration.tick.runnable_count: 1
│   └── Orchestration.ProcessTick (Step 2)
│       ├── orchestration.tick.step: 2
│       └── orchestration.tick.runnable_count: 1
└── orchestration.completed: true
```

## Configuration

### Environment Variables

- `OPENAI_API_KEY`: Your OpenAI API key (optional for mock demo)
- `OTEL_EXPORTER_OTLP_ENDPOINT`: Override the OTLP endpoint (default: http://localhost:4317)

### Custom Tempo Endpoint

To use a different Tempo endpoint:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://your-tempo-host:4317
dotnet run
```

## Integrating into Your Own Application

To add OpenTelemetry to your own LlmTornado.Agents application:

1. **Add NuGet packages**:
```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
```

2. **Configure OpenTelemetry**:
```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using LlmTornado.Agents.Telemetry;

// In your application startup:
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("YourServiceName", serviceVersion: "1.0.0"))
    .AddSource(AgentTelemetry.ActivitySourceName)
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://your-tempo-endpoint:4317");
    })
    .Build();

// Your agent code here - telemetry is automatic!
var agent = new TornadoAgent(...);
await agent.Run("Hello");
```

That's it! All agent and orchestration operations will be automatically traced.

## Troubleshooting

### Traces not appearing in Grafana?

1. Check if Tempo is running: `docker-compose ps`
2. Verify the endpoint: Look for "OTLP Exporter configured for: ..." in console output
3. Check Tempo logs: `docker-compose logs tempo`
4. Try the Search tab in Grafana instead of Query tab

### Connection refused errors?

Make sure Docker containers are running:
```bash
docker-compose up -d
docker-compose ps
```

### Want to see more details?

The console exporter shows immediate trace output. Look for lines containing activity IDs and tags.

## Stopping the Demo

To stop Grafana and Tempo:

```bash
docker-compose down
```

To also remove data volumes:

```bash
docker-compose down -v
```

## Learn More

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Grafana Tempo Documentation](https://grafana.com/docs/tempo/latest/)
- [LlmTornado Documentation](https://llmtornado.ai)

## Support

For issues or questions:
- LlmTornado: https://github.com/lofcz/LlmTornado
- OpenTelemetry: https://opentelemetry.io/community/
