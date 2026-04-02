using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace _7_PizzaOrderDirectWorkflow.Agents;

internal static class ChatClientFactory
{
    public static IChatClient Create()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");

        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";

        return new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deploymentName).AsIChatClient();
    }
}
