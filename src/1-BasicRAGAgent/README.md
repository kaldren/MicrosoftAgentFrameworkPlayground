# Monopoly RAG Agent

A .NET 10 console app that uses the [Microsoft Agent Framework](https://github.com/microsoft/agents) with Azure AI Foundry to run a Retrieval-Augmented Generation (RAG) agent ("Mr. Monopoly") that answers questions about the Monopoly board game.

## Technologies

- **[Microsoft Agent Framework](https://github.com/microsoft/agents)** (`Microsoft.Agents.AI.AzureAI`, `Microsoft.Agents.AI.AzureAI.Persistent`) — persistent agent sessions and multi-turn conversation management.
- **[PostgreSQL](https://www.postgresql.org/)** — used in the later samples of this solution (projects 3–5) as a chat store and vector store via pgvector. See [`3-PostgresChatStore`](../3-PostgresChatStore) and [`4-PostgresRAG`](../4-PostgresRAG).
- **OpenTelemetry** — distributed tracing exported to the console and Azure Monitor.

## Features

- **Conversational sessions** – maintains context across multiple questions using persistent agent sessions.
- **OpenTelemetry tracing** – traces are exported to both the console and Azure Monitor / Application Insights.

## Prerequisites

- .NET 10 SDK
- An Azure AI Foundry project with a deployed **MrMonopoly** agent
- Azure CLI authenticated (`az login`)

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string |
| `CONSOLE_TRACING_ENABLED` | *(optional)* Enable console trace output (`true`/`false`) |

## Run

```bash
dotnet run --project src/1-BasicRAGAgent
```
