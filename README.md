# Microsoft Agent Framework Playground

A collection of samples demonstrating **Microsoft Foundry Agents** — from basic RAG to agentic workflows, persistent chat stores, vector search, and semantic caching.

These projects were built for a demo on my YouTube channel **System Shogun**:  
▶️ [Watch the video](https://www.youtube.com/watch?v=eldhIGRLxIQ)

---

## Projects

### 1 — Basic RAG Agent

A simple multi-turn conversation with a Microsoft Foundry AI agent ("Mr Monopoly") traced via **OpenTelemetry** and **Application Insights**. Shows how to connect to Foundry, create a session, and send follow-up questions that retain conversation context.

### 2 — Agentic Flow

A **sequential multi-agent workflow** where a user request flows through a pipeline of specialised agents — Business Analyst → Tech Lead → Developer → QA → Change Report — using `AgentWorkflowBuilder.BuildSequential`. Responses are streamed in real time with OpenTelemetry tracing.

### 3 — PostgreSQL Chat Store

An interactive chat with a Foundry agent that **persists the full conversation history in PostgreSQL**. A disposable Postgres container is spun up automatically via **Testcontainers**, making the sample zero-config for local runs.

### 4 — PostgreSQL RAG (Local)

**Retrieval-Augmented Generation** backed by a local **pgvector** PostgreSQL container. Documents are embedded with `text-embedding-3-small`, stored as vectors, and the top-K most relevant chunks are retrieved to augment the agent's context before answering.

### 5 — Azure PostgreSQL RAG

The same RAG pattern as project 4, but targeting an **Azure Database for PostgreSQL Flexible Server** with pgvector, authenticated via **Microsoft Entra ID** (Azure CLI credential). Designed for cloud-deployed scenarios.

### 6 — Semantic Cache

Places a **Redis + vector search** semantic cache in front of a Foundry agent. Incoming questions are embedded and compared against cached answers; on a cache hit the stored response is returned instantly, avoiding an LLM call. Uses **Redis Stack** (started locally via Testcontainers when no connection string is provided).

---

## Prerequisites

| Requirement | Used by |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | All projects |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | 1, 2, 3 |
| `AZURE_OPENAI_ENDPOINT` | 4, 5, 6 |
| `AZURE_PG_CONNECTION_STRING` | 5 |
| `REDIS_CONNECTION_STRING` (optional) | 6 |
| **Docker** (for Testcontainers) | 3, 4, 6 |

## Getting Started

1. Clone the repo.
2. Set the required environment variables for the project you want to run.
3. Run the desired project:
   ```bash
   dotnet run --project src/<N>-<ProjectName>
   ```

---

## License

This repository is provided as-is for educational and demo purposes.
