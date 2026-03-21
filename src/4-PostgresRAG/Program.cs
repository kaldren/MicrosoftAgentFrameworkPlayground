using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Npgsql;
using Pgvector;
using Testcontainers.PostgreSql;

// ──────────────────────────────────────────────────────────────
//  RAG Chat — pgvector + Microsoft Foundry
//  Demonstrates retrieval-augmented generation backed by a
//  PostgreSQL vector store running in a disposable container locally.
// ──────────────────────────────────────────────────────────────

const string embeddingModel = "text-embedding-3-small";
const string agentName      = "RAGAgent";
const int    topK           = 3;          // number of context docs to retrieve

// ── Step 1: Read configuration ──────────────────────────────
var foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not set.");

var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set.");

// ── Step 2: Start PostgreSQL + pgvector container ───────────
Console.WriteLine("Starting PostgreSQL container with pgvector...");

await using var postgresContainer = new PostgreSqlBuilder("pgvector/pgvector:pg17")
    .WithDatabase("ragdb")
    .Build();
await postgresContainer.StartAsync();

Console.WriteLine("PostgreSQL container started.");

// ── Step 3: Initialise database schema ──────────────────────
//   The vector extension and table must exist *before* the
//   vector-aware data source is built so Npgsql can discover
//   the vector type in its type catalog.
await InitialiseDatabaseAsync(postgresContainer.GetConnectionString());

Console.WriteLine("Documents table with vector support created.");

// ── Step 4: Build vector-aware Npgsql data source ───────────
var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresContainer.GetConnectionString());
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();

// ── Step 5: Connect to Microsoft Foundry & embedding model ───
var credential      = new AzureCliCredential();
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
Console.WriteLine("=== RAG Chat (pgVector + Microsoft Foundry) ===");
Console.WriteLine("Type your question and press Enter. Type 'exit' to quit.");
Console.WriteLine();

await RunChatLoopAsync();

Console.WriteLine("Goodbye!");
return;

// ═══════════════════════════════════════════════════════════════
//  Local functions
// ═══════════════════════════════════════════════════════════════

async Task InitialiseDatabaseAsync(string connectionString)
{
    await using var bootstrapSource = NpgsqlDataSource.Create(connectionString);
    await using var conn = await bootstrapSource.OpenConnectionAsync();
    await using var cmd  = conn.CreateCommand();

    cmd.CommandText = """
        CREATE EXTENSION IF NOT EXISTS vector;
        CREATE TABLE documents (
            id        SERIAL PRIMARY KEY,
            content   TEXT         NOT NULL,
            embedding vector(1536)
        );
        """;

    await cmd.ExecuteNonQueryAsync();
}

async Task SeedKnowledgeBaseAsync()
{
    string[] knowledgeBase =
    [
        "The Eiffel Tower is a wrought-iron lattice tower on the Champ de Mars in Paris, France. It was constructed from 1887 to 1889 as the centerpiece of the 1889 World's Fair. The tower is 330 metres tall.",
        "C# is a modern, object-oriented programming language developed by Microsoft. It runs on the .NET platform and is used for web apps, desktop apps, games, and cloud services.",
        "PostgreSQL is a powerful, open-source object-relational database system with over 35 years of active development. pgvector is an extension that adds vector similarity search support.",
        "Microsoft Foundry is Microsoft's platform for building AI applications. It provides access to large language models, embedding models, and tools for building intelligent apps.",
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