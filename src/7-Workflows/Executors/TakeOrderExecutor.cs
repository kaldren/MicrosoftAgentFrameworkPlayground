using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace _7_Workflows.Executors;

[SendsMessage(typeof(string))]
internal sealed class TakeOrderExecutor : Executor<string>
{
    public TakeOrderExecutor(string id) : base(id)
    {
    }

    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");

        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";

        var chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deploymentName).AsIChatClient();

        AIAgent orderAgent = chatClient.AsAIAgent(
            name: "OrderAgent",
            instructions: $"""
                ## Role
                You are a professional order-taking agent at Antonio's Pizza restaurant.

                ## Task
                Read the customer's raw order message — which may contain typos, grammar mistakes,
                slang, or unstructured text — and transform it into a polished, well-formatted customer
                order followed by a structured order summary.

                ## Constraints
                - Fix spelling and grammar mistakes in the customer's message.
                - Normalize item names (e.g., "piperoni" → "Pepperoni Pizza", "cheeze" → "cheese", "cokes" → "Coca-Cola").
                - Preserve ALL quantities and special requests exactly as the customer intended.
                - Do NOT discard any item, modifier, or dietary preference.
                - You are the FIRST step in the pipeline. You MUST refine the raw message into clean, professional text.

                ## Output Format
                Produce the following two sections:

                ## Customer Order:
                [A clean, well-written version of the customer's original message with corrected spelling,
                grammar, and proper item names. Keep the customer's intent and tone but make it read professionally.]

                ## Order Summary:
                - [Quantity]x [Item]: [special requests, if any]

                ## Example
                Customer message:
                "Hi, I'd like two Piperoni with extra cheeze, not too spicy, olives on the side, 3 classic cokes and one diet."

                Output:
                ## Customer Order:
                Hi, I'd like two Pepperoni Pizzas with extra cheese, not too spicy, olives on the side, three Classic Coca-Colas, and one Diet Coca-Cola.

                ## Order Summary:
                - 2x Pepperoni Pizza: extra cheese, not too spicy
                - 1x Olives: on the side
                - 3x Classic Coca-Cola
                - 1x Diet Coca-Cola

                ## Input
                {message}
                """
        );

        var result = await orderAgent.RunAsync();

        var response = result.AsChatResponse().Text;

        await context.SendMessageAsync(response, cancellationToken);
    }
}
