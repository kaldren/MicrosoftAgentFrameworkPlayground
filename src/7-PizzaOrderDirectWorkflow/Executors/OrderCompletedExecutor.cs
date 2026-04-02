using _7_PizzaOrderDirectWorkflow.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

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
        var chatClient = ChatClientFactory.Create();
        var agent = OrderCompletedAgent.Create(chatClient, message);
        var result = await agent.RunAsync();

        await context.YieldOutputAsync(result.AsChatResponse().Text, cancellationToken);
    }
}
