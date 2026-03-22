# PostgreSQL RAG (Local pgvector)

A .NET 10 console app that demonstrates **Retrieval-Augmented Generation (RAG)** using a local **PostgreSQL + pgvector** vector store (via TestContainers) and a **Microsoft Foundry** agent.

## What It Does

1. Spins up an ephemeral PostgreSQL container with the **pgvector** extension (no manual setup)
2. Seeds a knowledge base by embedding documents with Azure OpenAI (`text-embedding-3-small`)
3. Runs an interactive chat loop where each question is:
   - Vectorised and matched against stored documents using cosine distance
   - The top-3 most relevant documents are injected as context into the prompt
   - The Foundry agent (`RAGAgent`) answers using **only** the retrieved context

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/) (for TestContainers)
- [Azure CLI](https://learn.microsoft.com/cli/azure/) (logged in)
- A deployed `RAGAgent` agent in [Azure AI Foundry](https://ai.azure.com/)
- An [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/) resource with `text-embedding-3-small` deployed

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI resource endpoint |

## Run

```bash
dotnet run --project src/4-PostgresRAG
```

## Connecting via pgAdmin

While the app is running, connect using `localhost`, port printed at startup, user `postgres`, password `postgres`.
