using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace FlightAssistant.Services;

public class FlightAgent : IFlightAgent
{
    public async Task<string> AskQuestionAsync(string question)
    {
        var flightAgent = await GetFlightAssistantAgentAsync();
        var flightDbAgent = await GetFlightDbAssistantAgentAsync();

        var workflow = new WorkflowBuilder(flightAgent)
            .AddEdge(flightAgent, flightDbAgent)
            .Build();

        try
        {

            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, "What is your earliest flight?"));

            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is AgentResponseUpdateEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
            }
        }
        catch (Exception ex)
        {

            throw;
        }


        throw new NotImplementedException();
    }

    private async Task<AIAgent> GetFlightAssistantAgentAsync()
    {
        // Get from ENV Variable FOUNDRY_PROJECT_ENDPOINT or throw
        var projectEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");

        var persistentAgentsClient = new PersistentAgentsClient(
        projectEndpoint,
        new DefaultAzureCredential());

        // Create a persistent agent
        var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
            model: "gpt-4.1-mini",
            name: "Flight Assistant",
            instructions: "You are a helpful assistant for answering questions about flights.");

        // Retrieve the agent that was just created as an AIAgent using its ID
        AIAgent flightAgent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);

        // Invoke the agent and output the text result.
        return flightAgent;
    }

    private async Task<AIAgent> GetFlightDbAssistantAgentAsync()
    {
        // Get from ENV Variable FOUNDRY_PROJECT_ENDPOINT or throw
        var projectEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");

        var persistentAgentsClient = new PersistentAgentsClient(
        projectEndpoint,
        new DefaultAzureCredential());

        // Create a persistent agent
        var agentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
            model: "gpt-4.1-mini",
            name: "Flight DB Assistant",
            instructions: "You are a helpful assistant for answering questions about the flights by querying a fictitious flight database.");

        // Retrieve the agent that was just created as an AIAgent using its ID
        AIAgent flightDbAgent = await persistentAgentsClient.GetAIAgentAsync(agentMetadata.Value.Id);

        // Invoke the agent and output the text result.
        return flightDbAgent;
    }
}