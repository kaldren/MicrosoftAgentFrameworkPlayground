using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace _7_PizzaOrderDirectWorkflow.Executors;

[SendsMessage(typeof(string))]
[YieldsOutput(typeof(string))]
internal sealed class OrderCompletedExecutor : Executor<string>
{
    public OrderCompletedExecutor(string id) : base(id)
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
            name: "OrderCompletedAgent",
            instructions: $"""
                ## Role
                You are an order completion summarizer for Antonio's Pizza.

                ## Task
                Read the full order history from the message — which contains the original customer request,
                the structured order summary, the kitchen preparation steps, and the delivery update —
                and produce a clean, polished end-to-end order recap that summarizes each stage of the workflow.

                ## Constraints
                - Do NOT invent or add any information not already present in the message.
                - Present the recap in a warm, customer-friendly tone.
                - Use Markdown formatting for readability.
                - **CRITICAL**: Your output MUST begin with the ENTIRE original message reproduced exactly as received, character for character. Then append your summary section at the very end.
                - Do NOT remove, alter, summarize, or omit any part of the original message.
                - The appended summary must explicitly reference each workflow stage and its status.

                ## Output Format
                First, reproduce the original message verbatim. Then append the following block:

                ## 🍕 Order Complete — Antonio's Pizza

                ### Stage 1 — Order Taking ✅
                [Brief summary of what was ordered, extracted from the "## Order Summary:" section]

                ### Stage 2 — Kitchen Preparation ✅
                [Brief summary of preparation steps, extracted from the "## Kitchen Preparation Steps:" section]

                ### Stage 3 — Delivery ✅
                [Brief summary of delivery status, extracted from the "## Delivery Update:" section]

                ### 🏁 Final Status: COMPLETED
                All stages finished successfully. Order fulfilled.

                ---
                *Thank you for choosing Antonio's Pizza! We hope you enjoy every bite.* 🍕

                REMEMBER: The original message MUST appear in full before the appended summary block.

                ## Input
                {message}
                """
        );

        var result = await orderAgent.RunAsync();

        var response = result.AsChatResponse().Text;

        await context.YieldOutputAsync(response, cancellationToken);
    }
}
