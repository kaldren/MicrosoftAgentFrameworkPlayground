// ============================================================================
//  Semantic Cache Demo — "Peak Pedal Bikes" FAQ Bot
// ============================================================================
//  This sample shows how to add a semantic cache (Redis + vector search) in
//  front of a Microsoft Foundry agent so that semantically similar questions
//  return an instant cached answer instead of calling the LLM every time.
//
//  How it works:
//    1. A customer question is turned into a vector embedding.
//    2. Redis searches for a cached answer whose embedding is close enough.
//    3. On a cache HIT  → the stored answer is returned immediately.
//       On a cache MISS → the AI agent generates an answer, which is then
//                         stored in Redis for future look-ups.
//
//  Prerequisites:
//    • FOUNDRY_PROJECT_ENDPOINT  — Microsoft Foundry project endpoint
//    • AZURE_OPENAI_ENDPOINT     — Azure OpenAI resource endpoint
//    • (optional) REDIS_CONNECTION_STRING — if not set, a local Redis Stack
//      container is started automatically via Testcontainers (requires Docker)
// ============================================================================

using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;
using System.Runtime.InteropServices;
using Testcontainers.Redis;

// ── 1. Configuration ────────────────────────────────────────────────────────

var foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not set.");

var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not set.");

// If no connection string is provided, spin up a local Redis Stack container.
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

RedisContainer? redisContainer = null;
if (string.IsNullOrEmpty(redisConnectionString))
{
    Console.WriteLine("No REDIS_CONNECTION_STRING found — starting local Redis Stack container …");
    redisContainer = new RedisBuilder()
        .WithImage("redis/redis-stack-server:latest")
        .Build();
    await redisContainer.StartAsync();
    redisConnectionString = redisContainer.GetConnectionString();
    Console.WriteLine($"Redis container started at {redisConnectionString}\n");
}

// ── 2. Microsoft Foundry + Embedding client ──────────────────────────────────

var credential      = new AzureCliCredential();
var projectClient   = new AIProjectClient(new Uri(foundryEndpoint), credential);
var openAIClient    = new AzureOpenAIClient(new Uri(openAiEndpoint), credential);
var embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-3-small");

// The agent acts as the "Peak Pedal Bikes" customer-support assistant.
const string agentInstructions = """
    You are the friendly customer-support assistant for "Peak Pedal Bikes", a
    fictitious online bicycle store. Answer questions about products, shipping,
    returns, warranties, store hours, and anything else a bike-shop customer
    might ask. Keep answers concise and helpful.
    """;

var agent   = await projectClient.CreateAIAgentAsync("PeakPedalBikesAgent", "gpt-4.1", agentInstructions);
var session = await agent.CreateSessionAsync();

// ── 3. Redis vector index setup ─────────────────────────────────────────────

var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
var db    = redis.GetDatabase();
var ft    = db.FT();

const string indexName           = "semantic-cache-idx";
const string prefix              = "cache:";
const double similarityThreshold = 0.3;   // 0 → 1; higher = stricter match

try
{
    await ft.CreateAsync(indexName,
        new FTCreateParams().On(IndexDataType.HASH).Prefix(prefix),
        new Schema()
            .AddVectorField("embedding", Schema.VectorField.VectorAlgo.HNSW, new Dictionary<string, object>
            {
                ["TYPE"]            = "FLOAT32",
                ["DIM"]             = 1536,       // text-embedding-3-small dimensions
                ["DISTANCE_METRIC"] = "COSINE"
            })
            .AddTextField("response"));
}
catch (RedisServerException)
{
    // Index already exists — safe to ignore.
}

// ── 4. Core helper — check cache or call the LLM ───────────────────────────

async Task<(string Response, string Source)> AskAsync(string question)
{
    // 4a. Turn the question into a vector embedding.
    var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(question);
    float[] vector      = embeddingResult.Value.ToFloats().ToArray();
    byte[] vectorBytes  = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();

    // 4b. Search Redis for the closest cached answer.
    var searchResult = await ft.SearchAsync(indexName,
        new Query("*=>[KNN 1 @embedding $blob AS score]")
            .AddParam("blob", vectorBytes)
            .SetSortBy("score")
            .ReturnFields("response", "score")
            .Limit(0, 1)
            .Dialect(2));

    if (searchResult.Documents.Count > 0)
    {
        var cosineDistance = double.Parse(searchResult.Documents[0]["score"].ToString()!);
        var similarity     = 1 - cosineDistance;

        if (similarity >= similarityThreshold)
        {
            return (
                searchResult.Documents[0]["response"].ToString()!,
                $"⚡ Cache HIT (similarity {similarity:P1})"
            );
        }
    }

    // 4c. Cache miss — ask the AI agent.
    var agentResponse = await agent.RunAsync(question, session);
    var answer        = agentResponse.ToString();

    // 4d. Store the new answer in the cache for next time.
    await db.HashSetAsync($"{prefix}{Guid.NewGuid()}",
    [
        new HashEntry("embedding", vectorBytes),
        new HashEntry("response",  answer)
    ]);

    return (answer, "Cache MISS → LLM");
}

// ── 5. Demo — simulate customer questions ───────────────────────────────────
//
//  First we ask 3 unique questions (all cache MISSes — the LLM answers them).
//  Then we ask the same 3 questions rephrased — these should be cache HITs
//  because the semantic meaning is close enough to the originals.

string[] customerQuestions =
[
    // ── Round 1: original questions (cache MISS → LLM) ──
    "How long does delivery take?",
    "What is your return policy?",
    "Do your bikes come with a warranty?",

    // ── Round 2: rephrased versions (cache HIT expected) ──
    "What are the shipping times for orders?",             // ~ "How long does delivery take?"
    "Can I return a bike if I change my mind?",            // ~ "What is your return policy?"
    "Is there a guarantee included with every bicycle?",   // ~ "Do your bikes come with a warranty?"
];

Console.WriteLine("Peak Pedal Bikes — Semantic Cache Demo\n");
Console.WriteLine(new string('─', 60));

foreach (var question in customerQuestions)
{
    Console.WriteLine($"\nCustomer: \"{question}\"");
    var (response, source) = await AskAsync(question);
    Console.WriteLine($"   {source}");
    Console.WriteLine($"   {response}");
    Console.WriteLine(new string('─', 60));
}

// ── 6. Cleanup ──────────────────────────────────────────────────────────────

if (redisContainer is not null)
{
    await redisContainer.DisposeAsync();
    Console.WriteLine("\nRedis container stopped.");
}