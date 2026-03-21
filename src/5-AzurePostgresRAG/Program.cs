using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Npgsql;
using Pgvector;

// ──────────────────────────────────────────────────────────────
//  RAG Chat — pgvector + Microsoft Foundry (Azure PostgreSQL)
//  Demonstrates retrieval-augmented generation backed by an
//  Azure Database for PostgreSQL Flexible Server with pgvector,
//  authenticated via Microsoft Entra ID.
// ──────────────────────────────────────────────────────────────

const string embeddingModel = "text-embedding-3-small";
const string agentName      = "RAGAgent";
const int    topK           = 3;          // number of context docs to retrieve

// ── Step 1: Read configuration ──────────────────────────────
var foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not set.");

var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set.");

var pgConnectionString = Environment.GetEnvironmentVariable("AZURE_PG_CONNECTION_STRING")
    ?? throw new InvalidOperationException("AZURE_PG_CONNECTION_STRING not set.");

// ── Step 2: Build Entra ID credential (shared across all clients) ──
var credential = new AzureCliCredential();

// ── Step 3: Initialise database schema ──────────────────────
//   The vector extension and table must exist *before* the
//   vector-aware data source is built so Npgsql can discover
//   the vector type in its type catalog.
Console.WriteLine("Initialising Azure PostgreSQL schema...");

await InitialiseDatabaseAsync(pgConnectionString, credential);

Console.WriteLine("Documents table with vector support created.");

// ── Step 4: Build vector-aware Npgsql data source ───────────
// Pre-fetch the initial token so the first connection doesn't race
// against the periodic provider's async background fetch.
var initialToken = await credential.GetTokenAsync(
    new Azure.Core.TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]));

var dataSourceBuilder = new NpgsqlDataSourceBuilder(pgConnectionString);
dataSourceBuilder.ConnectionStringBuilder.Password = initialToken.Token;
dataSourceBuilder.UseVector();

await using var dataSource = dataSourceBuilder.Build();

// ── Step 5: Connect to Microsoft Foundry & embedding model ───
var projectClient   = new AIProjectClient(new Uri(foundryEndpoint), credential);
var openAIClient    = new AzureOpenAIClient(new Uri(openAiEndpoint), credential);
var embeddingClient = openAIClient.GetEmbeddingClient(embeddingModel);

// ── Step 6: Seed knowledge base ─────────────────────────────
await SeedKnowledgeBaseAsync();

// ── Step 7: Prepare AI agent & session ──────────────────────
var agent   = await projectClient.GetAIAgentAsync(agentName);
var session = await agent.CreateSessionAsync();

// ── Step 8: Interactive RAG chat loop ───────────────────────
Console.WriteLine();
Console.WriteLine("=== RAG Chat (pgVector + Azure PostgreSQL + Microsoft Foundry) ===");
Console.WriteLine("Type your question and press Enter. Type 'exit' to quit.");
Console.WriteLine();

await RunChatLoopAsync();

Console.WriteLine("Goodbye!");
return;

// ═══════════════════════════════════════════════════════════════
//  Local functions
// ═══════════════════════════════════════════════════════════════

async Task InitialiseDatabaseAsync(string connectionString, AzureCliCredential cred)
{
    // Bootstrap: fetch token upfront — no periodic refresh needed for a one-shot connection
    var tokenResponse = await cred.GetTokenAsync(
        new Azure.Core.TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]));

    var bootstrapBuilder = new NpgsqlDataSourceBuilder(connectionString);
    bootstrapBuilder.ConnectionStringBuilder.Password = tokenResponse.Token;

    await using var bootstrapSource = bootstrapBuilder.Build();
    await using var conn = await bootstrapSource.OpenConnectionAsync();
    await using var cmd  = conn.CreateCommand();

    cmd.CommandText = """
        CREATE EXTENSION IF NOT EXISTS vector;
        CREATE TABLE IF NOT EXISTS documents (
            id        SERIAL PRIMARY KEY,
            content   TEXT         NOT NULL,
            embedding vector(1536)
        );
        """;

    await cmd.ExecuteNonQueryAsync();
}

async Task SeedKnowledgeBaseAsync()
{
    // Skip seeding if documents already exist (Azure DB is persistent)
    await using var checkConn = await dataSource.OpenConnectionAsync();
    await using var checkCmd  = checkConn.CreateCommand();
    checkCmd.CommandText = "SELECT COUNT(*) FROM documents";
    var count = (long)(await checkCmd.ExecuteScalarAsync())!;

    if (count > 0)
    {
        Console.WriteLine($"Knowledge base already contains {count} documents. Skipping seed.");
        return;
    }

    string[] knowledgeBase =
    [
        "The Eiffel Tower is a wrought-iron lattice tower on the Champ de Mars in Paris, France. It was constructed from 1887 to 1889 as the centerpiece of the 1889 World's Fair. The tower is 330 metres tall.",
        "C# is a modern, object-oriented programming language developed by Microsoft. It runs on the .NET platform and is used for web apps, desktop apps, games, and cloud services.",
        "PostgreSQL is a powerful, open-source object-relational database system with over 35 years of active development. pgvector is an extension that adds vector similarity search support.",
        "Azure AI Foundry is Microsoft's platform for building AI applications. It provides access to large language models, embedding models, and tools for building intelligent apps.",
        "Docker is a platform for developing, shipping, and running applications in containers. Containers package an application with all dependencies into a standardized unit.",
        "Testcontainers is a library providing lightweight, throwaway instances of databases and other services in Docker containers, commonly used for integration testing.",
        "The Great Wall of China is a series of fortifications built along the historical northern borders of China. It stretches over 13,000 miles and was built over many centuries.",
        "Machine learning is a subset of AI that enables systems to learn and improve from experience without being explicitly programmed. It uses algorithms to find patterns in data.",
    ];

    Console.WriteLine("Seeding knowledge base...");

    foreach (var doc in knowledgeBase)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(doc);
        var vector    = new Vector(embedding.Value.ToFloats().ToArray());

        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd  = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO documents (content, embedding) VALUES ($1, $2)";
        cmd.Parameters.AddWithValue(doc);
        cmd.Parameters.AddWithValue(vector);
        await cmd.ExecuteNonQueryAsync();
    }

    Console.WriteLine($"Seeded {knowledgeBase.Length} documents.");
}

async Task<List<string>> RetrieveRelevantDocumentsAsync(string question)
{
    var embedding = await embeddingClient.GenerateEmbeddingAsync(question);
    var vector    = new Vector(embedding.Value.ToFloats().ToArray());

    var docs = new List<string>();

    await using var conn   = await dataSource.OpenConnectionAsync();
    await using var cmd    = conn.CreateCommand();
    cmd.CommandText = "SELECT content FROM documents ORDER BY embedding <=> $1 LIMIT $2";
    cmd.Parameters.AddWithValue(vector);
    cmd.Parameters.AddWithValue(topK);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        docs.Add(reader.GetString(0));

    return docs;
}

async Task RunChatLoopAsync()
{
    while (true)
    {
        Console.Write("You: ");
        var question = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(question))
            continue;

        if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        // Retrieve the most relevant documents via cosine distance
        var relevantDocs = await RetrieveRelevantDocumentsAsync(question);

        // Build augmented prompt with retrieved context
        var context   = string.Join("\n---\n", relevantDocs);
        var ragPrompt = $"""
            Answer the following question using ONLY the provided context.
            If the context doesn't contain relevant information, say you don't have enough information.

            Context:
            {context}

            Question: {question}
            """;

        // Send augmented prompt to the agent
        var response = await agent.RunAsync(ragPrompt, session);
        Console.WriteLine($"Assistant: {response}");
        Console.WriteLine();
    }
}
