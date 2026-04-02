using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace _7_PizzaOrderDirectWorkflow.Executors;

[SendsMessage(typeof(string))]
internal sealed class OrderDeliveryExecutor : Executor<string>
{
    public OrderDeliveryExecutor(string id) : base(id)
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
            name: "OrderDeliveryAgent",
            instructions: $"""
                ## Role
                You are a friendly delivery agent for Antonio's Pizza.

                ## Task
                Read the prepared order from the message and write a short, upbeat delivery status narrative —
                as if you are narrating the real-time delivery from the restaurant to the customer's door.

                ## Constraints
                - Keep the narrative to 3–5 sentences.
                - Invent a realistic-sounding address and ETA.
                - Confirm that all items arrived intact and undamaged.
                - **CRITICAL**: Your output MUST begin with the ENTIRE original message reproduced exactly as received, character for character. Then append your section at the very end.
                - Do NOT remove, alter, summarize, or omit any part of the original message.

                ## Output Format
                First, reproduce the original message verbatim. Then append the following block:

                ## Delivery Update:
                [Narrative paragraph]

                REMEMBER: The original message MUST appear in full before the appended block.

                ## Example
                Appended output:
                ## Delivery Update:
                Your order has been picked up and is heading to 42 Maple Street! Our driver is currently
                10 minutes away. All items — 2 Pepperoni pizzas, olives, 3 classic Cokes, and 1 diet Coke —
                are packed securely. Your order arrived at 7:32 PM in perfect condition. Enjoy your meal! 🍕

                ## Input
                {message}
                """
        );

        var result = await orderAgent.RunAsync();

        var response = result.AsChatResponse().Text;

        await context.SendMessageAsync(response, cancellationToken);
    }
}
