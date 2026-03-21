# Agentic Flow

A .NET 10 console app that uses the [Microsoft Agent Framework](https://github.com/microsoft/agents) to orchestrate multiple AI agents deployed on [Azure AI Foundry](https://learn.microsoft.com/azure/ai-studio/) in a sequential workflow.

## What It Does

Five Foundry agents are chained together using `AgentWorkflowBuilder.BuildSequential` so the output of each agent feeds into the next:

**Business Analyst → Tech Lead → Developer → QA → Change Report**

The workflow is executed in-process with streaming, so each agent's response is printed to the console in real time as it arrives.

## Features

- **Sequential multi-agent orchestration** – powered by `Microsoft.Agents.AI.Workflows`.
- **Streaming output** – responses are streamed token-by-token via `InProcessExecution.RunStreamingAsync`.
- **OpenTelemetry tracing** – traces are exported to both the console and Azure Monitor / Application Insights.

## Prerequisites

- .NET 10 SDK
- An Azure AI Foundry project with the following agents deployed: **BusinessAnalyst**, **Techlead**, **Developer**, **QA**, **ChangeReport**
- Azure CLI authenticated (`az login`)

## Environment Variables

| Variable | Description |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry project endpoint |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string |
| `CONSOLE_TRACING_ENABLED` | *(optional)* Enable console trace output (`true` / `false`) |

## Run

```bash
dotnet run --project src/2-AgenticFlow
```
