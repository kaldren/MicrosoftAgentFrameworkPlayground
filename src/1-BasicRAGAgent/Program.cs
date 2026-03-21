using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

// ──────────────────────────────────────────────────────────────
//  Basic RAG Agent — Microsoft Foundry
//  Demonstrates a simple multi-turn conversation with an AI
//  agent backed by Microsoft Foundry, traced via OpenTelemetry
//  and Application Insights.
// ──────────────────────────────────────────────────────────────

// ── Step 1: Read configuration ──────────────────────────────
AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("APPLICATIONINSIGHTS_CONNECTION_STRING not set.");

var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not set.");

// ── Step 2: Configure OpenTelemetry tracing ─────────────────
using var source = new ActivitySource("MyMonopolyApp");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MonopolyLocalAgent"))
    .AddSource("MyMonopolyApp")
    .AddSource("Azure.AI.Projects.*")
    .AddConsoleExporter()
    .AddAzureMonitorTraceExporter(options =>
    {
        options.ConnectionString = connectionString;
    })
    .Build();

// ── Step 3: Connect to Foundry and prepare the agent ────────
var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());
var agent = await projectClient.GetAIAgentAsync("MrMonopoly");
var session = await agent.CreateSessionAsync();

// ── Step 4: Run a multi-turn conversation ───────────────────
using (var activity = source.StartActivity("MonopolySession"))
{
    Console.WriteLine("Human: How many players are allowed to play?");
    var response1 = await agent.RunAsync("How many players are allowed to play?", session);
    Console.WriteLine($"Mr Monopoly (AI Agent): {response1}");

    Console.WriteLine("Human: What are the rules of the game?");
    var response2 = await agent.RunAsync("What are the rules of the game?", session);
    Console.WriteLine($"Mr Monopoly (AI Agent): {response2}");

    Console.WriteLine("Human: What were my previous questions?");
    var response3 = await agent.RunAsync("What were my previous questions?", session);
    Console.WriteLine($"Mr Monopoly (AI Agent): {response3}");
}

Console.ReadLine();