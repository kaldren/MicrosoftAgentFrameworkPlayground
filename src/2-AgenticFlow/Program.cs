using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

// ──────────────────────────────────────────────────────────────
//  Agentic Flow — Sequential Multi-Agent Workflow
//  Demonstrates a sequential pipeline of specialised AI agents
//  (BA → Tech Lead → Developer → QA → Change Report) using
//  Microsoft Foundry Agents, with OpenTelemetry tracing and Application Insights.
// ──────────────────────────────────────────────────────────────

// ── Step 1: Read configuration ──────────────────────────────
AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("APPLICATIONINSIGHTS_CONNECTION_STRING not set.");

var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not set.");

// ── Step 2: Configure OpenTelemetry tracing ─────────────────
using var source = new ActivitySource("AgenticFlow");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgenticFlow"))
    .AddSource("AgenticFlow")
    .AddSource("Azure.AI.Projects.*")
    .AddConsoleExporter()
    .AddAzureMonitorTraceExporter(options =>
    {
        options.ConnectionString = connectionString;
    })
    .Build();

try
{
    // ── Step 3: Connect to Foundry and resolve agents ───────
    var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());

    var baAgent          = await projectClient.GetAIAgentAsync("BusinessAnalyst");
    var techleadAgent    = await projectClient.GetAIAgentAsync("Techlead");
    var developerAgent   = await projectClient.GetAIAgentAsync("Developer");
    var qaAgent          = await projectClient.GetAIAgentAsync("QA");
    var changeReportAgent = await projectClient.GetAIAgentAsync("ChangeReport");

    // ── Step 4: Build sequential workflow ───────────────────
    var workflow = AgentWorkflowBuilder.BuildSequential(
        [baAgent, techleadAgent, developerAgent, qaAgent, changeReportAgent]);

    var messages = new List<ChatMessage>
    {
        new(ChatRole.User, "Implement Clean Architecture")
    };

    // ── Step 5: Execute and stream agent responses ──────────
    string? currentAgent = null;
    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    List<ChatMessage> result = [];
    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        if (evt is AgentResponseUpdateEvent e)
        {
            if (e.ExecutorId != currentAgent)
            {
                if (currentAgent != null) Console.WriteLine("\n");
                currentAgent = e.ExecutorId;
                Console.WriteLine($"[{currentAgent}]:");
            }
            Console.Write(e.Data);
        }
        else if (evt is WorkflowOutputEvent outputEvt)
        {
            result = (List<ChatMessage>)outputEvt.Data!;
            Console.WriteLine("\n");
        }
    }

    // ── Step 6: Display final result ────────────────────────
    Console.WriteLine("=== Final Result ===");
    var finalMessage = result.LastOrDefault(m => m.Role == ChatRole.Assistant);
    Console.WriteLine(finalMessage?.Text ?? "(no output)");
    Console.WriteLine();
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
    throw;
}
finally
{
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
