using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Agents.Telemetry;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace LlmTornado.Agents.OpenTelemetryDemo;

/// <summary>
/// Demonstrates OpenTelemetry integration with LlmTornado.Agents
/// This demo shows how to configure and use OpenTelemetry to trace agent execution
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LlmTornado.Agents OpenTelemetry Demo ===\n");
        
        // Configure OpenTelemetry
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("LlmTornado.Agents.Demo", serviceVersion: "1.0.0"))
            .AddSource(AgentTelemetry.ActivitySourceName)
            .AddConsoleExporter() // Export to console for demonstration
            .AddOtlpExporter(options =>
            {
                // Configure OTLP exporter for Grafana/Tempo
                // Default endpoint is http://localhost:4317
                // You can override with environment variable: OTEL_EXPORTER_OTLP_ENDPOINT
                options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
                    ?? "http://localhost:4317");
                Console.WriteLine($"OTLP Exporter configured for: {options.Endpoint}");
            })
            .Build();

        Console.WriteLine("\nOpenTelemetry configured successfully!");
        Console.WriteLine("Traces will be exported to:");
        Console.WriteLine("  1. Console (for immediate visibility)");
        Console.WriteLine("  2. OTLP endpoint for Grafana/Tempo (if running)\n");

        // Check for API key
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("WARNING: OPENAI_API_KEY environment variable not set.");
            Console.WriteLine("This demo will run in mock mode without actual API calls.\n");
            
            // Run a mock demo without actual API calls
            await RunMockDemo();
            return;
        }

        // Run the actual demo with API calls
        await RunAgentDemo(apiKey);
    }

    static async Task RunMockDemo()
    {
        Console.WriteLine("=== Running Mock Demo (No API Calls) ===\n");
        
        // Create a simple activity to demonstrate telemetry
        using var activity = AgentTelemetry.ActivitySource.StartActivity("MockDemo.Execute");
        activity?.SetTag("demo.type", "mock");
        activity?.SetTag("demo.has_api_key", false);
        
        Console.WriteLine("Creating mock agent execution activities...");
        
        // Simulate agent execution steps
        for (int i = 1; i <= 3; i++)
        {
            using var stepActivity = AgentTelemetry.ActivitySource.StartActivity($"MockAgent.Step{i}");
            stepActivity?.SetTag("step.number", i);
            stepActivity?.SetTag("step.name", $"Processing Step {i}");
            stepActivity?.SetTag("agent.name", "MockAgent");
            
            Console.WriteLine($"  Step {i}: Processing...");
            await Task.Delay(500); // Simulate work
            
            stepActivity?.SetTag("step.completed", true);
            stepActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        
        activity?.SetTag("demo.steps_completed", 3);
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        Console.WriteLine("\nMock demo completed! OpenTelemetry traces have been exported.");
        Console.WriteLine("\nYou can see the traces in the console output above.");
        Console.WriteLine("If you have Grafana/Tempo running, check there for visual representation.");
    }

    static async Task RunAgentDemo(string apiKey)
    {
        Console.WriteLine("=== Running Agent Demo with OpenTelemetry ===\n");
        
        // Create the API client
        var api = new TornadoApi(apiKey, LLmProviders.OpenAi);
        
        // Example: Simple Agent with Telemetry
        Console.WriteLine("Running TornadoAgent with automatic OpenTelemetry tracing...\n");
        
        var agent = new TornadoAgent(
            client: api,
            model: new ChatModel("gpt-4o", LLmProviders.OpenAi),
            name: "DemoAssistant",
            instructions: "You are a helpful assistant. Provide brief, clear responses."
        );

        Console.WriteLine("Sending query to agent...");
        var result = await agent.Run("What is 2+2? Please answer briefly in one sentence.");
        
        var lastMessage = result.Messages.LastOrDefault();
        Console.WriteLine($"\nAgent Response: {lastMessage?.GetMessageContent() ?? "No response"}\n");
        
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("\n=== Demo Completed ===");
        Console.WriteLine("\nAll telemetry data has been exported to:");
        Console.WriteLine("  - Console (visible above)");
        Console.WriteLine("  - OTLP endpoint (if Grafana/Tempo is running)");
        Console.WriteLine("\nTo view traces in Grafana:");
        Console.WriteLine("  1. Ensure docker-compose is running (see README.md)");
        Console.WriteLine("  2. Open http://localhost:3000 (Grafana)");
        Console.WriteLine("  3. Navigate to Explore > Tempo data source");
        Console.WriteLine("  4. Search for traces from 'LlmTornado.Agents.Demo'");
        Console.WriteLine("\nThe traces show:");
        Console.WriteLine("  - TornadoAgent.Run span with agent metadata");
        Console.WriteLine("  - Timing information for agent execution");
        Console.WriteLine("  - Success/failure status");
    }
}
