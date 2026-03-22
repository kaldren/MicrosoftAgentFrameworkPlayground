# Azure PostgreSQL RAG

A .NET 10 console app that demonstrates **Retrieval-Augmented Generation (RAG)** using an **Azure Database for PostgreSQL Flexible Server** with **pgvector** and a **Microsoft Foundry** agent, authenticated via **Microsoft Entra ID**.

## What It Does

1. Connects to an Azure-hosted PostgreSQL instance (no Docker required)
2. Initialises the `vector` extension and `documents` table if they don't already exist
3. Seeds the knowledge base on first run (skips if documents already exist)
4. Runs an interactive chat loop where each question is:
   - Vectorised and matched against stored documents using cosine distance
   - The top-3 most relevant documents are injected as context into the prompt
   - The Foundry agent (`RAGAgent`) answers using **only** the retrieved context

## How It Differs from `4-PostgresRAG`

| | `4-PostgresRAG` | `5-AzurePostgresRAG` |
|---|---|---|
| **Database** | Local container (TestContainers) | Azure Database for PostgreSQL Flexible Server |
| **Authentication** | Container defaults | Microsoft Entra ID token |
| **Data persistence** | Ephemeral (lost on exit) | Persistent across runs |
| **Docker required** | Yes | No |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Azure CLI](https://learn.microsoft.com/cli/azure/) (logged in)
- An [Azure Database for PostgreSQL Flexible Server](https://learn.microsoft.com/azure/postgresql/) with pgvector enabled and Entra ID authentication configured
- A deployed `RAGAgent` agent in [Azure AI Foundry](https://ai.azure.com/)
- An [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/) resource with `text-embedding-3-small` deployed

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI resource endpoint |
| `AZURE_PG_CONNECTION_STRING` | ADO.NET connection string for the Azure PostgreSQL instance |

## Run

```bash
dotnet run --project src/5-AzurePostgresRAG
```
