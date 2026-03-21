using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Testcontainers.PostgreSql;

// ──────────────────────────────────────────────────────────────
//  PostgreSQL Chat Store — Microsoft Foundry
//  Demonstrates an interactive chat with an AI agent, persisting
//  the full conversation history in a PostgreSQL database
//  running inside a disposable local container.
// ──────────────────────────────────────────────────────────────

// ── Step 1: Read configuration ──────────────────────────────
AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
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
        options.ConnectionString = appInsightsConnectionString;
    })
    .Build();

// ── Step 3: Start PostgreSQL container ──────────────────────
Console.WriteLine("Starting PostgreSQL container...");
await using var postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
    .Build();

await postgresContainer.StartAsync();
Console.WriteLine("PostgreSQL container started.");

// ── Step 4: Initialise database schema ──────────────────────
var pgConnectionString = postgresContainer.GetConnectionString();

await using (var conn = new NpgsqlConnection(pgConnectionString))
{
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE chat_history (
            id SERIAL PRIMARY KEY,
            session_id TEXT NOT NULL,
            role TEXT NOT NULL,
            message TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("Chat history table created.");

// ── Step 5: Connect to Foundry and prepare the agent ────────
var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());
var agent = await projectClient.GetAIAgentAsync("MrMonopoly");
var session = await agent.CreateSessionAsync();
var sessionId = Guid.NewGuid().ToString();

// ── Step 6: Interactive chat loop ───────────────────────────
Console.WriteLine();
Console.WriteLine("=== Chat with Mr. Monopoly ===");
Console.WriteLine("Type your message and press Enter. Type 'exit' to quit, 'history' to view chat history.");
Console.WriteLine();

using (var activity = source.StartActivity("MonopolyPostgresChatSession"))
{
    while (true)
    {
        Console.Write("You: ");
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
            continue;

        if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        if (userInput.Equals("history", StringComparison.OrdinalIgnoreCase))
        {
            await PrintChatHistory(pgConnectionString, sessionId);
            continue;
        }

        // Store user message
        await SaveMessage(pgConnectionString, sessionId, "user", userInput);

        // Get agent response
        var response = await agent.RunAsync(userInput, session);
        var responseText = response.ToString();

        Console.WriteLine($"Mr. Monopoly: {responseText}");
        Console.WriteLine();

        // Store agent response
        await SaveMessage(pgConnectionString, sessionId, "assistant", responseText);
    }
}

Console.WriteLine();
Console.WriteLine("=== Final Chat History ===");
await PrintChatHistory(pgConnectionString, sessionId);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// ═══════════════════════════════════════════════════════════════
//  Local functions
// ═══════════════════════════════════════════════════════════════

static async Task SaveMessage(string connectionString, string sessionId, string role, string message)
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO chat_history (session_id, role, message) VALUES ($1, $2, $3)";
    cmd.Parameters.AddWithValue(sessionId);
    cmd.Parameters.AddWithValue(role);
    cmd.Parameters.AddWithValue(message);
    await cmd.ExecuteNonQueryAsync();
}

static async Task PrintChatHistory(string connectionString, string sessionId)
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT role, message, created_at FROM chat_history WHERE session_id = $1 ORDER BY created_at";
    cmd.Parameters.AddWithValue(sessionId);

    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("--- Chat History ---");
    while (await reader.ReadAsync())
    {
        var role = reader.GetString(0);
        var message = reader.GetString(1);
        var createdAt = reader.GetDateTime(2);
        var label = role == "user" ? "You" : "Mr. Monopoly";
        Console.WriteLine($"[{createdAt:HH:mm:ss}] {label}: {message}");
    }
    Console.WriteLine("--- End of History ---");
    Console.WriteLine();
}