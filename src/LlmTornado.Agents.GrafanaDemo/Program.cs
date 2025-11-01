using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.Telemetry;
using LlmTornado.Chat.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace LlmTornado.Agents.GrafanaDemo;

/// <summary>
/// Demo application showing OpenTelemetry integration with LlmTornado.Agents
/// This demo sends traces to Grafana via OTLP (OpenTelemetry Protocol)
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("LlmTornado.Agents OpenTelemetry + Grafana Demo");
        Console.WriteLine("================================================\n");

        // Check for API key
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("ERROR: OPENAI_API_KEY environment variable not set.");
            Console.WriteLine("Please set your OpenAI API key:");
            Console.WriteLine("  export OPENAI_API_KEY=your-key-here");
            return;
        }

        // Get OTLP endpoint from environment or use default
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
            ?? "http://localhost:4317";

        Console.WriteLine($"Telemetry endpoint: {otlpEndpoint}");
        Console.WriteLine("Starting demo...\n");

        // Create ActivitySource for the demo
        var activitySource = new ActivitySource("LlmTornado.Agents.Demo", "1.0.0");

        // Configure OpenTelemetry
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("LlmTornado.Agents.GrafanaDemo", serviceVersion: "1.0.0"))
            .AddSource(activitySource.Name)
            .AddConsoleExporter() // Also export to console for immediate feedback
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            })
            .Build();

        // Run demos
        try
        {
            await RunSimpleAgentDemo(apiKey, activitySource);
            await RunToolCallingDemo(apiKey, activitySource);
            await RunOrchestrationDemo(apiKey, activitySource);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }

        Console.WriteLine("\n\nDemo complete!");
        Console.WriteLine("View traces in Grafana at: http://localhost:3000");
        Console.WriteLine("  Username: admin");
        Console.WriteLine("  Password: admin");
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task RunSimpleAgentDemo(string apiKey, ActivitySource activitySource)
    {
        using var activity = activitySource.StartActivity("Demo.SimpleAgent");
        
        Console.WriteLine("=== Demo 1: Simple Agent with Telemetry ===");
        
        // Create API client
        var api = new TornadoApi(apiKey);
        
        // Create telemetry provider
        var telemetryProvider = new OpenTelemetryProvider(activitySource);
        
        // Create agent with telemetry
        var agent = new TornadoAgent(
            api,
            ChatModel.OpenAi.Gpt5.V5Mini,
            name: "SimpleAssistant",
            instructions: "You are a helpful assistant that provides concise answers."
        )
        {
            TelemetryProvider = telemetryProvider
        };

        Console.WriteLine("Asking: 'What is 15 + 27?'");
        
        var response = await agent.Run("What is 15 + 27?");
        
        Console.WriteLine($"Response: {response.Messages.Last().Content}");
        Console.WriteLine();
    }

    static async Task RunToolCallingDemo(string apiKey, ActivitySource activitySource)
    {
        using var activity = activitySource.StartActivity("Demo.ToolCalling");
        
        Console.WriteLine("=== Demo 2: Agent with Tool Calling ===");
        
        // Create API client
        var api = new TornadoApi(apiKey);
        
        // Create telemetry provider
        var telemetryProvider = new OpenTelemetryProvider(activitySource);
        
        // Define a simple tool
        string GetCurrentWeather(string location, string unit = "celsius")
        {
            return $"The weather in {location} is 22°{unit.ToUpper()[0]} and sunny.";
        }
        
        // Create agent with tool
        var agent = new TornadoAgent(
            api,
            ChatModel.OpenAi.Gpt5.V5Mini,
            name: "WeatherAssistant",
            instructions: "You are a weather assistant. Use the GetCurrentWeather function to answer weather queries.",
            tools: new List<Delegate> { GetCurrentWeather }
        )
        {
            TelemetryProvider = telemetryProvider
        };

        Console.WriteLine("Asking: 'What's the weather like in Paris?'");
        
        var response = await agent.Run("What's the weather like in Paris?");
        
        Console.WriteLine($"Response: {response.Messages.Last().Content}");
        Console.WriteLine();
    }

    static async Task RunOrchestrationDemo(string apiKey, ActivitySource activitySource)
    {
        using var activity = activitySource.StartActivity("Demo.Orchestration");
        
        Console.WriteLine("=== Demo 3: Orchestration with Telemetry ===");
        Console.WriteLine("(Using simple agent orchestration)");
        
        // Create API client
        var api = new TornadoApi(apiKey);
        
        // Create telemetry provider
        var telemetryProvider = new OpenTelemetryProvider(activitySource);
        
        // Create multiple agents for orchestration
        var plannerAgent = new TornadoAgent(
            api,
            ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Planner",
            instructions: "You are a planning assistant. Break down tasks into steps."
        )
        {
            TelemetryProvider = telemetryProvider
        };

        var executorAgent = new TornadoAgent(
            api,
            ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Executor",
            instructions: "You are an executor. You complete tasks based on plans."
        )
        {
            TelemetryProvider = telemetryProvider
        };

        Console.WriteLine("Step 1: Planning phase");
        var plan = await plannerAgent.Run("Create a plan to write a haiku about coding");
        Console.WriteLine($"Plan: {plan.Messages.Last().Content}\n");
        
        Console.WriteLine("Step 2: Execution phase");
        var result = await executorAgent.Run($"Execute this plan: {plan.Messages.Last().Content}");
        Console.WriteLine($"Result: {result.Messages.Last().Content}");
        Console.WriteLine();
    }
}
