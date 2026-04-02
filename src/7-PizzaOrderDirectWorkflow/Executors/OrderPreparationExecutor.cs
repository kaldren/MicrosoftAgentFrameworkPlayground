using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace _7_PizzaOrderDirectWorkflow.Executors;

[SendsMessage(typeof(string))]
internal sealed class OrderPreparationExecutor : Executor<string>
{
    public OrderPreparationExecutor(string id) : base(id)
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
            name: "OrderPreparationAgent",
            instructions: $"""
                ## Role
                You are a professional pizza chef at Antonio's Pizza.

                ## Task
                Read the order summary from the message and produce a concise, numbered preparation checklist.

                ## Constraints
                - List only the steps needed; do not over-explain each one.
                - Follow a logical preparation order: dough → toppings → sides → beverages.
                - **CRITICAL**: Your output MUST begin with the ENTIRE original message reproduced exactly as received, character for character. Then append your section at the very end.
                - Do NOT remove, alter, summarize, or omit any part of the original message.

                ## Output Format
                First, reproduce the original message verbatim. Then append the following block:

                ## Kitchen Preparation Steps:
                1. [Step]
                2. [Step]
                ...

                REMEMBER: The original message MUST appear in full before the appended block.

                ## Example
                Message contains:
                ## Order Summary:
                - 2x Pepperoni Pizza: extra cheese, not too spicy
                - 1x Olives: on the side
                - 3x Classic Coke
                - 1x Diet Coke

                Appended output:
                ## Kitchen Preparation Steps:
                1. Prepare dough for 2 pizzas.
                2. Add pepperoni and extra cheese; keep spice level mild.
                3. Plate a portion of olives on the side.
                4. Retrieve 3 classic Cokes and 1 diet Coke from the fridge.

                ## Input
                {message}
                """
        );

        var result = await orderAgent.RunAsync();

        var response = result.AsChatResponse().Text;

        await context.SendMessageAsync(response, cancellationToken);
    }
}
