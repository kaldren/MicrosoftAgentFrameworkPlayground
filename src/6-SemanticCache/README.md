# Semantic Cache

A .NET 10 console app that places a **semantic cache** (Redis + vector search) in front of a **Microsoft Foundry** agent. Semantically similar questions return an instant cached answer instead of calling the LLM every time.

## How It Works

1. A customer question is turned into a vector embedding (`text-embedding-3-small`)
2. Redis searches for a cached answer whose embedding is close enough (cosine similarity ≥ 0.3)
3. **Cache HIT** → the stored answer is returned immediately
4. **Cache MISS** → the Foundry agent generates an answer, which is stored in Redis for future look-ups

## Demo Flow

The app simulates a customer-support bot for a fictitious bicycle store ("Peak Pedal Bikes"):

- **Round 1** — 3 original questions are sent (all cache MISSes — the LLM answers them)
- **Round 2** — the same 3 questions rephrased — these return as cache HITs because the semantic meaning is close enough

## Redis Backend

The app supports two modes:

| Mode | Trigger | Details |
|---|---|---|
| **Azure Managed Redis** | `AZURE_REDIS_ENDPOINT` is set (or defaults to a preconfigured endpoint) | Authenticated via Microsoft Entra ID |
| **Local Redis Stack** | `AZURE_REDIS_ENDPOINT` is empty | A Redis Stack container is started automatically via TestContainers (requires Docker) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Azure CLI](https://learn.microsoft.com/cli/azure/) (logged in)
- A deployed agent-compatible model in [Azure AI Foundry](https://ai.azure.com/)
- An [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/) resource with `text-embedding-3-small` deployed
- **Either** an [Azure Managed Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/) instance **or** [Docker](https://www.docker.com/) (for the local fallback)

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI resource endpoint |
| `AZURE_REDIS_ENDPOINT` | *(optional)* Azure Managed Redis endpoint (e.g. `host:10000`). If not set, a local Redis Stack container is used |

## Run

```bash
dotnet run --project src/6-SemanticCache
```
