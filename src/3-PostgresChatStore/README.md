# PostgreSQL Chat Store

A console chat app using **Microsoft Agent Framework** with a **Foundry Agent** (`MrMonopoly`), storing chat history in **PostgreSQL** via **TestContainers**.

## What It Does

1. Spins up an ephemeral PostgreSQL container (no manual setup)
2. Connects to the `MrMonopoly` Foundry agent
3. Runs an interactive chat loop — every message and response is persisted to PostgreSQL
4. Type `history` to view stored chat, `exit` to quit

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/) (for TestContainers)
- [Azure CLI](https://learn.microsoft.com/cli/azure/) (logged in)
- A deployed `MrMonopoly` agent in [Azure AI Foundry](https://ai.azure.com/)

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string |

## Run

```bash
dotnet run --project src/3-PostgresChatStore
```

## Connecting via pgAdmin

While the app is running, connect using `localhost`, port printed at startup, user `postgres`, password `postgres`.
